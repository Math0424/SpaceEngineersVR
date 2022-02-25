using HarmonyLib;
using System;
using System.Reflection;

namespace SpaceEnginnersVR.Patches
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
            if (Main.Instance.Config.EnableCharacterRendering)
            {
                return true;
            }

            return false;
        }
    }
}
