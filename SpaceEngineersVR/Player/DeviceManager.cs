using SpaceEngineersVR.Plugin;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Valve.VR;
using VRage.Collections;
using VRageMath;

namespace SpaceEngineersVR.Player;

public static class DeviceManager
{
    public static readonly Headset Headset = new();
    public static readonly Controller LeftHand = new("/actions/common/in/LeftHand", "/actions/feedback/out/LeftHaptic");
    public static readonly Controller RightHand = new("/actions/common/in/RightHand", "/actions/feedback/out/RightHaptic");
    public static readonly MyConcurrentList<TrackedDevice> AllDevices = new(3);

    private static readonly TrackedDevicePose_t[] RenderPoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
    //Poses one frame in the future, on render thread
    private static readonly TrackedDevicePose_t[] FutureRenderPoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
    private static uint nextDeviceId = 0;

    private static readonly object SyncPosesLock = new();
    private static TrackedDevicePose_t[] SyncPoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];

    private static TrackedDevicePose_t[] SyncPosesToMain = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
    private static readonly TrackedDevicePose_t[] MainPoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];

    static DeviceManager()
    {
        AllDevices.Add(Headset);
        AllDevices.Add(LeftHand);
        AllDevices.Add(RightHand);
    }

    public static void UpdateMain()
    {
        try
        {
            Monitor.Enter(SyncPosesLock);
            TrackedDevicePose_t[] tmp = SyncPosesToMain;
            SyncPosesToMain = SyncPoses;
            SyncPoses = tmp;
        }
        finally
        {
            Monitor.Exit(SyncPosesLock);
        }

        for (int i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++)
        {
            if (SyncPosesToMain[i].bPoseIsValid)
            {
                MainPoses[i] = SyncPosesToMain[i];
                SyncPosesToMain[i].bPoseIsValid = false;
            }
        }

        foreach (TrackedDevice device in AllDevices)
        {
            if (device.deviceId is not OpenVR.k_unTrackedDeviceIndex_Hmd and not OpenVR.k_unTrackedDeviceIndexInvalid)
            {
                device.SetMainPoseData(MainPoses[device.deviceId]);
            }
        }
    }

    public static void UpdateRender()
    {
        OpenVR.Compositor.WaitGetPoses(RenderPoses, FutureRenderPoses);
        Compositor_FrameTiming timings = default;
        OpenVR.Compositor.GetFrameTiming(ref timings, 0);
        if (timings.m_nNumDroppedFrames != 0)
        {
            Logger.Warning("Dropping frames!");
            Logger.IncreaseIndent();
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("FrameInterval: " + timings.m_flClientFrameIntervalMs);
            builder.AppendLine("IdleTime     : " + timings.m_flCompositorIdleCpuMs);
            builder.AppendLine("RenderCPU    : " + timings.m_flCompositorRenderCpuMs);
            builder.AppendLine("RenderGPU    : " + timings.m_flCompositorRenderGpuMs);
            builder.AppendLine("SubmitTime   : " + timings.m_flSubmitFrameMs);
            builder.AppendLine("DroppedFrames: " + timings.m_nNumDroppedFrames);
            Logger.Warning(builder.ToString());
            Logger.Warning("");
        }

        bool lockTaken = false;
        try
        {
            Monitor.TryEnter(SyncPosesLock, ref lockTaken);
            if (lockTaken)
            {
                for (int i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; ++i)
                {
                    SyncPoses[i] = RenderPoses[i];
                }
            }
        }
        finally
        {
            if (lockTaken)
            {
                Monitor.Exit(SyncPosesLock);
            }
        }

        //Controllers literally work out whether they are left or right handed by their relative position to the headset at runtime.
        //I have no idea how OpenVR shipped like this
        //  - makes sense for vive controllers as theyre symmetrical i guess. hopefully this handles asymmetrical devices instantly
        {
            uint rightHandIndex = OpenVR.System.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.RightHand);
            if (rightHandIndex != OpenVR.k_unTrackedDeviceIndexInvalid)
            {
                RightHand.deviceId = rightHandIndex;
            }
        }
        {
            uint leftHandIndex = OpenVR.System.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.RightHand);
            if (leftHandIndex != OpenVR.k_unTrackedDeviceIndexInvalid)
            {
                LeftHand.deviceId = leftHandIndex;
            }
        }

        for (; nextDeviceId < OpenVR.k_unMaxTrackedDeviceCount; nextDeviceId++)
        {
            ETrackedDeviceClass deviceClass = OpenVR.System.GetTrackedDeviceClass(nextDeviceId);

            if (deviceClass == ETrackedDeviceClass.Invalid)
            {
                break;
            }

            if (deviceClass == ETrackedDeviceClass.GenericTracker)
            {
                TrackedDevice device = new TrackedDevice
                {
                    deviceId = nextDeviceId
                };
                AllDevices.Add(device);
            }
        }

        foreach (TrackedDevice device in AllDevices)
        {
            if (device.deviceId is not OpenVR.k_unTrackedDeviceIndexInvalid)
            {
                device.SetRenderPoseData(RenderPoses[device.deviceId]);
            }
        }
    }

    private static void OnNewDeviceConnected()
    {

    }
}
