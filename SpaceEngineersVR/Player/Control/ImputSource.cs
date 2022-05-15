using Valve.VR;

namespace SpaceEngineersVR.Player.Control
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