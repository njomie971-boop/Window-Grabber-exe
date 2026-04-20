using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using WindowGrabber.Helpers;
using WindowGrabber.Interop;
using WindowGrabber.Models;

namespace WindowGrabber.Services;

/// <summary>
/// Énumère les fenêtres top-level "utiles" et les matérialise en <see cref="WindowInfo"/>.
/// Filtre :
/// - fenêtres cloakées DWM (UWP en arrière-plan)
/// - tool windows sans AppWindow
/// - fenêtres sans titre
/// - fenêtres système (si demandé)
/// - la fenêtre de Window Grabber elle-même
/// </summary>
public sealed class WindowService
{
    private static readonly HashSet<string> SystemClassBlacklist = new(StringComparer.OrdinalIgnoreCase)
    {
        "Progman", "WorkerW", "Shell_TrayWnd", "Shell_SecondaryTrayWnd",
        "IME", "MSCTFIME UI", "Default IME", "NotifyIconOverflowWindow",
        "TaskListThumbnailWnd", "Windows.UI.Core.CoreWindow",
        // NB: "ApplicationFrameWindow" est la classe hôte des apps UWP — on la garde
        "MultitaskingViewFrame", "Xaml_WindowedPopupClass",
        "EdgeUiInputTopWndClass", "NativeHWNDHost"
    };

    private static readonly HashSet<string> SystemProcessBlacklist = new(StringComparer.OrdinalIgnoreCase)
    {
        "TextInputHost", "SearchHost", "StartMenuExperienceHost",
        "ShellExperienceHost", "SearchUI", "Cortana"
    };

    public IReadOnlyList<WindowInfo> GetVisibleWindows(
        IReadOnlyList<MonitorInfo> monitors,
        MonitorService monitorService,
        IntPtr excludeHwnd,
        bool hideSystemWindows)
    {
        var result = new List<WindowInfo>();

        NativeMethods.EnumWindows((hWnd, _) =>
        {
            try
            {
                if (!ShouldInclude(hWnd, excludeHwnd, hideSystemWindows))
                    return true;

                var info = BuildInfo(hWnd, monitors, monitorService);
                if (info != null) result.Add(info);
            }
            catch (Exception ex)
            {
                Logger.Warn($"Erreur énumération hWnd=0x{hWnd.ToInt64():X}", ex);
            }
            return true;
        }, IntPtr.Zero);

        return result;
    }

    private static bool ShouldInclude(IntPtr hWnd, IntPtr excludeHwnd, bool hideSystemWindows)
    {
        if (hWnd == excludeHwnd) return false;
        if (!NativeMethods.IsWindow(hWnd)) return false;
        if (!NativeMethods.IsWindowVisible(hWnd)) return false;

        // Filtrer les fenêtres DWM cloakées (UWP en arrière-plan)
        if (NativeMethods.DwmGetWindowAttribute(hWnd, NativeConstants.DWMWA_CLOAKED, out int cloaked, sizeof(int)) == 0)
        {
            if (cloaked != 0) return false;
        }

        // Titre non vide
        int len = NativeMethods.GetWindowTextLength(hWnd);
        if (len <= 0) return false;

        // Style filtering
        var exStyle = (long)NativeMethods.GetWindowLongPtr(hWnd, NativeConstants.GWL_EXSTYLE).ToInt64();
        bool isToolWindow = (exStyle & NativeConstants.WS_EX_TOOLWINDOW) != 0;
        bool isAppWindow = (exStyle & NativeConstants.WS_EX_APPWINDOW) != 0;
        if (isToolWindow && !isAppWindow) return false;

        // Fenêtre doit être top-level (pas de parent "owner" sauf AppWindow)
        // GetWindow avec GW_OWNER n'est pas exposé ici : on suppose que top-level visible suffit.

        // Blacklist classes système
        if (hideSystemWindows)
        {
            var cls = GetClassName(hWnd);
            if (!string.IsNullOrEmpty(cls) && SystemClassBlacklist.Contains(cls))
                return false;
        }

        return true;
    }

