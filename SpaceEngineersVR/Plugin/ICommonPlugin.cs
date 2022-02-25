using SpaceEnginnersVR.Config;
using SpaceEnginnersVR.Logging;

namespace SpaceEnginnersVR.Plugin
{
    public interface ICommonPlugin
    {
        IPluginLogger Log { get; }
        IPluginConfig Config { get; }
        long Tick { get; }
    }
}