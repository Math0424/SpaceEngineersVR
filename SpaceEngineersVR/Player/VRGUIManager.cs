using Sandbox.Game.Gui;
using Sandbox.Graphics.GUI;
using SharpDX.Direct3D11;
using SpaceEngineersVR.Wrappers;
using Valve.VR;
using VRageMath;

namespace SpaceEngineersVR.Player.Components
{
    public static class VRGUIManager
    {
        public static bool IsDebugHUDEnabled = true;

        private static readonly ulong overlayHandle = 0uL;

        static VRGUIManager()
        {
            OpenVR.Overlay.CreateOverlay("SEVR_DEBUG_OVERLAY", "SEVR_DEBUG_OVERLAY", ref overlayHandle);
            OpenVR.Overlay.SetOverlayWidthInMeters(overlayHandle, 3);

            HmdMatrix34_t transform = new HmdMatrix34_t
            {
                m0 = 1f, m1 = 0f, m2 = 0f, m3 = 0f,
                m4 = 0f, m5 = 1f, m6 = 0f, m7 = 1f,
                m8 = 0f, m9 = 0f, m10 = 1f, m11 = -2f
            };
            OpenVR.Overlay.SetOverlayTransformAbsolute(overlayHandle, ETrackingUniverseOrigin.TrackingUniverseStanding, ref transform);

            OpenVR.Overlay.SetOverlayCurvature(overlayHandle, 0.25f);
            OpenVR.Overlay.ShowOverlay(overlayHandle);
        }

        //called in Headset draw method
        //so make sure its fast :)
        public static void Draw()
        {
            DrawOverlay();
        }

        private static void DrawOverlay()
        {
            if (IsDebugHUDEnabled && IsAnyDialogOpen())
            {
                Texture2D guiTexture = (Texture2D)MyRender11.GetBackbuffer().GetResource();
                Texture_t textureUI = new Texture_t
                {
                    eColorSpace = EColorSpace.Auto,
                    eType = ETextureType.DirectX,
                    handle = guiTexture.NativePointer
                };

                OpenVR.Overlay.SetOverlayTexture(overlayHandle, ref textureUI);
                OpenVR.Overlay.ShowOverlay(overlayHandle);
            }
            else
            {
                OpenVR.Overlay.HideOverlay(overlayHandle);
            }
        }

        /// <summary>
        /// Returns true if any other screen other than MyGuiScreenGamePlay or MyGuiScreenHudSpace is opened.
        /// </summary>
        /// <returns>A boolean value.</returns>
        private static bool IsAnyDialogOpen()
        {
            foreach (MyGuiScreenBase screen in MyScreenManager.Screens)
            {
                if (!(screen is MyGuiScreenGamePlay) && !(screen is MyGuiScreenHudSpace))
                {
                    return true;
                }
            }
            return false;
        }
    }
}