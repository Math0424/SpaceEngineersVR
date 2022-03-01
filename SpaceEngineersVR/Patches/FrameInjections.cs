using HarmonyLib;
using SpaceEngineersVR.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using VRageMath;
using VRageRender;
using VRageRender.Messages;

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

            SpaceVR.Harmony.Patch(AccessTools.Constructor(
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
                }), transpiler: new HarmonyMethod(typeof(FrameInjections), nameof(Transpiler_AnselCameraConstructor)));

            SpaceVR.Harmony.Patch(AccessTools.Method("VRageRender.MyRender11:SetupCameraMatricesInternal"),
                transpiler: new HarmonyMethod(typeof(FrameInjections), nameof(Transpiler_SetupCameraMatrices)));

            new Logger().Write("Applied harmony game injections");
        }

        private static bool Prefix_DrawScene()
        {
            return DrawScene.Invoke();
        }

        private static bool Prefix_Present()
        {
            return !DisablePresent;
        }

        private static IEnumerable<CodeInstruction> Transpiler_AnselCameraConstructor(IEnumerable<CodeInstruction> instructions)
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
                    i--;
                }
            }

            return code;
        }

        public static Func<double, double, double, double, MatrixD> GetPerspectiveMatrix;
        private static MatrixD GetPerspectiveFov(double fov, double aspectRatio, double nearPlane, double farPlane)
        {
            return GetPerspectiveMatrix(fov, aspectRatio, nearPlane, farPlane);
        }

        public static Func<float, float, float, Matrix> GetPerspectiveMatrixRhInfiniteComplementary;
        private static Matrix GetPerspectiveFovRhInfiniteComplementary(float fov, float aspectRatio, float nearPlane)
        {
            return GetPerspectiveMatrixRhInfiniteComplementary(fov, aspectRatio, nearPlane);
        }
    }
}
