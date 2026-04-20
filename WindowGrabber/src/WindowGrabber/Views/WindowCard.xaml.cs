using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WindowGrabber.Models;
using WindowGrabber.ViewModels;

namespace WindowGrabber.Views;

public partial class WindowCard : UserControl
{
    public static readonly DependencyProperty LiveThumbnailsProperty =
        DependencyProperty.Register(nameof(LiveThumbnails), typeof(bool), typeof(WindowCard),
            new PropertyMetadata(true, OnLiveThumbnailsChanged));

    public bool LiveThumbnails
    {
        get => (bool)GetValue(LiveThumbnailsProperty);
        set => SetValue(LiveThumbnailsProperty, value);
    }

    public event EventHandler<WindowItemViewModel>? CardClicked;

    public WindowCard()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => RefreshThumbnail();
    private void OnUnloaded(object sender, RoutedEventArgs e) => Thumbnail.SourceHandle = IntPtr.Zero;
    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e) => RefreshThumbnail();

    private static void OnLiveThumbnailsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WindowCard c) c.RefreshThumbnail();
    }

    private void RefreshThumbnail()
    {
        if (DataContext is not WindowItemViewModel vm)
        {
            Thumbnail.SourceHandle = IntPtr.Zero;
            return;
        }

        // Les fenêtres minimisées n'ont pas de thumbnail DWM utile → on se contente du fallback (toujours visible)
        bool canThumb = LiveThumbnails && vm.State != WindowDisplayState.Minimized;
        Thumbnail.SourceHandle = canThumb ? vm.Handle : IntPtr.Zero;
    }

    private void Root_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is WindowItemViewModel vm)
            CardClicked?.Invoke(this, vm);
    }
}
