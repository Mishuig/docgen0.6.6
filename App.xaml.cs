using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using DocumentGenerator.Infrastructure;

namespace DocumentGenerator;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // ── 1. Найраніше можливе логування (ще до AppPaths) ──────────────────
        try
        {
            LogEnvironment();
        }
        catch { /* ignore */ }

        try
        {
            AppLog.Bootstrap("OnStartup: begin");

            // ── 2. Ініціалізація шляхів ───────────────────────────────────────
            AppLog.Bootstrap("OnStartup: AppPaths.Initialize...");
            AppPaths.Initialize();
            AppLog.Bootstrap($"OnStartup: Root = {AppPaths.Root}");
            AppLog.Bootstrap($"OnStartup: Database = {AppPaths.Database}");

            // ── 3. Базовий WPF startup ────────────────────────────────────────
            AppLog.Bootstrap("OnStartup: base.OnStartup...");
            base.OnStartup(e);

            // ── 4. SQLite ────────────────────────────────────────────────────
            AppLog.Bootstrap("OnStartup: EnsureCreated (SQLite)...");
            using (var db = new AppDbContext())
            {
                db.Database.EnsureCreated();
            }
            AppLog.Bootstrap("OnStartup: SQLite OK");

            // ── 5. Глобальні обробники помилок ────────────────────────────────
            DispatcherUnhandledException += (_, args) =>
            {
                AppLog.Write("Unhandled UI exception", args.Exception);
                try
                {
                    MessageBox.Show(
                        $"Сталася неочікувана помилка.\n\n{args.Exception.Message}\n\n" +
                        $"Деталі: logs/application.log\nтакож: startup.log (поруч з EXE)",
                        "Docugen", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch { /* ignore */ }
                args.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                AppLog.Write("Fatal AppDomain exception", args.ExceptionObject as Exception);
            };

            TaskScheduler.UnobservedTaskException += (_, args) =>
            {
                AppLog.Write("Unobserved task exception", args.Exception);
                args.SetObserved();
            };

            AppLog.Bootstrap("OnStartup: complete — MainWindow should appear");
            AppLog.Write("Application started successfully.");
        }
        catch (Exception ex)
        {
            AppLog.Bootstrap("OnStartup FAILED", ex);
            AppLog.Write("Startup failed", ex);

            var details =
                $"Не вдалося запустити Docugen.\n\n" +
                $"{ex.GetType().Name}: {ex.Message}\n\n" +
                $"Лог: {AppLog.StartupLogPath}\n" +
                $"(також logs/application.log якщо папка створена)\n\n" +
                $"Переконайтеся, що папка програми доступна для запису\n" +
                $"і що скопійовано ВСЮ папку (EXE + DLL).";

            try
            {
                MessageBox.Show(details, "Помилка запуску Docugen", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch
            {
                // Якщо MessageBox недоступний — хоча б у лог уже записано
            }

            Shutdown(1);
        }
    }

    /// <summary>Записує діагностику середовища в startup.log поруч із EXE.</summary>
    private static void LogEnvironment()
    {
        var sb = new StringBuilder();
        sb.AppendLine("========== Docugen startup diagnostics ==========");
        sb.AppendLine($"UTC:              {DateTime.UtcNow:O}");
        sb.AppendLine($"Local:            {DateTime.Now:O}");

        try { sb.AppendLine($"ProcessPath:      {Environment.ProcessPath}"); } catch { }
        try { sb.AppendLine($"BaseDirectory:    {AppContext.BaseDirectory}"); } catch { }
        try { sb.AppendLine($"CurrentDirectory:{Environment.CurrentDirectory}"); } catch { }
        try { sb.AppendLine($"CommandLine:      {Environment.CommandLine}"); } catch { }
        try { sb.AppendLine($"OS:               {Environment.OSVersion}"); } catch { }
        try { sb.AppendLine($"Is64BitProcess:   {Environment.Is64BitProcess}"); } catch { }
        try { sb.AppendLine($"Runtime:          {RuntimeInformation.FrameworkDescription}"); } catch { }
        try { sb.AppendLine($"OSArch:           {RuntimeInformation.OSArchitecture}"); } catch { }
        try { sb.AppendLine($"ProcessArch:      {RuntimeInformation.ProcessArchitecture}"); } catch { }

        try
        {
            var asm = Assembly.GetExecutingAssembly();
            sb.AppendLine($"Assembly:         {asm.FullName}");
            sb.AppendLine($"AssemblyLocation: {asm.Location}");
        }
        catch { }

        try
        {
            var processPath = Environment.ProcessPath;
            var dir = !string.IsNullOrEmpty(processPath)
                ? Path.GetDirectoryName(processPath)!
                : AppContext.BaseDirectory;
            sb.AppendLine($"ExeDir:           {dir}");
            sb.AppendLine($"ExeDir exists:    {Directory.Exists(dir)}");
            if (Directory.Exists(dir))
            {
                var files = Directory.GetFiles(dir);
                sb.AppendLine($"Files in ExeDir ({files.Length}):");
                foreach (var f in files.OrderBy(x => x))
                    sb.AppendLine($"  {Path.GetFileName(f)}  ({new FileInfo(f).Length:N0} bytes)");
            }
        }
        catch (Exception ex)
        {
            sb.AppendLine($"ExeDir listing failed: {ex.Message}");
        }

        sb.AppendLine("=================================================");
        sb.AppendLine();

        AppLog.Bootstrap(sb.ToString().TrimEnd());
    }
}
