using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Utils;
using VRageMath;
using VRageRender;
using Vector4 = VRageMath.Vector4;

namespace SpaceEnginnersVR.Utill
{
    internal class Util
    {
        private static MyStringId SQUARE = MyStringId.GetOrCompute("Square");

        public static string GetAssetFolder()
        {
            return Path.Combine(GetPluginsFolder(), "SEVRAssets");
        }

        public static string GetPluginsFolder()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        public static void DrawDebugLine(Vector3D pos, Vector3D dir, int r, int g, int b)
        {
            Vector4 color = new Vector4(r / 255f, g / 255f, b / 255f, 1);
            MySimpleObjectDraw.DrawLine(pos, pos + dir * 10, SQUARE, ref color, 0.01f);
        }

        public static void DrawDebugSphere(Vector3D pos, float radius, int r, int g, int b)
        {
            MatrixD x = MatrixD.Identity;
            x.Translation = pos;
            Color color = new Vector4(r / 255f, g / 255f, b / 255f, 1);
            MySimpleObjectDraw.DrawTransparentSphere(ref x, radius, ref color, MySimpleObjectRasterizer.SolidAndWireframe, 1, SQUARE, SQUARE);
        }

        public static void DrawDebugMatrix(Vector3D position, MatrixD pose, string name)
        {
            DrawDebugLine(position, pose.Forward, 255, 000, 000);
            DrawDebugLine(position, pose.Left, 000, 255, 000);
            DrawDebugLine(position, pose.Up, 000, 000, 255);
            DrawDebugText(position, name);
        }

        public static void DrawDebugText(Vector3D pos, string text)
        {
            MyRenderProxy.DebugDrawText3D(pos, text, Color.White, 1f, true);
        }

        public static void ExecuteInMain(Action action, bool sync)
        {
            SynchronizationContext synchronization = SynchronizationContext.Current;
            if (synchronization != null)
            {
                if (sync)
                {
                    synchronization.Send(_ => action(), null);
                }
                else
                {
                    synchronization.Post(_ => action(), null);
                }
            }
            else
            {
                Task.Factory.StartNew(action);
            }
        }

        public static MatrixD MapViewToWorldMatrix(MatrixD view, MatrixD worldMatrix)
        {

            worldMatrix.Right = view.Right; //Vector3D.Lerp(worldMatrix.Right, view.Right, .5);
            worldMatrix.Forward = view.Forward; //Vector3D.Lerp(worldMatrix.Forward, view.Forward, .5);
            return worldMatrix;

        }

        public static double GetAngle(Vector3D one, Vector3D two, Vector3D up)
        {
            //360 code
            double angle = (Math.Acos(Vector3D.Dot(Vector3D.Normalize(one), Vector3D.Normalize(two))) * 180 / Math.PI);
            Vector3D cross = Vector3D.Cross(one, two);
            if (Vector3D.Dot(Vector3D.Normalize(up), cross) > 0)
            {
                angle += angle - 90;
            }
            return angle;
        }

        public static double GetAngleRadians(Vector3D one, Vector3D two, Vector3D up)
        {
            //360 code
            double angle = Math.Acos(Vector3D.Dot(Vector3D.Normalize(one), Vector3D.Normalize(two)));
            Vector3D cross = Vector3D.Cross(one, two);
            if (Vector3D.Dot(Vector3D.Normalize(up), cross) > 0)
            {
                angle += angle - 1;
            }
            return angle;
        }

    }
}