using System.Text;

namespace BattleshipMaui;

internal static class CrashLog
{
    private static readonly object Sync = new();
    private static bool _initialized;

    private static string LogFilePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BattleshipMaui",
            "logs",
            "crash.log");

    public static void Initialize()
    {
        lock (Sync)
        {
            if (_initialized)
                return;

            _initialized = true;
        }

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            Write("AppDomain.UnhandledException", args.ExceptionObject as Exception, $"IsTerminating={args.IsTerminating}");
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Write("TaskScheduler.UnobservedTaskException", args.Exception);
        };

        Write("Initialize", null, $"Process={Environment.ProcessId}");
    }

#if WINDOWS
    public static void HookWinUiUnhandledException(Microsoft.UI.Xaml.Application app)
    {
        app.UnhandledException += (_, args) =>
        {
            Write("WinUI.UnhandledException", args.Exception, $"Handled={args.Handled}");
        };
    }
#endif

    public static void Write(string source, Exception? exception, string? detail = null)
    {
        try
        {
            string? directory = Path.GetDirectoryName(LogFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            var builder = new StringBuilder();
            builder.Append('[').Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")).Append("] ");
            builder.Append(source).AppendLine();

            if (!string.IsNullOrWhiteSpace(detail))
                builder.Append("DETAIL: ").AppendLine(detail);

            if (exception is not null)
            {
                builder.Append("TYPE: ").AppendLine(exception.GetType().FullName);
                builder.Append("MESSAGE: ").AppendLine(exception.Message);
                builder.Append("STACK: ").AppendLine(exception.ToString());
            }

            builder.AppendLine(new string('-', 80));

            lock (Sync)
            {
                File.AppendAllText(LogFilePath, builder.ToString());
            }
        }
        catch
        {
            // Logging must never block app startup or shutdown.
        }
    }
}
