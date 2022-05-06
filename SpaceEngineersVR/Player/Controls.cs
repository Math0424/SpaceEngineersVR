using SpaceEngineersVR.Player.Controller;
using SpaceEngineersVR.Plugin;
using System.Diagnostics.CodeAnalysis;
using Valve.VR;

// See:
// https://github.com/ValveSoftware/openvr/wiki/SteamVR-Input
// https://github.com/ValveSoftware/openvr/wiki/Action-manifest

namespace SpaceEngineersVR.Player
{

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class Controls
    {
        public static Controls Static = new Controls();

        // Walking
        public readonly Analog WalkLongitudinal;
        public readonly Analog WalkLatitudinal;

        public readonly Analog WalkRotate;

        public readonly Button JumpOrClimbUp;
        public readonly Button CrouchOrClimbDown;

        // Flying
        public readonly Analog ThrustLRUD;
        public readonly Analog ThrustLRFB;
        public readonly Analog ThrustUp;
        public readonly Analog ThrustDown;
        public readonly Analog ThrustForward;
        public readonly Analog ThrustBackward;
        public readonly Analog ThrustRotate;
        public readonly Button ThrustRoll;
        public readonly Button Dampener;

        // Tool
        public readonly Button Primary;
        public readonly Button Secondary;
        public readonly Button Reload;
        public readonly Button Unequip;
        public readonly Button CutGrid;
        public readonly Button CopyGrid;
        public readonly Button PasteGrid;

        // System
        public readonly Button Interact;
        public readonly Button Helmet;
        public readonly Button Jetpack;
        public readonly Button Broadcasting;
        public readonly Button Park;
        public readonly Button Power;
        public readonly Button Lights;
        public readonly Button Respawn;
        public readonly Button ToggleSignals;

        // Placement
        public readonly Button ToggleSymmetry;
        public readonly Button SymmetrySetup;
        public readonly Button PlacementMode;
        public readonly Button CubeSize;

        // Wrist tablet
        public readonly Button Terminal;
        public readonly Button Inventory;
        public readonly Button ColorSelector;
        public readonly Button ColorPicker;
        public readonly Button BuildPlanner;
        public readonly Button ToolbarConfig;
        public readonly Button BlockSelector;
        public readonly Button Contract;
        public readonly Button Chat;

        // Game
        public readonly Button ToggleView;
        public readonly Button Pause;
        public readonly Button VoiceChat;
        public readonly Button SignalMode;
        public readonly Button SpectatorMode;
        public readonly Button Teleport;

        // Hands
        public readonly Pose LeftHand;
        public readonly Pose RightHand;

        // Feedback
        public readonly Haptic LeftHaptic;
        public readonly Haptic RightHaptic;

        // Action sets
        private readonly ActionSets WalkingSets;
        private readonly ActionSets FlyingSets;

        public Controls()
        {
            OpenVR.Input.SetActionManifestPath(Common.ActionJsonPath);

            //worry about changing all the paths once we finalize this.
            //its far too early to be having all of this 'setup'

            //Walk = new Analog("/actions/walking/in/Walk");
            //WalkForward = new Analog("/actions/walking/in/WalkForward");
            //WalkBackward = new Analog("/actions/walking/in/WalkBackward");
            WalkRotate = new Analog("/actions/walking/in/WalkRotate");
            JumpOrClimbUp = new Button("/actions/walking/in/JumpOrClimbUp");
            CrouchOrClimbDown = new Button("/actions/walking/in/CrouchOrClimbDown");
            ThrustLRUD = new Analog("/actions/flying/in/ThrustLRUD");
            ThrustLRFB = new Analog("/actions/flying/in/ThrustLRFB");
            ThrustUp = new Analog("/actions/flying/in/ThrustUp");
            ThrustDown = new Analog("/actions/flying/in/ThrustDown");
            ThrustForward = new Analog("/actions/flying/in/ThrustForward");
            ThrustBackward = new Analog("/actions/flying/in/ThrustBackward");
            ThrustRotate = new Analog("/actions/flying/in/ThrustRotate");
            ThrustRoll = new Button("/actions/flying/in/ThrustRoll");
            Dampener = new Button("/actions/flying/in/Dampener");
            Primary = new Button("/actions/common/in/Primary");
            Secondary = new Button("/actions/common/in/Secondary");
            Reload = new Button("/actions/common/in/Reload");
            Unequip = new Button("/actions/common/in/Unequip");
            CutGrid = new Button("/actions/common/in/CutGrid");
            CopyGrid = new Button("/actions/common/in/CopyGrid");
            PasteGrid = new Button("/actions/common/in/PasteGrid");
            Interact = new Button("/actions/common/in/Interact");
            Helmet = new Button("/actions/common/in/Helmet");
            Jetpack = new Button("/actions/common/in/Jetpack");
            Broadcasting = new Button("/actions/common/in/Broadcasting");
            Park = new Button("/actions/common/in/Park");
            Power = new Button("/actions/common/in/Power");
            Lights = new Button("/actions/common/in/Lights");
            Respawn = new Button("/actions/common/in/Respawn");
            ToggleSignals = new Button("/actions/common/in/ToggleSignals");
            ToggleSymmetry = new Button("/actions/common/in/ToggleSymmetry");
            SymmetrySetup = new Button("/actions/common/in/SymmetrySetup");
            PlacementMode = new Button("/actions/common/in/PlacementMode");
            CubeSize = new Button("/actions/common/in/CubeSize");
            Terminal = new Button("/actions/common/in/Terminal");
            Inventory = new Button("/actions/common/in/Inventory");
            ColorSelector = new Button("/actions/common/out/ColorSelector");
            ColorPicker = new Button("/actions/common/out/ColorPicker");
            BuildPlanner = new Button("/actions/common/in/BuildPlanner");
            ToolbarConfig = new Button("/actions/common/in/ToolbarConfig");
            BlockSelector = new Button("/actions/common/in/BlockSelector");
            Contract = new Button("/actions/common/in/Contract");
            Chat = new Button("/actions/common/in/Chat");
            ToggleView = new Button("/actions/common/in/ToggleView");
            Pause = new Button("/actions/common/in/Pause");
            VoiceChat = new Button("/actions/common/in/VoiceChat");
            SignalMode = new Button("/actions/common/in/SignalMode");
            SpectatorMode = new Button("/actions/common/in/SpectatorMode");
            Teleport = new Button("/actions/common/in/Teleport");

            LeftHand = new Pose("/actions/common/in/LeftHand");
            RightHand = new Pose("/actions/common/in/RightHand");

            LeftHaptic = new Haptic("/actions/feedback/out/LeftHaptic");
            RightHaptic = new Haptic("/actions/feedback/out/RightHaptic");

            WalkingSets = new ActionSets("/actions/walking", "/actions/common");
            FlyingSets = new ActionSets("/actions/flying", "/actions/common");
        }

        public void UpdateWalk()
        {
            WalkingSets.Update();

            WalkLongitudinal.Update();
            WalkLatitudinal.Update();
            WalkRotate.Update();
            JumpOrClimbUp.Update();
            CrouchOrClimbDown.Update();

            UpdateCommon();
        }

        public void UpdateFlight()
        {
            FlyingSets.Update();

            ThrustLRUD.Update();
            ThrustLRFB.Update();
            ThrustUp.Update();
            ThrustDown.Update();
            ThrustForward.Update();
            ThrustBackward.Update();
            ThrustRotate.Update();
            ThrustRoll.Update();
            Dampener.Update();

            UpdateCommon();
        }

        private void UpdateCommon()
        {
            // NOTE: I know, it could be a loop.
            // But I don't want the overhead of the virtual function calls
            // and the loop itself for maximum performance. All these calls
            // should be inlined, so the method calls are also saved.
            Primary.Update();
            Secondary.Update();
            Reload.Update();
            Unequip.Update();
            CutGrid.Update();
            CopyGrid.Update();
            PasteGrid.Update();
            Interact.Update();
            Helmet.Update();
            Jetpack.Update();
            Broadcasting.Update();
            Park.Update();
            Power.Update();
            Lights.Update();
            Respawn.Update();
            ToggleSignals.Update();
            ToggleSymmetry.Update();
            SymmetrySetup.Update();
            PlacementMode.Update();
            CubeSize.Update();
            Terminal.Update();
            Inventory.Update();
            ColorSelector.Update();
            ColorPicker.Update();
            BuildPlanner.Update();
            ToolbarConfig.Update();
            BlockSelector.Update();
            Contract.Update();
            Chat.Update();
            ToggleView.Update();
            Pause.Update();
            VoiceChat.Update();
            SignalMode.Update();
            SpectatorMode.Update();
            Teleport.Update();
            LeftHand.Update();
            RightHand.Update();
        }
    }
}