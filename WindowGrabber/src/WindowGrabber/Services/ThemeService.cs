using Microsoft.Win32;
using System.Windows;
using WindowGrabber.Helpers;
using WindowGrabber.Models;

namespace WindowGrabber.Services;

/// <summary>
/// Charge au démarrage le thème (Dark / Light / System) dans
/// <see cref="Application.Resources"/>. Le changement de thème à l'exécution
/// nécessite un redémarrage — volontairement simple pour la V1.
/// </summary>
public static class ThemeService
{
    private const string DarkUri = "/WindowGrabber;component/Themes/DarkTheme.xaml";
    private const string LightUri = "/WindowGrabber;component/Themes/LightTheme.xaml";

    public static ThemeMode Resolve(ThemeMode mode)
    {
        if (mode != ThemeMode.System) return mode;
        return IsSystemLight() ? ThemeMode.Light : ThemeMode.Dark;
    }

    public static bool IsSystemLight()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var v = key?.GetValue("AppsUseLightTheme");
            if (v is int i) return i == 1;
        }
        catch (Exception ex) { Logger.Debug("IsSystemLight registry error: " + ex.Message); }
        return false;
    }

    public static void Apply(ThemeMode mode)
    {
        var resolved = Resolve(mode);
        var uri = new Uri(resolved == ThemeMode.Light ? LightUri : DarkUri, UriKind.Relative);
        var dict = new ResourceDictionary { Source = uri };

        var app = Application.Current;
        // Retire tout ancien dictionnaire de thème (repéré par la clé "BgBrush")
        for (int i = app.Resources.MergedDictionaries.Count - 1; i >= 0; i--)
        {
            if (app.Resources.MergedDictionaries[i].Contains("BgBrush"))
                app.Resources.MergedDictionaries.RemoveAt(i);
        }
        app.Resources.MergedDictionaries.Add(dict);
        Logger.Info($"Thème appliqué : {resolved} (mode configuré : {mode})");
    }
}
