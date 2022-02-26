using Sandbox.Game.Entities.Character.Components;
using SpaceEngineersVR.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.Components;

namespace ClientPlugin.Player.Components
{

    internal class VRMovementComponent : MyCharacterComponent
    {

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

        }

        public override string ComponentTypeDebugString => "VR Movement Component";

    }
}