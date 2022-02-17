using System.Diagnostics;
using System.Text;
using VRage.Utils;

namespace SpaceEngineersVR.Utils
{
    class Logger
    {

        private static MyLog MyLog;

        static Logger()
        {
            MyLog = new MyLog(true);
            MyLog.InitWithDate("SpaceEngineersVR", new StringBuilder(Globals.Version.ToString()), 7);
        }

        string CName;
        public Logger()
        {
            var methodInfo = new StackTrace().GetFrame(1).GetMethod();
            CName = methodInfo.ReflectedType.Name;
        }

        public void Write(object message)
        {
            MyLog.WriteLine($"{CName}: {message ?? "null"}");
        }

        public void IncreaseIndent()
        {
            MyLog.IncreaseIndent();
        }

        public void DecreaseIndent()
        {
            MyLog.DecreaseIndent();
        }
    }
}
