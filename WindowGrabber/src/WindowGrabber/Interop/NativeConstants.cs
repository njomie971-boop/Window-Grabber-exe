// ReSharper disable InconsistentNaming
namespace WindowGrabber.Interop;

/// <summary>
/// Constantes Win32 utilisées par l'application. Regroupées ici pour isolement strict
/// de toute la couche interop.
/// </summary>
internal static class NativeConstants
{
    // ShowWindow (nCmdShow)
    public const int SW_HIDE = 0;
    public const int SW_SHOWNORMAL = 1;
    public const int SW_SHOWMINIMIZED = 2;
    public const int SW_SHOWMAXIMIZED = 3;
    public const int SW_MAXIMIZE = 3;
    public const int SW_SHOWNOACTIVATE = 4;
    public const int SW_SHOW = 5;
    public const int SW_MINIMIZE = 6;
    public const int SW_RESTORE = 9;

    // GetWindowLong indexes
    public const int GWL_STYLE = -16;
    public const int GWL_EXSTYLE = -20;

    // Window styles
    public const long WS_VISIBLE = 0x10000000L;
    public const long WS_DISABLED = 0x08000000L;
    public const long WS_CAPTION = 0x00C00000L;
    public const long WS_POPUP = 0x80000000L;

    // Extended window styles
    public const long WS_EX_TOOLWINDOW = 0x00000080L;
    public const long WS_EX_APPWINDOW = 0x00040000L;
    public const long WS_EX_NOACTIVATE = 0x08000000L;
    public const long WS_EX_TRANSPARENT = 0x00000020L;
    public const long WS_EX_LAYERED = 0x00080000L;

    // SetWindowPos flags
    public const uint SWP_NOSIZE = 0x0001;
    public const uint SWP_NOMOVE = 0x0002;
    public const uint SWP_NOZORDER = 0x0004;
    public const uint SWP_NOACTIVATE = 0x0010;
    public const uint SWP_FRAMECHANGED = 0x0020;
    public const uint SWP_SHOWWINDOW = 0x0040;
    public const uint SWP_ASYNCWINDOWPOS = 0x4000;

    // HWND_* for SetWindowPos
    public static readonly IntPtr HWND_TOP = new(0);
    public static readonly IntPtr HWND_TOPMOST = new(-1);
    public static readonly IntPtr HWND_NOTOPMOST = new(-2);

    // MonitorFromWindow flags
    public const uint MONITOR_DEFAULTTONULL = 0x0;
    public const uint MONITOR_DEFAULTTOPRIMARY = 0x1;
    public const uint MONITOR_DEFAULTTONEAREST = 0x2;

    // DWM attributes
    public const int DWMWA_CLOAKED = 14;
    public const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

    // DWM cloaked states
    public const int DWM_CLOAKED_APP = 0x0000001;
    public const int DWM_CLOAKED_SHELL = 0x0000002;
    public const int DWM_CLOAKED_INHERITED = 0x0000004;

    // DWM thumbnail flags
    public const uint DWM_TNP_RECTDESTINATION = 0x00000001;
    public const uint DWM_TNP_RECTSOURCE = 0x00000002;
    public const uint DWM_TNP_OPACITY = 0x00000004;
    public const uint DWM_TNP_VISIBLE = 0x00000008;
    public const uint DWM_TNP_SOURCECLIENTAREAONLY = 0x00000010;

    // WM messages
    public const int WM_GETICON = 0x007F;
    public const int ICON_SMALL = 0;
    public const int ICON_BIG = 1;
    public const int ICON_SMALL2 = 2;

    // GetClassLong
    public const int GCLP_HICON = -14;
    public const int GCLP_HICONSM = -34;

    // Process access
    public const uint PROCESS_QUERY_INFORMATION = 0x0400;
    public const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
    public const uint PROCESS_VM_READ = 0x0010;
}
