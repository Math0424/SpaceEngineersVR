using Valve.VR;

namespace SpaceEngineersVR.Player
{
    public class Actions
    {
        #region Action handles

        // Movement
        private ulong ThrustForwardBackwardHandle;
        private ulong ThrustLeftRightHandle;
        private ulong ThrustUpDownHandle;
        private ulong ThrustDirectionHandle;
        private ulong RotateUpDownHandle;
        private ulong RotateLeftRightHandle;
        private ulong RollLeftRightHandle;
        private ulong JumpHandle;
        private ulong CrouchHandle;

        // Tool
        private ulong ToolHandHandle;
        private ulong PrimaryHandle;
        private ulong SecondaryHandle;
        private ulong ReloadHandle;
        private ulong UnequipHandle;
        private ulong PreviousHandle;
        private ulong NextHandle;

        // System
        private ulong InteractHandle;
        private ulong HelmetHandle;
        private ulong JetpackHandle;
        private ulong DampenersHandle;
        private ulong BroadcastingHandle;
        private ulong ParkHandle;
        private ulong PowerHandle;
        private ulong LightsHandle;
        private ulong RespawnHandle;
        private ulong VoxelHandsHandle;

        // Placement
        private ulong PositionHandle;
        private ulong ToggleSymmetryHandle;
        private ulong SymmetrySetupHandle;
        private ulong PlacementModeHandle;
        private ulong CubeSizeHandle;

        // Wrist tablet
        private ulong TabletHandHandle;
        private ulong TerminalHandle;
        private ulong InventoryHandle;
        private ulong ColorSelectorHandle;
        private ulong ColorPickerHandle;
        private ulong BuildPlannerHandle;
        private ulong ToolbarConfigHandle;
        private ulong BlockSelectorHandle;
        private ulong ContractHandle;
        private ulong ChatHandle;

        // Game
        private ulong ToggleViewHandle;
        private ulong PauseHandle;
        private ulong VoiceChatHandle;
        private ulong SignalModeHandle;
        private ulong SpectatorModeHandle;
        private ulong TeleportHandle;

        // Skeleton
        private ulong LeftHandSkeletonHandle;
        private ulong RightHandSkeletonHandle;

        // Feedback
        private ulong WeldingHandle;
        private ulong DrillingHandle;
        private ulong GrindingHandle;
        private ulong ShootingHandle;
        private ulong PlacingHandle;
        private ulong RemovingHandle;
        private ulong PlacementFitHandle;

        #endregion

        #region Action values

        private static readonly unsafe uint InputAnalogActionData_t_size = (uint)sizeof(InputAnalogActionData_t);
        private static readonly unsafe uint InputDigitalActionData_t_size = (uint)sizeof(InputDigitalActionData_t);

        // Movement
        public InputAnalogActionData_t ThrustForwardBackward;
        public ulong ThrustLeftRight;
        public ulong ThrustUpDown;
        public ulong ThrustDirection;
        public ulong RotateUpDown;
        public ulong RotateLeftRight;
        public ulong RollLeftRight;
        public ulong Jump;
        public ulong Crouch;

        // Tool
        public ulong ToolHand;
        public ulong Primary;
        public ulong Secondary;
        public ulong Reload;
        public ulong Unequip;
        public ulong Previous;
        public ulong Next;

        // System
        public ulong Interact;
        public ulong Helmet;
        public InputDigitalActionData_t Jetpack;
        public ulong Dampeners;
        public ulong Broadcasting;
        public ulong Park;
        public ulong Power;
        public ulong Lights;
        public ulong Respawn;
        public ulong VoxelHands;

        // Placement
        public ulong Position;
        public ulong ToggleSymmetry;
        public ulong SymmetrySetup;
        public ulong PlacementMode;
        public ulong CubeSize;

        // Wrist tablet
        public ulong TabletHand;
        public ulong Terminal;
        public ulong Inventory;
        public ulong ColorSelector;
        public ulong ColorPicker;
        public ulong BuildPlanner;
        public ulong ToolbarConfig;
        public ulong BlockSelector;
        public ulong Contract;
        public ulong Chat;

