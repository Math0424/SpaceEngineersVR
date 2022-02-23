﻿using ParallelTasks;
using Sandbox;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SpaceEngineersVR.Patches;
using SpaceEngineersVR.Wrappers;
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

namespace SpaceEngineersVR.Player
{
    internal class Headset
    {
        public MatrixD RealWorldPos;
        public Controller RightHand = default;
        public Controller LeftHand = default;
        public bool IsHeadsetConnected => OpenVR.IsHmdPresent();
        public bool IsHeadsetAlreadyDisconnected = false;
        public bool IsControllersConnected => (LeftHand.IsConnected = true) && (RightHand.IsConnected = true);
        public bool IsControllersAlreadyDisconnected = false;
        private readonly uint pnX, pnY, Height, Width;
        private VRTextureBounds_t TextureBounds;
        private readonly TrackedDevicePose_t[] RenderPositions;
        private readonly TrackedDevicePose_t[] GamePositions;

        private float ipd;
        private float ipdCorrection;
        private readonly float ipdCorrectionStep = 5e-5f;

        public Headset()
        {
            FrameInjections.DrawScene += FrameUpdate;
            SimulationUpdater.UpdateBeforeSim += UpdateBeforeSimulation;
            //MyRenderProxy.RenderThread.BeforeDraw += FrameUpdate;

            OpenVR.ExtendedDisplay.GetEyeOutputViewport(EVREye.Eye_Right, ref pnX, ref pnY, ref Width, ref Height);

            RenderPositions = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
            GamePositions = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];

            TextureBounds = new VRTextureBounds_t
            {
                uMax = 1,
                vMax = 1
            };

            Plugin.Instance.Log.Info($"Found headset with eye resolution of '{Width}x{Height}'");
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
                MyRender11.Resolution = new Vector2I((int)Width, (int)Height);
                MyRender11.CreateScreenResources();
                PlayerAndCameraDisabler.DisablePlayerAndCameraMovement = true;
                CharacterRenderDisabler.RenderCharacter = false;
                firstUpdate = false;
                return true;
            }


            //UNTESTED
            //Checks if one of the controllers got disconnected, shows a message if a controller is disconnected.
            if (!IsControllersConnected && !IsControllersAlreadyDisconnected)
            {
                //CreatePopup("Error: One of your controllers got disconnected, please reconnect it to continue gameplay.");
                if (MySession.Static.IsPausable())
                {
                    MySandboxGame.PausePush();
                    Plugin.Instance.Log.Info("Controller disconnected, pausing game.");
                }
                else
                {
                    Plugin.Instance.Log.Info("Controller disconnected, unable to pause game since it is a multiplayer session.");
                }

                IsControllersAlreadyDisconnected = true;
            }
            else if (IsControllersConnected && IsControllersAlreadyDisconnected)
            {
                if (MySession.Static.IsPausable())
                {
                    MySandboxGame.PausePop();
                    Plugin.Instance.Log.Info("Controller reconnected, unpausing game.");
                }
                else
                {
                    Plugin.Instance.Log.Info("Controller reconnected, unable to unpause game as game is already unpaused.");
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
                    Plugin.Instance.Log.Info("Headset disconnected, pausing game.");
                }
                else
                {
                    Plugin.Instance.Log.Info("Headset disconnected, unable to pause game since it is a multiplayer session.");
                }

                IsHeadsetAlreadyDisconnected = true;
            }
            else if (IsHeadsetConnected && IsHeadsetAlreadyDisconnected)
            {
                if (MySession.Static.IsPausable())
                {
                    MySandboxGame.PausePop();
                    Plugin.Instance.Log.Info("Headset reconnected, unpausing game.");
                }
                else
                {
                    Plugin.Instance.Log.Info("Headset reconnected, unable to unpause game as game is already unpaused.");
                }

                IsHeadsetAlreadyDisconnected = false;
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
            texture = MyManagers.RwTexturesPool.BorrowRtv("SpaceEngineersVR", (int)Width, (int)Height, Format.R8G8B8A8_UNorm_SRgb);

            //FrameInjections.DisablePresent = true;

            // Store the original matrices to remove the flickering
            MatrixD originalWM = cam.WorldMatrix;
            MatrixD originalVM = cam.ViewMatrix;

            // Stereo rendering
            cam.WorldMatrix = rightEye;
            DrawEye(EVREye.Eye_Right);
            cam.WorldMatrix = leftEye;
            DrawEye(EVREye.Eye_Left);

            // Restore original matrices to remove the flickering
            cam.WorldMatrix = originalWM;
            cam.ViewMatrix = originalVM;

            //FrameInjections.DisablePresent = false;

            IMyInput input = MyInput.Static;
            if (input.IsAnyAltKeyPressed() && input.IsAnyCtrlKeyPressed())
            {
                float before = ipdCorrection;
                if (input.IsKeyPress(MyKeys.Add) && ipdCorrection < 0.02)
                {
                    ipdCorrection += ipdCorrectionStep;
                }

                if (input.IsKeyPress(MyKeys.Subtract) && ipdCorrection > 0.02)
                {
                    ipdCorrection -= ipdCorrectionStep;
                }

                if (ipdCorrection != before)
                {
                    char sign = ipdCorrection >= 0 ? '+' : '-';
                    Plugin.Instance.Log.Debug($"IPD: {ipd:0.0000}{sign}{Math.Abs(ipdCorrection):0.0000}");
                }
            }

            return false;
        }

