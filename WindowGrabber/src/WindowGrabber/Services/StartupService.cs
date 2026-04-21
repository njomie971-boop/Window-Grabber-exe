using Microsoft.Win32;
using WindowGrabber.Helpers;

namespace WindowGrabber.Services;

/// <summary>
/// Gère l'entrée « Exécuter au démarrage » de Windows pour l'utilisateur courant
/// (HKCU\Software\Microsoft\Windows\CurrentVersion\Run). Réversible, pas de droits admin.
/// </summary>
public static class StartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "WindowGrabber";

    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
            return key?.GetValue(ValueName) is string s && !string.IsNullOrWhiteSpace(s);
        }
        catch (Exception ex) { Logger.Warn("StartupService.IsEnabled", ex); return false; }
    }

    public static void SetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true)
                            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath);
            if (enabled)
            {
                var exe = Environment.ProcessPath ?? System.Reflection.Assembly.GetExecutingAssembly().Location;
                key.SetValue(ValueName, $"\"{exe}\"", RegistryValueKind.String);
                Logger.Info($"StartupService: activé → {exe}");
            }
            else
            {
                key.DeleteValue(ValueName, false);
                Logger.Info("StartupService: désactivé");
            }
        }
        catch (Exception ex) { Logger.Warn("StartupService.SetEnabled", ex); }
    }
}
