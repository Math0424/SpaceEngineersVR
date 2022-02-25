using System.ComponentModel;

namespace SpaceEnginnersVR.Config
{
    public interface IPluginConfig: INotifyPropertyChanged
    {
        bool EnableKeyboardAndMouseControls { get; set; }
        bool EnableCharacterRendering { get; set; }

        // TODO: Add config properties here, then extend the implementing classes accordingly
    }
}