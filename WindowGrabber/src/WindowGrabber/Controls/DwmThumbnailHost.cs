using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using WindowGrabber.Helpers;
using WindowGrabber.Interop;

namespace WindowGrabber.Controls;

/// <summary>
/// Réserve un rectangle dans la mise en page WPF et demande à DWM (Desktop Window Manager) d'y
/// peindre la miniature live de la fenêtre source. DWM compose au-dessus de WPF, donc l'élément
/// lui-même ne rend rien visuellement — il sert uniquement de placeholder de layout.
///
/// Important : DWM exige une HWND *top-level* en destination. On utilise donc la HWND de la
/// <see cref="Window"/> WPF parente (via <see cref="PresentationSource"/>) et on calcule le
/// rectangle dans ses coordonnées client en pixels device.
///
/// Toutes les erreurs sont avalées et journalisées : si DWM refuse l'enregistrement (hr != 0,
/// ex. hr=0x80070057 E_INVALIDARG), le composant reste silencieux et le fallback (icône dessous
/// dans la grille WPF) apparaît naturellement.
/// </summary>
public sealed class DwmThumbnailHost : FrameworkElement
{
    private IntPtr _thumb = IntPtr.Zero;
    private HwndSource? _hwndSource;
    private bool _registered;

    public static readonly DependencyProperty SourceHandleProperty =
        DependencyProperty.Register(nameof(SourceHandle), typeof(IntPtr), typeof(DwmThumbnailHost),
            new PropertyMetadata(IntPtr.Zero, OnSourceHandleChanged));

    public IntPtr SourceHandle
    {
        get => (IntPtr)GetValue(SourceHandleProperty);
        set => SetValue(SourceHandleProperty, value);
    }

    public DwmThumbnailHost()
    {
        // Transparent pour les clics — on laisse WPF gérer les couches dessous (fallback)
        IsHitTestVisible = false;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        LayoutUpdated += OnLayoutUpdated;
        IsVisibleChanged += OnIsVisibleChanged;
    }

