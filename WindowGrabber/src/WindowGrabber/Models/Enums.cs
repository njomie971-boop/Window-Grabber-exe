namespace WindowGrabber.Models;

public enum WindowDisplayState
{
    Normal,
    Minimized,
    Maximized
}

public enum ConnectionType
{
    Unknown,
    VGA,
    DVI,
    HDMI,
    DisplayPort,
    USBC,
    Internal,
    Composite,
    SVideo,
    Component,
    Other
}

public enum ThemeMode
{
    System,
    Dark,
    Light
}
