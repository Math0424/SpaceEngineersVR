using System;
using System.Runtime.CompilerServices;
using System.Text;
using VRage.Utils;

namespace SpaceEngineersVR.Logging
{
    public class PluginLogger : LogFormatter, IPluginLogger
    {
        private static MyLog MyLog;
        public PluginLogger(string pluginName) : base($"{pluginName}: ")
        {
            MyLog = new MyLog(true);
            MyLog.InitWithDate(pluginName, new StringBuilder(Common.Common.Version.ToString()), 7);
        }

        public bool IsTraceEnabled => MyLog.Default.LogEnabled;
        public bool IsDebugEnabled => MyLog.Default.LogEnabled;
        public bool IsInfoEnabled => MyLog.Default.LogEnabled;
        public bool IsWarningEnabled => MyLog.Default.LogEnabled;
        public bool IsErrorEnabled => MyLog.Default.LogEnabled;
        public bool IsCriticalEnabled => MyLog.Default.LogEnabled;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Trace(Exception ex, string message, params object[] data)
        {
            if (!IsTraceEnabled)
            {
                return;
            }

            // Keen does not have a Trace log level, using Debug instead
            MyLog.Log(MyLogSeverity.Debug, Format(ex, message, data));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Debug(Exception ex, string message, params object[] data)
        {
            if (!IsDebugEnabled)
            {
                return;
            }

            MyLog.Log(MyLogSeverity.Debug, Format(ex, message, data));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Info(Exception ex, string message, params object[] data)
        {
            if (!IsInfoEnabled)
            {
                return;
            }

            MyLog.Log(MyLogSeverity.Info, Format(ex, message, data));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Warning(Exception ex, string message, params object[] data)
        {
            if (!IsWarningEnabled)
            {
                return;
            }

            MyLog.Log(MyLogSeverity.Warning, Format(ex, message, data));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Error(Exception ex, string message, params object[] data)
        {
            if (!IsErrorEnabled)
            {
                return;
            }

            MyLog.Log(MyLogSeverity.Error, Format(ex, message, data));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Critical(Exception ex, string message, params object[] data)
        {
            if (!IsCriticalEnabled)
            {
                return;
            }

            MyLog.Log(MyLogSeverity.Critical, Format(ex, message, data));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Trace(string message, params object[] data)
        {
            Trace(null, message, data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Debug(string message, params object[] data)
        {
            Debug(null, message, data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Info(string message, params object[] data)
        {
            Info(null, message, data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Warning(string message, params object[] data)
        {
            Warning(null, message, data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Error(string message, params object[] data)
        {
            Error(null, message, data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Critical(string message, params object[] data)
        {
            Critical(null, message, data);
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
