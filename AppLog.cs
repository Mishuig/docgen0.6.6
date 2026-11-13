using System.IO;
using System.Text;

namespace DocumentGenerator.Infrastructure;

/// <summary>
/// Журнал. Пише в logs/application.log після ініціалізації AppPaths.
/// До цього (і як fallback) — у startup.log поруч із EXE.
/// Ніколи не кидає винятків.
/// </summary>
public static class AppLog
{
    private static readonly object Gate = new();
    private static string? _startupLogPath;

    /// <summary>Шлях до startup.log поруч із EXE (доступний ще до AppPaths.Initialize).</summary>
    public static string StartupLogPath
    {
        get
        {
            if (_startupLogPath is not null) return _startupLogPath;
            try
            {
                var processPath = Environment.ProcessPath;
                var dir = !string.IsNullOrEmpty(processPath)
                    ? Path.GetDirectoryName(processPath)!
                    : AppContext.BaseDirectory;
                _startupLogPath = Path.Combine(dir, "startup.log");
            }
            catch
            {
                _startupLogPath = Path.Combine(Path.GetTempPath(), "Docugen-startup.log");
            }
            return _startupLogPath;
        }
    }

    public static void Bootstrap(string message, Exception? ex = null)
    {
        try
        {
            lock (Gate)
            {
                var sb = new StringBuilder();
                sb.Append(DateTime.UtcNow.ToString("O"));
                sb.Append(" | ");
                sb.AppendLine(message);
                if (ex is not null)
                {
                    sb.AppendLine(ex.ToString());
                    sb.AppendLine();
                }
                File.AppendAllText(StartupLogPath, sb.ToString());
            }
        }
        catch { /* ignore */ }
    }

    public static void Write(string message, Exception? ex = null)
    {
        try
        {
            lock (Gate)
            {
                var text = ex is null
                    ? $"{DateTime.UtcNow:O} | {message}\n\n"
                    : $"{DateTime.UtcNow:O} | {message}\n{ex}\n\n";

                // Основний лог (якщо папка вже є)
                try
                {
                    if (!string.IsNullOrEmpty(AppPaths.Root) && Directory.Exists(AppPaths.Logs))
                        File.AppendAllText(Path.Combine(AppPaths.Logs, "application.log"), text);
                }
                catch { /* ignore */ }

                // Завжди дублюємо в startup.log поруч із EXE
                File.AppendAllText(StartupLogPath, text);
            }
        }
        catch { /* журнал не повинен ламати програму */ }
    }
}
