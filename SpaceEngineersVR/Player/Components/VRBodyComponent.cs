using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Character.Components;
using SpaceEngineersVR.Plugin;
using System.Reflection;
using VRageMath;
using VRageRender.Animations;

namespace SpaceEngineersVR.Player.Components
{
    internal class VRBodyComponent : MyCharacterComponent
    {
        public BodyCalibration characterCalibration;

        private static readonly FieldInfo RightHandIndexField = HarmonyLib.AccessTools.Field(typeof(MyCharacter), "m_rightHandIKEndBone");
        private static readonly FieldInfo LeftHandIndexField = HarmonyLib.AccessTools.Field(typeof(MyCharacter), "m_leftHandIKEndBone");

        private static readonly FieldInfo RightArmIKStartIndexField = HarmonyLib.AccessTools.Field(typeof(MyCharacter), "m_rightHandIKStartBone");
        private static readonly FieldInfo LeftArmIKStartIndexField = HarmonyLib.AccessTools.Field(typeof(MyCharacter), "m_leftHandIKStartBone");

        private int rightHandIndex;
        private int leftHandIndex;

        public override void OnAddedToScene()
        {
            Init();
        }

        public override void OnAddedToContainer()
        {
            NeedsUpdateBeforeSimulation = true;
            if (Character.InScene)
            {
                Init();
            }
        }

        private void Init()
        {
            Logger.Debug("Initalizing VR hands");

            MyCharacterBone[] bones = Character.AnimationController.CharacterBones;

            MyCharacterBone headBone = bones[Character.HeadBoneIndex];
            Vector3 headPos = headBone.GetAbsoluteRigTransform().Translation;

            characterCalibration.height = headPos.Y;


            rightHandIndex = (int)RightHandIndexField.GetValue(Character);
            leftHandIndex = (int)LeftHandIndexField.GetValue(Character);

            MyCharacterBone rightHandBone = bones[rightHandIndex];
            MyCharacterBone leftHandBone =  bones[leftHandIndex];

            int rightArmIKStartIndex = (int)RightArmIKStartIndexField.GetValue(Character);
            int leftArmIKStartIndex = (int)LeftArmIKStartIndexField.GetValue(Character);

            MyCharacterBone leftShoulder = bones[leftArmIKStartIndex];
            MyCharacterBone rightShoulder = bones[rightArmIKStartIndex];

            float lengthR = CalculateArmLength(leftHandBone, leftShoulder,  headPos);
            float lengthL = CalculateArmLength(rightHandBone, rightShoulder, headPos);
            float shoulderWidth = Vector3.Distance(leftShoulder.GetAbsoluteRigTransform().Translation, rightShoulder.GetAbsoluteRigTransform().Translation);

            characterCalibration.armSpan = lengthL + lengthR + shoulderWidth;

            static float CalculateArmLength(MyCharacterBone hand, MyCharacterBone shoulder, Vector3 headPos)
            {
                float totalLength = 0f;
                for (MyCharacterBone bone = hand; bone != shoulder; bone = bone.Parent)
                {
                    totalLength += bone.BindTransform.Translation.Length();
                }
                return totalLength;
            }
        }

        public override void OnCharacterDead()
        {
        }

        public override void UpdateBeforeSimulation()
        {
            Update(Player.RightHand, rightHandIndex);
            Update(Player.LeftHand,  leftHandIndex);

            void Update(Controller controller, int boneIndex)
            {
                if (!controller.pose_Main.isTracked)
                    return;

                MyCharacterBone hand = Character.AnimationController.CharacterBones[boneIndex];

                Matrix mat = controller.deviceToPlayer;

                hand.SetCompleteTransformFromAbsoluteMatrix(ref mat, false);
            }
        }

        public override string ComponentTypeDebugString => "VR Hands Component";
    }
}