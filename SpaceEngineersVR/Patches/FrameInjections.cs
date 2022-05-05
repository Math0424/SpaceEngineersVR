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
        public static Func<double, double, double, double, MatrixD> GetPerspectiveMatrix;
        public static Func<float, float, float, Matrix> GetPerspectiveMatrixRhInfiniteComplementary;
        
        static FrameInjections()
        {
            Type t = AccessTools.TypeByName("VRageRender.MyRender11");

            Common.Plugin.Harmony.Patch(AccessTools.Method(t, "Present"), new HarmonyMethod(typeof(FrameInjections), nameof(Prefix_Present)));

            Common.Plugin.Harmony.Patch(AccessTools.Method(t, "DrawScene"), new HarmonyMethod(typeof(FrameInjections), nameof(Prefix_DrawScene)));

            Common.Plugin.Harmony.Patch(AccessTools.Constructor(
                AccessTools.TypeByName("VRage.Ansel.MyAnselCamera"),
                new Type[]
                {
                    typeof(MatrixD),  //viewMatrix
                    typeof(float),    //fov
                    typeof(float),    //aspectRatio
                    typeof(float),    //nearPlane
                    typeof(float),    //farPlane
                    typeof(float),    //farFarPlane
                    typeof(Vector3D), //position
                    typeof(float),    //projectionOffsetX
                    typeof(float),    //projectionOffset
                }), transpiler: new HarmonyMethod(typeof(FrameInjections), nameof(Transpiler_ReplaceCreatePerspectiveFOV)));

            /*
            This seems to be used mainly to calculate the culling volume, and it really doesn't like the matrixes that my valve index gives
            Common.Plugin.Harmony.Patch(AccessTools.Method("VRage.Game.Utils.MyCamera:UpdatePropertiesInternal"),
                transpiler: new HarmonyMethod(typeof(FrameInjections), nameof(Transpiler_ReplaceCreatePerspectiveFOV)));
            */

            Common.Plugin.Harmony.Patch(AccessTools.Method("VRageRender.MyRender11:SetupCameraMatricesInternal"),
                transpiler: new HarmonyMethod(typeof(FrameInjections), nameof(Transpiler_SetupCameraMatrices)));

            Common.Plugin.Harmony.Patch(AccessTools.Method(typeof(Sandbox.Game.Gui.MyGuiScreenGamePlay), "Draw"),
                transpiler: new HarmonyMethod(typeof(FrameInjections), nameof(Transpiler_RemoveCallsToControlCameraAndUpdate)));
            Common.Plugin.Harmony.Patch(AccessTools.Method(typeof(Sandbox.Game.Gui.MyGuiScreenLoadInventory), "DrawScene"),
                transpiler: new HarmonyMethod(typeof(FrameInjections), nameof(Transpiler_RemoveCallsToControlCameraAndUpdate)));

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

        private static IEnumerable<CodeInstruction> Transpiler_ReplaceCreatePerspectiveFOV(IEnumerable<CodeInstruction> instructions)
        {
            //Replace calls to MatrixD.CreatePerspectiveFieldOfView with calls to GetPerspectiveFov

            MethodInfo createPerspectiveFoV = AccessTools.Method(
                typeof(MatrixD), "CreatePerspectiveFieldOfView",
                new Type[] { typeof(double), typeof(double), typeof(double), typeof(double) });
            foreach(CodeInstruction instruction in instructions)
            {
                if(instruction.Calls(createPerspectiveFoV))
                {
                    instruction.operand = AccessTools.Method(typeof(FrameInjections), nameof(GetPerspectiveFov));
                }

                yield return instruction;
            }
        }

        private static IEnumerable<CodeInstruction> Transpiler_SetupCameraMatrices(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> code = new List<CodeInstruction>(instructions);

            //Replace calls to Matrix.CreatePerspectiveFovRhInfiniteComplementary with calls to GetPerspectiveFovRhInfiniteComplementary
            {
                MethodInfo createPerspectiveFovRhInfiniteComplementary = AccessTools.Method(
                    typeof(Matrix),
                    "CreatePerspectiveFovRhInfiniteComplementary",
                    new Type[] { typeof(float), typeof(float), typeof(float) });

                for(int i = 0; i < code.Count; ++i)
                {
                    CodeInstruction instruction = code[i + 0];
                    if(instruction.Calls(createPerspectiveFovRhInfiniteComplementary))
                    {
                        instruction.operand = AccessTools.Method(typeof(FrameInjections), nameof(GetPerspectiveFovRhInfiniteComplementary));
                    }
                }
            }

            //Removes assignment to Matrix.M31 and M32 from MyRenderMessageSetCameraViewMatrix.ProjectionOffsetX and ProjectionOffsetY
            for(int i = 0; i < code.Count - 3; ++i)
            {
                CodeInstruction i0 = code[i + 0];
                CodeInstruction i1 = code[i + 1];
                CodeInstruction i2 = code[i + 2];
                CodeInstruction i3 = code[i + 3];

                if(
                    i0.opcode == OpCodes.Ldloca_S &&
                    i1.opcode == OpCodes.Ldarg_0 &&
                    (
                        i2.LoadsField(AccessTools.Field(typeof(MyRenderMessageSetCameraViewMatrix), "ProjectionOffsetX")) ||
                        i2.LoadsField(AccessTools.Field(typeof(MyRenderMessageSetCameraViewMatrix), "ProjectionOffsetY"))
                    ) && (
                        i3.StoresField(AccessTools.Field(typeof(Matrix), "M31")) ||
                        i3.StoresField(AccessTools.Field(typeof(Matrix), "M32"))
                    )
                ) {
                    //TODO: FIXME: Not moving lables/blocks that may have been added by other patches
                    code.RemoveRange(i, 4);
                    --i;
                }
            }

            return code;
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

        private static MatrixD GetPerspectiveFov(double fov, double aspectRatio, double nearPlane, double farPlane)
        {
            return GetPerspectiveMatrix(fov, aspectRatio, nearPlane, farPlane);
        }

        private static Matrix GetPerspectiveFovRhInfiniteComplementary(float fov, float aspectRatio, float nearPlane)
        {
            return GetPerspectiveMatrixRhInfiniteComplementary(fov, aspectRatio, nearPlane);
        }
    }
}