using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using WindowGrabber.Helpers;
using WindowGrabber.Interop;

namespace WindowGrabber.Controls;

/// <summary>
/// <see cref="HwndHost"/> qui héberge une fenêtre enfant Win32 invisible servant de support à un
/// DWM thumbnail live de la fenêtre source. La miniature se met à jour automatiquement lors de
/// la mise en page WPF. Si DWM n'est pas disponible ou si l'enregistrement échoue, le contrôle
/// s'efface silencieusement — la couche UI affichera l'icône de secours.
/// </summary>
public sealed class DwmThumbnailHost : HwndHost
{
    private const string ChildClassName = "WindowGrabberDwmThumb";
    private const int WS_CHILD = 0x40000000;
    private const int WS_VISIBLE = 0x10000000;

    private IntPtr _thumb = IntPtr.Zero;
    private IntPtr _childHwnd = IntPtr.Zero;
    private IntPtr _parentHwnd = IntPtr.Zero;

    public static readonly DependencyProperty SourceHandleProperty =
        DependencyProperty.Register(nameof(SourceHandle), typeof(IntPtr), typeof(DwmThumbnailHost),
            new PropertyMetadata(IntPtr.Zero, OnSourceHandleChanged));

    public IntPtr SourceHandle
    {
        get => (IntPtr)GetValue(SourceHandleProperty);
        set => SetValue(SourceHandleProperty, value);
    }

    private static void OnSourceHandleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DwmThumbnailHost host)
            host.UpdateThumbnail();
    }

    protected override HandleRef BuildWindowCore(HandleRef hwndParent)
    {
        _parentHwnd = hwndParent.Handle;

        EnsureClassRegistered();

        _childHwnd = CreateWindowEx(
            0, ChildClassName, "", WS_CHILD | WS_VISIBLE,
            0, 0, 1, 1,
            _parentHwnd, IntPtr.Zero, GetModuleHandle(null), IntPtr.Zero);

        if (_childHwnd == IntPtr.Zero)
        {
            Logger.Warn("DwmThumbnailHost: CreateWindowEx a retourné 0");
        }
        else
        {
            UpdateThumbnail();
        }

        return new HandleRef(this, _childHwnd);
    }

    protected override void DestroyWindowCore(HandleRef hwnd)
    {
        UnregisterThumb();
        if (_childHwnd != IntPtr.Zero)
        {
            DestroyWindow(_childHwnd);
            _childHwnd = IntPtr.Zero;
        }
    }

    protected override void OnWindowPositionChanged(Rect rcBoundingBox)
    {
        base.OnWindowPositionChanged(rcBoundingBox);
        UpdateThumbnail();
    }

    protected override Size MeasureOverride(Size constraint) => constraint;

    private void UpdateThumbnail()
    {
        if (_childHwnd == IntPtr.Zero) return;
        if (SourceHandle == IntPtr.Zero)
        {
            UnregisterThumb();
            return;
        }

        try
        {
            if (_thumb == IntPtr.Zero)
            {
                int hr = NativeMethods.DwmRegisterThumbnail(_childHwnd, SourceHandle, out _thumb);
                if (hr != 0 || _thumb == IntPtr.Zero)
                {
                    Logger.Debug($"DwmRegisterThumbnail a échoué (hr=0x{hr:X})");
                    _thumb = IntPtr.Zero;
                    return;
                }
            }

            // Taille source
            NativeMethods.DwmQueryThumbnailSourceSize(_thumb, out var srcSize);

            // Calculer un rect destination préservant le ratio, dans les bornes de l'host
            int targetW = (int)Math.Max(1, ActualWidth);
            int targetH = (int)Math.Max(1, ActualHeight);
            int destW = targetW, destH = targetH;

            if (srcSize.cx > 0 && srcSize.cy > 0)
            {
                double scale = Math.Min((double)targetW / srcSize.cx, (double)targetH / srcSize.cy);
                destW = Math.Max(1, (int)(srcSize.cx * scale));
                destH = Math.Max(1, (int)(srcSize.cy * scale));
            }

            int x = (targetW - destW) / 2;
            int y = (targetH - destH) / 2;

            var props = new DWM_THUMBNAIL_PROPERTIES
            {
                dwFlags = NativeConstants.DWM_TNP_VISIBLE
                        | NativeConstants.DWM_TNP_RECTDESTINATION
                        | NativeConstants.DWM_TNP_OPACITY
                        | NativeConstants.DWM_TNP_SOURCECLIENTAREAONLY,
                rcDestination = new RECT { Left = x, Top = y, Right = x + destW, Bottom = y + destH },
                opacity = 255,
                fVisible = true,
                fSourceClientAreaOnly = false
            };

            NativeMethods.DwmUpdateThumbnailProperties(_thumb, ref props);
        }
        catch (Exception ex)
        {
            Logger.Warn("DwmThumbnailHost.UpdateThumbnail a échoué", ex);
            UnregisterThumb();
        }
    }

    private void UnregisterThumb()
    {
        if (_thumb != IntPtr.Zero)
        {
            try { NativeMethods.DwmUnregisterThumbnail(_thumb); }
            catch { /* ignore */ }
            _thumb = IntPtr.Zero;
        }
    }

    // ====== Win32 helpers locaux (classe fenêtre minimale) ======
    private static bool _classRegistered;
    private static void EnsureClassRegistered()
    {
        if (_classRegistered) return;

        WNDCLASS wc = new()
        {
            lpszClassName = ChildClassName,
            lpfnWndProc = DefWindowProcDelegate,
            hInstance = GetModuleHandle(null)
        };
        RegisterClass(ref wc);
        _classRegistered = true;
    }

    private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    private static readonly WndProc DefWindowProcDelegate = DefWindowProc;

    [DllImport("user32.dll", EntryPoint = "DefWindowProcW")]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", EntryPoint = "CreateWindowExW", CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateWindowEx(int dwExStyle, string lpClassName, string lpWindowName,
        int dwStyle, int x, int y, int nWidth, int nHeight,
        IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll", EntryPoint = "RegisterClassW", CharSet = CharSet.Unicode)]
    private static extern ushort RegisterClass([In] ref WNDCLASS lpWndClass);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASS
    {
        public uint style;
        [MarshalAs(UnmanagedType.FunctionPtr)]
        public WndProc lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? lpszMenuName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpszClassName;
    }
}
