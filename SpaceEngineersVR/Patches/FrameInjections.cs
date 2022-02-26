using HarmonyLib;
using SpaceEnginnersVR.Logging;
using SpaceEnginnersVR.Plugin;
using System;

namespace SpaceEnginnersVR.Patches
{
    public static class FrameInjections
    {

        public static Func<bool> DrawScene;
        public static bool DisablePresent = false;

        static FrameInjections()
        {
            Type t = AccessTools.TypeByName("VRageRender.MyRender11");

            Common.Plugin.Harmony.Patch(AccessTools.Method(t, "Present"), new HarmonyMethod(typeof(FrameInjections), nameof(Prefix_Present)));

            Common.Plugin.Harmony.Patch(AccessTools.Method(t, "DrawScene"), new HarmonyMethod(typeof(FrameInjections), nameof(Prefix_DrawScene)));

            Logger.Info("Applied harmony game injections for renderer.");
        }

        public static bool Prefix_DrawScene()
        {
            return DrawScene.Invoke();
        }

        public static bool Prefix_Present()
        {
            return !DisablePresent;
        }
    }
}
