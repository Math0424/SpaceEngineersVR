using SpaceEngineersVR.Patches;
using SpaceEngineersVR.Util;
using SpaceEngineersVR.Wrappers;
using ParallelTasks;
using Sandbox;
using Sandbox.Game.Entities;
using Sandbox.Game.Screens.Helpers.RadialMenuActions;
using Sandbox.Game.SessionComponents.Clipboard;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SpaceEngineersVR.Plugin;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Forms;
using Valve.VR;
using VRage.Game.ModAPI;
using VRage.Input;
using VRageMath;
using VRageRender;
using VRageRender.Messages;
using VRage.Game.Utils;

// See MyRadialMenuItemFactory for actions

namespace SpaceEngineersVR.Player
{
    internal class Headset
    {
        public MatrixD hmdAbsolute = Matrix.Identity;

        public bool IsHeadsetConnected => OpenVR.IsHmdPresent();
        public bool IsHeadsetAlreadyDisconnected = false;
        public bool IsControllersConnected => true; //(LeftHand.IsConnected = true) && (RightHand.IsConnected = true);
        public bool IsControllersAlreadyDisconnected = false;

        private readonly uint pnX;
        private readonly uint pnY;
        private readonly uint height;
        private readonly uint width;

        private VRTextureBounds_t textureBounds;
        private readonly TrackedDevicePose_t[] renderPositions;
        private readonly TrackedDevicePose_t[] gamePositions;

        private bool enableNotifications = false;

        private Vector3 offset = new Vector3(0f, -1.73f, 0f);
        private static EVREye currentlyRenderingEye;

        public Headset()
        {
            FrameInjections.DrawScene += FrameUpdate;
            FrameInjections.GetPerspectiveMatrix = GetPerspectiveMatrix;
            FrameInjections.GetPerspectiveMatrixRhInfiniteComplementary = GetPerspectiveFovRhInfiniteComplementary;
            SimulationUpdater.UpdateBeforeSim += UpdateBeforeSimulation;
            //MyRenderProxy.RenderThread.BeforeDraw += FrameUpdate;

            OpenVR.ExtendedDisplay.GetEyeOutputViewport(EVREye.Eye_Right, ref pnX, ref pnY, ref width, ref height);

            renderPositions = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
            gamePositions = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];

            textureBounds = new VRTextureBounds_t
            {
                uMax = 1,
                vMax = 1
            };

            Logger.Info($"Found headset with eye resolution of '{width}x{height}'");
        }

        #region DrawingLogic

        private bool firstUpdate = true;
        private BorrowedRtvTexture texture;
        public static bool UsingControllerMovement;

        private bool FrameUpdate()
        {
            GetNewPositions();

            // log.Write("Frame update");

            MyCamera cam = MySector.MainCamera;
            if (cam == null)
            {
                firstUpdate = true;
                return true;
            }

            if (firstUpdate)
            {
                //MyRender11.ResizeSwapChain((int)Width, (int)Height);
                MyRender11.Resolution = new Vector2I((int)width, (int)height);
                MyRender11.CreateScreenResources();
                SetOffset();
                firstUpdate = false;
                return true;
            }

            MySession.Static.CameraController.ControlCamera(cam);
            cam.Update(0f);

            //MyRender11.FullDrawScene(false);
            texture?.Release();
            texture = MyManagers.RwTexturesPool.BorrowRtv("SpaceEngineersVR", (int)width, (int)height, Format.R8G8B8A8_UNorm_SRgb);

            //FrameInjections.DisablePresent = true;

            // Store the original matrices to remove the flickering
            var originalWm = cam.WorldMatrix;
            var originalVm = cam.ViewMatrix;

            Matrix headToWorld = hmdAbsolute; //Matrix.Invert(RealWorldPos).GetOrientation();
            headToWorld.Translation += offset;
            headToWorld = Matrix.Invert(headToWorld.GetOrientation()) * Matrix.CreateTranslation(headToWorld.Translation) * originalWm;

            // Stereo rendering
            DrawEye(EVREye.Eye_Right, headToWorld, cam);
            DrawEye(EVREye.Eye_Left,  headToWorld, cam);

            // Restore original matrices to remove the flickering
            cam.WorldMatrix = originalWm;
            cam.ViewMatrix = originalVm;

            //FrameInjections.DisablePresent = false;

            if(MyInput.Static.IsKeyPress(MyKeys.Enter))
                SetOffset();

            if(MyInput.Static.IsKeyPress(MyKeys.NumPad2))
                offset.Z += 0.1f;
            if(MyInput.Static.IsKeyPress(MyKeys.NumPad8))
                offset.Z -= 0.1f;
            if(MyInput.Static.IsKeyPress(MyKeys.NumPad6))
                offset.X += 0.1f;
            if(MyInput.Static.IsKeyPress(MyKeys.NumPad4))
                offset.X -= 0.1f;
            if(MyInput.Static.IsKeyPress(MyKeys.NumPad7))
                offset.Y += 0.1f;
            if(MyInput.Static.IsKeyPress(MyKeys.NumPad1))
                offset.Y -= 0.1f;
            if(MyInput.Static.IsKeyPress(MyKeys.NumPad5))
                offset = new Vector3(0);

            return false;
        }

