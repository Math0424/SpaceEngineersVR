using SpaceEnginnersVR.Utill;
using System.Runtime.CompilerServices;
using Valve.VR;
using VRageMath;

namespace SpaceEnginnersVR.Player.Controller
{
    public class Pose
    {
        // FIXME: Make it configurable between seated and standing
        public static ETrackingUniverseOrigin TrackingOrigin = ETrackingUniverseOrigin.TrackingUniverseStanding;

        private static readonly unsafe uint InputPoseActionData_t_size = (uint)sizeof(InputPoseActionData_t);

        private InputPoseActionData_t data;
        private readonly ulong handle;

        public bool Active => data.bActive;
        public bool DeviceConnected => data.pose.bDeviceIsConnected;
        public bool Valid => data.pose.bPoseIsValid;
        public Vector3 Velocity => data.pose.vVelocity.ToVector();
        public Vector3 AngularVelocity => data.pose.vAngularVelocity.ToVector();
        public MatrixD AbsoluteTracking => data.pose.mDeviceToAbsoluteTracking.ToMatrix();

        public Vector3 TenFrameVelocity
        {
            get
            {
                Vector3 result = Vector3.Zero;
                for (int i = 0; i < 10; i++)
                    result += rollingVelocity[i];
                return (result / 10f);
            }
        }

        private Vector3[] rollingVelocity = new Vector3[10];
        private int update = 0;

        public Pose(string name)
        {
            OpenVR.Input.GetActionHandle(name, ref handle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update()
        {
            rollingVelocity[update++ % 10] = data.pose.vVelocity.ToVector();
            OpenVR.Input.GetPoseActionDataForNextFrame(handle, TrackingOrigin, ref data, InputPoseActionData_t_size, OpenVR.k_ulInvalidInputValueHandle);
        }
    }
}