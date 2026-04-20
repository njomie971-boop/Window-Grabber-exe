using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
            Thumbnail.Visibility = Visibility.Collapsed;
            Fallback.Visibility = Visibility.Visible;
            return;
        }

        // Les fenêtres minimisées ne rendent pas de thumbnail DWM utile → fallback icône
        bool canThumb = LiveThumbnails && vm.State != Models.WindowDisplayState.Minimized;
        Thumbnail.SourceHandle = canThumb ? vm.Handle : IntPtr.Zero;
        Thumbnail.Visibility = canThumb ? Visibility.Visible : Visibility.Collapsed;
        Fallback.Visibility = canThumb ? Visibility.Collapsed : Visibility.Visible;
    }

    private void Root_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is WindowItemViewModel vm)
            CardClicked?.Invoke(this, vm);
    }
}
