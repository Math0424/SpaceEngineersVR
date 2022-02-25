using HarmonyLib;
using SharpDX.Direct3D11;
using System;
using System.Reflection;

namespace SpaceEnginnersVR.Wrappers
{
    public class MyBackbuffer
    {
        public object Instance { get; }

        static MyBackbuffer()
        {
            Type t = AccessTools.TypeByName("VRage.Render11.Resources.MyBackbuffer");
            release = t.GetMethod("Release", BindingFlags.Public | BindingFlags.Instance);
            get_Resource = t.GetProperty("Resource", BindingFlags.Public | BindingFlags.Instance).GetGetMethod();
        }

        public MyBackbuffer(object instance)
        {
            Instance = instance;
        }

        private static readonly MethodInfo release;
        public void Release()
        {
            release.Invoke(Instance, new object[0]);
        }

        private static readonly MethodInfo get_Resource;
        public Resource GetResource()
        {
            return (Resource)get_Resource.Invoke(Instance, new object[0]);
        }

    }
}
