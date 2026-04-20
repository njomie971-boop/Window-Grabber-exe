using System.Diagnostics;
using System.Management;
using System.Text;
using WindowGrabber.Helpers;
using WindowGrabber.Models;

namespace WindowGrabber.Services;

/// <summary>
/// Best-effort : interroge WMI (<c>WmiMonitorConnectionParams</c> + <c>WmiMonitorID</c>) pour mapper
/// chaque <c>\\.\DISPLAYn</c> vers un <see cref="ConnectionType"/> et un nom lisible.
/// Si WMI est indisponible ou si les drivers ne renseignent pas ces classes, on retourne des maps
/// vides et la couche appelante assumera "Inconnu".
/// </summary>
public sealed class ConnectionTypeService
{
    /// <summary>
    /// Mappage interne des constantes <c>VideoOutputTechnology</c> de Windows.
    /// Source officielle : DXGI / WMI <c>VideoOutputTechnology</c> enum.
    /// </summary>
    private static ConnectionType MapVideoTech(uint tech) => tech switch
    {
        0 => ConnectionType.VGA,
        1 => ConnectionType.SVideo,
        2 => ConnectionType.Composite,
        3 => ConnectionType.Component,
        4 => ConnectionType.DVI,
        5 => ConnectionType.HDMI,
        6 => ConnectionType.Other, // LVDS
        8 => ConnectionType.DVI,   // DJPN
        9 => ConnectionType.DisplayPort, // DisplayPort external
        10 => ConnectionType.DisplayPort, // DisplayPort embedded
        11 => ConnectionType.USBC, // UDI external
        12 => ConnectionType.USBC, // UDI embedded
        13 => ConnectionType.Other, // SDTVDONGLE
        14 => ConnectionType.Other, // MIRACAST
        15 => ConnectionType.USBC,  // INDIRECT_WIRED (USB display)
        0x80000000 => ConnectionType.Internal,
        _ => ConnectionType.Unknown
    };

    public (Dictionary<string, ConnectionType> Connections, Dictionary<string, string> FriendlyNames)
        BuildConnectionMap()
    {
        var connections = new Dictionary<string, ConnectionType>(StringComparer.OrdinalIgnoreCase);
        var friendly = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Le namespace WMI WmiMonitorConnectionParams / WmiMonitorID expose InstanceName
        // sous la forme "DISPLAY\GSM5A50\4&abc&0&UID123_0". Pour le relier à \\.\DISPLAYn il
        // faut croiser avec SetupAPI / EnumDisplayDevices — ce qui n'est pas totalement fiable.
        // On alimente donc par ORDRE de monitors actifs : Windows les énumère dans le même
        // ordre que EnumDisplayMonitors. C'est suffisant en best-effort.

        try
        {
            using var searcher = new ManagementObjectSearcher(@"\\.\root\wmi", "SELECT * FROM WmiMonitorConnectionParams");
            var connRecords = new List<(string Instance, uint Tech)>();
            foreach (var obj in searcher.Get())
            {
                var inst = obj["InstanceName"]?.ToString() ?? "";
                var tech = Convert.ToUInt32(obj["VideoOutputTechnology"] ?? 0u);
                connRecords.Add((inst, tech));
            }

            using var searcher2 = new ManagementObjectSearcher(@"\\.\root\wmi", "SELECT * FROM WmiMonitorID");
            var idRecords = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var obj in searcher2.Get())
            {
                var inst = obj["InstanceName"]?.ToString() ?? "";
                string name = DecodeUInt16Array(obj["UserFriendlyName"] as ushort[])
                              ?? DecodeUInt16Array(obj["ProductCodeID"] as ushort[])
                              ?? "";
                if (!string.IsNullOrWhiteSpace(name))
                    idRecords[inst] = name.Trim();
            }

            // Index basé sur l'ordre d'énumération actif (DISPLAY1, DISPLAY2, ...) — EnumDisplayDevices
            // donne le nom \\.\DISPLAYn pour chaque device actif, dans l'ordre.
            var devicePaths = EnumerateActiveDisplayDevices();

            int count = Math.Min(connRecords.Count, devicePaths.Count);
            for (int i = 0; i < count; i++)
            {
                connections[devicePaths[i]] = MapVideoTech(connRecords[i].Tech);
            }

            // Friendly names : alignement par ordre également
            int fCount = Math.Min(idRecords.Count, devicePaths.Count);
            int j = 0;
            foreach (var kv in idRecords)
            {
                if (j >= fCount) break;
                friendly[devicePaths[j]] = kv.Value;
                j++;
            }
        }
        catch (Exception ex)
        {
            Logger.Warn("WMI WmiMonitorConnectionParams indisponible", ex);
        }

        return (connections, friendly);
    }

    private static List<string> EnumerateActiveDisplayDevices()
    {
        var list = new List<string>();
        uint i = 0;
        while (true)
        {
            var dd = new Interop.DISPLAY_DEVICE { cb = System.Runtime.InteropServices.Marshal.SizeOf<Interop.DISPLAY_DEVICE>() };
            if (!Interop.NativeMethods.EnumDisplayDevices(null, i, ref dd, 0))
                break;

            const int DISPLAY_DEVICE_ACTIVE = 0x00000001;
            const int DISPLAY_DEVICE_MIRRORING_DRIVER = 0x00000008;
            if ((dd.StateFlags & DISPLAY_DEVICE_ACTIVE) != 0 &&
                (dd.StateFlags & DISPLAY_DEVICE_MIRRORING_DRIVER) == 0)
            {
                list.Add(dd.DeviceName);
            }
            i++;
            if (i > 64) break;
        }
        return list;
    }

    private static string? DecodeUInt16Array(ushort[]? arr)
    {
        if (arr == null || arr.Length == 0) return null;
        var sb = new StringBuilder();
        foreach (var c in arr)
        {
            if (c == 0) break;
            sb.Append((char)c);
        }
        var s = sb.ToString();
        return string.IsNullOrWhiteSpace(s) ? null : s;
    }
}