        private void SetOffset()
        {
            offset = -hmdAbsolute.Translation;
        }

        private void DrawEye(EVREye eye, Matrix headToWorld, MyCamera cam)
        {
            Matrix eyeToHead = OpenVR.System.GetEyeToHeadTransform(eye).ToMatrix();

            cam.WorldMatrix = eyeToHead * headToWorld;

            UploadCameraViewMatrix(eye, eyeToHead, cam);
            MyRender11.DrawGameScene(texture, out _);

            Texture2D texture2D = texture.GetResource(); //(Texture2D) MyRender11.GetBackbuffer().GetResource(); //= texture.GetResource();
            Texture_t input = new Texture_t
            {
                eColorSpace = EColorSpace.Auto,
                eType = ETextureType.DirectX,
                handle = texture2D.NativePointer
            };
            OpenVR.Compositor.Submit(eye, ref input, ref textureBounds, EVRSubmitFlags.Submit_Default);
        }

        private void UploadCameraViewMatrix(EVREye eye, Matrix eyeToHead, MyCamera cam)
        {
            //ViewMatrix is the inverse of WorldMatrix
            cam.ViewMatrix = Matrix.Invert(cam.WorldMatrix);

            //float left = 0f, right = 0f, top = 0f, bottom = 0f;
            //OpenVR.System.GetProjectionRaw(currentlyRenderingEye, ref left, ref right, ref top, ref bottom);
            //float fov = MathHelper.Atan(right) - MathHelper.Atan(left);

            Matrix proj = OpenVR.System.GetProjectionMatrix(eye, cam.NearPlaneDistance, cam.FarPlaneDistance).ToMatrix();
            float fov = MathHelper.Atan(1f / proj.M22) * 2f;


            MyRenderMessageSetCameraViewMatrix msg = MyRenderProxy.MessagePool.Get<MyRenderMessageSetCameraViewMatrix>(MyRenderMessageEnum.SetCameraViewMatrix);

            msg.ViewMatrix = cam.ViewMatrix;
            msg.CameraPosition = cam.Position;

            //These matrixes are just used for culling. I can't figure out why using the matrixes from OpenVR aren't working
            //TODO: Correct these, or override the render pipeline to do a single cull for both eyes
            msg.ProjectionMatrix    = cam.ProjectionMatrix;
            msg.ProjectionFarMatrix = cam.ProjectionMatrixFar;

            //Other attempts:
            //Very close, but culling is visible at bottom of view, and occasionally on far left of left view and far right of right view
            //msg.ProjectionMatrix    = Matrix.CreatePerspectiveFieldOfView(fov, cam.AspectRatio, cam.NearPlaneDistance, cam.FarPlaneDistance);
            //msg.ProjectionFarMatrix = Matrix.CreatePerspectiveFieldOfView(fov, cam.AspectRatio, cam.NearPlaneDistance, cam.FarFarPlaneDistance);

            //Lots wrong, seems to have too small width, and rotated inwards. probably due to weird matrix values to undistort canted displays
            //maybe extract info from these to build a new projection matrix?
            //msg.ProjectionMatrix    = OpenVR.System.GetProjectionMatrix(eye, cam.NearPlaneDistance, cam.FarPlaneDistance).ToMatrix();
            //msg.ProjectionFarMatrix = OpenVR.System.GetProjectionMatrix(eye, cam.NearPlaneDistance, cam.FarFarPlaneDistance).ToMatrix();


            msg.FOV = fov;
            msg.FOVForSkybox = fov;
            msg.NearPlane = cam.NearPlaneDistance;
            msg.FarPlane = cam.FarPlaneDistance;
            msg.FarFarPlane = cam.FarFarPlaneDistance;

            msg.UpdateTime = VRage.Library.Utils.MyTimeSpan.Zero;
            msg.LastMomentUpdateIndex = 0;
            msg.ProjectionOffsetX = 0f; //Patched to do nothing, previously the renderer would ignore the matrix's translation X,Y and use these instead for some reason
            msg.ProjectionOffsetY = 0f;
            msg.Smooth = false;

            currentlyRenderingEye = eye;
            MyRender11.SetupCameraMatrices(msg);
        }