        private void DrawEye(EVREye eye)
        {
            UploadCameraViewMatrix(eye);
            MyRender11.DrawGameScene(texture, out _);

            Texture2D texture2D = texture.GetResource();//(Texture2D) MyRender11.GetBackbuffer().GetResource(); //= texture.GetResource();
            Texture_t input = new Texture_t
            {
                eColorSpace = EColorSpace.Auto,
                eType = EGraphicsAPIConvention.API_DirectX,
                handle = texture2D.NativePointer
            };
            OpenVR.Compositor.Submit(eye, ref input, ref TextureBounds, EVRSubmitFlags.Submit_Default);

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

            Matrix matrix = OpenVR.System.GetProjectionMatrix(eye, cam.NearPlaneDistance, cam.FarPlaneDistance, EGraphicsAPIConvention.API_DirectX).ToMatrix();
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
            OpenVR.Compositor.WaitGetPoses(RenderPositions, GamePositions);
            Compositor_FrameTiming timings = default;
            OpenVR.Compositor.GetFrameTiming(ref timings, 0);
            if (timings.m_nNumDroppedFrames != 0)
            {
                Plugin.Instance.Log.Warning("Dropping frames!");
                Plugin.Instance.Log.IncreaseIndent();
                StringBuilder builder = new StringBuilder();
                builder.AppendLine("FrameInterval: " + timings.m_flClientFrameIntervalMs);
                builder.AppendLine("IdleTime     : " + timings.m_flCompositorIdleCpuMs);
                builder.AppendLine("RenderCPU    : " + timings.m_flCompositorRenderCpuMs);
                builder.AppendLine("RenderGPU    : " + timings.m_flCompositorRenderGpuMs);
                builder.AppendLine("SubmitTime   : " + timings.m_flSubmitFrameMs);
                builder.AppendLine("DroppedFrames: " + timings.m_nNumDroppedFrames);
                builder.AppendLine("FidelityLevel: " + timings.m_nFidelityLevel);
                Plugin.Instance.Log.Warning(builder.ToString());
                Plugin.Instance.Log.Warning("");
            }

            //Update positions
            if (!RenderPositions[0].bPoseIsValid || !RenderPositions[0].bDeviceIsConnected)
            {
                Plugin.Instance.Log.Error("HMD pos invalid!");
                return;
            }

            RealWorldPos = RenderPositions[0].mDeviceToAbsoluteTracking.ToMatrix();

            if (!RightHand.IsConnected || !LeftHand.IsConnected)
            {
                if (MySandboxGame.TotalTimeInTicks % 1000 == 0)
                {
                    Plugin.Instance.Log.Error("Unable to find controller(s)!");
                }

                for (uint i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++)
                {
                    ETrackedDeviceClass x = OpenVR.System.GetTrackedDeviceClass(i);
                    if (x == ETrackedDeviceClass.Controller)
                    {
                        ETrackedControllerRole role = OpenVR.System.GetControllerRoleForTrackedDeviceIndex(i);
                        if (role == ETrackedControllerRole.LeftHand)
                        {
                            LeftHand.ControllerID = i;
                        }
                        if (role == ETrackedControllerRole.RightHand)
                        {
                            RightHand.ControllerID = i;
                        }
                    }
                }
            }
        }
        #endregion

