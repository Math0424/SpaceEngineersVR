namespace SpaceEngineersVR.Player
{
    public class Actions
    {
        // Movement
        public readonly Joystick Move;
        public readonly Joystick Rotate;
        public readonly Joystick Thrust;
        public readonly Joystick Forward;
        public readonly Joystick Backward;
        public readonly Button Vertical;
        public readonly Button Roll;
        public readonly Button Jump;
        public readonly Button Crouch;

        // Tool
        public readonly Pose ToolHand;
        public readonly Button Primary;
        public readonly Button Secondary;
        public readonly Button Reload;
        public readonly Button Unequip;
        public readonly Button Previous;
        public readonly Button Next;

        // System
        public readonly Button Interact;
        public readonly Button Helmet;
        public readonly Button Jetpack;
        public readonly Button Dampeners;
        public readonly Button Broadcasting;
        public readonly Button Park;
        public readonly Button Power;
        public readonly Button Lights;
        public readonly Button Respawn;
        public readonly Button VoxelHands;

        // Placement
        public readonly Button ToggleSymmetry;
        public readonly Button SymmetrySetup;
        public readonly Button PlacementMode;
        public readonly Button CubeSize;

        // Wrist tablet
        public readonly Pose TabletHand;
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

        // Skeleton
        public readonly Button LeftHandSkeleton;
        public readonly Button RightHandSkeleton;

        // Feedback
        public readonly Haptic Welding;
        public readonly Haptic Drilling;
        public readonly Haptic Grinding;
        public readonly Haptic Shooting;
        public readonly Haptic Placing;
        public readonly Haptic Removing;
        public readonly Haptic PlacementFit;

        public Actions()
        {
            Move = new Joystick("/actions/movement/in/Move");
            Rotate = new Joystick("/actions/movement/in/Rotate");
            Thrust = new Joystick("/actions/movement/in/Thrust");
            Forward = new Joystick("/actions/movement/in/Forward");
            Backward = new Joystick("/actions/movement/in/Backward");
            Vertical = new Button("/actions/movement/in/Vertical");
            Roll = new Button("/actions/movement/in/Roll");
            Jump = new Button("/actions/movement/in/Jump");
            Crouch = new Button("/actions/movement/in/Crouch");
            ToolHand = new Pose("/actions/tool/in/ToolHand");
            Primary = new Button("/actions/tool/in/Primary");
            Secondary = new Button("/actions/tool/in/Secondary");
            Reload = new Button("/actions/tool/in/Reload");
            Unequip = new Button("/actions/tool/in/Unequip");
            Previous = new Button("/actions/tool/in/Previous");
            Next = new Button("/actions/tool/in/Next");
            Interact = new Button("/actions/system/in/Interact");
            Helmet = new Button("/actions/system/in/Helmet");
            Jetpack = new Button("/actions/system/in/Jetpack");
            Dampeners = new Button("/actions/system/in/Dampeners");
            Broadcasting = new Button("/actions/system/in/Broadcasting");
            Park = new Button("/actions/system/in/Park");
            Power = new Button("/actions/system/in/Power");
            Lights = new Button("/actions/system/in/Lights");
            Respawn = new Button("/actions/system/in/Respawn");
            VoxelHands = new Button("/actions/system/in/VoxelHands");
            ToggleSymmetry = new Button("/actions/placement/in/ToggleSymmetry");
            SymmetrySetup = new Button("/actions/placement/in/SymmetrySetup");
            PlacementMode = new Button("/actions/placement/in/PlacementMode");
            CubeSize = new Button("/actions/placement/in/CubeSize");
            TabletHand = new Pose("/actions/tablet/out/TabletHand");
            Terminal = new Button("/actions/tablet/in/Terminal");
            Inventory = new Button("/actions/tablet/in/Inventory");
            ColorSelector = new Button("/actions/tablet/out/ColorSelector");
            ColorPicker = new Button("/actions/tablet/out/ColorPicker");
            BuildPlanner = new Button("/actions/tablet/in/BuildPlanner");
            ToolbarConfig = new Button("/actions/tablet/in/ToolbarConfig");
            BlockSelector = new Button("/actions/tablet/in/BlockSelector");
            Contract = new Button("/actions/tablet/in/Contract");
            Chat = new Button("/actions/tablet/in/Chat");
            ToggleView = new Button("/actions/game/in/ToggleView");
            Pause = new Button("/actions/game/in/Pause");
            VoiceChat = new Button("/actions/game/in/VoiceChat");
            SignalMode = new Button("/actions/game/in/SignalMode");
            SpectatorMode = new Button("/actions/game/in/SpectatorMode");
            Teleport = new Button("/actions/game/in/Teleport");
            LeftHandSkeleton = new Button("/actions/skeleton/in/LeftHandSkeleton");
            RightHandSkeleton = new Button("/actions/skeleton/in/RightHandSkeleton");
            Welding = new Haptic("/actions/feedback/out/Welding");
            Drilling = new Haptic("/actions/feedback/out/Drilling");
            Grinding = new Haptic("/actions/feedback/out/Grinding");
            Shooting = new Haptic("/actions/feedback/out/Shooting");
            Placing = new Haptic("/actions/feedback/out/Placing");
            Removing = new Haptic("/actions/feedback/out/Removing");
            PlacementFit = new Haptic("/actions/feedback/out/PlacementFit");
        }

        public void Update()
        {
            // NOTE: I know, it could be a loop.
            // But I don't want the overhead of the virtual function calls
            // and the loop for maximum performance.
            Move.Update();
            Rotate.Update();
            Thrust.Update();
            Forward.Update();
            Backward.Update();
            Vertical.Update();
            Roll.Update();
            Jump.Update();
            Crouch.Update();
            ToolHand.Update();
            Primary.Update();
            Secondary.Update();
            Reload.Update();
            Unequip.Update();
            Previous.Update();
            Next.Update();
            Interact.Update();
            Helmet.Update();
            Jetpack.Update();
            Dampeners.Update();
            Broadcasting.Update();
            Park.Update();
            Power.Update();
            Lights.Update();
            Respawn.Update();
            VoxelHands.Update();
            ToggleSymmetry.Update();
            SymmetrySetup.Update();
            PlacementMode.Update();
            CubeSize.Update();
            TabletHand.Update();
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
            LeftHandSkeleton.Update();
            RightHandSkeleton.Update();
        }
    }
}