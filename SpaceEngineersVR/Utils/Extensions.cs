using Valve.VR;
using VRageMath;

namespace SpaceEngineersVR.Util
{
    internal static class Extensions
    {

        public static Vector3 ToVector(this HmdVector3_t v)
        {
            return new Vector3(v.v0, v.v1, v.v2);
        }

        public static Vector3D ToVector(this HmdVector3d_t v)
        {
            return new Vector3D(v.v0, v.v1, v.v2);
        }


        //Matrix
        //11 12 13 right
        //21 22 23 up
        //31 32 33 backward
        //41 42 43 translation

        //HmdMatrix34_t
        //0 1  2 right
        //4 5  6 up
        //8 9 10 backward
        //3 7 11 translation

        public static Matrix ToMatrix(this HmdMatrix34_t hmd)
        {
            return new Matrix(
                hmd.m0, hmd.m1, hmd.m2,  0f,
                hmd.m4, hmd.m5, hmd.m6,  0f,
                hmd.m8, hmd.m9, hmd.m10, 0f,
                hmd.m3, hmd.m7, hmd.m11, 1f);
        }

        public static Matrix ToMatrix(this HmdMatrix44_t hmd)
        {
            return new Matrix(
                hmd.m0, hmd.m1, hmd.m2,  hmd.m12,
                hmd.m4, hmd.m5, hmd.m6,  hmd.m13,
                hmd.m8, hmd.m9, hmd.m10, hmd.m14,
                hmd.m3, hmd.m7, hmd.m11, hmd.m15);
        }
    }
}