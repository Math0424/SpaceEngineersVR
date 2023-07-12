﻿using SpaceEngineersVR.Plugin;
using SpaceEngineersVR.Util;
using System.Text;
using System.Threading;
using Valve.VR;
using VRage;
using VRage.Collections;
using VRage.Input;
using VRageMath;

namespace SpaceEngineersVR.Player
{
    public static class Player
    {
        private const int CalibrationTimeTicks = 60 * 5;


        public static readonly Headset Headset = new Headset();
        public static readonly Controller HandL = new Controller("/actions/common/in/LeftHand", "/actions/feedback/out/LeftHaptic");
        public static readonly Controller HandR = new Controller("/actions/common/in/RightHand", "/actions/feedback/out/RightHaptic");
        public static readonly MyConcurrentList<TrackedDevice> AllDevices = new MyConcurrentList<TrackedDevice>(3);

        public static BodyCalibration GetBodyCalibration()
        {
            using (PlayerCalibrationLock.AcquireSharedUsing())
            {
                return PlayerCalibration;
            }
        }

        public static bool IsCalibrating => CalibratingTicksLeft > 0;

        private static readonly FastResourceLock PlayerCalibrationLock = new FastResourceLock();
        private static BodyCalibration PlayerCalibration;

        private static int CalibratingTicksLeft = 0;
        private static BodyCalibration CalibrationInProgress;


        public static MatrixAndInvert PlayerToAbsolute { get; private set; } = MatrixAndInvert.Identity;

        //PlayerToAbsolute that is synced for render thread
        public static MatrixAndInvert RenderPlayerToAbsolute = MatrixAndInvert.Identity;

        private static readonly FastResourceLock SyncPlayerToAbsoluteLock = new FastResourceLock();
        private static MatrixAndInvert SyncPlayerToAbsolute = MatrixAndInvert.Identity;


        private static uint NextDeviceId = 0;

        private static readonly TrackedDevicePose_t[] RenderPoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
        private static readonly TrackedDevicePose_t[] RenderPosesFuture = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount]; //Poses one frame in the future

        private static readonly object SyncPosesLock = new object();
        private static TrackedDevicePose_t[] SyncPoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];

        private static TrackedDevicePose_t[] Poses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];

        static Player()
        {
            AllDevices.Add(Headset);
            AllDevices.Add(HandL);
            AllDevices.Add(HandR);

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
                    HandR.deviceId = rightHandIndex;
                }
            }
            {
                uint leftHandIndex = OpenVR.System.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.LeftHand);
                if (leftHandIndex != OpenVR.k_unTrackedDeviceIndexInvalid)
                {
                    HandL.deviceId = leftHandIndex;
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
                if (!(device.deviceId is OpenVR.k_unTrackedDeviceIndexInvalid))
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
                TrackedDevicePose_t[] tmp = Poses;
                Poses = SyncPoses;
                SyncPoses = tmp;
            }
            finally
            {
                Monitor.Exit(SyncPosesLock);
            }

            foreach (TrackedDevice device in AllDevices)
            {
                if (!(device.deviceId is OpenVR.k_unTrackedDeviceIndexInvalid))
                {
                    device.SetMainPoseData(Poses[device.deviceId]);
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
            if (Headset.pose.isTracked)
            {
                Vector3 headPos = Headset.pose.deviceToAbsolute.matrix.Translation;
                float height = headPos.Y;

                if (CalibrationInProgress.height < height)
                    CalibrationInProgress.height = height;
            }

            if (HandL.pose.isTracked && HandR.pose.isTracked)
            {
                Vector3 lPos = HandL.pose.deviceToAbsolute.matrix.Translation;
                Vector3 rPos = HandR.pose.deviceToAbsolute.matrix.Translation;
                float armSpan = Vector2.Distance(new Vector2(lPos.X, lPos.Z), new Vector2(rPos.X, rPos.Z));

                if (CalibrationInProgress.armSpan < armSpan)
                    CalibrationInProgress.armSpan = armSpan;
            }
        }

        public static void FinishCalibration()
        {
            using (PlayerCalibrationLock.AcquireExclusiveUsing())
            {
                if (CalibrationInProgress.height > 0f)
                    PlayerCalibration.height = CalibrationInProgress.height;
                if (CalibrationInProgress.armSpan > 0f)
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

            PlayerToAbsolute = new MatrixAndInvert(floor);

            using (SyncPlayerToAbsoluteLock.AcquireExclusiveUsing())
            {
                SyncPlayerToAbsolute = PlayerToAbsolute;
            }
        }

        public static void ResetPlayerFloor()
        {
            Matrix floor;
            if (Common.Config.UseHeadRotationForCharacter)
                floor = Util.Util.ZeroPitchAndRoll(Headset.pose.deviceToAbsolute.matrix);
            else
                floor = Matrix.CreateTranslation(Headset.pose.deviceToAbsolute.matrix.Translation);

            floor.M42 = 0f; //Translation.Y = 0f

            PlayerToAbsolute = new MatrixAndInvert(floor);

            using (SyncPlayerToAbsoluteLock.AcquireExclusiveUsing())
            {
                SyncPlayerToAbsolute = PlayerToAbsolute;
            }
        }

        private static void OnNewDeviceConnected()
        {

        }
    }
}