using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace Shared.Logging
{
    public class LogFormatter
    {
        private const int MaxExceptionDepth = 100;
        private readonly ThreadLocal<StringBuilder> threadLocalStringBuilder = new ThreadLocal<StringBuilder>();
        private readonly string prefix;

        protected LogFormatter(string prefix)
        {
            this.prefix = prefix;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected string Format(Exception ex, string message, object[] data)
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

            sb.Append(prefix);

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