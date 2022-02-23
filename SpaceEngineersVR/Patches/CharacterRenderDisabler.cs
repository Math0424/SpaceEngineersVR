using HarmonyLib;
using System;

namespace SpaceEngineersVR.Patches
{
    public static class CharacterRenderDisabler
    {
        public static bool RenderCharacter = false;
        static CharacterRenderDisabler()
        {
            Type t = AccessTools.TypeByName("Sandbox.Game.Components.MyRenderComponentCharacter");

            Plugin.Instance.Harmony.Patch(AccessTools.Method(t, "Draw"), new HarmonyMethod(typeof(CharacterRenderDisabler), nameof(Prefix_Draw)));
        }

        public static bool Prefix_Draw()
        {
            if (RenderCharacter)
            {
                return true;
            }

            return false;
        }

    }
}
