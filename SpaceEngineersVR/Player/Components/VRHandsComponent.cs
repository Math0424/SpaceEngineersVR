using Sandbox.Game.Entities.Character.Components;
using SpaceEngineersVR.Player;
using SpaceEngineersVR.Plugin;
using SpaceEngineersVR.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.Components;

namespace ClientPlugin.Player.Components
{

    internal class VRHandsComponent : MyCharacterComponent
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
            this.NeedsUpdateSimulation = true;
        }

        private void Init()
        {
            Logger.Debug("Initalizing VR hands");
            //TODO: use InternalChangeModelAndCharacter and swap models
            //Character.ChangeModelAndColor();
        }

        public override void OnCharacterDead()
        {

        }

        public override void Simulate()
        {
            var right = Controls.Static.RightHand;
            var left = Controls.Static.LeftHand;
               
            if (right.Valid)
            {
                Util.DrawDebugMatrix(right.AbsoluteTracking.Translation, right.AbsoluteTracking, "RightHand");
            }

            if (left.Valid)
            {
                Util.DrawDebugMatrix(left.AbsoluteTracking.Translation, left.AbsoluteTracking, "LeftHand");
            }
        }

        public override string ComponentTypeDebugString => "VR Hands Component";

    }
}