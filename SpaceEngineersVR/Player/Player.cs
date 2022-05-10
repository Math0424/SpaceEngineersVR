using SpaceEngineersVR.Plugin;
using SpaceEngineersVR.Util;
using System.Text;
using System.Threading;
using Valve.VR;
using VRage;
using VRage.Collections;
using VRage.Input;
using VRageMath;

namespace SpaceEngineersVR.Player;

public static class Player
{
    private const int CalibrationTimeTicks = 60 * 5;


    public static readonly Headset Headset = new();
    public static readonly Controller LeftHand = new("/actions/common/in/LeftHand", "/actions/feedback/out/LeftHaptic");
    public static readonly Controller RightHand = new("/actions/common/in/RightHand", "/actions/feedback/out/RightHaptic");
    public static readonly MyConcurrentList<TrackedDevice> AllDevices = new(3);

    public static BodyCalibration GetBodyCalibration()
    {
        using (PlayerCalibrationLock.AcquireSharedUsing())
        {
            return PlayerCalibration;
        }
    }

    public static bool IsCalibrating => CalibratingTicksLeft > 0;

    private static readonly FastResourceLock PlayerCalibrationLock = new();
    private static BodyCalibration PlayerCalibration;

    private static int CalibratingTicksLeft = 0;
    private static BodyCalibration CalibrationInProgress;


    public static MatrixAndInvert PlayerToAbsolute { get; private set; } = MatrixAndInvert.Identity;

    //Gets PlayerToAbsolute that is synced for render thread
    public static MatrixAndInvert RenderPlayerToAbsolute = MatrixAndInvert.Identity;

    private static readonly FastResourceLock SyncPlayerToAbsoluteLock = new();
    private static MatrixAndInvert SyncPlayerToAbsolute = MatrixAndInvert.Identity;


    private static uint NextDeviceId = 0;

