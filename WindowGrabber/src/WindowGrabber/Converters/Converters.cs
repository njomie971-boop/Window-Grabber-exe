using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using WindowGrabber.Models;

namespace WindowGrabber.Converters;

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool b = value is bool v && v;
        bool invert = parameter as string == "invert";
        if (invert) b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool invert = parameter as string == "invert";
        bool isNull = value == null || (value is string s && string.IsNullOrEmpty(s));
        if (invert) return isNull ? Visibility.Visible : Visibility.Collapsed;
        return isNull ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class IsOnTargetToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool onTarget = value is bool v && v;
        var app = Application.Current;
        if (app == null) return Brushes.Transparent;
        return onTarget
            ? app.Resources["AccentGreenBrush"]!
            : app.Resources["BorderBrush"]!;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class StateToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is WindowDisplayState s ? s switch
        {
            WindowDisplayState.Maximized => "Maximisée",
            WindowDisplayState.Minimized => "Minimisée",
            _ => "Normale"
        } : "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class ConnectionToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is ConnectionType c ? c switch
        {
            ConnectionType.HDMI => "HDMI",
            ConnectionType.DisplayPort => "DisplayPort",
            ConnectionType.USBC => "USB-C",
            ConnectionType.DVI => "DVI",
            ConnectionType.VGA => "VGA",
            ConnectionType.Internal => "Intégré",
            ConnectionType.Composite => "Composite",
            ConnectionType.Component => "Composantes",
            ConnectionType.SVideo => "S-Video",
            ConnectionType.Other => "Autre",
            _ => "Inconnue"
        } : "Inconnue";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
