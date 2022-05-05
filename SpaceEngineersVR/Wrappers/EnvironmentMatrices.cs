using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace SpaceEngineersVR.Wrappers
{
	public class EnvironmentMatrices
	{
		internal Vector3D CameraPosition;
		internal Matrix ViewAt0;
		internal Matrix InvViewAt0;
		internal Matrix ViewProjectionAt0;
		internal Matrix InvViewProjectionAt0;
		internal Matrix Projection;
		internal Matrix ProjectionForSkybox;
		internal Matrix InvProjection;
		internal MatrixD ViewD;
		internal MatrixD InvViewD;
		internal Matrix OriginalProjection;
		internal Matrix OriginalProjectionFar;
		internal MatrixD ViewProjectionD;
		internal MatrixD InvViewProjectionD;
		internal BoundingFrustumD ViewFrustumClippedD;
		internal BoundingFrustumD ViewFrustumClippedFarD;
		internal float NearClipping;
		internal float LargeDistanceFarClipping;
		internal float FarClipping;
		internal float FovH;
		internal float FovV;
		internal bool LastUpdateWasSmooth;
    }
}
