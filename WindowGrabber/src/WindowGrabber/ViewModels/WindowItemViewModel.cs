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
    public WindowDisplayState State => Info.State;

    public string StateLabel => State switch
    {
        WindowDisplayState.Maximized => "Maximisée",
        WindowDisplayState.Minimized => "Minimisée",
        _ => "Normale"
    };

    public bool IsOnTargetMonitor { get; set; }

    public string SearchKey => (Title + " " + ProcessName).ToLowerInvariant();
}