        private MatrixD GetPerspectiveMatrix(double fov, double aspectRatio, double nearPlane, double farPlane)
        {
            return OpenVR.System.GetProjectionMatrix(currentlyRenderingEye, (float)nearPlane, (float)farPlane).ToMatrix();
        }
        private Matrix GetPerspectiveFovRhInfiniteComplementary(float fov, float aspectRatio, float nearPlane)
        {
            float left = 0f, right = 0f, top = 0f, bottom = 0f;
            OpenVR.System.GetProjectionRaw(currentlyRenderingEye, ref left, ref right, ref top, ref bottom);

            //Adapted from decompilation of Matrix.CreatePerspectiveFovRhInfiniteComplementary, Matrix.CreatePerspectiveFieldOfView
            //and https://github.com/ValveSoftware/openvr/wiki/IVRSystem::GetProjectionRaw

            float idx = 1f / (right - left);
            float idy = 1f / (bottom - top);
            float sx = right + left;
            float sy = bottom + top;

            return new Matrix(
                2*idx,  0f,     0f,        0f,
                0f,     2*idy,  0f,        0f,
                sx*idx, sy*idy, 0f,        -1f,
                0f,     0f,     nearPlane, 0f);
        }

        private void GetNewPositions()
        {
            OpenVR.Compositor.WaitGetPoses(renderPositions, gamePositions);
            Compositor_FrameTiming timings = default;
            OpenVR.Compositor.GetFrameTiming(ref timings, 0);
            if (timings.m_nNumDroppedFrames != 0)
            {
                Logger.Warning("Dropping frames!");
                Logger.IncreaseIndent();
                StringBuilder builder = new StringBuilder();
                builder.AppendLine("FrameInterval: " + timings.m_flClientFrameIntervalMs);
                builder.AppendLine("IdleTime     : " + timings.m_flCompositorIdleCpuMs);
                builder.AppendLine("RenderCPU    : " + timings.m_flCompositorRenderCpuMs);
                builder.AppendLine("RenderGPU    : " + timings.m_flCompositorRenderGpuMs);
                builder.AppendLine("SubmitTime   : " + timings.m_flSubmitFrameMs);
                builder.AppendLine("DroppedFrames: " + timings.m_nNumDroppedFrames);
                Logger.Warning(builder.ToString());
                Logger.Warning("");
            }

            //Update positions
            if (!renderPositions[0].bPoseIsValid || !renderPositions[0].bDeviceIsConnected)

            {
                Logger.Error("HMD pos invalid!");
                return;
            }

            hmdAbsolute = renderPositions[0].mDeviceToAbsoluteTracking.ToMatrix();
        }

        #endregion

        #region Control logic

        // TODO: Configurable rotation speed and step by step rotation instead of continuous
        private const float RotationSpeed = 10f;

