using System.Windows;
using System.Windows.Threading;
using WindowGrabber.Helpers;

namespace WindowGrabber;

public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Logger.Info($"Window Grabber démarre (v{typeof(App).Assembly.GetName().Version})");
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Logger.Error("Exception non gérée (UI)", e.Exception);
        MessageBox.Show(
            "Une erreur inattendue est survenue.\n\nDétails journalisés dans :\n" + Logger.LogFilePath,
            "Window Grabber",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    private static void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Logger.Error("Exception non gérée (AppDomain)", e.ExceptionObject as Exception);
    }
}
