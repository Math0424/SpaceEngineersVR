using SpaceEngineersVR.Util;
using Valve.VR;
using VRageMath;

namespace SpaceEngineersVR.Player
{
    public class TrackedDevice
    {
        public Matrix deviceToPlayer => pose.deviceToAbsolute.matrix * Player.PlayerToAbsolute.inverted;

        public struct Pose
        {
            public bool isConnected;
            public bool isTracked;

            public MatrixAndInvert deviceToAbsolute;

            public Vector3 velocity;
            public Vector3 angularVelocity;

            public static Pose Create()
            {
                return new Pose()
                {
                    deviceToAbsolute = MatrixAndInvert.Identity,
                };
            }
        }
        public Pose renderPose = Pose.Create();
        public Pose pose = Pose.Create();

        public uint deviceId = OpenVR.k_unTrackedDeviceIndexInvalid;

        private readonly ulong hapticsActionHandle;
        private readonly ulong actionHandle;

        public TrackedDevice(string actionName = null, string hapticsName = "/actions/feedback/out/GenericHaptic")
        {
            if (!string.IsNullOrEmpty(actionName))
                OpenVR.Input.GetActionHandle(actionName, ref actionHandle);
            OpenVR.Input.GetInputSourceHandle(hapticsName, ref hapticsActionHandle);
        }

        public void Vibrate(float delay, float duration, float frequency, float amplitude)
        {
            if (hapticsActionHandle == 0)
                return;

            OpenVR.Input.TriggerHapticVibrationAction(hapticsActionHandle, delay, duration, frequency, amplitude, OpenVR.k_ulInvalidInputValueHandle);
        }


        public virtual void MainUpdate()
        {
        }

        public void SetMainPoseData(TrackedDevicePose_t value)
        {
            SetPoseData(ref pose, value, out bool wasConnected, out bool wasDisconnected, out bool startedTracking, out bool lostTracking);

            if (wasConnected)
                OnConnected();
            if (wasDisconnected)
                OnDisconnected();

            if (startedTracking)
                OnStartTracking();
            if (lostTracking)
                OnLostTracking();
        }

        public void SetRenderPoseData(TrackedDevicePose_t value)
        {
            SetPoseData(ref renderPose, value, out _, out _, out _, out _);
        }

        private static void SetPoseData(ref Pose pose, TrackedDevicePose_t value, out bool wasConnected, out bool wasDisconnected, out bool startedTracking, out bool lostTracking)
        {
            wasConnected = false;
            wasDisconnected = false;
            startedTracking = false;
            lostTracking = false;

            if (pose.isConnected != value.bDeviceIsConnected)
            {
                pose.isConnected = value.bDeviceIsConnected;

                if (value.bDeviceIsConnected)
                    wasConnected = true;
                else
                    wasDisconnected = true;
            }

            if (pose.isTracked != value.bPoseIsValid)
            {
                pose.isTracked = value.bPoseIsValid;

                if (value.bPoseIsValid)
                    startedTracking = true;
                else
                    lostTracking = true;
            }

            if (pose.isTracked)
            {
                pose.deviceToAbsolute = new MatrixAndInvert(value.mDeviceToAbsoluteTracking.ToMatrix());
                pose.velocity = value.vVelocity.ToVector();
                pose.angularVelocity = value.vAngularVelocity.ToVector();
            }
        }


        protected virtual void OnConnected()
        {
        }
        protected virtual void OnDisconnected()
        {
        }

        protected virtual void OnStartTracking()
        {
        }
        protected virtual void OnLostTracking()
        {
        }
    }
}