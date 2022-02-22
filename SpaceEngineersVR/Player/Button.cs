using System.Runtime.CompilerServices;
using Valve.VR;

namespace SpaceEngineersVR.Player
{
    public class Button
    {
        private static readonly unsafe uint InputDigitalActionData_t_size = (uint)sizeof(InputDigitalActionData_t);

        private InputDigitalActionData_t data;
        private readonly ulong handle;
        private bool previous;

        public bool Active => data.bActive;
        public bool IsPressed => data.bState;
        public bool HasChanged => IsPressed != previous;
        public bool HasPressed => IsPressed && HasChanged;
        public bool HasReleased => IsPressed && HasChanged;

        public Button(string name)
        {
            OpenVR.Input.GetActionHandle(name, ref handle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update()
        {
            previous = IsPressed;
            OpenVR.Input.GetDigitalActionData(handle, ref data, InputDigitalActionData_t_size, OpenVR.k_ulInvalidInputValueHandle);
        }
    }
}