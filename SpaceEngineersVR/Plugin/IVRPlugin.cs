using HarmonyLib;
using SpaceEnginnersVR.Config;

namespace ClientPlugin.Plugin
{
    public interface IVRPlugin
    {

        Harmony Harmony { get; }
        IPluginConfig Config { get; }

    }
}
