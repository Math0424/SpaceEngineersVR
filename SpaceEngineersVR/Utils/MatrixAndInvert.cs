using VRageMath;

namespace SpaceEngineersVR.Util
{
    public struct MatrixAndInvert
    {
        public MatrixAndInvert(Matrix matrix)
        {
            this.matrix = matrix;
            inverted = Matrix.Invert(matrix);
        }
        public MatrixAndInvert(Matrix matrix, Matrix inverted)
        {
            this.matrix = matrix;
            this.inverted = inverted;
        }

        public Matrix matrix;
        public Matrix inverted;

        public static readonly MatrixAndInvert Identity = new MatrixAndInvert(Matrix.Identity, Matrix.Identity);
    }
    public struct MatrixDAndInvert
    {
        public MatrixDAndInvert(MatrixD matrix)
        {
            this.matrix = matrix;
            inverted = MatrixD.Invert(matrix);
        }
        public MatrixDAndInvert(MatrixD matrix, MatrixD inverted)
        {
            this.matrix = matrix;
            this.inverted = inverted;
        }

        public MatrixD matrix;
        public MatrixD inverted;

        public static readonly MatrixDAndInvert Identity = new MatrixDAndInvert(MatrixD.Identity, MatrixD.Identity);
    }
}