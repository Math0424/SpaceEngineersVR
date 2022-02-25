using HarmonyLib;
using System;
using System.Reflection;

namespace ClientPlugin.Wrappers
{
    public static class MyManagers
    {
        static MyManagers()
        {
            Type t = AccessTools.TypeByName("VRage.Render11.Common.MyManagers");
            rwTexturesPool = t.GetField("RwTexturesPool", BindingFlags.Public | BindingFlags.Static);
        }

        private static readonly FieldInfo rwTexturesPool;
        public static MyBorrowedRwTextureManager RwTexturesPool => new MyBorrowedRwTextureManager(rwTexturesPool.GetValue(null));
    }
}
