using ClientPlugin.Player.Components;
using HarmonyLib;
using SpaceEngineersVR.Plugin;
using System;

namespace SpaceEngineersVR.Patches
{
    [Util.InitialiseOnStart]
    public static class FrameInjections
    {
        public static bool DisablePresent = false;

        static FrameInjections()
        {
            Type t = AccessTools.TypeByName("VRageRender.MyRender11");

            Common.Plugin.Harmony.Patch(AccessTools.Method(t, "Present"), new HarmonyMethod(typeof(FrameInjections), nameof(Prefix_Present)));

            Common.Plugin.Harmony.Patch(AccessTools.Method(t, "DrawScene"), new HarmonyMethod(typeof(FrameInjections), nameof(Prefix_DrawScene)));

            Logger.Info("Applied harmony game injections for renderer.");
        }

        private static bool Prefix_DrawScene()
        {
            Player.DeviceManager.UpdateRender();
            Player.DeviceManager.Headset.UpdateRender();
            VRGUIManager.Draw();

            return true;
        }

        private static bool Prefix_Present()
        {
            return !DisablePresent;
        }
    }
}