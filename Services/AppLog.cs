using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace ImageReorder.Services;

internal static class AppLog
{
    private static readonly object Gate = new();

    static AppLog()
    {
        LogDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ImageReorder",
            "logs");
        LogFilePath = Path.Combine(LogDirectory, $"imagereorder-{DateTime.Now:yyyyMMdd}.log");
        IsTraceEnabled = string.Equals(
            Environment.GetEnvironmentVariable("IMAGEREORDER_TRACE"),
            "1",
            StringComparison.Ordinal);
    }

    public static string LogDirectory { get; }

    public static string LogFilePath { get; }

    /// <summary>Per-mouse-move drag tracing. Enable by setting IMAGEREORDER_TRACE=1.</summary>
    public static bool IsTraceEnabled { get; }

    public static void StartSession()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
        Write("INFO", $"--- Session start · v{version} · .NET {Environment.Version} · PID {Environment.ProcessId} · trace={IsTraceEnabled} ---");
    }

    public static void Info(string message) => Write("INFO", message);

    public static void Trace(string message)
    {
        if (IsTraceEnabled)
        {
            Write("TRACE", message);
        }
    }

    public static void Warn(string message, Exception? exception = null) => Write("WARN", message, exception);

    public static void Error(string message, Exception? exception = null) => Write("ERROR", message, exception);

    public static void Fatal(string message, Exception? exception = null) => Write("FATAL", message, exception);

    private static void Write(string level, string message, Exception? exception = null)
    {
        var builder = new StringBuilder()
            .Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"))
            .Append(" [").Append(level).Append("] ")
            .Append("t").Append(Environment.CurrentManagedThreadId).Append(' ')
            .Append(message);

        if (exception is not null)
        {
            builder.AppendLine().Append(exception);
        }

        var line = builder.ToString();
        Debug.WriteLine(line);

        lock (Gate)
        {
            try
            {
                Directory.CreateDirectory(LogDirectory);
                File.AppendAllText(LogFilePath, line + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception)
            {
                // Logging must never be the reason the app fails.
            }
        }
    }
}