    private static readonly TrackedDevicePose_t[] RenderPoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
    private static readonly TrackedDevicePose_t[] RenderPosesFuture = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount]; //Poses one frame in the future

    private static readonly object SyncPosesLock = new();
    private static TrackedDevicePose_t[] SyncPoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];

    private static TrackedDevicePose_t[] MainPoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];

    static Player()
    {
        AllDevices.Add(Headset);
        AllDevices.Add(LeftHand);
        AllDevices.Add(RightHand);

        using (PlayerCalibrationLock.AcquireExclusiveUsing())
        {
            PlayerCalibration.height = Common.Config.PlayerHeight;
            PlayerCalibration.armSpan = Common.Config.PlayerArmSpan;
        }
    }

    public static void RenderUpdate()
    {
        //Check for new devices
        for (; NextDeviceId < OpenVR.k_unMaxTrackedDeviceCount; NextDeviceId++)
        {
            //In OpenVR, once a device is connected once, its ID is unique, even if disconnected
            ETrackedDeviceClass deviceClass = OpenVR.System.GetTrackedDeviceClass(NextDeviceId);

            if (deviceClass == ETrackedDeviceClass.Invalid)
            {
                break;
            }

            if (deviceClass == ETrackedDeviceClass.GenericTracker)
            {
                TrackedDevice device = new TrackedDevice
                {
                    deviceId = NextDeviceId
                };
                AllDevices.Add(device);
            }
        }

        OpenVR.Compositor.WaitGetPoses(RenderPoses, RenderPosesFuture);

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

        {
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
        }

        //Vive controllers work out whether they are left or right handed by their relative position to the headset. they can even change at runtime
        {
            uint rightHandIndex = OpenVR.System.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.RightHand);
            if (rightHandIndex != OpenVR.k_unTrackedDeviceIndexInvalid)
            {
                RightHand.deviceId = rightHandIndex;
            }
        }
        {
            uint leftHandIndex = OpenVR.System.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.LeftHand);
            if (leftHandIndex != OpenVR.k_unTrackedDeviceIndexInvalid)
            {
                LeftHand.deviceId = leftHandIndex;
            }
        }

        {
            bool lockTaken = false;
            try
            {
                //Could cause weird sync issues where some objects render in the wrong place for a frame or two, but better than reducing frame rate
                //maybe we should have a list of objects that we override the rendering for to be relative to a certain device at render-time?
                lockTaken = SyncPlayerToAbsoluteLock.TryAcquireShared();
                if (lockTaken)
                    RenderPlayerToAbsolute = SyncPlayerToAbsolute;
            }
            finally
            {
                if (lockTaken)
                    SyncPlayerToAbsoluteLock.ReleaseShared();
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

    public static void MainUpdate()
    {
        try
        {
            Monitor.Enter(SyncPosesLock);
            TrackedDevicePose_t[] tmp = MainPoses;
            MainPoses = SyncPoses;
            SyncPoses = tmp;
        }
        finally
        {
            Monitor.Exit(SyncPosesLock);
        }

        foreach (TrackedDevice device in AllDevices)
        {
            if (device.deviceId is not OpenVR.k_unTrackedDeviceIndexInvalid)
            {
                device.SetMainPoseData(MainPoses[device.deviceId]);
            }
        }

        if (MyInput.Static.IsKeyPress(MyKeys.NumPad0))
            StartCalibration();

        if (CalibratingTicksLeft > 0)
        {
            CalibrationUpdate();

            CalibratingTicksLeft--;

            if (CalibratingTicksLeft <= 0)
            {
                FinishCalibration();
            }
        }

        foreach (TrackedDevice device in AllDevices)
        {
            device.MainUpdate();
        }
    }

    public static void StartCalibration(int timeTicks = CalibrationTimeTicks)
    {
        CalibratingTicksLeft = timeTicks;

        CalibrationInProgress.height = 0f;
        CalibrationInProgress.armSpan = 0f;
    }

    private static void CalibrationUpdate()
    {
        if (Headset.pose_Main.isTracked)
        {
            Vector3 headPos = Headset.pose_Main.deviceToAbsolute.matrix.Translation;
            float height = headPos.Y;

            if (CalibrationInProgress.height < height)
                CalibrationInProgress.height = height;
        }

        if (LeftHand.pose_Main.isTracked && RightHand.pose_Main.isTracked)
        {
            Vector3 lPos = LeftHand.pose_Main.deviceToAbsolute.matrix.Translation;
            Vector3 rPos = RightHand.pose_Main.deviceToAbsolute.matrix.Translation;
            float armSpan = Vector2.Distance(new(lPos.X, lPos.Z), new(rPos.X, rPos.Z));

            if (CalibrationInProgress.armSpan < armSpan)
                CalibrationInProgress.armSpan = armSpan;
        }
    }

    public static void FinishCalibration()
    {
        using (PlayerCalibrationLock.AcquireExclusiveUsing())
        {
            if(CalibrationInProgress.height > 0f)
                PlayerCalibration.height = CalibrationInProgress.height;
            if(CalibrationInProgress.armSpan > 0f)
                PlayerCalibration.armSpan = CalibrationInProgress.armSpan;
        }

        if (CalibrationInProgress.height > 0f)
            Common.Config.PlayerHeight = CalibrationInProgress.height;
        if (CalibrationInProgress.armSpan > 0f)
            Common.Config.PlayerArmSpan = CalibrationInProgress.armSpan;

        ResetPlayerFloor();

        CalibratingTicksLeft = 0;
    }
    public static void CancelCalibration()
    {
        CalibratingTicksLeft = 0;
    }

    //TODO: Call this from character movement due to player movement
    public static void MovePlayerFloor(Vector3 movement, float rotation)
    {
        Matrix floor = PlayerToAbsolute.matrix;
        floor.Translation += movement;
        floor *= Matrix.CreateRotationY(rotation);

        PlayerToAbsolute = new(floor);

        using (SyncPlayerToAbsoluteLock.AcquireExclusiveUsing())
        {
            SyncPlayerToAbsolute = PlayerToAbsolute;
        }
    }

    public static void ResetPlayerFloor()
    {
        Matrix floor;
        if (Common.Config.UseHeadRotationForCharacter)
            floor = Util.Util.ZeroPitchAndRoll(Headset.pose_Main.deviceToAbsolute.matrix);
        else
            floor = Matrix.CreateTranslation(Headset.pose_Main.deviceToAbsolute.matrix.Translation);

        floor.M42 = 0f; //Translation.Y = 0f

        PlayerToAbsolute = new(floor);

        using (SyncPlayerToAbsoluteLock.AcquireExclusiveUsing())
        {
            SyncPlayerToAbsolute = PlayerToAbsolute;
        }
    }

    private static void OnNewDeviceConnected()
    {

    }
}
