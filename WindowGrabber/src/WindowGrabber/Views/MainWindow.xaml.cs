using System.Windows;
using System.Windows.Interop;
using WindowGrabber.Helpers;
using WindowGrabber.Services;
using WindowGrabber.ViewModels;

namespace WindowGrabber.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow()
    {
        InitializeComponent();

        // Composition manuelle (pas de DI container pour éviter une dépendance supplémentaire sur V1)
        var settingsService = new SettingsService();
        var settings = settingsService.Load();
        var connService = new ConnectionTypeService();
        var monitorService = new MonitorService(connService);
        var windowService = new WindowService();
        var windowMover = new WindowMover();

        _vm = new MainViewModel(monitorService, windowService, windowMover, settingsService, settings)
        {
            GetOwnWindowHandle = GetOwnHandle
        };
        DataContext = _vm;

        _vm.OpenSettingsRequested += OnOpenSettingsRequested;

        Width = Math.Max(MinWidth, settings.WindowWidth);
        Height = Math.Max(MinHeight, settings.WindowHeight);

        // Taille initiale persistée
        SizeChanged += (_, _) =>
        {
            _vm.Settings.WindowWidth = (int)Width;
            _vm.Settings.WindowHeight = (int)Height;
        };

        Loaded += OnLoaded;
        Closed += OnClosed;
        LocationChanged += OnLocationChanged;
    }

    private void OnLocationChanged(object? sender, EventArgs e)
    {
        // Léger : on ne recalcule que le moniteur cible (pas la liste des fenêtres)
        _vm.UpdateTargetMonitor();
    }

    private IntPtr GetOwnHandle()
    {
        try
        {
            return new WindowInteropHelper(this).EnsureHandle();
        }
        catch
        {
            return IntPtr.Zero;
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _vm.Refresh();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        try { _vm.SaveSettings(); } catch (Exception ex) { Logger.Warn("Save settings au close", ex); }
    }

    private void OnOpenSettingsRequested(object? sender, EventArgs e)
    {
        var win = new SettingsWindow
        {
            Owner = this,
            DataContext = new SettingsViewModel(new SettingsService(), _vm.Settings)
        };
        win.ShowDialog();
        _vm.Refresh();
    }

    private void Card_CardClicked(object? sender, WindowItemViewModel vm)
    {
        _vm.GrabCommand.Execute(vm);
    }
}
