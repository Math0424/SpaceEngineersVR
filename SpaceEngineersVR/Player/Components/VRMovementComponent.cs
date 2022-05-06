using Sandbox;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Character.Components;
using Sandbox.Game.Screens.Helpers.RadialMenuActions;
using Sandbox.Game.SessionComponents.Clipboard;
using Sandbox.Game.World;
using SpaceEngineersVR.Player;
using SpaceEngineersVR.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRageMath;

namespace ClientPlugin.Player.Components
{

    internal class VRMovementComponent : MyCharacterComponent
    {
        public enum RotationType
        {
            Step,
            Continuous,
        }

        public enum MovementType
        {
            Head,
            Hand
        }

        public Matrix cameraMatrix = Matrix.Identity;
        public Matrix rotationOffset = Matrix.Identity;

        public RotationType rotationType = RotationType.Continuous;
        public MovementType movementType = MovementType.Hand;

        public bool ControllerMovement;
        // TODO: Configurable rotation speed and step by step rotation instead of continuous
        public float RotationSpeed = 10;


        public override void Init(MyComponentDefinitionBase definition)
        {
            if (Character.InScene)
            {
                Init();
            }
        }

        public override void OnAddedToScene() => Init();

        public override void OnAddedToContainer()
        {
            this.NeedsUpdateBeforeSimulation = true;
        }

        private void Init()
        {
            //TODO: use InternalChangeModelAndCharacter and swap models
            //Character.ChangeModelAndColor();
        }

        public override void OnCharacterDead()
        {

        }

        public override void Simulate()
        {

        }

