using HarmonyLib;
using SpaceEnginnersVR.Plugin;
using System;
using System.Reflection;

namespace SpaceEnginnersVR.Patches
{

    //TODO: Remove this class
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
            if (Common.Config.EnableCharacterRendering)
            {
                return true;
            }

            return false;
        }
    }
}
