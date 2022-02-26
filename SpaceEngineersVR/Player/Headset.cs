using SpaceEnginnersVR.Patches;
using SpaceEnginnersVR.Utill;
using SpaceEnginnersVR.Wrappers;
using ParallelTasks;
using Sandbox;
using Sandbox.Game.Entities;
using Sandbox.Game.Screens.Helpers.RadialMenuActions;
using Sandbox.Game.SessionComponents.Clipboard;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SpaceEnginnersVR.Plugin;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Valve.VR;
using VRage.Game.ModAPI;
using VRage.Input;
using VRageMath;
using VRageRender;
using VRageRender.Messages;
using SpaceEnginnersVR.Logging;

// See MyRadialMenuItemFactory for actions

namespace SpaceEnginnersVR.Player
{
    internal class Headset
    {
        public MatrixD RealWorldPos;

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

        private float ipd;
        private float ipdCorrection;
        private const float IpdCorrectionStep = 5e-5f;

        private bool enableNotifications = false;

        private readonly Controls controls = new Controls();

        public Headset()
        {
            FrameInjections.DrawScene += FrameUpdate;
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

        private bool FrameUpdate()
        {
            GetNewPositions();

            // log.Write("Frame update");

            VRage.Game.Utils.MyCamera cam = MySector.MainCamera;
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
                firstUpdate = false;
                return true;
            }

            // Eye position and orientation
            MatrixD orientation = Matrix.Invert(RealWorldPos).GetOrientation();

            MatrixD rightEye = MatrixD.Multiply(orientation, OpenVR.System.GetEyeToHeadTransform(EVREye.Eye_Right).ToMatrix());
            MatrixD leftEye = MatrixD.Multiply(orientation, OpenVR.System.GetEyeToHeadTransform(EVREye.Eye_Left).ToMatrix());

            ipd = (float)(rightEye.Translation - leftEye.Translation).Length();

            rightEye = MatrixD.Multiply(rightEye, cam.WorldMatrix);
            leftEye = MatrixD.Multiply(leftEye, cam.WorldMatrix);

            //MyRender11.FullDrawScene(false);
            texture?.Release();
            texture = MyManagers.RwTexturesPool.BorrowRtv("SpaceEngineersVR", (int)width, (int)height, Format.R8G8B8A8_UNorm_SRgb);

            //FrameInjections.DisablePresent = true;

            // Store the original matrices to remove the flickering
            var originalWm = cam.WorldMatrix;
            var originalVm = cam.ViewMatrix;


            // Stereo rendering
            cam.WorldMatrix = rightEye;
            DrawEye(EVREye.Eye_Right);
            cam.WorldMatrix = leftEye;
            DrawEye(EVREye.Eye_Left);

            // Restore original matrices to remove the flickering
            cam.WorldMatrix = originalWm;
            cam.ViewMatrix = originalVm;

            //FrameInjections.DisablePresent = false;

            UpdateIpdCorrection();

            return false;
        }

        private void UpdateIpdCorrection()
        {
            var input = MyInput.Static;
            if (input.IsAnyAltKeyPressed() && input.IsAnyCtrlKeyPressed())
            {
                var modified = false;
                if (input.IsKeyPress(MyKeys.Add) && ipdCorrection < 0.1)
                {
                    ipdCorrection += IpdCorrectionStep;
                    modified = true;
                }

                if (input.IsKeyPress(MyKeys.Subtract) && ipdCorrection > 0.1)
                {
                    ipdCorrection -= IpdCorrectionStep;
                    modified = true;
                }

                if (modified)
                {
                    var sign = ipdCorrection >= 0 ? '+' : '-';
                    Logger.Debug($"IPD: {ipd:0.0000}{sign}{Math.Abs(ipdCorrection):0.0000}");
                }
            }
        }

