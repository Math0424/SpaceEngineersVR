using Valve.VR;

namespace SpaceEnginnersVR.Player.Controller
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