using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Character.Components;
using SpaceEngineersVR.Plugin;
using System;
using System.Reflection;
using VRageMath;
using VRageRender.Animations;

namespace SpaceEngineersVR.Player.Components
{
    internal class VRBodyComponent : MyCharacterComponent
    {
        public BodyCalibration characterCalibration;

        private static readonly Matrix HandExtraTransformL = Matrix.CreateRotationZ(MathHelper.Pi / 2f);
        private static readonly Matrix HandExtraTransformR = Matrix.CreateRotationZ(MathHelper.Pi / 2f) * Matrix.CreateRotationY(MathHelper.Pi);

        private static readonly FieldInfo HandIndexFieldL = HarmonyLib.AccessTools.Field(typeof(MyCharacter), "m_leftHandIKEndBone");
        private static readonly FieldInfo HandIndexFieldR = HarmonyLib.AccessTools.Field(typeof(MyCharacter), "m_rightHandIKEndBone");

        private static readonly FieldInfo ArmIKStartIndexFieldL = HarmonyLib.AccessTools.Field(typeof(MyCharacter), "m_leftHandIKStartBone");
        private static readonly FieldInfo ArmIKStartIndexFieldR = HarmonyLib.AccessTools.Field(typeof(MyCharacter), "m_rightHandIKStartBone");

        private static readonly MethodInfo CalculateHandIK = HarmonyLib.AccessTools.Method(typeof(MyCharacter), "CalculateHandIK", new Type[]
        {
            typeof(int), //startBoneIndex
            typeof(int), //endBoneIndex
            typeof(MatrixD).MakeByRefType(), //targetTransform
        });

        private int handIndexL;
        private int handIndexR;

        private int armIKStartIndexL;
        private int armIKStartIndexR;


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


            handIndexL = (int)HandIndexFieldL.GetValue(Character);
            handIndexR = (int)HandIndexFieldR.GetValue(Character);

            armIKStartIndexL = (int)ArmIKStartIndexFieldL.GetValue(Character);
            armIKStartIndexR = (int)ArmIKStartIndexFieldR.GetValue(Character);

            MyCharacterBone handBoneL = handIndexL >= 0 ? bones[handIndexL] : null;
            MyCharacterBone handBoneR = handIndexR >= 0 ? bones[handIndexR] : null;

            MyCharacterBone shoulderL = armIKStartIndexL >= 0 ? bones[armIKStartIndexL] : null;
            MyCharacterBone shoulderR = armIKStartIndexR >= 0 ? bones[armIKStartIndexR] : null;

            float lengthL = CalculateArmLength(handBoneL, shoulderL);
            float lengthR = CalculateArmLength(handBoneR, shoulderR);
            float shoulderWidth = Vector3.Distance(shoulderL.GetAbsoluteRigTransform().Translation, shoulderR.GetAbsoluteRigTransform().Translation);

            characterCalibration.armSpan = lengthL + lengthR + shoulderWidth;

            float CalculateArmLength(MyCharacterBone hand, MyCharacterBone shoulder)
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
            Update(Player.HandL, armIKStartIndexL, handIndexL, HandExtraTransformL);
            Update(Player.HandR, armIKStartIndexR, handIndexR, HandExtraTransformR);

            void Update(Controller controller, int ikStartIndex, int handBoneIndex, Matrix rotationMatrix)
            {
                if (!controller.pose.isTracked)
                    return;

                MatrixD mat = rotationMatrix * controller.deviceToPlayer * Character.WorldMatrix;
                CalculateHandIK.Invoke(Character, new object[] { ikStartIndex, handBoneIndex, mat });
            }
        }

        public override string ComponentTypeDebugString => "VR Hands Component";
    }
}