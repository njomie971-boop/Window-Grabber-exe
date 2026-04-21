using System.Collections.ObjectModel;
using System.Windows.Media;
using WindowGrabber.Helpers;
using WindowGrabber.Models;
using WindowGrabber.Services;

namespace WindowGrabber.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    private readonly SettingsService _service;
    public AppSettings Settings { get; }

    public ObservableCollection<MonitorColorEntry> MonitorColorEntries { get; } = new();

    public SettingsViewModel(SettingsService service, AppSettings settings,
        IEnumerable<MonitorInfo> monitors)
    {
        _service = service;
        Settings = settings;
        SaveCommand = new RelayCommand(_ => _service.Save(Settings));
        ResetColorsCommand = new RelayCommand(_ => ResetColors());

        int i = 0;
        foreach (var m in monitors)
        {
            var defaultHex = MonitorColorService.DefaultPalette[i % MonitorColorService.DefaultPalette.Length];
            if (!Settings.MonitorColors.TryGetValue(m.DeviceName, out var hex))
                hex = defaultHex;
            MonitorColorEntries.Add(new MonitorColorEntry(m, hex, h =>
            {
                Settings.MonitorColors[m.DeviceName] = h;
                _service.Save(Settings);
            }));
            i++;
        }
    }

    public RelayCommand SaveCommand { get; }
    public RelayCommand ResetColorsCommand { get; }

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

    public bool AutoRefreshOnWindowEvents
    {
        get => Settings.AutoRefreshOnWindowEvents;
        set { if (Settings.AutoRefreshOnWindowEvents != value) { Settings.AutoRefreshOnWindowEvents = value; Raise(); _service.Save(Settings); } }
    }

    public bool AutoRefreshOnMove
    {
        get => Settings.AutoRefreshOnMove;
        set { if (Settings.AutoRefreshOnMove != value) { Settings.AutoRefreshOnMove = value; Raise(); _service.Save(Settings); } }
    }

    // ThemeMode : index 0=System, 1=Dark, 2=Light (aligné avec l'enum)
    public int ThemeIndex
    {
        get => (int)Settings.ThemeMode;
        set
        {
            var mode = (ThemeMode)value;
            if (Settings.ThemeMode != mode)
            {
                Settings.ThemeMode = mode;
                Raise();
                _service.Save(Settings);
            }
        }
    }

    public bool HotKeyEnabled
    {
        get => Settings.HotKeyEnabled;
        set { if (Settings.HotKeyEnabled != value) { Settings.HotKeyEnabled = value; Raise(); _service.Save(Settings); } }
    }

    public string HotKey
    {
        get => Settings.HotKey;
        set { if (Settings.HotKey != value) { Settings.HotKey = value ?? "Ctrl+Alt+W"; Raise(); _service.Save(Settings); } }
    }

    public bool RunAtStartup
    {
        get => Settings.RunAtStartup;
        set
        {
            if (Settings.RunAtStartup != value)
            {
                Settings.RunAtStartup = value;
                Raise();
                try { StartupService.SetEnabled(value); } catch (Exception ex) { Logger.Warn("Startup toggle", ex); }
                _service.Save(Settings);
            }
        }
    }

    public string LogFilePath => Logger.LogFilePath;

    private void ResetColors()
    {
        Settings.MonitorColors.Clear();
        for (int i = 0; i < MonitorColorEntries.Count; i++)
        {
            var hex = MonitorColorService.DefaultPalette[i % MonitorColorService.DefaultPalette.Length];
            MonitorColorEntries[i].SetHex(hex);
        }
        _service.Save(Settings);
    }
}

/// <summary>
/// Un moniteur + sa couleur associée + la palette disponible pour la choisir.
/// </summary>
public sealed class MonitorColorEntry : ViewModelBase
{
    private readonly Action<string> _onChange;
    public MonitorInfo Monitor { get; }
    public ObservableCollection<ColorSwatch> Swatches { get; } = new();

    public MonitorColorEntry(MonitorInfo monitor, string hex, Action<string> onChange)
    {
        Monitor = monitor;
        _onChange = onChange;
        _currentHex = hex;

        foreach (var s in MonitorColorService.DefaultPalette)
        {
            if (MonitorColorService.TryParseHex(s, out var c))
            {
                var brush = new SolidColorBrush(c);
                brush.Freeze();
                Swatches.Add(new ColorSwatch(s, brush, this));
            }
        }
    }

    private string _currentHex;
    public string CurrentHex
    {
        get => _currentHex;
        private set
        {
            if (Set(ref _currentHex, value))
            {
                Raise(nameof(CurrentBrush));
                foreach (var s in Swatches) s.Refresh();
            }
        }
    }

    public Brush CurrentBrush
    {
        get
        {
            MonitorColorService.TryParseHex(_currentHex, out var c);
            var b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }
    }

    public void SetHex(string hex)
    {
        CurrentHex = hex;
        _onChange(hex);
    }
}

public sealed class ColorSwatch : ViewModelBase
{
    private readonly MonitorColorEntry _parent;
    public string Hex { get; }
    public Brush SwatchBrush { get; }
    public RelayCommand PickCommand { get; }

    public ColorSwatch(string hex, Brush brush, MonitorColorEntry parent)
    {
        Hex = hex;
        SwatchBrush = brush;
        _parent = parent;
        PickCommand = new RelayCommand(_ => _parent.SetHex(hex));
    }

    public bool IsSelected => string.Equals(Hex, _parent.CurrentHex, StringComparison.OrdinalIgnoreCase);

    public void Refresh() => Raise(nameof(IsSelected));
}