        // Game
        public ulong ToggleView;
        public ulong Pause;
        public ulong VoiceChat;
        public ulong SignalMode;
        public ulong SpectatorMode;
        public ulong Teleport;

        // Skeleton
        public ulong LeftHandSkeleton;
        public ulong RightHandSkeleton;

        // Feedback
        public ulong Welding;
        public ulong Drilling;
        public ulong Grinding;
        public ulong Shooting;
        public ulong Placing;
        public ulong Removing;
        public ulong PlacementFit;

        #endregion

        public Actions()
        {
            var input = OpenVR.Input;

            // Movement
            input.GetActionHandle("/actions/movement/in/ThrustForwardBackward", ref ThrustForwardBackwardHandle);
            input.GetActionHandle("/actions/movement/in/ThrustLeftRight", ref ThrustLeftRightHandle);
            input.GetActionHandle("/actions/movement/in/ThrustUpDown", ref ThrustUpDownHandle);
            input.GetActionHandle("/actions/movement/in/ThrustDirection", ref ThrustDirectionHandle);
            input.GetActionHandle("/actions/movement/in/RotateUpDown", ref RotateUpDownHandle);
            input.GetActionHandle("/actions/movement/in/RotateLeftRight", ref RotateLeftRightHandle);
            input.GetActionHandle("/actions/movement/in/RollLeftRight", ref RollLeftRightHandle);
            input.GetActionHandle("/actions/movement/in/Jump", ref JumpHandle);
            input.GetActionHandle("/actions/movement/in/Crouch", ref CrouchHandle);

            // Tool
            input.GetActionHandle("/actions/tool/in/ToolHand", ref ToolHandHandle);
            input.GetActionHandle("/actions/tool/in/Primary", ref PrimaryHandle);
            input.GetActionHandle("/actions/tool/in/Secondary", ref SecondaryHandle);
            input.GetActionHandle("/actions/tool/in/Reload", ref ReloadHandle);
            input.GetActionHandle("/actions/tool/in/Unequip", ref UnequipHandle);
            input.GetActionHandle("/actions/tool/in/Previous", ref PreviousHandle);
            input.GetActionHandle("/actions/tool/in/Next", ref NextHandle);

            // System
            input.GetActionHandle("/actions/system/in/Interact", ref InteractHandle);
            input.GetActionHandle("/actions/system/in/Helmet", ref HelmetHandle);
            input.GetActionHandle("/actions/system/in/Jetpack", ref JetpackHandle);
            input.GetActionHandle("/actions/system/in/Dampeners", ref DampenersHandle);
            input.GetActionHandle("/actions/system/in/Broadcasting", ref BroadcastingHandle);
            input.GetActionHandle("/actions/system/in/Park", ref ParkHandle);
            input.GetActionHandle("/actions/system/in/Power", ref PowerHandle);
            input.GetActionHandle("/actions/system/in/Lights", ref LightsHandle);
            input.GetActionHandle("/actions/system/in/Respawn", ref RespawnHandle);
            input.GetActionHandle("/actions/system/in/VoxelHands", ref VoxelHandsHandle);

            // Placement
            input.GetActionHandle("/actions/placement/in/Position", ref PositionHandle);
            input.GetActionHandle("/actions/placement/in/ToggleSymmetry", ref ToggleSymmetryHandle);
            input.GetActionHandle("/actions/placement/in/SymmetrySetup", ref SymmetrySetupHandle);
            input.GetActionHandle("/actions/placement/in/PlacementMode", ref PlacementModeHandle);
            input.GetActionHandle("/actions/placement/in/CubeSize", ref CubeSizeHandle);

            // Wrist tablet
            input.GetActionHandle("/actions/tablet/out/TabletHand", ref TabletHandHandle);
            input.GetActionHandle("/actions/tablet/in/Terminal", ref TerminalHandle);
            input.GetActionHandle("/actions/tablet/in/Inventory", ref InventoryHandle);
            input.GetActionHandle("/actions/tablet/out/ColorSelector", ref ColorSelectorHandle);
            input.GetActionHandle("/actions/tablet/out/ColorPicker", ref ColorPickerHandle);
            input.GetActionHandle("/actions/tablet/in/BuildPlanner", ref BuildPlannerHandle);
            input.GetActionHandle("/actions/tablet/in/ToolbarConfig", ref ToolbarConfigHandle);
            input.GetActionHandle("/actions/tablet/in/BlockSelector", ref BlockSelectorHandle);
            input.GetActionHandle("/actions/tablet/in/Contract", ref ContractHandle);
            input.GetActionHandle("/actions/tablet/in/Chat", ref ChatHandle);

            // Game
            input.GetActionHandle("/actions/game/in/ToggleView", ref ToggleViewHandle);
            input.GetActionHandle("/actions/game/in/Pause", ref PauseHandle);
            input.GetActionHandle("/actions/game/in/VoiceChat", ref VoiceChatHandle);
            input.GetActionHandle("/actions/game/in/SignalMode", ref SignalModeHandle);
            input.GetActionHandle("/actions/game/in/SpectatorMode", ref SpectatorModeHandle);
            input.GetActionHandle("/actions/game/in/Teleport", ref TeleportHandle);

            // Skeleton
            input.GetActionHandle("/actions/skeleton/in/LeftHandSkeleton", ref LeftHandSkeletonHandle);
            input.GetActionHandle("/actions/skeleton/in/RightHandSkeleton", ref RightHandSkeletonHandle);

            // Feedback
            input.GetActionHandle("/actions/feedback/out/Welding", ref WeldingHandle);
            input.GetActionHandle("/actions/feedback/out/Drilling", ref DrillingHandle);
            input.GetActionHandle("/actions/feedback/out/Grinding", ref GrindingHandle);
            input.GetActionHandle("/actions/feedback/out/Shooting", ref ShootingHandle);
            input.GetActionHandle("/actions/feedback/out/Placing", ref PlacingHandle);
            input.GetActionHandle("/actions/feedback/out/Removing", ref RemovingHandle);
            input.GetActionHandle("/actions/feedback/out/PlacementFit", ref PlacementFitHandle);
        }

