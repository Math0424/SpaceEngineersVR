using HarmonyLib;
using SharpDX.Direct3D11;
using System;
using System.Reflection;

namespace SpaceEnginnersVR.Wrappers
{
    public class BorrowedRtvTexture
    {
        public object Instance { get; }

        static BorrowedRtvTexture()
        {
            Type t = AccessTools.TypeByName("VRage.Render11.Resources.Textures.MyBorrowedTexture");
            release = t.GetMethod("Release", BindingFlags.Public | BindingFlags.Instance);
            t = AccessTools.TypeByName("VRage.Render11.Resources.Textures.MyBorrowedRtvTexture");
            get_Resource = t.GetProperty("Resource", BindingFlags.Public | BindingFlags.Instance).GetGetMethod();
        }

        public BorrowedRtvTexture(object instance)
        {
            Instance = instance;
        }

        private static readonly MethodInfo release;
        public void Release()
        {
            release.Invoke(Instance, new object[0]);
        }

        private static readonly MethodInfo get_Resource;
        public Texture2D GetResource()
        {
            return (Texture2D)get_Resource.Invoke(Instance, new object[0]);
        }

    }
}
