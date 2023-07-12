using System;
using System.Reflection;

namespace SpaceEngineersVR.Util
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface)]
    internal sealed class InitialiseOnStartAttribute : Attribute
    {
        public static bool IsInitialised = false;

        public static void FindAndInitialise()
        {
            if (IsInitialised)
                throw new Exception("Tried to initialise on start attributes twice");

            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                InitialiseOnStartAttribute[] dependencies = (InitialiseOnStartAttribute[])type.GetCustomAttributes(typeof(InitialiseOnStartAttribute), true);
                foreach (InitialiseOnStartAttribute attribute in dependencies)
                {
                    System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(type.TypeHandle);
                }
            }

            IsInitialised = true;
        }
    }
}