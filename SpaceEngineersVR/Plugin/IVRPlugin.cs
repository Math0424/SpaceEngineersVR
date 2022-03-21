using HarmonyLib;
using SpaceEngineersVR.Config;

namespace ClientPlugin.Plugin
{
    public interface IVRPlugin
    {

        Harmony Harmony { get; }
        IPluginConfig Config { get; }

    }
}