        private void DrawEye(EVREye eye)
        {
            UploadCameraViewMatrix(eye);
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

        private void UploadCameraViewMatrix(EVREye eye)
        {
            VRage.Game.Utils.MyCamera cam = MySector.MainCamera;

            //ViewMatrix is the inverse of WorldMatrix
            cam.ViewMatrix = Matrix.Invert(cam.WorldMatrix);

            MyRenderMessageSetCameraViewMatrix msg = MyRenderProxy.MessagePool.Get<MyRenderMessageSetCameraViewMatrix>(MyRenderMessageEnum.SetCameraViewMatrix);

            msg.ViewMatrix = cam.ViewMatrix;
            msg.CameraPosition = cam.Position;

            msg.ProjectionMatrix = cam.ProjectionMatrix;
            msg.ProjectionFarMatrix = cam.ProjectionMatrixFar;

            var matrix = OpenVR.System.GetProjectionMatrix(eye, cam.NearPlaneDistance, cam.FarPlaneDistance).ToMatrix();
            float fov = MathHelper.Atan(1.0f / matrix.M22) * 2f;

            msg.FOV = fov;
            msg.FOVForSkybox = fov;
            msg.NearPlane = cam.NearPlaneDistance;
            msg.FarPlane = cam.FarPlaneDistance;
            msg.FarFarPlane = cam.FarFarPlaneDistance;

            msg.UpdateTime = VRage.Library.Utils.MyTimeSpan.Zero;
            msg.LastMomentUpdateIndex = 0;
            msg.ProjectionOffsetX = (eye == EVREye.Eye_Left ? -1 : 1) * (ipd + ipdCorrection);
            msg.ProjectionOffsetY = 0;
            msg.Smooth = false;

            MyRender11.SetupCameraMatrices(msg);
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

            RealWorldPos = renderPositions[0].mDeviceToAbsoluteTracking.ToMatrix();
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

            if (character.Visible == true && !Common.Config.EnableCharacterRendering)
            {
                character.Visible = false;
            }
            else if (character.Visible == false && Common.Config.EnableCharacterRendering)
            {
                character.Visible = true;
            }

            //((MyVRageInput)MyInput.Static).EnableInput(false);

            if (character.EnabledThrusts || MySession.Static.ControlledEntity is IMyShipController)
                ControlFlight(character);
            else
                ControlWalk(character);

            ControlCommonFunctions(character);

            // TODO: Wrist GUI

            // TODO: Configurable left handed mode (swap LeftHand with RightHand, also swap visuals and mirror the hand tools)
        }

        private void ControlWalk(IMyCharacter character)
        {
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

            move = Vector3.Clamp(move, -Vector3.One, Vector3.One);

            if (move == Vector3.Zero && rotate == Vector2.Zero)
                MySession.Static.ControlledEntity?.MoveAndRotateStopped();
            else
                MySession.Static.ControlledEntity?.MoveAndRotate(move, rotate, 0f);
        }

        private void ControlFlight(IMyCharacter character)
        {
            var move = Vector3.Zero;
            var rotate = Vector2.Zero;
            var roll = 0f;

            if (controls.ThrustLRUD.Active)
            {
                var v = controls.ThrustLRUD.Position;
                move.X += v.X;
                move.Y += v.Y;
            }

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
                rotate.Y = v.X * RotationSpeed;
                rotate.X = -v.Y * RotationSpeed;
            }

            if (controls.ThrustRoll.Active)
            {
                roll = controls.ThrustRotate.Position.X * RotationSpeed;
            }

            if (controls.Dampeners.HasPressed)
                character.SwitchDamping();

            move = Vector3.Clamp(move, -Vector3.One, Vector3.One);
            if (move == Vector3.Zero && rotate == Vector2.Zero && roll == 0f)
                MySession.Static.ControlledEntity?.MoveAndRotateStopped();
            else
                MySession.Static.ControlledEntity?.MoveAndRotate(move, rotate, roll);
        }

        private void ControlCommonFunctions(IMyCharacter character)
        {
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