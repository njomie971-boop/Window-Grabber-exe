using System.Windows;
using System.Windows.Media.Imaging;

namespace WindowGrabber.Models;

/// <summary>
/// Description d'une fenêtre top-level capturable.
/// Immuable : un nouveau <see cref="WindowInfo"/> est produit à chaque actualisation.
/// </summary>
public sealed class WindowInfo
{
    public required IntPtr Handle { get; init; }
    public required string Title { get; init; }
    public required string ProcessName { get; init; }
    public string? ProcessPath { get; init; }
    public required uint ProcessId { get; init; }
    public required Rect Bounds { get; init; }
    public required WindowDisplayState State { get; init; }
    public required string MonitorId { get; init; }
    public required string MonitorLabel { get; init; }
    public required int MonitorIndex { get; init; }
    public BitmapSource? Icon { get; init; }
    public string ClassName { get; init; } = string.Empty;
}
