using HarmonyLib;
using SharpDX.DXGI;
using System;
using System.Reflection;
using VRageMath;
using VRageRender;
using VRageRender.Messages;

namespace SpaceEngineersVR.Wrappers
{
    public static class MyRender11
    {
        public static Vector2I Resolution
        {
            get => (Vector2I)resolution.GetValue(null);
            set => SetResolution(value);
        }

        static MyRender11()
        {
            Type t = AccessTools.TypeByName("VRageRender.MyRender11");

            /*log.Write("Fields:");
            foreach (var s in AccessTools.GetFieldNames(t))
                log.Write(s);         
            log.Write("Methods:");
            foreach (var s in AccessTools.GetMethodNames(t))
                log.Write(s);*/

            m_debugOverrides = AccessTools.Field(t, "m_debugOverrides");
            m_rc = AccessTools.Field(t, "m_rc");
            settings = AccessTools.Field(t, "Settings");
            m_settings = AccessTools.Field(t, "m_settings");

            setupCameraMatricesDel = (Action<MyRenderMessageSetCameraViewMatrix>)Delegate.CreateDelegate(typeof(Action<MyRenderMessageSetCameraViewMatrix>), AccessTools.Method(t, "SetupCameraMatrices"));
            createScreenResourcesDel = (Action)Delegate.CreateDelegate(typeof(Action), AccessTools.Method(t, "CreateScreenResources"));
            fullDrawScene = (Action<bool>)Delegate.CreateDelegate(typeof(Action<bool>), AccessTools.Method(t, "FullDraw"));

            processMessageQueueDel = (Action)Delegate.CreateDelegate(typeof(Action), AccessTools.Method(t, "ProcessMessageQueue"));
            processUpdatesDel = (Action)Delegate.CreateDelegate(typeof(Action), AccessTools.Method(t, "ProcessUpdates"));
            updateGameSceneDel = (Action)Delegate.CreateDelegate(typeof(Action), AccessTools.Method(t, "UpdateGameScene"));

            backbuffer = AccessTools.Field(t, "<Backbuffer>k__BackingField");
            resolution = AccessTools.Field(t, "m_resolution");

            drawGameScene = AccessTools.Method(t, "DrawGameScene");
            resizeSwapChain = AccessTools.Method(t, "ResizeSwapchain");

            get_deviceInstance = AccessTools.Property(t, "DeviceInstance").GetGetMethod(true);

            environment = AccessTools.Field(t, "Environment");
            environment_matrices = AccessTools.Field("VRageRender.MyEnvironment:Matrices");
        }

        //TODO; make delegate
        private static readonly FieldInfo backbuffer;
        public static MyBackbuffer GetBackbuffer() { return new MyBackbuffer(backbuffer.GetValue(null)); }


        private static readonly FieldInfo m_debugOverrides;
        public static MyRenderDebugOverrides DebugOverrides => (MyRenderDebugOverrides)m_debugOverrides.GetValue(null);

        private static readonly FieldInfo settings;
        public static MyRenderSettings Settings
        {
            get => (MyRenderSettings)settings.GetValue(null);
            set => settings.SetValue(null, value);
        }

        private static readonly FieldInfo m_settings;
        public static MyRenderDeviceSettings m_Settings
        {
            get => (MyRenderDeviceSettings)m_settings.GetValue(null);
            set => settings.SetValue(null, value);
        }


        private static readonly MethodInfo resizeSwapChain;
        public static void ResizeSwapChain(int width, int height)
        {
            resizeSwapChain.Invoke(null, new object[] { width, height });
        }

        private static readonly Action<MyRenderMessageSetCameraViewMatrix> setupCameraMatricesDel;
        public static void SetupCameraMatrices(MyRenderMessageSetCameraViewMatrix message)
        {
            setupCameraMatricesDel.Invoke(message);
        }

        private static readonly Action processMessageQueueDel;
        public static void ProcessMessageQueue()
        {
            processMessageQueueDel.Invoke();
        }

        private static readonly Action processUpdatesDel;
        public static void ProcessUpdates()
        {
            processUpdatesDel.Invoke();
        }

        private static readonly Action createScreenResourcesDel;
        public static void CreateScreenResources()
        {
            createScreenResourcesDel.Invoke();
        }

        private static readonly Action updateGameSceneDel;
        public static void UpdateGameScene()
        {
            updateGameSceneDel.Invoke();
        }

        private static readonly FieldInfo resolution;
        private static void SetResolution(Vector2I vector)
        {
            resolution.SetValue(null, vector);
        }


        private static readonly Action<bool> fullDrawScene;
        public static void FullDrawScene(bool draw = false)
        {
            fullDrawScene.Invoke(draw);
        }

        private static readonly MethodInfo drawGameScene;
        public static void DrawGameScene(BorrowedRtvTexture renderTarget, out object debugAmbientOcclusion)
        {
            object[] args = new object[] { renderTarget.Instance, null };
            drawGameScene.Invoke(null, args);
            debugAmbientOcclusion = args[1];
        }

        private static readonly MethodInfo get_deviceInstance;
        public static Device1 DeviceInstance => (Device1)get_deviceInstance.Invoke(null, new object[0]);

        private static readonly FieldInfo m_rc;
        public static MyRenderContext RC => new MyRenderContext(m_rc.GetValue(null));

        private static readonly FieldInfo environment;
        private static readonly FieldInfo environment_matrices;
        public static MyEnvironmentMatrices Environment_Matrices => new MyEnvironmentMatrices(environment_matrices.GetValue(environment.GetValue(null)));
    }

    public static class MyImmediateRC
    {
        public static MyRenderContext RC => MyRender11.RC;
    }

}