    private static WindowInfo? BuildInfo(IntPtr hWnd, IReadOnlyList<MonitorInfo> monitors, MonitorService monitorService)
    {
        string title = GetWindowTitle(hWnd);
        if (string.IsNullOrWhiteSpace(title)) return null;

        NativeMethods.GetWindowThreadProcessId(hWnd, out uint pid);
        var (pname, ppath) = GetProcessInfo(pid);

        if (string.IsNullOrWhiteSpace(pname)) pname = "Inconnu";

        // skip système via processus
        if (SystemProcessBlacklist.Contains(pname)) return null;

        if (!NativeMethods.GetWindowRect(hWnd, out var rect)) return null;

        WindowDisplayState state;
        if (NativeMethods.IsIconic(hWnd)) state = WindowDisplayState.Minimized;
        else if (NativeMethods.IsZoomed(hWnd)) state = WindowDisplayState.Maximized;
        else state = WindowDisplayState.Normal;

        var bounds = new Rect(rect.Left, rect.Top, Math.Max(0, rect.Width), Math.Max(0, rect.Height));

        // Pour une fenêtre minimisée, GetWindowRect retourne -32000, on prend la position restored
        if (state == WindowDisplayState.Minimized)
        {
            var wp = new WINDOWPLACEMENT { length = Marshal.SizeOf<WINDOWPLACEMENT>() };
            if (NativeMethods.GetWindowPlacement(hWnd, ref wp))
            {
                var r = wp.rcNormalPosition;
                bounds = new Rect(r.Left, r.Top, Math.Max(0, r.Width), Math.Max(0, r.Height));
            }
        }

        var monitor = monitorService.GetMonitorForWindow(hWnd, monitors)
                      ?? monitors.FirstOrDefault();
        if (monitor == null) return null;

        var icon = IconExtractor.TryGetWindowIcon(hWnd) ?? IconExtractor.TryGetFileIcon(ppath);
        var className = GetClassName(hWnd);

        return new WindowInfo
        {
            Handle = hWnd,
            Title = title,
            ProcessName = pname,
            ProcessPath = ppath,
            ProcessId = pid,
            Bounds = bounds,
            State = state,
            MonitorId = monitor.Id,
            MonitorLabel = monitor.DisplayLabel,
            MonitorIndex = monitor.Index,
            Icon = icon,
            ClassName = className
        };
    }

    private static string GetWindowTitle(IntPtr hWnd)
    {
        int len = NativeMethods.GetWindowTextLength(hWnd);
        if (len <= 0) return string.Empty;
        var sb = new StringBuilder(len + 1);
        NativeMethods.GetWindowText(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }

    private static string GetClassName(IntPtr hWnd)
    {
        var sb = new StringBuilder(256);
        NativeMethods.GetClassName(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }

    private static (string name, string? path) GetProcessInfo(uint pid)
    {
        if (pid == 0) return ("", null);

        // 1) Chemin via QueryFullProcessImageName (rapide, sans accès au module)
        IntPtr h = NativeMethods.OpenProcess(NativeConstants.PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
        string? path = null;
        if (h != IntPtr.Zero)
        {
            try
            {
                var sb = new StringBuilder(1024);
                uint capacity = (uint)sb.Capacity;
                if (NativeMethods.QueryFullProcessImageName(h, 0, sb, ref capacity))
                {
                    path = sb.ToString();
                }
            }
            finally
            {
                NativeMethods.CloseHandle(h);
            }
        }

        string name;
        if (!string.IsNullOrEmpty(path))
        {
            name = System.IO.Path.GetFileNameWithoutExtension(path);
        }
        else
        {
            try
            {
                using var p = Process.GetProcessById((int)pid);
                name = p.ProcessName;
            }
            catch
            {
                name = "";
            }
        }

        return (name, path);
    }
}