        public override void UpdateBeforeSimulation()
        {
            if (MySession.Static.ControlledEntity is MyShipController)
            {
                ControlShip();
            }
            else if(((IMyCharacter)Character).EnabledThrusts)
            {
                ControlFlight();
            }
            else
            {
                ControlWalk();
            }
            ControlCommonFunctions();
            OrientateCharacterToHMD();
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ControlShip()
        {
            //tmp
            ControlFlight();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ControlWalk()
        {
            var controls = Controls.Static;

            controls.UpdateWalk();

            var move = Vector3.Zero;
            var rotate = Vector2.Zero;

            if (controls.Walk.Active)
            {
                var v = controls.Walk.Position;
                move.X += v.X;
                move.Z -= v.Y;
            }

            if (controls.WalkForward.Active)
                move.Z -= controls.WalkForward.Position.X;

            if (controls.WalkBackward.Active)
                move.Z += controls.WalkForward.Position.X;

            // TODO: Configurable rotation speed and step by step rotation instead of continuous
            if (controls.WalkRotate.Active)
            {
                var v = controls.WalkRotate.Position;
                rotate.Y = v.X * RotationSpeed;
                rotate.X = -v.Y * RotationSpeed;
            }

            if (controls.JumpOrClimbUp.HasPressed)
                Character.Jump(Vector3.Up);

            if (controls.CrouchOrClimbDown.HasPressed)
                Character.Crouch();

            if (controls.JumpOrClimbUp.IsPressed)
                move.Y = 1f;

            if (controls.CrouchOrClimbDown.IsPressed)
                move.Y = -1f;

            ApplyMoveAndRotation(move, rotate, 0f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ControlFlight()
        {
            var controls = Controls.Static;

            var move = Vector3.Zero;
            var rotate = Vector2.Zero;
            var roll = 0f;

            controls.UpdateFlight();

            if (controls.ThrustLRUD.Active)
            {
                var v = controls.ThrustLRUD.Position;
                move.X += v.X;
                move.Y += v.Y;
            }

            if (controls.ThrustLRFB.Active)
            {
                var v = controls.ThrustLRFB.Position;
                move.X += v.X;
                move.Z -= v.Y;
            }

            if (controls.ThrustUp.Active)
                move.Y += controls.ThrustUp.Position.X;

            if (controls.ThrustDown.Active)
                move.Y -= controls.ThrustDown.Position.X;

            if (controls.ThrustForward.Active)
                move.Z -= controls.ThrustForward.Position.X;

            if (controls.ThrustBackward.Active)
                move.Z += controls.ThrustBackward.Position.X;

            if (controls.ThrustRotate.Active)
            {
                var v = controls.ThrustRotate.Position;

                if (controls.ThrustRoll.IsPressed)
                    roll = v.X * RotationSpeed;
                else
                    rotate.Y = v.X * RotationSpeed;

                rotate.X = -v.Y * RotationSpeed;
            }

            if (controls.Dampener.HasPressed)
                MySession.Static.ControlledEntity?.SwitchDamping();

            ApplyMoveAndRotation(move, rotate, roll);
        }


        void OrientateCharacterToHMD()
        {
            Matrix absoluteRotation = Main.Headset.hmdAbsolute;
            absoluteRotation *= rotationOffset;

            Matrix characterRotation = Character.WorldMatrix;
            characterRotation.Translation = Vector3.Zero;


            //Character.MoveAndRotate();
        }


        void ApplyMoveAndRotation(Vector3 move, Vector2 rotate, float roll)
        {
            move = Vector3.Clamp(move, -Vector3.One, Vector3.One);

            ControllerMovement = move != Vector3.Zero || rotate != Vector2.Zero || roll != 0f;

            if (ControllerMovement)
                MySession.Static.ControlledEntity?.MoveAndRotate(move, rotate, roll);
            else
                MySession.Static.ControlledEntity?.MoveAndRotateStopped();
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ControlCommonFunctions()
        {
            var controls = Controls.Static;

            var controlledEntity = MySession.Static.ControlledEntity;

            if (controls.Primary.HasPressed)
            {
                controlledEntity?.BeginShoot(MyShootActionEnum.PrimaryAction);
            }
            else if (controls.Primary.HasReleased)
            {
                controlledEntity?.EndShoot(MyShootActionEnum.PrimaryAction);
            }

            if (controls.Secondary.HasPressed)
            {
                controlledEntity?.BeginShoot(MyShootActionEnum.SecondaryAction);
            }
            else if (controls.Secondary.HasReleased)
            {
                controlledEntity?.EndShoot(MyShootActionEnum.SecondaryAction);
            }

            if (controls.Reload.HasPressed)
            {
                // TODO
            }

            if (controls.Unequip.HasPressed)
            {
                controlledEntity?.SwitchToWeapon(null);
            }

            if (controls.CutGrid.HasPressed)
            {
                new MyActionCutGrid().ExecuteAction();
            }

            if (controls.CopyGrid.HasPressed)
            {
                new MyActionCopyGrid().ExecuteAction();
            }

            if (controls.PasteGrid.HasPressed)
            {
                new MyActionPasteGrid().ExecuteAction();
            }

            if (controls.Interact.HasPressed)
            {
                controlledEntity?.Use();
            }

            if (controls.Helmet.HasPressed)
            {
                ((IMyCharacter)Character).SwitchHelmet();
            }

            if (controls.Jetpack.HasPressed)
            {
                ((IMyCharacter)Character).SwitchThrusts();
            }

            if (controls.Broadcasting.HasPressed)
            {
                controlledEntity?.SwitchBroadcasting();
            }

            if (controls.Park.HasPressed)
            {
                controlledEntity?.SwitchHandbrake();
            }

            if (controls.Power.HasPressed)
            {
                new MyActionTogglePower().ExecuteAction();
            }

            if (controls.Lights.HasPressed)
            {
                Character.SwitchLights();
            }

            if (controls.Respawn.HasPressed)
            {
                controlledEntity?.Die();
            }

            if (controls.ToggleSignals.HasPressed)
            {
                new MyActionToggleSignals().ExecuteAction();
            }

            if (controls.ToggleSymmetry.HasPressed)
            {
                new MyActionToggleSymmetry().ExecuteAction();
            }

            if (controls.SymmetrySetup.HasPressed)
            {
                new MyActionSymmetrySetup().ExecuteAction();
            }

            if (controls.PlacementMode.HasPressed)
            {
                MyClipboardComponent.Static.ChangeStationRotation();
                MyCubeBuilder.Static.CycleCubePlacementMode();
            }

            if (controls.CubeSize.HasPressed)
            {
                // TODO
            }

            if (controls.Terminal.HasPressed)
            {
                ((IMyCharacter)Character).ShowTerminal();
            }

            if (controls.Inventory.HasPressed)
            {
                ((IMyCharacter)Character).ShowInventory();
            }

            if (controls.ColorSelector.HasPressed)
            {
                new MyActionColorTool().ExecuteAction();
            }

            if (controls.ColorPicker.HasPressed)
            {
                new MyActionColorPicker().ExecuteAction();
            }

            if (controls.BuildPlanner.HasPressed)
            {
                // TODO
            }

            if (controls.ToolbarConfig.HasPressed)
            {
                // TODO
            }

            if (controls.BlockSelector.HasPressed)
            {
                // TODO
            }

            if (controls.Contract.HasPressed)
            {
                // TODO
            }

            if (controls.Chat.HasPressed)
            {
                // TODO
            }

            if (controls.ToggleView.HasPressed)
            {
                ((IMyCharacter)Character).IsInFirstPersonView = !((IMyCharacter)Character).IsInFirstPersonView;
            }

            if (controls.Pause.HasPressed)
            {
                MySandboxGame.IsPaused = !MySandboxGame.IsPaused;
            }

            if (controls.VoiceChat.HasPressed)
            {
                // TODO
            }

            if (controls.SignalMode.HasPressed)
            {
                // TODO
            }

            if (controls.SpectatorMode.HasPressed)
            {
                // TODO
            }

            if (controls.Teleport.HasPressed)
            {
                // TODO: character.Teleport();
            }
        }

        public override string ComponentTypeDebugString => "VR Movement Component";

    }
}