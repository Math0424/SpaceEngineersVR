using Sandbox.Game.World;
using Sandbox.ModAPI;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SpaceEngineersVR.Patches;
using SpaceEngineersVR.Utils;
using SpaceEngineersVR.Wrappers;
using System;
using System.Drawing;
using System.IO;
using System.Text;
using Sandbox;
using Sandbox.Game.Entities;
using Sandbox.Game.Screens.Helpers.RadialMenuActions;
using Sandbox.Game.SessionComponents.Clipboard;
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
    class Headset
    {
        Logger log = new Logger();

        public MatrixD headToRealWorld;

        private readonly uint pnX;
        private readonly uint pnY;
        private readonly uint height;
        private readonly uint width;

        private VRTextureBounds_t textureBounds;
        private readonly TrackedDevicePose_t[] renderPositions;
        private readonly TrackedDevicePose_t[] gamePositions;

        private bool enableNotifications = false;

        private readonly Controls controls = new Controls();

        public Headset()
        {
            FrameInjections.DrawScene += FrameUpdate;
            FrameInjections.GetPerspectiveMatrix = GetPerspectiveMatrix;
            FrameInjections.GetPerspectiveMatrixRhInfiniteComplementary = GetPerspectiveMatrixRhInfiniteComplementary;
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

            log.Write($"Found headset with eye resolution of '{width}x{height}'");
        }

        #region DrawingLogic

        bool firstUpdate = true;
        BorrowedRtvTexture texture;

        private bool FrameUpdate()
        {
            GetNewPositions();

            // log.Write("Frame update");

            var cam = MySector.MainCamera;
            if (cam == null)
            {
                firstUpdate = true;
                return true;
            }

            if (firstUpdate)
            {
                //MyRender11.ResizeSwapChain((int)Width, (int)Height);
                MyRender11.SetResolution(new Vector2I((int)width, (int)height));
                MyRender11.CreateScreenResources();
                firstUpdate = false;
                return true;
            }

            //MyRender11.FullDrawScene(false);
            texture?.Release();
            texture = MyManagers.RwTexturesPool.BorrowRtv("SpaceEngineersVR", (int)width, (int)height, Format.R8G8B8A8_UNorm_SRgb);

            //FrameInjections.DisablePresent = true;

            // Store the original matrices to remove the flickering
            var originalWm = cam.WorldMatrix;
            var originalVm = cam.ViewMatrix;

            Matrix head = headToRealWorld; //Matrix.Invert(RealWorldPos).GetOrientation();
            head.Translation += new Vector3(0f, -1.73f, 0f);

            // Stereo rendering
            DrawEye(EVREye.Eye_Right, head, originalWm, cam);
            DrawEye(EVREye.Eye_Left,  head, originalWm, cam);

            // Restore original matrices to remove the flickering
            cam.WorldMatrix = originalWm;
            cam.ViewMatrix = originalVm;

            //FrameInjections.DisablePresent = false;

            return false;
        }


        private void DrawEye(EVREye eye, Matrix headToRealWorld, MatrixD worldMat, MyCamera cam)
        {
            Matrix eyeToHead = OpenVR.System.GetEyeToHeadTransform(eye).ToMatrix();

            if(MyInput.Static.IsKeyPress(MyKeys.Enter))
            {
                log.Write($"EyeToHead:   {eyeToHead}");
                log.Write($"EyeToHead-1: {Matrix.Invert(eyeToHead)}");
                log.Write($"HeadToReal:   {headToRealWorld}");
                log.Write($"HeadToReal-1: {Matrix.Invert(headToRealWorld)}");
            }

            cam.WorldMatrix =
                Matrix.Invert(eyeToHead) *
                Matrix.Invert(headToRealWorld).GetOrientation() *
                Matrix.CreateTranslation(Matrix.Invert(headToRealWorld).Translation) *
                worldMat;

            UploadCameraViewMatrix(eye, eyeToHead, cam);
            MyRender11.DrawGameScene(texture, out _);

            Texture2D texture2D = texture.GetResource(); //(Texture2D) MyRender11.GetBackbuffer().GetResource(); //= texture.GetResource();
            var input = new Texture_t
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

            MyRenderMessageSetCameraViewMatrix msg = MyRenderProxy.MessagePool.Get<MyRenderMessageSetCameraViewMatrix>(MyRenderMessageEnum.SetCameraViewMatrix);

            msg.ViewMatrix = cam.ViewMatrix;
            msg.CameraPosition = cam.Position;

            Matrix proj = OpenVR.System.GetProjectionMatrix(eye, cam.NearPlaneDistance, cam.FarPlaneDistance).ToMatrix();
            Matrix proj2 = eyeToHead * Matrix.Invert(proj);
            msg.ProjectionMatrix = proj2;

            Matrix projFar = OpenVR.System.GetProjectionMatrix(eye, cam.NearPlaneDistance, cam.FarFarPlaneDistance).ToMatrix();
            Matrix projFar2 = eyeToHead * Matrix.Invert(projFar);
            msg.ProjectionFarMatrix = projFar2;

            float fov = MathHelper.Atan(1f / proj.M22) * 2f;

            msg.FOV = fov;
            msg.FOVForSkybox = fov;
            msg.NearPlane = cam.NearPlaneDistance;
            msg.FarPlane = cam.FarPlaneDistance;
            msg.FarFarPlane = cam.FarFarPlaneDistance;

            msg.UpdateTime = VRage.Library.Utils.MyTimeSpan.Zero;
            msg.LastMomentUpdateIndex = 0;
            msg.ProjectionOffsetX = 0f;
            msg.ProjectionOffsetY = 0f;
            msg.Smooth = false;

            currentlyRenderingEye = eye;
            MyRender11.SetupCameraMatrices(msg);
        }

        private static EVREye currentlyRenderingEye;
        private MatrixD GetPerspectiveMatrix(double fov, double aspectRatio, double nearPlane, double farPlane)
        {
            Matrix eyeToHead = OpenVR.System.GetEyeToHeadTransform(currentlyRenderingEye).ToMatrix();
            Matrix proj = OpenVR.System.GetProjectionMatrix(currentlyRenderingEye, (float)nearPlane, (float)farPlane).ToMatrix();
            return eyeToHead * Matrix.Invert(proj);
        }
        private Matrix GetPerspectiveMatrixRhInfiniteComplementary(float fov, float aspectRatio, float nearPlane)
        {
            Matrix eyeToHead = OpenVR.System.GetEyeToHeadTransform(currentlyRenderingEye).ToMatrix();
            float left = 0f, right = 0f, top = 0f, bottom = 0f;
            OpenVR.System.GetProjectionRaw(currentlyRenderingEye, ref left, ref right, ref top, ref bottom);

            //Adapted from decompilation of Matrix.CreatePerspectiveMatrixRhInfiniteComplementary, Matrix.CreatePerspectiveFieldOfView
            //and https://github.com/ValveSoftware/openvr/wiki/IVRSystem::GetProjectionRaw

            float idx = 1f / (right - left);
            float idy = 1f / (bottom - top);
            float sx = right + left;
            float sy = bottom + top;

            Matrix result = new Matrix(
                idy,     0f,      0f,         0f,
                0f,      idx,     0f,         0f,
                sx*idx,  sy*idy,  0f,         -1f,
                0f,      0f,      -nearPlane, 0f);

            return result * Matrix.Invert(eyeToHead);
        }

        private void GetNewPositions()
        {
            OpenVR.Compositor.WaitGetPoses(renderPositions, gamePositions);
            Compositor_FrameTiming timings = default;
            OpenVR.Compositor.GetFrameTiming(ref timings, 0);
            if (timings.m_nNumDroppedFrames != 0)
            {
                log.Write("Dropping frames!");
                log.IncreaseIndent();
                StringBuilder builder = new StringBuilder();
                builder.AppendLine("FrameInterval: " + timings.m_flClientFrameIntervalMs);
                builder.AppendLine("IdleTime     : " + timings.m_flCompositorIdleCpuMs);
                builder.AppendLine("RenderCPU    : " + timings.m_flCompositorRenderCpuMs);
                builder.AppendLine("RenderGPU    : " + timings.m_flCompositorRenderGpuMs);
                builder.AppendLine("SubmitTime   : " + timings.m_flSubmitFrameMs);
                builder.AppendLine("DroppedFrames: " + timings.m_nNumDroppedFrames);
                log.Write(builder.ToString());
                log.DecreaseIndent();
                log.Write("");
            }

            //Update positions
            if (!renderPositions[0].bPoseIsValid || !renderPositions[0].bDeviceIsConnected)
            {
                log.Write("HMD pos invalid!");
                return;
            }

            headToRealWorld = renderPositions[0].mDeviceToAbsoluteTracking.ToMatrix();
        }

        #endregion

        #region Control logic

        // TODO: Configurable rotation speed and step by step rotation instead of continuous
        private const float RotationSpeed = 10f;

        private void UpdateBeforeSimulation()
        {
            var character = MyAPIGateway.Session?.Player?.Character;
            if (character == null)
                return;

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
            var logoPath = Path.Combine(Util.GetAssetFolder(), "logo.png");
            Bitmap img = new Bitmap(File.OpenRead(logoPath));
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
            log.Write("Pop-up created with message: " + message);

            bitmap.UnlockBits(textureData);
        }

        #endregion
    }
}