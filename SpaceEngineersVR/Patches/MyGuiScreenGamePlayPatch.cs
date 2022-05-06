using HarmonyLib;
using Sandbox.Game.Gui;
using SpaceEngineersVR.Player;
using SpaceEngineersVR.Plugin;

namespace SpaceEngineersVR.Patches
{
    [HarmonyPatch(typeof(MyGuiScreenGamePlay))]
    public static class PlayerAndCameraDisabler
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(MyGuiScreenGamePlay.MoveAndRotatePlayerOrCamera))]
        public static bool Prefix()
        {
            if (Headset.UsingControllerMovement)
                return false;

            return Common.Config.EnableKeyboardAndMouseControls;
        }
    }
}