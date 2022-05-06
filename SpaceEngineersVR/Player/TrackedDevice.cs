using SpaceEngineersVR.Util;
using System.Threading;
using Valve.VR;
using VRage;
using VRageMath;

namespace SpaceEngineersVR.Player;

public class TrackedDevice
{
    public Matrix worldTransform => mainPose.transformCalibrated;
    public Vector3 velocity => mainPose.velocity;
    public Vector3 angularVelocity => mainPose.angularVelocity;

    public bool isConnected => mainPose.isConnected;
    public bool isTracked => mainPose.isTracked;


    public struct Pose
    {
        public bool isConnected = false;
        public bool isTracked = false;

        public Matrix transformAbsolute = Matrix.Identity;
        public Matrix transformCalibrated = Matrix.Identity;

        public Vector3 velocity;
        public Vector3 angularVelocity;
    }
    public Pose renderPose;
    public Pose mainPose;

    private readonly FastResourceLock calibrationLock = new FastResourceLock();
    private Vector3 calibration = Vector3.Zero;

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
        OpenVR.Input.TriggerHapticVibrationAction(hapticsActionHandle, delay, duration, frequency, amplitude, OpenVR.k_ulInvalidInputValueHandle);
    }


    public virtual void Update()
    {
    }

    public void SetMainPoseData(TrackedDevicePose_t pose)
    {
        mainPose.isConnected = pose.bDeviceIsConnected;
        mainPose.isTracked = pose.bPoseIsValid;

        if (mainPose.isTracked)
        {
            mainPose.transformAbsolute = pose.mDeviceToAbsoluteTracking.ToMatrix();
            mainPose.transformCalibrated = mainPose.transformAbsolute;
            using (calibrationLock.AcquireSharedUsing())
            {
                //TODO: Figure out how to use the whole matrix for calibration
                mainPose.transformCalibrated.Translation += calibration;
            }
            mainPose.velocity = pose.vVelocity.ToVector();
            mainPose.angularVelocity = pose.vAngularVelocity.ToVector();
        }
    }

    public void Calibrate(Matrix absoluteTransform)
    {
        SetCalibration(-absoluteTransform.Translation);
    }
    public void CalibrateIgnorePitchRoll(Matrix absoluteTransform)
    {
        /*
        TODO: Use this when adding full matrix calibration instead of just a vector
        Vector3 forward = Vector3.Normalize(Vector3.Cross(Vector3.Up, absoluteTransform.Right));
        Vector3 right = Vector3.Cross(forward, Vector3.Up);

        absoluteTransform.Up = Vector3.Up;
        absoluteTransform.Forward = forward;
        absoluteTransform.Right = right;

        SetCalibration(Matrix.Invert(absoluteTransform));
        */
        SetCalibration(-absoluteTransform.Translation);
    }
    public void CalibrateIgnoreRotation(Matrix absoluteTransform)
    {
        /*
        TODO: Use this when adding full matrix calibration instead of just a vector
        SetCalibration(Matrix.CreateTranslation(-absoluteTransform.Translation));
        */
        SetCalibration(-absoluteTransform.Translation);
    }

    public void SetCalibration(Vector3 calibration)
    {
        using (calibrationLock.AcquireExclusiveUsing())
        {
            this.calibration = calibration;
        }
    }
    public void SetRenderPoseData(TrackedDevicePose_t pose)
    {
        if (renderPose.isConnected != pose.bDeviceIsConnected)
        {
            renderPose.isConnected = pose.bDeviceIsConnected;

            if (pose.bDeviceIsConnected)
                OnConnected();
            else
                OnDisconnected();
        }

        if (renderPose.isTracked != pose.bPoseIsValid)
        {
            renderPose.isTracked = pose.bPoseIsValid;

            if (pose.bPoseIsValid)
                OnStartTracking();
            else
                OnLostTracking();
        }

        if (renderPose.isTracked)
        {
            renderPose.transformAbsolute = pose.mDeviceToAbsoluteTracking.ToMatrix();
            renderPose.transformCalibrated = renderPose.transformAbsolute;
            using (calibrationLock.AcquireSharedUsing())
            {
                //TODO: Figure out how to use the whole matrix for calibration
                renderPose.transformCalibrated.Translation += calibration;
            }
            renderPose.velocity = pose.vVelocity.ToVector();
            renderPose.angularVelocity = pose.vAngularVelocity.ToVector();
        }
    }


    //Warning: Called on render thread
    protected virtual void OnConnected()
    {
    }
    //Warning: Called on render thread
    protected virtual void OnDisconnected()
    {
    }

    //Warning: Called on render thread
    protected virtual void OnStartTracking()
    {
    }
    //Warning: Called on render thread
    protected virtual void OnLostTracking()
    {
    }
}