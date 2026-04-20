using System.IO;

namespace WindowGrabber.Helpers;

/// <summary>
/// Logger fichier minimal et thread-safe. Pas de dépendance externe.
/// Fichier : %APPDATA%\WindowGrabber\logs\windowgrabber.log
/// Rotation simple : un seul fichier, tronqué s'il dépasse 2 MB au démarrage.
/// </summary>
public static class Logger
{
    private static readonly object Gate = new();
    private static readonly string LogDir;
    private static readonly string LogPath;

    static Logger()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        LogDir = Path.Combine(appData, "WindowGrabber", "logs");
        LogPath = Path.Combine(LogDir, "windowgrabber.log");

        try
        {
            Directory.CreateDirectory(LogDir);
            if (File.Exists(LogPath) && new FileInfo(LogPath).Length > 2 * 1024 * 1024)
            {
                File.Delete(LogPath);
            }
        }
        catch
        {
            // si on ne peut pas, on log simplement en console
        }
    }

    public static string LogFilePath => LogPath;

    public static void Info(string message) => Write("INFO", message, null);
    public static void Warn(string message, Exception? ex = null) => Write("WARN", message, ex);
    public static void Error(string message, Exception? ex = null) => Write("ERROR", message, ex);
    public static void Debug(string message) => Write("DEBUG", message, null);

    private static void Write(string level, string message, Exception? ex)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level,-5}] {message}";
        if (ex != null)
            line += $" | {ex.GetType().Name}: {ex.Message}";

        try
        {
            lock (Gate)
            {
                File.AppendAllText(LogPath, line + Environment.NewLine);
            }
        }
        catch
        {
            // on ne fait rien : le log ne doit jamais planter l'app
        }

        System.Diagnostics.Debug.WriteLine(line);
    }
}
