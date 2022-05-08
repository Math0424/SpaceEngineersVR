using Sandbox.Game.Entities.Character.Components;
using SpaceEngineersVR.Player;
using SpaceEngineersVR.Plugin;
using SpaceEngineersVR.Util;
using VRage.Game;
using VRageMath;

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
            /*
            var right = DeviceManager.RightHand;
            var left = DeviceManager.LeftHand;

            if (right.isTracked)
            {
                Matrix world = right.transform * Character.WorldMatrix;
                Util.DrawDebugMatrix(world.Translation, world, "RightHand");
                SetBoneTransform(right, "SE_RigRPalm");
            }

            if (left.isTracked)
            {
                Matrix world = left.transform * Character.WorldMatrix;
                Util.DrawDebugMatrix(world.Translation, world, "LeftHand");
                SetBoneTransform(left, "SE_RigLPalm");
            }


            void SetBoneTransform(Controller hand, string boneName)
            {
                Matrix mat = hand.transform;
                Vector3 position = mat.Translation;
                Quaternion rotation = Quaternion.CreateFromRotationMatrix(mat);

                Character.AnimationController.FindBone(boneName, out _)?.SetCompleteTransform(ref position, ref rotation);
            }
            */
        }

        public override string ComponentTypeDebugString => "VR Hands Component";

    }
}