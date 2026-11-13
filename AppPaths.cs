using System.IO;

namespace DocumentGenerator.Infrastructure;

public static class AppPaths
{
    public static string Root { get; private set; } = AppContext.BaseDirectory;
    public static string Data => Path.Combine(Root, "data");
    public static string Templates => Path.Combine(Root, "templates");
    public static string Exports => Path.Combine(Root, "exports");
    public static string Backups => Path.Combine(Root, "backups");
    public static string Cache => Path.Combine(Root, "cache");
    public static string Logs => Path.Combine(Root, "logs");
    public static string Database => Path.Combine(Data, "document-generator.db");

    public static void Initialize()
    {
        // Portable / single-file: data must live next to the EXE, not in the
        // temporary extraction folder that AppContext.BaseDirectory points to.
        var processPath = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(processPath))
            Root = Path.GetDirectoryName(processPath)!;
        else
            Root = AppContext.BaseDirectory;

        foreach (var folder in new[] { Data, Templates, Exports, Backups, Logs, Cache })
            Directory.CreateDirectory(folder);

        // Fail fast if the folder is read-only (e.g. Program Files without elevation).
        var probe = Path.Combine(Data, $".write-test-{Guid.NewGuid():N}");
        File.WriteAllText(probe, "ok");
        File.Delete(probe);
    }

    public static string ResolveTemplate(string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(Root, relativePath));
        var rootPath = Path.GetFullPath(Templates) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Некоректний шлях до шаблону.");
        return fullPath;
    }
}
