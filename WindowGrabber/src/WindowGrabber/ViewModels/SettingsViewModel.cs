using WindowGrabber.Helpers;
using WindowGrabber.Models;
using WindowGrabber.Services;

namespace WindowGrabber.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    private readonly SettingsService _service;
    public AppSettings Settings { get; }

    public SettingsViewModel(SettingsService service, AppSettings settings)
    {
        _service = service;
        Settings = settings;
        SaveCommand = new RelayCommand(_ => _service.Save(Settings));
    }

    public bool LiveThumbnailsEnabled
    {
        get => Settings.LiveThumbnailsEnabled;
        set { if (Settings.LiveThumbnailsEnabled != value) { Settings.LiveThumbnailsEnabled = value; Raise(); _service.Save(Settings); } }
    }

    public bool ShowConnectionType
    {
        get => Settings.ShowConnectionType;
        set { if (Settings.ShowConnectionType != value) { Settings.ShowConnectionType = value; Raise(); _service.Save(Settings); } }
    }

    public bool HideSystemWindows
    {
        get => Settings.HideSystemWindows;
        set { if (Settings.HideSystemWindows != value) { Settings.HideSystemWindows = value; Raise(); _service.Save(Settings); } }
    }

    public bool DarkTheme
    {
        get => Settings.DarkTheme;
        set { if (Settings.DarkTheme != value) { Settings.DarkTheme = value; Raise(); _service.Save(Settings); } }
    }

    public string LogFilePath => Logger.LogFilePath;

    public RelayCommand SaveCommand { get; }
}
