using HarmonyLib;
using SpaceEngineersVR.Utils;
using System;
using VRageRender;

namespace SpaceEngineersVR.Patches
{
    public static class FrameInjections
    {

        public static Func<bool> DrawScene;
        public static bool DisablePresent = false;

        static FrameInjections()
        {
            Type t = AccessTools.TypeByName("VRageRender.MyRender11");

            SpaceVR.Harmony.Patch(AccessTools.Method(t, "Present"), new HarmonyMethod(typeof(FrameInjections), nameof(Prefix_Present)));

            SpaceVR.Harmony.Patch(AccessTools.Method("VRageRender.MyRender11:DrawScene"), new HarmonyMethod(typeof(FrameInjections), nameof(Prefix_DrawScene)));
            
            new Logger().Write("Applied harmony game injections");
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