        #region GameLogic
        public void GameUpdate()
        {
            RightHand.UpdateControllerPosition(RenderPositions[RightHand.ControllerID]);
            LeftHand.UpdateControllerPosition(RenderPositions[LeftHand.ControllerID]);
        }

        private void UpdateBeforeSimulation()
        {
            IMyCharacter character = MyAPIGateway.Session?.Player?.Character;
            if (character != null)
            {
                CharacterMovement(character);
            }
            //TODO: movement
        }

        private void CharacterMovement(IMyCharacter character)
        {
            //((MyVRageInput)MyInput.Static).EnableInput(false);
            Vector3 move = Vector3.Zero;
            Vector2 rotate = Vector2.Zero;

            if (!character.EnabledThrusts)
            {
                if (LeftHand.IsValid)
                {
                    Vector2 vec = LeftHand.GetAxis(Axis.Joystick);
                    move.X = vec.X;
                    move.Z = -vec.Y;
                }

                if (RightHand.IsValid)
                {
                    Vector2 vec = RightHand.GetAxis(Axis.Joystick);
                    rotate.Y = vec.X * 10;
                }

                if (RightHand.IsNewButtonDown(Button.A))
                {
                    //character.Jump();
                }
            }
            else
            {
                if (RightHand.IsValid)
                {
                    if (RightHand.IsButtonDown(Button.B))
                    {
                        Vector3D v = Vector3D.Normalize(Vector3D.Lerp(LeftHand.WorldPos.Up, LeftHand.WorldPos.Forward, .5)) * .5f;
                        v.Y *= -1;
                        //move += v;
                    }

                    Vector2 vec = RightHand.GetAxis(Axis.Joystick);
                    rotate.Y = vec.X * 10;
                    rotate.X = -vec.Y * 10;

                    //Util.DrawDebugLine(character.GetHeadMatrix(true).Translation + character.GetHeadMatrix(true).Forward, RightHand.WorldPos.Down, 0, 0, 255);
                    //Util.DrawDebugLine(character.GetHeadMatrix(true).Translation + character.GetHeadMatrix(true).Forward, Vector3D.Normalize(Vector3D.Lerp(RightHand.WorldPos.Down, RightHand.WorldPos.Forward, .5)), 255, 0, 0);
                    //Util.DrawDebugLine(character.GetHeadMatrix(true).Translation + character.GetHeadMatrix(true).Forward, RightHand.WorldPos.Forward, 0, 255, 0);
                }
                if (LeftHand.IsValid)
                {
                    if (LeftHand.IsButtonDown(Button.B))
                    {
                        Vector3D v = Vector3D.Normalize(Vector3D.Lerp(LeftHand.WorldPos.Up, LeftHand.WorldPos.Forward, .5)) * .5f;
                        v.Y *= -1;
                        //move += v;
                    }

                }
            }

            if (LeftHand.IsNewButtonDown(Button.A))
            {
                //TODO: wrist GUI
                character.SwitchThrusts();
            }

            character.MoveAndRotate(move, rotate, 0f);
        }
        #endregion

        #region Utils
        public void CreatePopup(string message)
        {
            Bitmap img = new Bitmap(File.OpenRead(Util.GetAssetFolder() + "logo.png"));
            CreatePopup(EVRNotificationType.Transient, message, ref img);
        }

        public void CreatePopup(EVRNotificationType type, string message, ref Bitmap bitmap)
        {
            ulong handle = 0;
            OpenVR.Overlay.CreateOverlay(Guid.NewGuid().ToString(), "SpaceEngineersVR", ref handle);

            System.Drawing.Imaging.BitmapData TextureData =
            bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb
            );
            NotificationBitmap_t image = new NotificationBitmap_t()
            {
                bytes = TextureData.Scan0,
                width = TextureData.Width,
                height = TextureData.Height,
                depth = 4
            };
            //OpenVR..CreateNotification(handle, 0, type, message, EVRNotificationStyle.Application, ref image, ref id);
            Plugin.Instance.Log.Info("Pop-up created with message: " + message);

            bitmap.UnlockBits(TextureData);
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
                Plugin.Instance.Log.Info($"Messagebox created with the message: {msg}");
                DialogResult result = MessageBox.Show(msg, caption, MessageBoxButtons.OKCancel);
                return result;
            });
            return DialogResult.None;
        }
        #endregion

    }
}