        public void Update()
        {
            var input = OpenVR.Input;

            input.GetAnalogActionData(ThrustForwardBackwardHandle, ref ThrustForwardBackward, InputAnalogActionData_t_size, OpenVR.k_ulInvalidInputValueHandle);

            // Movement
            // input.GetActionHandle("/actions/movement/in/ThrustForwardBackward", ref ThrustForwardBackwardHandle);
            // input.GetActionHandle("/actions/movement/in/ThrustLeftRight", ref ThrustLeftRightHandle);
            // input.GetActionHandle("/actions/movement/in/ThrustUpDown", ref ThrustUpDownHandle);
            // input.GetActionHandle("/actions/movement/in/ThrustDirection", ref ThrustDirectionHandle);
            // input.GetActionHandle("/actions/movement/in/RotateUpDown", ref RotateUpDownHandle);
            // input.GetActionHandle("/actions/movement/in/RotateLeftRight", ref RotateLeftRightHandle);
            // input.GetActionHandle("/actions/movement/in/RollLeftRight", ref RollLeftRightHandle);
            // input.GetActionHandle("/actions/movement/in/Jump", ref JumpHandle);
            // input.GetActionHandle("/actions/movement/in/Crouch", ref CrouchHandle);

            // Tool
            // input.GetActionHandle("/actions/tool/in/ToolHand", ref ToolHandHandle);
            // input.GetActionHandle("/actions/tool/in/Primary", ref PrimaryHandle);
            // input.GetActionHandle("/actions/tool/in/Secondary", ref SecondaryHandle);
            // input.GetActionHandle("/actions/tool/in/Reload", ref ReloadHandle);
            // input.GetActionHandle("/actions/tool/in/Unequip", ref UnequipHandle);
            // input.GetActionHandle("/actions/tool/in/Previous", ref PreviousHandle);
            // input.GetActionHandle("/actions/tool/in/Next", ref NextHandle);

            // System
            // input.GetActionHandle("/actions/system/in/Interact", ref InteractHandle);
            // input.GetActionHandle("/actions/system/in/Helmet", ref HelmetHandle);
            input.GetDigitalActionData(JetpackHandle, ref Jetpack, InputDigitalActionData_t_size, OpenVR.k_ulInvalidInputValueHandle);
            // input.GetActionHandle("/actions/system/in/Dampeners", ref DampenersHandle);
            // input.GetActionHandle("/actions/system/in/Broadcasting", ref BroadcastingHandle);
            // input.GetActionHandle("/actions/system/in/Park", ref ParkHandle);
            // input.GetActionHandle("/actions/system/in/Power", ref PowerHandle);
            // input.GetActionHandle("/actions/system/in/Lights", ref LightsHandle);
            // input.GetActionHandle("/actions/system/in/Respawn", ref RespawnHandle);
            // input.GetActionHandle("/actions/system/in/VoxelHands", ref VoxelHandsHandle);

            return;

            // Placement
            input.GetActionHandle("/actions/placement/in/Position", ref PositionHandle);
            input.GetActionHandle("/actions/placement/in/ToggleSymmetry", ref ToggleSymmetryHandle);
            input.GetActionHandle("/actions/placement/in/SymmetrySetup", ref SymmetrySetupHandle);
            input.GetActionHandle("/actions/placement/in/PlacementMode", ref PlacementModeHandle);
            input.GetActionHandle("/actions/placement/in/CubeSize", ref CubeSizeHandle);

            // Wrist tablet
            input.GetActionHandle("/actions/tablet/out/TabletHand", ref TabletHandHandle);
            input.GetActionHandle("/actions/tablet/in/Terminal", ref TerminalHandle);
            input.GetActionHandle("/actions/tablet/in/Inventory", ref InventoryHandle);
            input.GetActionHandle("/actions/tablet/out/ColorSelector", ref ColorSelectorHandle);
            input.GetActionHandle("/actions/tablet/out/ColorPicker", ref ColorPickerHandle);
            input.GetActionHandle("/actions/tablet/in/BuildPlanner", ref BuildPlannerHandle);
            input.GetActionHandle("/actions/tablet/in/ToolbarConfig", ref ToolbarConfigHandle);
            input.GetActionHandle("/actions/tablet/in/BlockSelector", ref BlockSelectorHandle);
            input.GetActionHandle("/actions/tablet/in/Contract", ref ContractHandle);
            input.GetActionHandle("/actions/tablet/in/Chat", ref ChatHandle);

            // Game
            input.GetActionHandle("/actions/game/in/ToggleView", ref ToggleViewHandle);
            input.GetActionHandle("/actions/game/in/Pause", ref PauseHandle);
            input.GetActionHandle("/actions/game/in/VoiceChat", ref VoiceChatHandle);
            input.GetActionHandle("/actions/game/in/SignalMode", ref SignalModeHandle);
            input.GetActionHandle("/actions/game/in/SpectatorMode", ref SpectatorModeHandle);
            input.GetActionHandle("/actions/game/in/Teleport", ref TeleportHandle);

            // Skeleton
            input.GetActionHandle("/actions/skeleton/in/LeftHandSkeleton", ref LeftHandSkeletonHandle);
            input.GetActionHandle("/actions/skeleton/in/RightHandSkeleton", ref RightHandSkeletonHandle);

            // Feedback
            input.GetActionHandle("/actions/feedback/out/Welding", ref WeldingHandle);
            input.GetActionHandle("/actions/feedback/out/Drilling", ref DrillingHandle);
            input.GetActionHandle("/actions/feedback/out/Grinding", ref GrindingHandle);
            input.GetActionHandle("/actions/feedback/out/Shooting", ref ShootingHandle);
            input.GetActionHandle("/actions/feedback/out/Placing", ref PlacingHandle);
            input.GetActionHandle("/actions/feedback/out/Removing", ref RemovingHandle);
            input.GetActionHandle("/actions/feedback/out/PlacementFit", ref PlacementFitHandle);
        }
    }
}