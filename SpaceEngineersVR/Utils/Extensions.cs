using Valve.VR;
using VRageMath;

namespace SpaceEnginnersVR.Util
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

        public static Matrix ToMatrix(this HmdMatrix34_t hmd)
        {
            return new Matrix(
                hmd.m0, hmd.m1, hmd.m2, 0,
                hmd.m4, hmd.m5, hmd.m6, 0,
                hmd.m8, hmd.m9, hmd.m10, 0,
                hmd.m3, hmd.m7, hmd.m11, 1);

            //Matrix
            //11 12 13 right
            //21 22 23 up
            //31 32 33 forward
            //41 42 43 xyz

            //HmdMatrix34_t
            //         x y z  xyz
            //forward [0 1 2]  3
            //up      [4 5 6]  7
            //right   [8 9 10] 11
        }

        public static Matrix ToMatrix(this HmdMatrix44_t hmd)
        {
            return new Matrix(
                hmd.m0, hmd.m1, hmd.m2, 0,
                hmd.m4, hmd.m5, hmd.m6, 0,
                hmd.m8, hmd.m9, hmd.m10, 0,
                hmd.m3, hmd.m7, hmd.m11, 1);
        }




    }
}
