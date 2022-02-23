using System.ComponentModel;

namespace SpaceEngineersVR.Config
{
    public interface IPluginConfig : INotifyPropertyChanged
    {
        // Enables the plugin
        bool Enabled { get; set; }

        // TODO: Add config properties here, then extend the implementing classes accordingly
    }
}