        private void UpdateBeforeSimulation()
        {
            //UNTESTED
            //Checks if one of the controllers got disconnected, shows a message if a controller is disconnected.
            if (!IsControllersConnected && !IsControllersAlreadyDisconnected)
            {
                //CreatePopup("Error: One of your controllers got disconnected, please reconnect it to continue gameplay.");
                if (MySession.Static.IsPausable())
                {
                    MySandboxGame.PausePush();
                    Logger.Info("Controller disconnected, pausing game.");
                }
                else
                {
                    Logger.Info("Controller disconnected, unable to pause game since it is a multiplayer session.");
                }

                IsControllersAlreadyDisconnected = true;
            }
            else if (IsControllersConnected && IsControllersAlreadyDisconnected)
            {
                if (MySession.Static.IsPausable())
                {
                    MySandboxGame.PausePop();
                    Logger.Info("Controller reconnected, unpausing game.");
                }
                else
                {
                    Logger.Info("Controller reconnected, unable to unpause game as game is already unpaused.");
                }

                IsControllersAlreadyDisconnected = false;
            }

            //UNTESTED
            //Checks if the headset got disconnected, shows a message if the headset is disconnected.
            if (!IsHeadsetConnected && !IsHeadsetAlreadyDisconnected)
            {
                //ShowMessageBoxAsync("Your headset got disconnected, please reconnect it to continue gameplay.", "Headset Disconnected");
                if (MySession.Static.IsPausable())
                {
                    MySandboxGame.PausePush();
                    Logger.Info("Headset disconnected, pausing game.");
                }
                else
                {
                    Logger.Info("Headset disconnected, unable to pause game since it is a multiplayer session.");
                }

                IsHeadsetAlreadyDisconnected = true;
            }
            else if (IsHeadsetConnected && IsHeadsetAlreadyDisconnected)
            {
                if (MySession.Static.IsPausable())
                {
                    MySandboxGame.PausePop();
                    Logger.Info("Headset reconnected, unpausing game.");
                }
                else
                {
                    Logger.Info("Headset reconnected, unable to unpause game as game is already unpaused.");
                }

                IsHeadsetAlreadyDisconnected = false;
            }

            var character = MyAPIGateway.Session?.Player?.Character;
            if (character == null)
                return;

            if (character.Visible && !Common.Config.EnableCharacterRendering)
            {
                character.Visible = false;
            }
            else if (!character.Visible && Common.Config.EnableCharacterRendering)
            {
                character.Visible = true;
            }

            //((MyVRageInput)MyInput.Static).EnableInput(false);

            if (character.EnabledThrusts || MySession.Static.ControlledEntity is MyShipController)
                ControlFlight(character);
            else
                ControlWalk(character);

            ControlCommonFunctions(character);

            // TODO: !move control logic to the VR character component!
            // TODO: Wrist GUI

            // TODO: Configurable left handed mode (swap LeftHand with RightHand, also swap visuals and mirror the hand tools)
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ControlWalk(IMyCharacter character)
        {
            var controls = Controls.Static;

            controls.UpdateWalk();

            var move = Vector3.Zero;
            var rotate = Vector2.Zero;

            if (controls.Walk.Active)
            {
                var v = controls.Walk.Position;
                move.X += v.X;
                move.Z -= v.Y;
            }

            if (controls.WalkForward.Active)
                move.Z -= controls.WalkForward.Position.X;

            if (controls.WalkBackward.Active)
                move.Z += controls.WalkForward.Position.X;

            // TODO: Configurable rotation speed and step by step rotation instead of continuous
            if (controls.WalkRotate.Active)
            {
                var v = controls.WalkRotate.Position;
                rotate.Y = v.X * RotationSpeed;
                rotate.X = -v.Y * RotationSpeed;
            }

            if (controls.JumpOrClimbUp.HasPressed)
                character.Jump();

            if (controls.CrouchOrClimbDown.HasPressed)
                character.Crouch();

            if (controls.JumpOrClimbUp.IsPressed)
                move.Y = 1f;

            if (controls.CrouchOrClimbDown.IsPressed)
                move.Y = -1f;

            ApplyMoveAndRotation(move, rotate, 0f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ControlFlight(IMyCharacter character)
        {
            var controls = Controls.Static; 

            var move = Vector3.Zero;
            var rotate = Vector2.Zero;
            var roll = 0f;

            controls.UpdateFlight();

            if (controls.ThrustLRUD.Active)
            {
                var v = controls.ThrustLRUD.Position;
                move.X += v.X;
                move.Y += v.Y;
            }

            if (controls.ThrustLRFB.Active)
            {
                var v = controls.ThrustLRFB.Position;
                move.X += v.X;
                move.Z -= v.Y;
            }

            if (controls.ThrustUp.Active)
                move.Y += controls.ThrustUp.Position.X;

            if (controls.ThrustDown.Active)
                move.Y -= controls.ThrustDown.Position.X;

            if (controls.ThrustForward.Active)
                move.Z -= controls.ThrustForward.Position.X;

            if (controls.ThrustBackward.Active)
                move.Z += controls.ThrustBackward.Position.X;

            if (controls.ThrustRotate.Active)
            {
                var v = controls.ThrustRotate.Position;

                if (controls.ThrustRoll.IsPressed)
                    roll = v.X * RotationSpeed;
                else
                    rotate.Y = v.X * RotationSpeed;

                rotate.X = -v.Y * RotationSpeed;
            }

            if (controls.Dampener.HasPressed)
                MySession.Static.ControlledEntity?.SwitchDamping();

            ApplyMoveAndRotation(move, rotate, roll);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ApplyMoveAndRotation(Vector3 move, Vector2 rotate, float roll)
        {
            move = Vector3.Clamp(move, -Vector3.One, Vector3.One);

            UsingControllerMovement = move != Vector3.Zero || rotate != Vector2.Zero || roll != 0f;

            if (UsingControllerMovement)
                MySession.Static.ControlledEntity?.MoveAndRotate(move, rotate, roll);
            else
                MySession.Static.ControlledEntity?.MoveAndRotateStopped();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ControlCommonFunctions(IMyCharacter character)
        {
            var controls = Controls.Static;

            var controlledEntity = MySession.Static.ControlledEntity;

            if (controls.Primary.HasPressed)
            {
                controlledEntity?.BeginShoot(MyShootActionEnum.PrimaryAction);
            }
            else if (controls.Primary.HasReleased)
            {
                controlledEntity?.EndShoot(MyShootActionEnum.PrimaryAction);
            }

            if (controls.Secondary.HasPressed)
            {
                controlledEntity?.BeginShoot(MyShootActionEnum.SecondaryAction);
            }
            else if (controls.Secondary.HasReleased)
            {
                controlledEntity?.EndShoot(MyShootActionEnum.SecondaryAction);
            }

            if (controls.Reload.HasPressed)
            {
                // TODO
            }

            if (controls.Unequip.HasPressed)
            {
                controlledEntity?.SwitchToWeapon(null);
            }

            if (controls.CutGrid.HasPressed)
            {
                new MyActionCutGrid().ExecuteAction();
            }

            if (controls.CopyGrid.HasPressed)
            {
                new MyActionCopyGrid().ExecuteAction();
            }

            if (controls.PasteGrid.HasPressed)
            {
                new MyActionPasteGrid().ExecuteAction();
            }

            if (controls.Interact.HasPressed)
            {
                controlledEntity?.Use();
            }

            if (controls.Helmet.HasPressed)
            {
                character.SwitchHelmet();
            }

            if (controls.Jetpack.HasPressed)
            {
                character.SwitchThrusts();
            }

            if (controls.Broadcasting.HasPressed)
            {
                controlledEntity?.SwitchBroadcasting();
            }

            if (controls.Park.HasPressed)
            {
                controlledEntity?.SwitchHandbrake();
            }

            if (controls.Power.HasPressed)
            {
                new MyActionTogglePower().ExecuteAction();
            }

            if (controls.Lights.HasPressed)
            {
                character.SwitchLights();
            }

            if (controls.Respawn.HasPressed)
            {
                controlledEntity?.Die();
            }

            if (controls.ToggleSignals.HasPressed)
            {
                new MyActionToggleSignals().ExecuteAction();
            }

            if (controls.ToggleSymmetry.HasPressed)
            {
                new MyActionToggleSymmetry().ExecuteAction();
            }

            if (controls.SymmetrySetup.HasPressed)
            {
                new MyActionSymmetrySetup().ExecuteAction();
            }

            if (controls.PlacementMode.HasPressed)
            {
                MyClipboardComponent.Static.ChangeStationRotation();
                MyCubeBuilder.Static.CycleCubePlacementMode();
            }

            if (controls.CubeSize.HasPressed)
            {
                // TODO
            }

            if (controls.Terminal.HasPressed)
            {
                character.ShowTerminal();
            }

            if (controls.Inventory.HasPressed)
            {
                character.ShowInventory();
            }

            if (controls.ColorSelector.HasPressed)
            {
                new MyActionColorTool().ExecuteAction();
            }

            if (controls.ColorPicker.HasPressed)
            {
                new MyActionColorPicker().ExecuteAction();
            }

            if (controls.BuildPlanner.HasPressed)
            {
                // TODO
            }

            if (controls.ToolbarConfig.HasPressed)
            {
                // TODO
            }

            if (controls.BlockSelector.HasPressed)
            {
                // TODO
            }

            if (controls.Contract.HasPressed)
            {
                // TODO
            }

            if (controls.Chat.HasPressed)
            {
                // TODO
            }

            if (controls.ToggleView.HasPressed)
            {
                character.IsInFirstPersonView = !character.IsInFirstPersonView;
            }

            if (controls.Pause.HasPressed)
            {
                MySandboxGame.IsPaused = !MySandboxGame.IsPaused;
            }

            if (controls.VoiceChat.HasPressed)
            {
                // TODO
            }

            if (controls.SignalMode.HasPressed)
            {
                // TODO
            }

            if (controls.SpectatorMode.HasPressed)
            {
                // TODO
            }

            if (controls.Teleport.HasPressed)
            {
                // TODO: character.Teleport();
            }
        }

        #endregion

        #region Utils

        public void CreatePopup(string message)
        {
            Bitmap img = new Bitmap(File.OpenRead(Common.IconPngPath));
            CreatePopup(EVRNotificationType.Transient, message, ref img);
        }

        public void CreatePopup(EVRNotificationType type, string message, ref Bitmap bitmap)
        {
            if (!enableNotifications)
                return;

            ulong handle = 0;
            OpenVR.Overlay.CreateOverlay(Guid.NewGuid().ToString(), "SpaceEngineersVR", ref handle);

            System.Drawing.Imaging.BitmapData textureData =
                bitmap.LockBits(
                    new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    System.Drawing.Imaging.ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb
                );

            var image = new NotificationBitmap_t()
            {
                m_pImageData = textureData.Scan0,
                m_nWidth = textureData.Width,
                m_nHeight = textureData.Height,
                m_nBytesPerPixel = 4
            };
            // FIXME: Notification on overlay
            //OpenVR..CreateNotification(handle, 0, type, message, EVRNotificationStyle.Application, ref image, ref id);
            Logger.Debug("Pop-up created with message: " + message);

            bitmap.UnlockBits(textureData);
        }

        /// <summary>
        /// Shows a messagebox async to prevent calling thread from being paused.
        /// </summary>
        /// <param name="msg">The message of the messagebox.</param>
        /// <param name="caption">The caption of the messagebox.</param>
        /// <returns>The button that the user clicked as System.Windows.Forms.DialogResult.</returns>
        public DialogResult ShowMessageBoxAsync(string msg, string caption)
        {
            Parallel.Start(() =>
            {
                Logger.Info($"Messagebox created with the message: {msg}");
                DialogResult result = MessageBox.Show(msg, caption, MessageBoxButtons.OKCancel);
                return result;
            });
            return DialogResult.None;
        }

        #endregion
    }
}