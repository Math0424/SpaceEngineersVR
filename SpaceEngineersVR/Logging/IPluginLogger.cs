using System;

namespace SpaceEngineersVR.Logging
{
    public interface IPluginLogger
    {
        bool IsTraceEnabled { get; }
        bool IsDebugEnabled { get; }
        bool IsInfoEnabled { get; }
        bool IsWarningEnabled { get; }
        bool IsErrorEnabled { get; }
        bool IsCriticalEnabled { get; }

        void Trace(Exception ex, string message, params object[] data);
        void Debug(Exception ex, string message, params object[] data);
        void Info(Exception ex, string message, params object[] data);
        void Warning(Exception ex, string message, params object[] data);
        void Error(Exception ex, string message, params object[] data);
        void Critical(Exception ex, string message, params object[] data);

        void Trace(string message, params object[] data);
        void Debug(string message, params object[] data);
        void Info(string message, params object[] data);
        void Warning(string message, params object[] data);
        void Error(string message, params object[] data);
        void Critical(string message, params object[] data);

        public void IncreaseIndent();
        public void DecreaseIndent();
    }
}
