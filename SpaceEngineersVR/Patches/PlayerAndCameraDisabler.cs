using HarmonyLib;

namespace SpaceEngineersVR.Patches
{
    public static class PlayerAndCameraDisabler
    {
        public static bool DisablePlayerAndCameraMovement = false;
        static PlayerAndCameraDisabler()
        {
            Plugin.Instance.Harmony.Patch(AccessTools.Method("Sandbox.Game.Gui.MyGuiScreenGamePlay:MoveAndRotatePlayerOrCamera"), new HarmonyMethod(typeof(PlayerAndCameraDisabler), nameof(Prefix)));
        }

        public static bool Prefix()
        {
            if (DisablePlayerAndCameraMovement)
            {
                return false;
            }
            return true;
        }
    }
}
