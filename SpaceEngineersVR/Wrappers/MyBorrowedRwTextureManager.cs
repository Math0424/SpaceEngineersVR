using HarmonyLib;
using SharpDX.DXGI;
using System;
using System.Reflection;

namespace SpaceEngineersVR.Wrappers
{
    public class MyBorrowedRwTextureManager
    {
        private readonly object instance;

        static MyBorrowedRwTextureManager()
        {
            Type t = AccessTools.TypeByName("VRage.Render11.Resources.MyBorrowedRwTextureManager");
            borrowRtv = AccessTools.Method(t, "BorrowRtv", new Type[] { typeof(string), typeof(int), typeof(int), typeof(Format), typeof(int), typeof(int) });
        }

        public MyBorrowedRwTextureManager(object instance)
        {
            this.instance = instance;
        }

        private static readonly MethodInfo borrowRtv;
        public BorrowedRtvTexture BorrowRtv(string debugName, int width, int height, Format format, int samplesCount = 1, int samplesQuality = 0)
        {
            object x = borrowRtv.Invoke(instance, new object[] { debugName, width, height, format, samplesCount, samplesQuality });
            if (x == null)
            {
                return null;
            }

            return new BorrowedRtvTexture(x);
        }
    }
}
