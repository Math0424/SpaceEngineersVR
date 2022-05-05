using HarmonyLib;
using System;
using System.Reflection;
using VRageMath;

namespace SpaceEngineersVR.Wrappers
{
	public class MyEnvironmentMatrices
    {
        static MyEnvironmentMatrices()
        {
            Type t = AccessTools.TypeByName("VRageRender.MyEnvironmentMatrices");

            cameraPosition = AccessTools.Field(t, "CameraPosition");
            viewAt0 = AccessTools.Field(t, "ViewAt0");
            invViewAt0 = AccessTools.Field(t, "InvViewAt0");
            viewProjectionAt0 = AccessTools.Field(t, "ViewProjectionAt0");
            invViewProjectionAt0 = AccessTools.Field(t, "InvViewProjectionAt0");
            projection = AccessTools.Field(t, "Projection");
            projectionForSkybox = AccessTools.Field(t, "ProjectionForSkybox");
            invProjection = AccessTools.Field(t, "InvProjection");
            viewD = AccessTools.Field(t, "ViewD");
            invViewD = AccessTools.Field(t, "InvViewD");
            originalProjection = AccessTools.Field(t, "OriginalProjection");
            originalProjectionFar = AccessTools.Field(t, "OriginalProjectionFar");
            viewProjectionD = AccessTools.Field(t, "ViewProjectionD");
            invViewProjectionD = AccessTools.Field(t, "InvViewProjectionD");
            viewFrustumClippedD = AccessTools.Field(t, "ViewFrustumClippedD");
            viewFrustumClippedFarD = AccessTools.Field(t, "ViewFrustumClippedFarD");
            nearClipping = AccessTools.Field(t, "NearClipping");
            largeDistanceFarClipping = AccessTools.Field(t, "LargeDistanceFarClipping");
            farClipping = AccessTools.Field(t, "FarClipping");
            fovH = AccessTools.Field(t, "FovH");
            fovV = AccessTools.Field(t, "FovV");
            lastUpdateWasSmooth = AccessTools.Field(t, "LastUpdateWasSmooth");
        }

        public MyEnvironmentMatrices(object realObj)
        {
            this.realObj = realObj;
        }

        private readonly object realObj;


        private static readonly FieldInfo cameraPosition;
        public Vector3D CameraPosition
        {
            get => (Vector3D)cameraPosition.GetValue(realObj);
            set => cameraPosition.SetValue(realObj, value);
        }
        private static readonly FieldInfo viewAt0;
        public Matrix ViewAt0
        {
            get => (Matrix)viewAt0.GetValue(realObj);
            set => viewAt0.SetValue(realObj, value);
        }
        private static readonly FieldInfo invViewAt0;
        public Matrix InvViewAt0
        {
            get => (Matrix)invViewAt0.GetValue(realObj);
            set => invViewAt0.SetValue(realObj, value);
        }
        private static readonly FieldInfo viewProjectionAt0;
        public Matrix ViewProjectionAt0
        {
            get => (Matrix)viewProjectionAt0.GetValue(realObj);
            set => viewProjectionAt0.SetValue(realObj, value);
        }
        private static readonly FieldInfo invViewProjectionAt0;
        public Matrix InvViewProjectionAt0
        {
            get => (Matrix)invViewProjectionAt0.GetValue(realObj);
            set => invViewProjectionAt0.SetValue(realObj, value);
        }
        private static readonly FieldInfo projection;
        public Matrix Projection
        {
            get => (Matrix)projection.GetValue(realObj);
            set => projection.SetValue(realObj, value);
        }
        private static readonly FieldInfo projectionForSkybox;
        public Matrix ProjectionForSkybox
        {
            get => (Matrix)projectionForSkybox.GetValue(realObj);
            set => projectionForSkybox.SetValue(realObj, value);
        }
        private static readonly FieldInfo invProjection;
        public Matrix InvProjection
        {
            get => (Matrix)invProjection.GetValue(realObj);
            set => invProjection.SetValue(realObj, value);
        }
        private static readonly FieldInfo viewD;
        public MatrixD ViewD
        {
            get => (MatrixD)viewD.GetValue(realObj);
            set => viewD.SetValue(realObj, value);
        }
        private static readonly FieldInfo invViewD;
        public MatrixD InvViewD
        {
            get => (MatrixD)invViewD.GetValue(realObj);
            set => invViewD.SetValue(realObj, value);
        }
        private static readonly FieldInfo originalProjection;
        public Matrix OriginalProjection
        {
            get => (Matrix)originalProjection.GetValue(realObj);
            set => originalProjection.SetValue(realObj, value);
        }
        private static readonly FieldInfo originalProjectionFar;
        public Matrix OriginalProjectionFar
        {
            get => (Matrix)originalProjectionFar.GetValue(realObj);
            set => originalProjectionFar.SetValue(realObj, value);
        }
        private static readonly FieldInfo viewProjectionD;
        public MatrixD ViewProjectionD
        {
            get => (MatrixD)viewProjectionD.GetValue(realObj);
            set => viewProjectionD.SetValue(realObj, value);
        }
        private static readonly FieldInfo invViewProjectionD;
        public MatrixD InvViewProjectionD
        {
            get => (MatrixD)invViewProjectionD.GetValue(realObj);
            set => invViewProjectionD.SetValue(realObj, value);
        }
        private static readonly FieldInfo viewFrustumClippedD;
        public BoundingFrustumD ViewFrustumClippedD
        {
            get => (BoundingFrustumD)viewFrustumClippedD.GetValue(realObj);
            set => viewFrustumClippedD.SetValue(realObj, value);
        }
        private static readonly FieldInfo viewFrustumClippedFarD;
        public BoundingFrustumD ViewFrustumClippedFarD
        {
            get => (BoundingFrustumD)viewFrustumClippedFarD.GetValue(realObj);
            set => viewFrustumClippedFarD.SetValue(realObj, value);
        }
        private static readonly FieldInfo nearClipping;
        public float NearClipping
        {
            get => (float)nearClipping.GetValue(realObj);
            set => nearClipping.SetValue(realObj, value);
        }
        private static readonly FieldInfo largeDistanceFarClipping;
        public float LargeDistanceFarClipping
        {
            get => (float)largeDistanceFarClipping.GetValue(realObj);
            set => largeDistanceFarClipping.SetValue(realObj, value);
        }
        private static readonly FieldInfo farClipping;
        public float FarClipping
        {
            get => (float)farClipping.GetValue(realObj);
            set => farClipping.SetValue(realObj, value);
        }
        private static readonly FieldInfo fovH;
        public float FovH
        {
            get => (float)fovH.GetValue(realObj);
            set => fovH.SetValue(realObj, value);
        }
        private static readonly FieldInfo fovV;
        public float FovV
        {
            get => (float)fovV.GetValue(realObj);
            set => fovV.SetValue(realObj, value);
        }
        private static readonly FieldInfo lastUpdateWasSmooth;
        public bool LastUpdateWasSmooth
        {
            get => (bool)lastUpdateWasSmooth.GetValue(realObj);
            set => lastUpdateWasSmooth.SetValue(realObj, value);
        }
    }
}