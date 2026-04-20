namespace WindowGrabber.Models;

/// <summary>
/// Préférences utilisateur persistées en JSON.
/// </summary>
public sealed class AppSettings
{
    public bool LiveThumbnailsEnabled { get; set; } = true;
    public bool ShowConnectionType { get; set; } = true;
    public bool HideSystemWindows { get; set; } = true;
    public bool DarkTheme { get; set; } = true;
    public int WindowWidth { get; set; } = 1280;
    public int WindowHeight { get; set; } = 800;
}
