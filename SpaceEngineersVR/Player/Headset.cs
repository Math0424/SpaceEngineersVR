using Sandbox;
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
using Valve.VR;
using VRage.Game.ModAPI;
using VRage.Input;
using VRageMath;
using VRageRender;
using VRageRender.Messages;

namespace SpaceEngineersVR.Player
{
    class Headset
    {
        Logger log = new Logger();

        public MatrixD RealWorldPos;

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

        private readonly Actions actions = new Actions();

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

            // Eye position and orientation
            MatrixD orientation = Matrix.Invert(RealWorldPos).GetOrientation();

            var rightEye = MatrixD.Multiply(orientation, OpenVR.System.GetEyeToHeadTransform(EVREye.Eye_Right).ToMatrix());
            var leftEye = MatrixD.Multiply(orientation, OpenVR.System.GetEyeToHeadTransform(EVREye.Eye_Left).ToMatrix());

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
                    log.Write($"IPD: {ipd:0.0000}{sign}{Math.Abs(ipdCorrection):0.0000}");
                }
            }
        }

        private void DrawEye(EVREye eye)
        {
            UploadCameraViewMatrix(eye);
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

        private void UploadCameraViewMatrix(EVREye eye)
        {
            var cam = MySector.MainCamera;

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

            RealWorldPos = renderPositions[0].mDeviceToAbsoluteTracking.ToMatrix();
        }

        #endregion

        #region GameLogic

        private void UpdateBeforeSimulation()
        {
            var character = MyAPIGateway.Session?.Player?.Character;
            if (character == null)
                return;

            actions.Update();

            CharacterMovement(character);

            // TODO: Wrist GUI

            // TODO: Configurable left handed mode (swap LeftHand with RightHand, also swap visuals and mirror the hand tools)
        }

        private void CharacterMovement(IMyCharacter character)
        {
            //((MyVRageInput)MyInput.Static).EnableInput(false);

            var move = Vector3.Zero;
            var rotate = Vector2.Zero;
            var roll = 0f;

            CommonMovement(ref move);
            CommonRotation(ref rotate, ref roll);

            if (character.EnabledThrusts)
                JetpackMovement(character, ref move);
            else
                WalkMovement(character);

            if (actions.Jetpack.HasPressed)
                character.SwitchThrusts();

            character.MoveAndRotate(move, rotate, roll);
        }

        private void WalkMovement(IMyCharacter character)
        {
            if (actions.Jump.HasPressed)
                character.Jump();

            if (actions.Crouch.HasPressed)
                character.Crouch();
        }

        private void JetpackMovement(IMyCharacter character, ref Vector3 move)
        {
            if (actions.Dampeners.HasPressed)
                 character.SwitchDamping();
        }

        private void CommonMovement(ref Vector3 move)
        {
            if (actions.Move.Active)
            {
                var pos = actions.Move.Position;

                move.X = pos.X;

                if (actions.Vertical.IsPressed || actions.Thrust.Active || actions.Forward.Active && actions.Backward.Active)
                    move.Y = pos.Y;
                else
                    move.Z = -pos.Y;
            }

            if (actions.TabletHand.Active)
            {
                if (actions.Jump.IsPressed)
                    move = actions.TabletHand.AbsoluteTracking.Forward;

                if (actions.Crouch.HasPressed)
                    move = -actions.TabletHand.AbsoluteTracking.Forward;

                if (actions.Forward.Active)
                    move += actions.Forward.Position.X * actions.TabletHand.AbsoluteTracking.Forward;

                if (actions.Backward.Active)
                    move = -actions.Backward.Position.X * actions.TabletHand.AbsoluteTracking.Forward;
            }
        }

        private const float RotationSpeed = 10f;

        private void CommonRotation(ref Vector2 rotate, ref float roll)
        {
            if (!actions.Move.Active)
                return;

            var pos = actions.Rotate.Position;
            rotate.X = -pos.Y * RotationSpeed;

            if (actions.Roll.IsPressed)
                roll = pos.X * RotationSpeed;
            else
                rotate.Y = pos.X * RotationSpeed;
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