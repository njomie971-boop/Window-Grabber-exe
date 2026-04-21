namespace WindowGrabber.Models;

/// <summary>
/// Préférences utilisateur persistées en JSON dans %APPDATA%\WindowGrabber\settings.json.
/// Les champs ajoutés sont optionnels et gardent des valeurs par défaut cohérentes avec la V1.
/// </summary>
public sealed class AppSettings
{
    // ==== Affichage ====
    public bool LiveThumbnailsEnabled { get; set; } = false;
    public bool ShowConnectionType { get; set; } = true;
    public bool HideSystemWindows { get; set; } = true;

    // Thème (remplace DarkTheme bool). System = suit les paramètres Windows.
    public ThemeMode ThemeMode { get; set; } = ThemeMode.System;
    [System.Text.Json.Serialization.JsonIgnore]
    public bool DarkTheme { get => ThemeMode == ThemeMode.Dark; set { /* legacy compat */ } }

    // ==== Taille fenêtre ====
    public int WindowWidth { get; set; } = 1280;
    public int WindowHeight { get; set; } = 800;

    // ==== Auto-refresh ====
    public bool AutoRefreshOnWindowEvents { get; set; } = true;
    public bool AutoRefreshOnMove { get; set; } = true;

    // ==== Raccourci global ====
    public bool HotKeyEnabled { get; set; } = false;
    public string HotKey { get; set; } = "Ctrl+Alt+W";

    // ==== Démarrage Windows ====
    public bool RunAtStartup { get; set; } = false;

    // ==== Couleurs par moniteur : clé = DeviceName (\\.\DISPLAYn) → hex "#RRGGBB" ====
    public Dictionary<string, string> MonitorColors { get; set; } = new();
}
