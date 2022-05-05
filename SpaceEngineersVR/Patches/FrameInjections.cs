using HarmonyLib;
using SpaceEngineersVR.Plugin;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using VRageMath;
using VRageRender.Messages;
using VRage.Library.Utils;

namespace SpaceEngineersVR.Patches
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

        private static bool Prefix_DrawScene()
        {
            return DrawScene.Invoke();
        }

        private static bool Prefix_Present()
        {
            return !DisablePresent;
        }

        private static IEnumerable<CodeInstruction> Transpiler_RemoveCallsToControlCameraAndUpdate(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> code = new List<CodeInstruction>(instructions);

            MethodInfo getMySectorMainCamera = AccessTools.PropertyGetter(typeof(Sandbox.Game.World.MySector), "MainCamera");

            {
                MethodInfo getMySessionStatic = AccessTools.PropertyGetter(typeof(Sandbox.Game.World.MySession), "Static");
                MethodInfo getMySessionCameraController = AccessTools.PropertyGetter(typeof(Sandbox.Game.World.MySession), "CameraController");
                MethodInfo controlCamera = AccessTools.Method(typeof(VRage.Game.ModAPI.Interfaces.IMyCameraController), "ControlCamera");
                for(int i = 0; i < code.Count - 3; ++i)
                {
                    if(
                        code[i + 0].Calls(getMySessionStatic) &&
                        code[i + 1].Calls(getMySessionCameraController) &&
                        code[i + 2].Calls(getMySectorMainCamera) &&
                        code[i + 3].Calls(controlCamera)
                    ) {
                        //TODO: FIXME: Not moving lables/blocks that may have been added by other patches
                        code.RemoveRange(i, 4);
                        --i;
                    }
                }
            }

            {
                MethodInfo cameraUpdate = AccessTools.Method(typeof(VRage.Game.Utils.MyCamera), "Update");
                for(int i = 0; i < code.Count - 2; ++i)
                {
                    if(
                        code[i + 0].Calls(getMySectorMainCamera) &&
                        code[i + 1].LoadsConstant() &&
                        code[i + 2].Calls(cameraUpdate)
                    ) {
                        //TODO: FIXME: Not moving lables/blocks that may have been added by other patches
                        code.RemoveRange(i, 3);
                        --i;
                    }
                }
            }

            /*
            Enabling this one seems to completely prevent the game from getting past the loading screen
            Tried calling it from our rendering, didnt fix
            {
                MethodInfo uploadViewMatrixToRender = AccessTools.Method(typeof(VRage.Game.Utils.MyCamera), "UploadViewMatrixToRender");
                for(int i = 0; i < code.Count - 1; ++i)
                {
                    if(
                        code[i + 0].Calls(getMySectorMainCamera) &&
                        code[i + 1].Calls(uploadViewMatrixToRender)
                    ) {
                        //TODO: FIXME: Not moving lables/blocks that may have been added by other patches
                        code.RemoveRange(i, 2);
                        --i;
                    }
                }
            }
            */

            return code;
        }
    }
}