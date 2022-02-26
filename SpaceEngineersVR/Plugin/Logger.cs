using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using VRage.Utils;

namespace SpaceEngineersVR.Plugin
{
    public static class Logger
    {
        private static MyLog myLog;
        private const int MaxExceptionDepth = 100;
        private static readonly ThreadLocal<StringBuilder> threadLocalStringBuilder = new ThreadLocal<StringBuilder>();

        static Logger()
        {
            myLog = new MyLog(true);
            myLog.InitWithDate(Common.Name, new StringBuilder(Common.Version.ToString()), 7);
            myLog.LogFlag(LoggingOptions.ALL);
        }

        public static bool IsTraceEnabled => MyLog.Default.LogEnabled;
        public static bool IsDebugEnabled => MyLog.Default.LogEnabled;
        public static bool IsInfoEnabled => MyLog.Default.LogEnabled;
        public static bool IsWarningEnabled => MyLog.Default.LogEnabled;
        public static bool IsErrorEnabled => MyLog.Default.LogEnabled;
        public static bool IsCriticalEnabled => MyLog.Default.LogEnabled;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Trace(Exception ex, string message, params object[] data)
        {
            if (!IsTraceEnabled)
                return;
            myLog.Log(MyLogSeverity.Debug, Format(ex, message, data));
            myLog.Flush();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Debug(Exception ex, string message, params object[] data)
        {
            if (!IsDebugEnabled)
                return;
            myLog.Log(MyLogSeverity.Debug, Format(ex, message, data));
            myLog.Flush();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Info(Exception ex, string message, params object[] data)
        {
            if (!IsInfoEnabled)
                return;
            myLog.Log(MyLogSeverity.Info, Format(ex, message, data));
            myLog.Flush();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Warning(Exception ex, string message, params object[] data)
        {
            if (!IsWarningEnabled)
                return;
            myLog.Log(MyLogSeverity.Warning, Format(ex, message, data));
            myLog.Flush();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Error(Exception ex, string message, params object[] data)
        {
            if (!IsErrorEnabled)
                return;
            myLog.Log(MyLogSeverity.Error, Format(ex, message, data));
            myLog.Flush();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Critical(Exception ex, string message, params object[] data)
        {
            if (!IsCriticalEnabled)
                return;
            myLog.Log(MyLogSeverity.Critical, Format(ex, message, data));
            myLog.Flush();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Trace(string message, params object[] data)
        {
            Trace(null, message, data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Debug(string message, params object[] data)
        {
            Debug(null, message, data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Info(string message, params object[] data)
        {
            Info(null, message, data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Warning(string message, params object[] data)
        {
            Warning(null, message, data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Error(string message, params object[] data)
        {
            Error(null, message, data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Critical(string message, params object[] data)
        {
            Critical(null, message, data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IncreaseIndent()
        {
            myLog.IncreaseIndent();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DecreaseIndent()
        {
            myLog.DecreaseIndent();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string Format(Exception ex, string message, object[] data)
        {
            // Allocate a single StringBuilder object per thread
            var sb = threadLocalStringBuilder.Value;
            if (sb == null)
            {
                sb = new StringBuilder();
                threadLocalStringBuilder.Value = sb;
            }

            if (message == null)
                message = "";

            sb.Append(Common.Name);

            //Spacer
            sb.Append(": ");

            sb.Append(data == null || data.Length == 0 ? message : string.Format(message, data));

            FormatException(sb, ex);

            var text = sb.ToString();
            sb.Clear();

            return text;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void FormatException(StringBuilder sb, Exception ex)
        {
            if (ex == null)
                return;

            for (var i = 0; i < MaxExceptionDepth; i++)
            {
                sb.Append("\r\n[");
                sb.Append(ex.GetType().Name);
                sb.Append("] ");
                sb.Append(ex.Message);

                if (ex.TargetSite != null)
                {
                    sb.Append("\r\nMethod: ");
                    sb.Append(ex.TargetSite);
                }

                if (ex.Data.Count > 0)
                {
                    sb.Append("\r\nData:");
                    foreach (var key in ex.Data.Keys)
                    {
                        sb.Append("\r\n");
                        sb.Append(key);
                        sb.Append(" = ");
                        sb.Append(ex.Data[key]);
                    }
                }

                sb.Append("\r\nTraceback:\r\n");
                sb.Append(ex.StackTrace);

                ex = ex.InnerException;
                if (ex == null)
                    return;

                sb.Append("\r\nInner exception:\r\n");
            }

            sb.Append($"WARNING: Not logging more than {MaxExceptionDepth} inner exceptions");
        }


    }
}