using System.Windows;
using WindowGrabber.Helpers;
using WindowGrabber.Interop;
using WindowGrabber.Models;

namespace WindowGrabber.Services;

/// <summary>
/// Énumère les moniteurs physiques via <c>EnumDisplayMonitors</c>.
/// Pas de code WPF ici — uniquement de la logique pure.
/// </summary>
public sealed class MonitorService
{
    private readonly ConnectionTypeService _connectionTypeService;

    public MonitorService(ConnectionTypeService connectionTypeService)
    {
        _connectionTypeService = connectionTypeService;
    }

    public IReadOnlyList<MonitorInfo> GetMonitors(bool includeConnectionType = true)
    {
        var raw = new List<(IntPtr hMon, MONITORINFOEX info)>();

        bool ok = NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr _, ref RECT _, IntPtr _) =>
        {
            var mi = new MONITORINFOEX { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<MONITORINFOEX>() };
            if (NativeMethods.GetMonitorInfo(hMonitor, ref mi))
                raw.Add((hMonitor, mi));
            return true;
        }, IntPtr.Zero);

        if (!ok)
            Logger.Warn("EnumDisplayMonitors a retourné false");

        // Construire un mapping device -> connexion (best-effort)
        Dictionary<string, ConnectionType> connectionMap = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string> friendlyMap = new(StringComparer.OrdinalIgnoreCase);
        if (includeConnectionType)
        {
            try
            {
                (connectionMap, friendlyMap) = _connectionTypeService.BuildConnectionMap();
            }
            catch (Exception ex)
            {
                Logger.Warn("Impossible de récupérer le type de connexion", ex);
            }
        }

        // Trier: primaire d'abord, puis par X croissant
        var ordered = raw
            .Select((t, i) => (t.hMon, t.info, originalIndex: i))
            .OrderByDescending(t => (t.info.dwFlags & MONITORINFOEX.MONITORINFOF_PRIMARY) != 0)
            .ThenBy(t => t.info.rcMonitor.Left)
            .ToList();

        var result = new List<MonitorInfo>();
        int index = 1;
        foreach (var (hMon, info, _) in ordered)
        {
            var bounds = new Rect(info.rcMonitor.Left, info.rcMonitor.Top, info.rcMonitor.Width, info.rcMonitor.Height);
            var work = new Rect(info.rcWork.Left, info.rcWork.Top, info.rcWork.Width, info.rcWork.Height);
            var id = hMon.ToInt64().ToString("X");

            connectionMap.TryGetValue(info.szDevice, out var conn);
            friendlyMap.TryGetValue(info.szDevice, out var friendly);

            result.Add(new MonitorInfo
            {
                Id = id,
                Index = index++,
                DeviceName = info.szDevice,
                FriendlyName = string.IsNullOrWhiteSpace(friendly) ? info.szDevice : friendly!,
                Bounds = bounds,
                WorkArea = work,
                IsPrimary = (info.dwFlags & MONITORINFOEX.MONITORINFOF_PRIMARY) != 0,
                Connection = conn,
                HMonitor = hMon
            });
        }

        return result;
    }

    public MonitorInfo? GetMonitorForWindow(IntPtr hWnd, IReadOnlyList<MonitorInfo> monitors)
    {
        var h = NativeMethods.MonitorFromWindow(hWnd, NativeConstants.MONITOR_DEFAULTTONEAREST);
        return monitors.FirstOrDefault(m => m.HMonitor == h) ?? monitors.FirstOrDefault(m => m.IsPrimary);
    }

    public MonitorInfo? GetMonitorForPoint(int x, int y, IReadOnlyList<MonitorInfo> monitors)
    {
        var h = NativeMethods.MonitorFromPoint(new POINT(x, y), NativeConstants.MONITOR_DEFAULTTONEAREST);
        return monitors.FirstOrDefault(m => m.HMonitor == h) ?? monitors.FirstOrDefault(m => m.IsPrimary);
    }
}
