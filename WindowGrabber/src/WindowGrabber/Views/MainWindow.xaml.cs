using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using WindowGrabber.Helpers;
using WindowGrabber.Services;
using WindowGrabber.ViewModels;

namespace WindowGrabber.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private readonly HotKeyService _hotKeyService = new();
    private readonly WindowEventsService _windowEvents = new();
    private readonly DispatcherTimer _moveDebounce;

    public MainWindow()
    {
        InitializeComponent();

        // Composition manuelle (pas de DI container)
        var settingsService = new SettingsService();
        var settings = settingsService.Load();
        var connService = new ConnectionTypeService();
        var monitorService = new MonitorService(connService);
        var windowService = new WindowService();
        var windowMover = new WindowMover();
        var colorService = new MonitorColorService(settings);

        _vm = new MainViewModel(monitorService, windowService, windowMover, settingsService, colorService, settings)
        {
            GetOwnWindowHandle = GetOwnHandle
        };
        DataContext = _vm;

        _vm.OpenSettingsRequested += OnOpenSettingsRequested;

        Width = Math.Max(MinWidth, settings.WindowWidth);
        Height = Math.Max(MinHeight, settings.WindowHeight);

        SizeChanged += (_, _) =>
        {
            _vm.Settings.WindowWidth = (int)Width;
            _vm.Settings.WindowHeight = (int)Height;
        };

        // Debounce pour le déplacement de la fenêtre WG d'un écran à l'autre.
        _moveDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _moveDebounce.Tick += (_, _) =>
        {
            _moveDebounce.Stop();
            if (_vm.Settings.AutoRefreshOnMove) _vm.Refresh();
        };

        Loaded += OnLoaded;
        Closed += OnClosed;
        Activated += OnActivated;
        Deactivated += OnDeactivated;
        LocationChanged += OnLocationChanged;
    }

    private IntPtr GetOwnHandle()
    {
        try { return new WindowInteropHelper(this).EnsureHandle(); }
        catch { return IntPtr.Zero; }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Premier refresh différé — démarrage instantané
        Dispatcher.BeginInvoke(new Action(() => _vm.Refresh()), DispatcherPriority.ContextIdle);

        // Enregistrer le hotkey global si activé
        if (_vm.Settings.HotKeyEnabled && !string.IsNullOrWhiteSpace(_vm.Settings.HotKey))
        {
            _hotKeyService.Register(this, _vm.Settings.HotKey);
            _hotKeyService.Pressed += BringToFront;
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        try { _vm.SaveSettings(); } catch (Exception ex) { Logger.Warn("Save settings au close", ex); }
        _hotKeyService.Dispose();
        _windowEvents.Dispose();
    }

    private void OnActivated(object? sender, EventArgs e)
    {
        // Au premier plan : activer l'écoute des events fenêtre (création/destruction)
        if (_vm.Settings.AutoRefreshOnWindowEvents && !_windowEvents.IsRunning)
        {
            _windowEvents.Start(() => Dispatcher.BeginInvoke(new Action(() => _vm.Refresh()),
                DispatcherPriority.Background));
        }
    }

    private void OnDeactivated(object? sender, EventArgs e)
    {
        // En arrière-plan : libérer complètement l'écoute (pas de consommation inutile)
        _windowEvents.Stop();
    }

    private void OnLocationChanged(object? sender, EventArgs e)
    {
        // Léger : le moniteur cible change vite visuellement
        _vm.UpdateTargetMonitor();
        // Debounce : ré-énumère seulement quand le déplacement s'est calmé
        if (_vm.Settings.AutoRefreshOnMove)
        {
            _moveDebounce.Stop();
            _moveDebounce.Start();
        }
    }

    private void OnOpenSettingsRequested(object? sender, EventArgs e)
    {
        var settingsVm = new SettingsViewModel(new SettingsService(), _vm.Settings, _vm.Monitors);
        var win = new SettingsWindow { Owner = this, DataContext = settingsVm };
        win.ShowDialog();

        // Re-synchroniser les effets dépendant des paramètres
        ApplyHotKeySetting();
        _vm.Refresh();
    }

    private void ApplyHotKeySetting()
    {
        _hotKeyService.Pressed -= BringToFront;
        _hotKeyService.Unregister();
        if (_vm.Settings.HotKeyEnabled && !string.IsNullOrWhiteSpace(_vm.Settings.HotKey))
        {
            _hotKeyService.Register(this, _vm.Settings.HotKey);
            _hotKeyService.Pressed += BringToFront;
        }
    }

    private void BringToFront()
    {
        try
        {
            if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
            Show();
            Activate();
            Topmost = true;
            Topmost = false;
            Focus();
        }
        catch (Exception ex) { Logger.Warn("BringToFront exception", ex); }
    }

    private void Card_CardClicked(object? sender, WindowItemViewModel vm)
    {
        _vm.GrabCommand.Execute(vm);
    }
}
