using System.Windows.Media;
using WindowGrabber.Models;

namespace WindowGrabber.Services;

/// <summary>
/// Attribue une couleur d'accent à chaque moniteur. La couleur est stockée dans
/// <see cref="AppSettings.MonitorColors"/> par <c>DeviceName</c> (clé stable entre
/// sessions). Si aucune couleur n'est définie pour un moniteur, on en assigne une
/// depuis la palette par défaut (en rotation sur l'index).
/// </summary>
public sealed class MonitorColorService
{
    public static readonly string[] DefaultPalette =
    {
        "#3FA7FF", // Bleu
        "#2EE6A5", // Vert
        "#FFA24C", // Orange
        "#E06BFF", // Violet
        "#FF6B8A", // Rose
        "#F7D65A", // Jaune
        "#4ED6C5", // Teal
        "#B4E04C"  // Lime
    };

    private readonly AppSettings _settings;

    public MonitorColorService(AppSettings settings) { _settings = settings; }

    public Color GetColor(MonitorInfo monitor)
    {
        return GetColor(monitor.DeviceName, monitor.Index);
    }

    public Color GetColor(string deviceName, int index)
    {
        if (_settings.MonitorColors.TryGetValue(deviceName, out var hex) && TryParseHex(hex, out var c))
            return c;

        var fallback = DefaultPalette[(Math.Max(1, index) - 1) % DefaultPalette.Length];
        return TryParseHex(fallback, out var fc) ? fc : Colors.DeepSkyBlue;
    }

    public void SetColor(string deviceName, string hex)
    {
        if (string.IsNullOrWhiteSpace(deviceName) || !TryParseHex(hex, out _)) return;
        _settings.MonitorColors[deviceName] = hex;
    }

    public static bool TryParseHex(string hex, out Color color)
    {
        color = default;
        try
        {
            var obj = ColorConverter.ConvertFromString(hex);
            if (obj is Color c) { color = c; return true; }
        }
        catch { }
        return false;
    }
}
