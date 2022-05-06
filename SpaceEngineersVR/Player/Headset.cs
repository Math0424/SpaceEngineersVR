using ClientPlugin.Player.Components;
using ParallelTasks;
using Sandbox;
using Sandbox.Game.World;
using Sandbox.Game.Gui;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SpaceEngineersVR.Patches;
using SpaceEngineersVR.Plugin;
using SpaceEngineersVR.Util;
using SpaceEngineersVR.Wrappers;
using System;
using System.Text;
using System.Windows.Forms;
using Valve.VR;
using VRage.Input;
using VRage.Utils;
using VRageMath;
using VRageRender;

// See MyRadialMenuItemFactory for actions

namespace SpaceEngineersVR.Player
{
    public class Headset
    {
        public Matrix hmdAbsolute = Matrix.Identity;

        public bool IsHeadsetConnected => OpenVR.IsHmdPresent();
        public bool IsHeadsetAlreadyDisconnected = false;
        public bool IsControllersConnected => true; //(LeftHand.IsConnected = true) && (RightHand.IsConnected = true);
        public bool IsControllersAlreadyDisconnected = false;

        private readonly uint pnX;
        private readonly uint pnY;
        private readonly uint height;
        private readonly uint width;

        private readonly float FovH;
        private readonly float FovV;

        private VRTextureBounds_t imageBounds;
        private readonly TrackedDevicePose_t[] renderPositions;
        private readonly TrackedDevicePose_t[] gamePositions;

        private bool enableNotifications = false;

        private Vector3 offset = new Vector3(0f, -1.73f, 0f);

        public Headset()
        {
            FrameInjections.DrawScene += FrameUpdate;
            SimulationUpdater.UpdateBeforeSim += UpdateBeforeSimulation;

            OpenVR.ExtendedDisplay.GetEyeOutputViewport(EVREye.Eye_Left, ref pnX, ref pnY, ref width, ref height);
            FovH = MathHelper.Atan((pnY - pnX) / 2) * 2f;
            FovV = MathHelper.Atan((height - width) / 2) * 2f;

            renderPositions = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
            gamePositions = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];

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
        public static bool UsingControllerMovement;

        private bool FrameUpdate()
        {
            GetNewPositions();

            if (MySector.MainCamera == null)
            {
                firstUpdate = true;
                return true;
            }

            if (firstUpdate)
            {
                MyRender11.Resolution = new Vector2I((int)width, (int)height);
                MyRender11.CreateScreenResources();
                SetOffset();
                firstUpdate = false;
                return true;
            }

            texture?.Release();
            texture = MyManagers.RwTexturesPool.BorrowRtv("SpaceEngineersVR", (int)width, (int)height, Format.R8G8B8A8_UNorm_SRgb);


            EnvironmentMatrices envMats = MyRender11.Environment_Matrices;
            MatrixD viewMatrix = hmdAbsolute;
            viewMatrix.Translation += offset;
            viewMatrix = envMats.ViewD * Matrix.CreateTranslation(-viewMatrix.Translation) * viewMatrix.GetOrientation();


            BoundingFrustumD viewFrustum = envMats.ViewFrustumClippedD;
            MyUtils.Init(ref viewFrustum);
            viewFrustum.Matrix = viewMatrix * envMats.OriginalProjection;
            envMats.ViewFrustumClippedD = viewFrustum;

            BoundingFrustumD viewFrustumFar = envMats.ViewFrustumClippedFarD;
            MyUtils.Init(ref viewFrustumFar);
            viewFrustumFar.Matrix = viewMatrix * envMats.OriginalProjectionFar;
            envMats.ViewFrustumClippedFarD = viewFrustumFar;

            //TODO: Redo this frustum culling such that it encompasses both eye's projection matrixes
            //theres a thread on unity forums with the math involved, will have to do some searching to find it again.
            //I think someone posted a link to it in the discord

            envMats.FovH = FovH;
            envMats.FovV = FovV;

            MatrixD eyeToHead = OpenVR.System.GetEyeToHeadTransform(EVREye.Eye_Left).ToMatrix();
            LoadEnviromentMatrices(EVREye.Eye_Left, viewMatrix * Matrix.Invert(eyeToHead), ref envMats);
            VRGUIManager.Draw(viewMatrix);
            DrawScene(EVREye.Eye_Left);

            eyeToHead = OpenVR.System.GetEyeToHeadTransform(EVREye.Eye_Right).ToMatrix();
            LoadEnviromentMatrices(EVREye.Eye_Right, viewMatrix * Matrix.Invert(eyeToHead), ref envMats);
            VRGUIManager.Draw(viewMatrix);
            DrawScene(EVREye.Eye_Right);

            if (MyInput.Static.IsKeyPress(MyKeys.NumPad5))
                offset = new Vector3(0);


            return true;
        }

        private void SetOffset()
        {
            offset = -hmdAbsolute.Translation;
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

        private static Matrix GetPerspectiveFovRhInfiniteComplementary(EVREye eye, double nearPlane)
        {
            float left = 0f, right = 0f, top = 0f, bottom = 0f;
            OpenVR.System.GetProjectionRaw(eye, ref left, ref right, ref top, ref bottom);

            //Adapted from decompilation of Matrix.CreatePerspectiveFovRhInfiniteComplementary, Matrix.CreatePerspectiveFieldOfView
            //and https://github.com/ValveSoftware/openvr/wiki/IVRSystem::GetProjectionRaw

            double idx = 1f / (right - left);
            double idy = 1f / (bottom - top);
            double sx = right + left;
            double sy = bottom + top;

            return new MatrixD(
                2 * idx, 0f, 0f, 0f,
                0f, 2 * idy, 0f, 0f,
                sx * idx, sy * idy, 0f, -1f,
                0f, 0f, nearPlane, 0f);
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

        //TODO: move this elsewhere
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