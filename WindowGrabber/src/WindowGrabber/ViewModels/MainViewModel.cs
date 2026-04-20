using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using WindowGrabber.Helpers;
using WindowGrabber.Models;
using WindowGrabber.Services;

namespace WindowGrabber.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly MonitorService _monitorService;
    private readonly WindowService _windowService;
    private readonly WindowMover _windowMover;
    private readonly SettingsService _settingsService;

    public AppSettings Settings { get; }

    public ObservableCollection<MonitorInfo> Monitors { get; } = new();
    public ObservableCollection<WindowItemViewModel> Windows { get; } = new();
    public ICollectionView WindowsView { get; }

    private MonitorInfo? _targetMonitor;
    public MonitorInfo? TargetMonitor
    {
        get => _targetMonitor;
        set
        {
            if (Set(ref _targetMonitor, value))
            {
                Raise(nameof(TargetMonitorLabel));
                Raise(nameof(TargetMonitorDetails));
            }
        }
    }

    public string TargetMonitorLabel => TargetMonitor?.DisplayLabel ?? "—";

    public string TargetMonitorDetails
    {
        get
        {
            if (TargetMonitor == null) return "";
            var m = TargetMonitor;
            var conn = Settings.ShowConnectionType ? $" · {FormatConnection(m.Connection)}" : "";
            return $"{m.Width}×{m.Height}{conn}{(m.IsPrimary ? " · principal" : "")}";
        }
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (Set(ref _searchText, value))
                WindowsView.Refresh();
        }
    }

    private int _sortMode; // 0 = par moniteur, 1 = par titre, 2 = par application
    public int SortMode
    {
        get => _sortMode;
        set
        {
            if (Set(ref _sortMode, value))
                ApplySort();
        }
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => Set(ref _isBusy, value);
    }

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set => Set(ref _statusMessage, value);
    }

    public int WindowCount => Windows.Count;

    public RelayCommand RefreshCommand { get; }
    public RelayCommand OpenSettingsCommand { get; }
    public RelayCommand<WindowItemViewModel> GrabCommand { get; }

    public event EventHandler? OpenSettingsRequested;

    /// <summary>Ventilée par la View pour que le VM sache quelle fenêtre s'exclure (la sienne) et repérer son moniteur.</summary>
    public Func<IntPtr>? GetOwnWindowHandle { get; set; }

    public MainViewModel(
        MonitorService monitorService,
        WindowService windowService,
        WindowMover windowMover,
        SettingsService settingsService,
        AppSettings settings)
    {
        _monitorService = monitorService;
        _windowService = windowService;
        _windowMover = windowMover;
        _settingsService = settingsService;
        Settings = settings;

        WindowsView = CollectionViewSource.GetDefaultView(Windows);
        WindowsView.Filter = FilterPredicate;
        ApplySort();

        RefreshCommand = new RelayCommand(_ => Refresh());
        OpenSettingsCommand = new RelayCommand(_ => OpenSettingsRequested?.Invoke(this, EventArgs.Empty));
        GrabCommand = new RelayCommand<WindowItemViewModel>(Grab);
    }

    private bool FilterPredicate(object? obj)
    {
        if (obj is not WindowItemViewModel vm) return false;
        if (string.IsNullOrWhiteSpace(_searchText)) return true;
        return vm.SearchKey.Contains(_searchText.Trim().ToLowerInvariant(), StringComparison.Ordinal);
    }

    private void ApplySort()
    {
        WindowsView.SortDescriptions.Clear();
        switch (_sortMode)
        {
            case 1:
                WindowsView.SortDescriptions.Add(new SortDescription(nameof(WindowItemViewModel.Title), ListSortDirection.Ascending));
                break;
            case 2:
                WindowsView.SortDescriptions.Add(new SortDescription(nameof(WindowItemViewModel.ProcessName), ListSortDirection.Ascending));
                WindowsView.SortDescriptions.Add(new SortDescription(nameof(WindowItemViewModel.Title), ListSortDirection.Ascending));
                break;
            default:
                WindowsView.SortDescriptions.Add(new SortDescription(nameof(WindowItemViewModel.MonitorIndex), ListSortDirection.Ascending));
                WindowsView.SortDescriptions.Add(new SortDescription(nameof(WindowItemViewModel.ProcessName), ListSortDirection.Ascending));
                break;
        }
    }

    public void Refresh()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Actualisation...";

            // 1. Monitors
            var monitors = _monitorService.GetMonitors(Settings.ShowConnectionType);
            Monitors.Clear();
            foreach (var m in monitors) Monitors.Add(m);

            // 2. Moniteur cible = celui sous la fenêtre de Window Grabber
            var ownHwnd = GetOwnWindowHandle?.Invoke() ?? IntPtr.Zero;
            MonitorInfo? target = null;
            if (ownHwnd != IntPtr.Zero)
                target = _monitorService.GetMonitorForWindow(ownHwnd, monitors);
            target ??= monitors.FirstOrDefault(m => m.IsPrimary) ?? monitors.FirstOrDefault();
            TargetMonitor = target;

            // 3. Fenêtres
            var windows = _windowService.GetVisibleWindows(
                monitors, _monitorService, ownHwnd, Settings.HideSystemWindows);

            Windows.Clear();
            foreach (var w in windows)
            {
                var vm = new WindowItemViewModel(w) { IsOnTargetMonitor = target != null && w.MonitorId == target.Id };
                Windows.Add(vm);
            }

            Raise(nameof(WindowCount));
            StatusMessage = $"{Windows.Count} fenêtre{(Windows.Count > 1 ? "s" : "")} détectée{(Windows.Count > 1 ? "s" : "")}";
        }
        catch (Exception ex)
        {
            Logger.Error("Erreur durant l'actualisation", ex);
            StatusMessage = $"Erreur : {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Grab(WindowItemViewModel? vm)
    {
        if (vm == null || TargetMonitor == null) return;
        var ok = _windowMover.MoveWindowToMonitor(vm.Info, TargetMonitor);
        StatusMessage = ok
            ? $"« {Trim(vm.Title, 60)} » ramenée sur {TargetMonitor.DisplayLabel}"
            : $"Impossible de déplacer « {Trim(vm.Title, 60)} »";
        // Mode léger : on ne ré-énumère pas tout après un grab. L'utilisateur peut
        // cliquer sur « Actualiser » s'il veut voir le nouvel état reflété dans la liste.
    }

    public void SaveSettings() => _settingsService.Save(Settings);

    /// <summary>
    /// Recalcule uniquement le moniteur cible (appelé quand la fenêtre WG bouge).
    /// Pas de ré-énumération des fenêtres — léger.
    /// </summary>
    public void UpdateTargetMonitor()
    {
        if (Monitors.Count == 0) return;
        var ownHwnd = GetOwnWindowHandle?.Invoke() ?? IntPtr.Zero;
        MonitorInfo? target = null;
        if (ownHwnd != IntPtr.Zero)
            target = _monitorService.GetMonitorForWindow(ownHwnd, Monitors);
        target ??= Monitors.FirstOrDefault(m => m.IsPrimary) ?? Monitors.FirstOrDefault();
        if (target != null && target.Id != TargetMonitor?.Id)
            TargetMonitor = target;
    }

    private static string Trim(string s, int max) => s.Length > max ? s[..max] + "…" : s;

    private static string FormatConnection(ConnectionType c) => c switch
    {
        ConnectionType.HDMI => "HDMI",
        ConnectionType.DisplayPort => "DisplayPort",
        ConnectionType.USBC => "USB-C",
        ConnectionType.DVI => "DVI",
        ConnectionType.VGA => "VGA",
        ConnectionType.Internal => "Intégré",
        ConnectionType.Composite => "Composite",
        ConnectionType.Component => "Composantes",
        ConnectionType.SVideo => "S-Video",
        ConnectionType.Other => "Autre",
        _ => "Inconnue"
    };
}

/// <summary>Variante générique de <see cref="RelayCommand"/>.</summary>
public sealed class RelayCommand<T> : System.Windows.Input.ICommand
{
    private readonly Action<T?> _execute;
    private readonly Predicate<T?>? _canExecute;

    public RelayCommand(Action<T?> execute, Predicate<T?>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;
    public void Execute(object? parameter) => _execute((T?)parameter);

    public event EventHandler? CanExecuteChanged
    {
        add => System.Windows.Input.CommandManager.RequerySuggested += value;
        remove => System.Windows.Input.CommandManager.RequerySuggested -= value;
    }
}
