using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SpaceEnginnersVR.Config
{
    public class PluginConfig : IPluginConfig
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void SetValue<T>(ref T field, T value, [CallerMemberName] string propName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;

            field = value;

            OnPropertyChanged(propName);
        }

        private void OnPropertyChanged([CallerMemberName] string propName = "")
        {
            PropertyChangedEventHandler propertyChanged = PropertyChanged;
            if (propertyChanged == null)
                return;

            propertyChanged(this, new PropertyChangedEventArgs(propName));
        }

        private bool enableKeyboardAndMouseControls = true;
        private bool enableCharacterRendering = true;

        public bool EnableKeyboardAndMouseControls
        {
            get => enableKeyboardAndMouseControls;
            set => SetValue(ref enableKeyboardAndMouseControls, value);
        }

        public bool EnableCharacterRendering
        {
            get => enableCharacterRendering;
            set => SetValue(ref enableCharacterRendering, value);
        }
    }
}