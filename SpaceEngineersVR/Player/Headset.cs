using SpaceEngineersVR.Player.Components;
using ParallelTasks;
using Sandbox;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SpaceEngineersVR.Patches;
using SpaceEngineersVR.Plugin;
using SpaceEngineersVR.Util;
using SpaceEngineersVR.Wrappers;
using System;
using System.Windows.Forms;
using Valve.VR;
using VRage.Input;
using VRage.Utils;
using VRageMath;
using VRageRender;

// See MyRadialMenuItemFactory for actions

namespace SpaceEngineersVR.Player
{
    public class Headset : TrackedDevice
    {
        private readonly uint pnX;
        private readonly uint pnY;
        private readonly uint height;
        private readonly uint width;

        private readonly float FovH;
        private readonly float FovV;

        private VRTextureBounds_t imageBounds;

        private bool enableNotifications = false;

        public Headset()
            : base(actionName: "")
        {
            deviceId = OpenVR.k_unTrackedDeviceIndex_Hmd;

            OpenVR.ExtendedDisplay.GetEyeOutputViewport(EVREye.Eye_Left, ref pnX, ref pnY, ref width, ref height);

            float left = 0f, right = 0f, top = 0f, bottom = 0f;
            OpenVR.System.GetProjectionRaw(EVREye.Eye_Left, ref left, ref right, ref top, ref bottom);
            FovH = MathHelper.Atan((right - left) / 2) * 2f;
            FovV = MathHelper.Atan((bottom - top) / 2) * 2f;

            imageBounds = new VRTextureBounds_t
            {
                uMax = 1,
                vMax = 1,
            };

            ETrackedPropertyError error = ETrackedPropertyError.TrackedProp_Success;
            int refreshRate = (int)Math.Ceiling(OpenVR.System.GetFloatTrackedDeviceProperty(0, ETrackedDeviceProperty.Prop_DisplayFrequency_Float, ref error));
            if (error != ETrackedPropertyError.TrackedProp_Success)
            {
                Logger.Critical("Failed to get HMD refresh rate! defaulting to 80");
                refreshRate = 80;
            }

            MyRenderDeviceSettings x = MyRender11.m_Settings;
            x.RefreshRate = refreshRate;
            //x.BackBufferHeight = (int)height;
            //x.BackBufferWidth = (int)width;
            x.SettingsMandatory = true;
            MySandboxGame.Static.SwitchSettings(x);

            Logger.Info($"Found headset with eye resolution of '{width}x{height}' and refresh rate of {refreshRate}");
        }

        #region DrawingLogic

        private bool firstUpdate = true;
        private BorrowedRtvTexture texture;

        public void RenderUpdate()
        {
            if (!MyRender11.m_DrawScene)
            {
                firstUpdate = true;
                return;
            }

            if (firstUpdate && renderPose.isTracked)
            {
                MyRender11.Resolution = new Vector2I((int)width, (int)height);
                MyRender11.CreateScreenResources();
                firstUpdate = false;
                return;
            }

            texture?.Release();
            texture = MyManagers.RwTexturesPool.BorrowRtv("SpaceEngineersVR", (int)width, (int)height, Format.R8G8B8A8_UNorm_SRgb);

            EnvironmentMatrices envMats = MyRender11.Environment_Matrices;

            Matrix deviceToAbsolute = renderPose.deviceToAbsolute.matrix;
            deviceToAbsolute.M42 -= Player.GetBodyCalibration().height;
            Matrix m = deviceToAbsolute * Player.RenderPlayerToAbsolute.inverted;
            MatrixD viewMatrix = envMats.ViewD * MatrixD.Invert(m);


            //TODO: Redo this frustum culling such that it encompasses both eye's projection matrixes
            //theres a thread on unity forums with the math involved, will have to do some searching to find it again.
            //I think someone posted a link to it in the discord
            BoundingFrustumD viewFrustum = envMats.ViewFrustumClippedD;
            MyUtils.Init(ref viewFrustum);
            viewFrustum.Matrix = viewMatrix * envMats.OriginalProjection;
            envMats.ViewFrustumClippedD = viewFrustum;

            BoundingFrustumD viewFrustumFar = envMats.ViewFrustumClippedFarD;
            MyUtils.Init(ref viewFrustumFar);
            viewFrustumFar.Matrix = viewMatrix * envMats.OriginalProjectionFar;
            envMats.ViewFrustumClippedFarD = viewFrustumFar;


            envMats.FovH = FovH;
            envMats.FovV = FovV;

            LoadEnviromentMatrices(EVREye.Eye_Left, viewMatrix, ref envMats);
            DrawScene(EVREye.Eye_Left);

            LoadEnviromentMatrices(EVREye.Eye_Right, viewMatrix, ref envMats);
            DrawScene(EVREye.Eye_Right);
        }

