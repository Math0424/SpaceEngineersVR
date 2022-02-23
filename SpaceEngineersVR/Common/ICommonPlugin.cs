using SpaceEngineersVR.Config;
using SpaceEngineersVR.Logging;

namespace SpaceEngineersVR.Common
{
    public interface ICommonPlugin
    {
        IPluginLogger Log { get; }
        IPluginConfig Config { get; }
        long Tick { get; }
    }
}
