using HarmonyLib;
using System.Reflection;

namespace ClientPlugin.Patches
{
    [HarmonyPatch]
    public static class PlayerAndCameraDisabler
    {
        public static MethodBase TargetMethod()
        {
            return AccessTools.Method("Sandbox.Game.Gui.MyGuiScreenGamePlay:MoveAndRotatePlayerOrCamera");
        }

        public static bool Prefix()
        {
            if (Plugin.Instance.Config.EnableKeyboardAndMouseControls)
            {
                return true;
            }
            return false;
        }
    }
}
