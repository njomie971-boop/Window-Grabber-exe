using System.Windows;

namespace WindowGrabber.Models;

/// <summary>
/// Description d'un moniteur physique détecté. Coordonnées en pixels (espace virtuel Windows).
/// </summary>
public sealed class MonitorInfo
{
    /// <summary>Identifiant interne stable dans la session.</summary>
    public required string Id { get; init; }

    /// <summary>1-based pour affichage humain ("Moniteur 1", "Moniteur 2"...).</summary>
    public required int Index { get; init; }

    /// <summary>Nom technique du device (ex: \\.\DISPLAY1).</summary>
    public required string DeviceName { get; init; }

    /// <summary>Nom lisible (ex: "Dell U2720Q") si disponible, sinon DeviceName.</summary>
    public string FriendlyName { get; set; } = string.Empty;

    /// <summary>Rectangle du moniteur dans l'espace virtuel.</summary>
    public required Rect Bounds { get; init; }

    /// <summary>Zone de travail (exclut la barre des tâches, etc.).</summary>
    public required Rect WorkArea { get; init; }

    public required bool IsPrimary { get; init; }

    public ConnectionType Connection { get; set; } = ConnectionType.Unknown;

    /// <summary>Handle HMONITOR (utilisé uniquement par la couche interop).</summary>
    internal IntPtr HMonitor { get; init; }

    public int Width => (int)Bounds.Width;
    public int Height => (int)Bounds.Height;

    public string DisplayLabel => $"Moniteur {Index}";
}
