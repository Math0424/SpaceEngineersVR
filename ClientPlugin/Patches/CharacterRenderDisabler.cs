using HarmonyLib;
using System;
using System.Reflection;

namespace ClientPlugin.Patches
{
    [HarmonyPatch]
    public static class CharacterRenderDisabler
    {
        public static MethodBase TargetMethod()
        {
            Type t = AccessTools.TypeByName("Sandbox.Game.Components.MyRenderComponentCharacter");
            return AccessTools.Method(t, "Draw");
        }

        public static bool Prefix()
        {
            if (Plugin.Instance.Config.EnableCharacterRendering)
            {
                return true;
            }

            return false;
        }
    }
}
