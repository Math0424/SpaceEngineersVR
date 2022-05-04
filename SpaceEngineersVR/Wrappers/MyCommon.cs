using HarmonyLib;
using SharpDX.DXGI;
using System;
using System.Reflection;
using VRageMath;
using VRageRender;
using VRageRender.Messages;

namespace SpaceEngineersVR.Wrappers
{
    public static class MyCommon
    {

        static MyCommon()
        {
            Type t = AccessTools.TypeByName("VRageRender.MyCommon");

            UpdateTimersDel = (Action)Delegate.CreateDelegate(typeof(Action), AccessTools.Method(t, "UpdateTimers"));
        }

        private static readonly Action UpdateTimersDel;
        public static void UpdateTimers()
        {
            UpdateTimersDel.Invoke();
        }

    }

}