        private void DrawScene(EVREye eye)
        {
            MyRender11.DrawGameScene(texture, out _);

            Texture2D texture2D = texture.GetResource();
            Texture_t input = new Texture_t
            {
                eColorSpace = EColorSpace.Auto,
                eType = ETextureType.DirectX,
                handle = texture2D.NativePointer
            };
            OpenVR.Compositor.Submit(eye, ref input, ref imageBounds, EVRSubmitFlags.Submit_Default);
        }

        private void LoadEnviromentMatrices(EVREye eye, MatrixD viewMatrix, ref EnvironmentMatrices envMats)
        {
            Matrix eyeToHead = OpenVR.System.GetEyeToHeadTransform(eye).ToMatrix();
            viewMatrix *= Matrix.Invert(eyeToHead);

            MatrixD worldMat = MatrixD.Invert(viewMatrix);
            Vector3D cameraPosition = worldMat.Translation;

            envMats.CameraPosition = cameraPosition;
            envMats.ViewD = viewMatrix;
            envMats.InvViewD = MatrixD.Invert(viewMatrix);

            MatrixD viewAt0 = viewMatrix;
            viewAt0.M14 = 0.0;
            viewAt0.M24 = 0.0;
            viewAt0.M34 = 0.0;
            viewAt0.M41 = 0.0;
            viewAt0.M42 = 0.0;
            viewAt0.M43 = 0.0;
            viewAt0.M44 = 1.0;
            envMats.ViewAt0 = viewAt0;
            envMats.InvViewAt0 = MatrixD.Invert(viewAt0);

            MatrixD projection = GetPerspectiveFovRhInfiniteComplementary(eye, envMats.NearClipping);
            envMats.Projection = projection;
            envMats.ProjectionForSkybox = projection;
            envMats.InvProjection = MatrixD.Invert(projection);

            envMats.ViewProjectionD = viewMatrix * projection;
            envMats.InvViewProjectionD = MatrixD.Invert(envMats.ViewProjectionD);

            MatrixD viewProjectionAt0 = viewAt0 * projection;
            envMats.ViewProjectionAt0 = viewProjectionAt0;
            envMats.InvViewProjectionAt0 = MatrixD.Invert(viewProjectionAt0);

            //TODO: add a way to write to this
            //VRage.Render11.Scene.MyScene11.Instance.Environment.CameraPosition = cameraPosition;
        }

        private static MatrixD GetPerspectiveFovRhInfiniteComplementary(EVREye eye, double nearPlane)
        {
            float left = 0f, right = 0f, top = 0f, bottom = 0f;
            OpenVR.System.GetProjectionRaw(eye, ref left, ref right, ref top, ref bottom);

            //Adapted from decompilation of Matrix.CreatePerspectiveFovRhInfiniteComplementary, Matrix.CreatePerspectiveFieldOfView
            //and https://github.com/ValveSoftware/openvr/wiki/IVRSystem::GetProjectionRaw

            double idx = 1d / (right - left);
            double idy = 1d / (bottom - top);
            double sx = right + left;
            double sy = bottom + top;

            return new MatrixD(
                2d * idx, 0d,       0d,        0d,
                0d,       2d * idy, 0d,        0d,
                sx * idx, sy * idy, 0d,        -1d,
                0d,       0d,       nearPlane, 0d);
        }

        #endregion
        #region Control logic

        protected override void OnConnected()
        {
            if (MySession.Static == null)
                return;

            if (MySession.Static.IsPausable())
            {
                MySandboxGame.PausePop();
                Logger.Info("Headset reconnected, unpausing game.");
            }
            else
            {
                Logger.Info("Headset reconnected, unable to unpause game as game is already unpaused.");
            }
        }
        protected override void OnDisconnected()
        {
            if (MySession.Static == null)
                return;

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
        }

        protected override void OnStartTracking()
        {
            Player.ResetPlayerFloor();
        }



        public override void MainUpdate()
        {
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
        }

        #endregion

        #region Utils

        public void CreatePopup(string message)
        {
            //System.Drawing.Bitmap img = new Bitmap(File.OpenRead(Common.IconPngPath));
            //CreatePopup(EVRNotificationType.Transient, message, ref img);
        }

        public void CreatePopup(EVRNotificationType type, string message, ref System.Drawing.Bitmap bitmap)
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
                DialogResult result = System.Windows.Forms.MessageBox.Show(msg, caption, MessageBoxButtons.OKCancel);
                return result;
            });
            return DialogResult.None;
        }

        #endregion
    }
}