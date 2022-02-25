using HarmonyLib;
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

            Main.Instance.Harmony.Patch(AccessTools.Method(t, "Present"), new HarmonyMethod(typeof(FrameInjections), nameof(Prefix_Present)));

            Main.Instance.Harmony.Patch(AccessTools.Method("VRageRender.MyRender11:DrawScene"), new HarmonyMethod(typeof(FrameInjections), nameof(Prefix_DrawScene)));

            Main.Instance.Log.Info("Applied harmony game injections for renderer.");
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
