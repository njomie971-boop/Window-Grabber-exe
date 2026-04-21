using System.Windows.Media;
using System.Windows.Media.Imaging;
using WindowGrabber.Models;

namespace WindowGrabber.ViewModels;

public sealed class WindowItemViewModel : ViewModelBase
{
    public WindowInfo Info { get; }

    public WindowItemViewModel(WindowInfo info)
    {
        Info = info;
    }

    public IntPtr Handle => Info.Handle;
    public string Title => Info.Title;
    public string ProcessName => Info.ProcessName;
    public BitmapSource? Icon => Info.Icon;
    public string MonitorLabel => Info.MonitorLabel;
    public int MonitorIndex => Info.MonitorIndex;
    public string MonitorDeviceName => Info.MonitorDeviceName;
    public WindowDisplayState State => Info.State;

    public string StateLabel => State switch
    {
        WindowDisplayState.Maximized => "Maximisée",
        WindowDisplayState.Minimized => "Minimisée",
        _ => "Normale"
    };

    public bool IsOnTargetMonitor { get; set; }

    /// <summary>Couleur de l'accent du moniteur porteur de la fenêtre (assignée par MainViewModel).</summary>
    private Brush _monitorBrush = Brushes.DeepSkyBlue;
    public Brush MonitorBrush
    {
        get => _monitorBrush;
        set => Set(ref _monitorBrush, value);
    }

    public string SearchKey => (Title + " " + ProcessName).ToLowerInvariant();
}
