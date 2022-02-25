using Valve.VR;

namespace SpaceEnginnersVR.Player
{
    public class InputSource
    {
        private readonly ulong handle;

        public InputSource(string name)
        {
            OpenVR.Input.GetInputSourceHandle(name, ref handle);
        }
    }
}