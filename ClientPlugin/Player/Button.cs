using System.Runtime.CompilerServices;
using Valve.VR;

namespace ClientPlugin.Player
{
    public class Button
    {
        private static readonly unsafe uint InputDigitalActionData_t_size = (uint)sizeof(InputDigitalActionData_t);

        private InputDigitalActionData_t data;
        private readonly ulong handle;
        private bool previous;

        public bool Active => data.bActive;
        public bool IsPressed => data.bState;
        public bool HasChanged => data.bChanged;
        public bool HasPressed => data.bState && data.bChanged;
        public bool HasReleased => !data.bState && data.bChanged;

        public Button(string name)
        {
            OpenVR.Input.GetActionHandle(name, ref handle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update()
        {
            OpenVR.Input.GetDigitalActionData(handle, ref data, InputDigitalActionData_t_size, OpenVR.k_ulInvalidInputValueHandle);
        }
    }
}