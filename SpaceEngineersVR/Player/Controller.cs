using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Valve.VR;
using VRageMath;

namespace SpaceEngineersVR.Player
{
    [Flags]
    public enum Button : ulong
    {
        System = 1ul << EVRButtonId.k_EButton_System,
        ApplicationMenu = 1ul << EVRButtonId.k_EButton_ApplicationMenu,
        Grip = 1ul << EVRButtonId.k_EButton_Grip,
        Touchpad = 1ul << EVRButtonId.k_EButton_SteamVR_Touchpad,
        Trigger = 1ul << EVRButtonId.k_EButton_SteamVR_Trigger,

        A = 1ul << EVRButtonId.k_EButton_A,
        B = 1ul << EVRButtonId.k_EButton_ApplicationMenu,

        DPadDown = 1ul << EVRButtonId.k_EButton_DPad_Down,
    }

    public enum Axis
    {
        Joystick = 0,
        Trigger = 1,
        Grip = 2,
        Axis3 = 3,
        Axis4 = 4,
    }

    struct Controller
    {
        private Queue<Vector3> TenFrameSpeed;

        public MatrixD WorldPos;
        
        public Vector3 TenFramVel;
        public Vector3 CurrentVel;
        public Vector3 AngularVel;

        public VRControllerState_t PrevState;
        public VRControllerState_t CurrentState;
        public uint ControllerID;

        public bool IsValid;
        public bool IsConnected;

        private float hairTriggerLimit;
        private bool hairTriggerState, hairTriggerPrevState;

        public bool IsButtonDown(Button btn) => (CurrentState.ulButtonPressed & (ulong)btn) != 0;
        public bool IsNewButtonDown(Button btn) => (CurrentState.ulButtonPressed & (ulong)btn) != 0 && (PrevState.ulButtonPressed & (ulong)btn) == 0;
        public bool IsNewButtonUp(Button btn) => (CurrentState.ulButtonPressed & (ulong)btn) == 0 && (PrevState.ulButtonPressed & (ulong)btn) != 0;

        public bool IsTouchDown(Button btn) => (CurrentState.ulButtonTouched & (ulong)btn) != 0;
        public bool IsNewTouchDown(Button btn) => (CurrentState.ulButtonTouched & (ulong)btn) != 0 && (PrevState.ulButtonTouched & (ulong)btn) == 0;
        public bool IsNewTouchUp(Button btn) => (CurrentState.ulButtonTouched & (ulong)btn) == 0 && (PrevState.ulButtonTouched & (ulong)btn) != 0;

        public ulong GetButtonPushed => CurrentState.ulButtonPressed;


        public bool IsHairTrigger() => hairTriggerState;
        public bool IsNewHairTriggerDown() => hairTriggerState && !hairTriggerPrevState;
        public bool IsNewHairTriggerUp() => !hairTriggerState && hairTriggerPrevState;

        public Vector2 GetAxis(Axis axis = Axis.Joystick)
        {
            switch (axis)
            {
                case Axis.Joystick: return new Vector2(CurrentState.rAxis0.x, CurrentState.rAxis0.y);
                case Axis.Trigger: return new Vector2(CurrentState.rAxis1.x, CurrentState.rAxis1.y);
                case Axis.Grip: return new Vector2(CurrentState.rAxis2.x, CurrentState.rAxis2.y);
                case Axis.Axis3: return new Vector2(CurrentState.rAxis3.x, CurrentState.rAxis3.y);
                case Axis.Axis4: return new Vector2(CurrentState.rAxis4.x, CurrentState.rAxis4.y);
            }
            return Vector2.Zero;
        }

        public void TriggerHapticPulse(ushort duration = 500, Button button = Button.Touchpad)
        {
            OpenVR.System.TriggerHapticPulse(ControllerID, (uint)button, (char)duration);
        }

        public void UpdateControllerPosition(TrackedDevicePose_t pos)
        {
            PrevState = CurrentState;

            IsConnected = pos.bDeviceIsConnected;
            IsValid = OpenVR.System.GetControllerStateWithPose(ETrackingUniverseOrigin.TrackingUniverseStanding, ControllerID, ref CurrentState, ref pos);

            WorldPos = pos.mDeviceToAbsoluteTracking.ToMatrix();

            AngularVel = pos.vAngularVelocity.ToVector();
            CurrentVel = pos.vVelocity.ToVector();

            if (TenFrameSpeed == null)
                TenFrameSpeed = new Queue<Vector3>();
            TenFrameSpeed.Enqueue(CurrentVel);
            if (TenFrameSpeed.Count > 10)
                TenFrameSpeed.Dequeue();
            foreach (var q in TenFrameSpeed)
                TenFramVel += q;
            TenFramVel /= 10;


            hairTriggerPrevState = hairTriggerState;
            var value = CurrentState.rAxis1.x;
            if (hairTriggerState)
            {
                if (value < hairTriggerLimit - 0.1 || value <= 0.0f)
                    hairTriggerState = false;
            }
            else
            {
                if (value > hairTriggerLimit + 0.1 || value >= 1.0f)
                    hairTriggerState = true;
            }
            hairTriggerLimit = hairTriggerState ? Math.Max(hairTriggerLimit, value) : Math.Min(hairTriggerLimit, value);
        }

    }
}
