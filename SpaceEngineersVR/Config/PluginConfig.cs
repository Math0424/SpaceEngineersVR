using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SpaceEngineersVR.Config
{
    public class PluginConfig : INotifyPropertyChanged
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

        private bool useHeadRotationForCharacter = true;


        private float playerHeight = 1.69f;
        private float playerArmSpan = 1.66f;

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

        public bool UseHeadRotationForCharacter
        {
            get => useHeadRotationForCharacter;
            set => SetValue(ref useHeadRotationForCharacter, value);
        }

        public float PlayerHeight
        {
            get => playerHeight;
            set => SetValue(ref playerHeight, value);
        }
        public float PlayerArmSpan
        {
            get => playerArmSpan;
            set => SetValue(ref playerArmSpan, value);
        }
    }
}