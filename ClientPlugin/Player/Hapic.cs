using System.Runtime.CompilerServices;
using Valve.VR;

namespace SpaceEnginnersVR.Player
{
    public class Haptic
    {
        private readonly ulong handle;

        public Haptic(string name)
        {
            OpenVR.Input.GetActionHandle(name, ref handle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Vibrate(float delay, float duration, float frequency, float amplitude)
        {
            OpenVR.Input.TriggerHapticVibrationAction(handle, delay, duration, frequency, amplitude, OpenVR.k_ulInvalidInputValueHandle);
        }
    }
}