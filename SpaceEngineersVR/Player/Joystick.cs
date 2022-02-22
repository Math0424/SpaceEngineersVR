using System.Runtime.CompilerServices;
using Valve.VR;
using VRageMath;

namespace SpaceEngineersVR.Player
{
    public class Joystick
    {
        private static readonly unsafe uint InputAnalogActionData_t_size = (uint)sizeof(InputAnalogActionData_t);

        private InputAnalogActionData_t data;
        private readonly ulong handle;

        public bool Active => data.bActive;
        public Vector2 Position => new Vector2(data.x, data.y);
        public Vector2 Delta => new Vector2(data.deltaX, data.deltaY);

        public Joystick(string name)
        {
            OpenVR.Input.GetActionHandle(name, ref handle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update()
        {
            OpenVR.Input.GetAnalogActionData(handle, ref data, InputAnalogActionData_t_size, OpenVR.k_ulInvalidInputValueHandle);
        }
    }
}