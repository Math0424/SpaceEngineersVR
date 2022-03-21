using HarmonyLib;
using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Reflection;

namespace SpaceEngineersVR.Wrappers
{
    public class MyRenderContext
    {
        private readonly object instance;

        static MyRenderContext()
        {
            Type t = AccessTools.TypeByName("VRage.Render11.RenderContext.MyRenderContext");
            Type tIResource = AccessTools.TypeByName("VRage.Render11.Resources.IResource");
            copyResource = AccessTools.Method(t, "CopyResource", new Type[] { tIResource, typeof(Resource) });
            mapSubresource = AccessTools.Method(t, "MapSubresource", new Type[] { typeof(Texture2D), typeof(int), typeof(int), typeof(MapMode), typeof(MapFlags), typeof(DataStream).MakeByRefType() });
            unmapSubresource = AccessTools.Method(t, "UnmapSubresource", new Type[] { typeof(Resource), typeof(int) });
        }

        public MyRenderContext(object instance)
        {
            this.instance = instance;
        }

        private static readonly MethodInfo copyResource;
        public void CopyResource(object source, Resource destination)
        {
            copyResource.Invoke(instance, new object[] { source, destination });
        }

        private static readonly MethodInfo mapSubresource;
        public DataBox MapSubresource(Texture2D resource, int mipSlice, int arraySlice, MapMode mode, MapFlags flags, out DataStream stream)
        {
            object[] args = new object[] { resource, mipSlice, arraySlice, mode, flags, null };
            DataBox result = (DataBox)mapSubresource.Invoke(instance, args);
            stream = (DataStream)args[5];
            return result;
        }

        private static readonly MethodInfo unmapSubresource;
        public void UnmapSubresource(Resource resourceRef, int subresource)
        {
            unmapSubresource.Invoke(instance, new object[] { resourceRef, subresource });
        }
    }
}