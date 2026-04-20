using System.Runtime.InteropServices;

// ReSharper disable InconsistentNaming
namespace WindowGrabber.Interop;

[StructLayout(LayoutKind.Sequential)]
internal struct RECT
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;

    public int Width => Right - Left;
    public int Height => Bottom - Top;

    public override string ToString() => $"({Left},{Top})-({Right},{Bottom}) {Width}x{Height}";
}

[StructLayout(LayoutKind.Sequential)]
internal struct POINT
{
    public int X;
    public int Y;
    public POINT(int x, int y) { X = x; Y = y; }
}

[StructLayout(LayoutKind.Sequential)]
internal struct SIZE
{
    public int cx;
    public int cy;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct MONITORINFOEX
{
    public int cbSize;
    public RECT rcMonitor;
    public RECT rcWork;
    public uint dwFlags;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string szDevice;

    public const uint MONITORINFOF_PRIMARY = 0x00000001;
}

[StructLayout(LayoutKind.Sequential)]
internal struct WINDOWPLACEMENT
{
    public int length;
    public int flags;
    public int showCmd;
    public POINT ptMinPosition;
    public POINT ptMaxPosition;
    public RECT rcNormalPosition;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DWM_THUMBNAIL_PROPERTIES
{
    public uint dwFlags;
    public RECT rcDestination;
    public RECT rcSource;
    public byte opacity;
    [MarshalAs(UnmanagedType.Bool)]
    public bool fVisible;
    [MarshalAs(UnmanagedType.Bool)]
    public bool fSourceClientAreaOnly;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct DISPLAY_DEVICE
{
    public int cb;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string DeviceName;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string DeviceString;
    public int StateFlags;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string DeviceID;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string DeviceKey;
}

[StructLayout(LayoutKind.Sequential)]
internal struct BITMAPINFOHEADER
{
    public uint biSize;
    public int biWidth;
    public int biHeight;
    public ushort biPlanes;
    public ushort biBitCount;
    public uint biCompression;
    public uint biSizeImage;
    public int biXPelsPerMeter;
    public int biYPelsPerMeter;
    public uint biClrUsed;
    public uint biClrImportant;
}
