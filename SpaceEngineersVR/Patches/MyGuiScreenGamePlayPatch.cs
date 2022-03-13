using HarmonyLib;
using SpaceEngineersVR.Plugin;
using Sandbox.Game.Gui;
using SpaceEngineersVR.Player;

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