    private static void OnSourceHandleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DwmThumbnailHost host)
            host.Resync();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _hwndSource = PresentationSource.FromVisual(this) as HwndSource;
        Resync();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Unregister();
        _hwndSource = null;
    }

    private void OnLayoutUpdated(object? sender, EventArgs e) => UpdateThumbnailRect();

    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) => UpdateThumbnailRect();

    /// <summary>
    /// Désinscrit puis réinscrit le thumbnail si nécessaire. Appelé quand la fenêtre source
    /// change ou quand l'élément est chargé.
    /// </summary>
    private void Resync()
    {
        // Toujours désinscrire avant : source ou state a pu changer
        Unregister();

        if (_hwndSource == null || _hwndSource.Handle == IntPtr.Zero) return;
        if (SourceHandle == IntPtr.Zero) return;
        if (!NativeMethods.IsWindow(SourceHandle)) return;

        try
        {
            int hr = NativeMethods.DwmRegisterThumbnail(_hwndSource.Handle, SourceHandle, out _thumb);
            if (hr != 0 || _thumb == IntPtr.Zero)
            {
                // hr=0x80070057 (E_INVALIDARG) peut arriver sur certaines fenêtres UWP protégées
                // ou quand la source est cloakée. On reste silencieux : le fallback WPF prend le relais.
                Logger.Debug($"DwmRegisterThumbnail ignoré (hr=0x{hr:X}) pour hwnd=0x{SourceHandle.ToInt64():X}");
                _thumb = IntPtr.Zero;
                _registered = false;
                return;
            }

            _registered = true;
            UpdateThumbnailRect();
        }
        catch (Exception ex)
        {
            Logger.Debug($"DwmRegisterThumbnail exception ignorée: {ex.Message}");
            _thumb = IntPtr.Zero;
            _registered = false;
        }
    }

    private void UpdateThumbnailRect()
    {
        if (!_registered || _thumb == IntPtr.Zero || _hwndSource == null) return;

        try
        {
            // Si pas attaché / pas visible / taille nulle → masquer le thumbnail mais le garder enregistré
            bool visible = IsVisible && ActualWidth > 1 && ActualHeight > 1 && PresentationSource.FromVisual(this) != null;

            RECT destRect = default;
            if (visible)
            {
                var root = _hwndSource.RootVisual as UIElement;
                if (root == null) { SetVisibility(false); return; }

                // Rect de notre élément dans le repère de la Window WPF (en DIPs)
                var transform = TransformToAncestor(root);
                var dipRect = transform.TransformBounds(new Rect(0, 0, ActualWidth, ActualHeight));

                // Si totalement hors de la window, masquer
                var windowBounds = new Rect(0, 0, root.RenderSize.Width, root.RenderSize.Height);
                if (!windowBounds.IntersectsWith(dipRect))
                {
                    SetVisibility(false);
                    return;
                }

                // Clip au bord de la Window pour ne jamais peindre à côté
                dipRect.Intersect(windowBounds);

                // Conversion DIP → device pixels (DPI aware)
                var m = _hwndSource.CompositionTarget?.TransformToDevice ?? Matrix.Identity;
                var tl = m.Transform(new Point(dipRect.Left, dipRect.Top));
                var br = m.Transform(new Point(dipRect.Right, dipRect.Bottom));

                // Taille source (si 0, la fenêtre est minimisée ou cloakée → masquer)
                NativeMethods.DwmQueryThumbnailSourceSize(_thumb, out var srcSize);
                if (srcSize.cx <= 0 || srcSize.cy <= 0)
                {
                    SetVisibility(false);
                    return;
                }

                // Rectangle cible préservant le ratio, centré dans dipRect (en device pixels)
                int destW = (int)Math.Max(1, br.X - tl.X);
                int destH = (int)Math.Max(1, br.Y - tl.Y);
                double scale = Math.Min((double)destW / srcSize.cx, (double)destH / srcSize.cy);
                int fittedW = Math.Max(1, (int)(srcSize.cx * scale));
                int fittedH = Math.Max(1, (int)(srcSize.cy * scale));

                int fittedX = (int)tl.X + (destW - fittedW) / 2;
                int fittedY = (int)tl.Y + (destH - fittedH) / 2;

                destRect = new RECT
                {
                    Left = fittedX,
                    Top = fittedY,
                    Right = fittedX + fittedW,
                    Bottom = fittedY + fittedH
                };
            }
            else
            {
                SetVisibility(false);
                return;
            }

            var props = new DWM_THUMBNAIL_PROPERTIES
            {
                dwFlags = NativeConstants.DWM_TNP_VISIBLE
                        | NativeConstants.DWM_TNP_RECTDESTINATION
                        | NativeConstants.DWM_TNP_OPACITY
                        | NativeConstants.DWM_TNP_SOURCECLIENTAREAONLY,
                rcDestination = destRect,
                opacity = 255,
                fVisible = true,
                fSourceClientAreaOnly = false
            };

            int hr = NativeMethods.DwmUpdateThumbnailProperties(_thumb, ref props);
            if (hr != 0)
            {
                Logger.Debug($"DwmUpdateThumbnailProperties hr=0x{hr:X}");
                // Si l'update échoue trop souvent, on désinscrit proprement
                Unregister();
            }
        }
        catch (Exception ex)
        {
            Logger.Debug($"UpdateThumbnailRect exception ignorée: {ex.Message}");
            Unregister();
        }
    }

    private void SetVisibility(bool visible)
    {
        if (!_registered || _thumb == IntPtr.Zero) return;
        try
        {
            var props = new DWM_THUMBNAIL_PROPERTIES
            {
                dwFlags = NativeConstants.DWM_TNP_VISIBLE,
                fVisible = visible
            };
            NativeMethods.DwmUpdateThumbnailProperties(_thumb, ref props);
        }
        catch { /* ignore */ }
    }

    private void Unregister()
    {
        if (_thumb != IntPtr.Zero)
        {
            try { NativeMethods.DwmUnregisterThumbnail(_thumb); }
            catch { /* ignore */ }
            _thumb = IntPtr.Zero;
        }
        _registered = false;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // Ne consomme aucune place par lui-même — la Grid parente donne la taille
        return new Size(
            double.IsPositiveInfinity(availableSize.Width) ? 0 : availableSize.Width,
            double.IsPositiveInfinity(availableSize.Height) ? 0 : availableSize.Height);
    }
}
