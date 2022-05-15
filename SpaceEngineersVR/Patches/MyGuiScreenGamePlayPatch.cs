using SpaceEngineersVR.Player.Components;
using HarmonyLib;
using Sandbox.Game.Gui;
using SpaceEngineersVR.Player;
using SpaceEngineersVR.Plugin;

namespace SpaceEngineersVR.Patches
{
    [HarmonyPatch(typeof(MyGuiScreenGamePlay))]
    public static class MyGuiScreenGamePlayPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(MyGuiScreenGamePlay.MoveAndRotatePlayerOrCamera))]
        public static bool Prefix()
        {
            if (VRMovementComponent.UsingControllerMovement)
                return false;

            return Common.Config.EnableKeyboardAndMouseControls;
        }
    }
}