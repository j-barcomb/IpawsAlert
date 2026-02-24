using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace IpawsAlert.Wpf.Converters;

/// <summary>True → Visible, False → Collapsed</summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
public sealed class BoolToVisConverter : IValueConverter
{
    public static readonly BoolToVisConverter Instance = new();
    public object Convert(object v, Type t, object p, CultureInfo c) =>
        v is true ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object v, Type t, object p, CultureInfo c) =>
        v is Visibility.Visible;
}

/// <summary>True → Collapsed, False → Visible (inverse)</summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
public sealed class InverseBoolToVisConverter : IValueConverter
{
    public static readonly InverseBoolToVisConverter Instance = new();
    public object Convert(object v, Type t, object p, CultureInfo c) =>
        v is true ? Visibility.Collapsed : Visibility.Visible;
    public object ConvertBack(object v, Type t, object p, CultureInfo c) =>
        v is Visibility.Collapsed;
}

/// <summary>Severity string → SolidColorBrush</summary>
[ValueConversion(typeof(string), typeof(Brush))]
public sealed class SeverityToBrushConverter : IValueConverter
{
    public static readonly SeverityToBrushConverter Instance = new();
    public object Convert(object v, Type t, object p, CultureInfo c)
    {
        var s = v as string ?? string.Empty;
        return s switch
        {
            "Extreme"  => new SolidColorBrush(Color.FromRgb(0xFF, 0x3B, 0x30)),
            "Severe"   => new SolidColorBrush(Color.FromRgb(0xFF, 0x95, 0x00)),
            "Moderate" => new SolidColorBrush(Color.FromRgb(0xFF, 0xD6, 0x0A)),
            "Minor"    => new SolidColorBrush(Color.FromRgb(0x34, 0xC7, 0x59)),
            _          => new SolidColorBrush(Color.FromRgb(0x8B, 0x94, 0x9E))
        };
    }
    public object ConvertBack(object v, Type t, object p, CultureInfo c) =>
        throw new NotSupportedException();
}

/// <summary>Status string → color. "Actual" is red, "Test" is amber, others neutral.</summary>
[ValueConversion(typeof(string), typeof(Brush))]
public sealed class CapStatusToBrushConverter : IValueConverter
{
    public static readonly CapStatusToBrushConverter Instance = new();
    public object Convert(object v, Type t, object p, CultureInfo c)
    {
        var s = v as string ?? string.Empty;
        return s switch
        {
            "Actual"   => new SolidColorBrush(Color.FromRgb(0xFF, 0x3B, 0x30)),
            "Test"     => new SolidColorBrush(Color.FromRgb(0xFF, 0x95, 0x00)),
            "Exercise" => new SolidColorBrush(Color.FromRgb(0x34, 0xC7, 0x59)),
            _          => new SolidColorBrush(Color.FromRgb(0x8B, 0x94, 0x9E))
        };
    }
    public object ConvertBack(object v, Type t, object p, CultureInfo c) =>
        throw new NotSupportedException();
}

/// <summary>IsSuccess bool → green (true) or red (false) brush.</summary>
[ValueConversion(typeof(bool), typeof(Brush))]
public sealed class SuccessToBrushConverter : IValueConverter
{
    public static readonly SuccessToBrushConverter Instance = new();
    public object Convert(object v, Type t, object p, CultureInfo c) =>
        v is true
            ? new SolidColorBrush(Color.FromRgb(0x34, 0xC7, 0x59))
            : new SolidColorBrush(Color.FromRgb(0xFF, 0x3B, 0x30));
    public object ConvertBack(object v, Type t, object p, CultureInfo c) =>
        throw new NotSupportedException();
}

/// <summary>int → string with "/" max label. e.g. "45 / 90"</summary>
public sealed class CharCountLabelConverter : IMultiValueConverter
{
    public static readonly CharCountLabelConverter Instance = new();
    public object Convert(object[] v, Type t, object p, CultureInfo c)
    {
        if (v.Length == 2 && v[0] is int count && v[1] is int max)
            return $"{count} / {max}";
        return string.Empty;
    }
    public object[] ConvertBack(object v, Type[] t, object p, CultureInfo c) =>
        throw new NotSupportedException();
}

/// <summary>int overLimit? → red brush when over, muted otherwise.</summary>
[ValueConversion(typeof(bool), typeof(Brush))]
public sealed class OverLimitToBrushConverter : IValueConverter
{
    public static readonly OverLimitToBrushConverter Instance = new();
    public object Convert(object v, Type t, object p, CultureInfo c) =>
        v is true
            ? new SolidColorBrush(Color.FromRgb(0xFF, 0x3B, 0x30))
            : new SolidColorBrush(Color.FromRgb(0x8B, 0x94, 0x9E));
    public object ConvertBack(object v, Type t, object p, CultureInfo c) =>
        throw new NotSupportedException();
}

/// <summary>Null → Collapsed, non-null → Visible</summary>
public sealed class NullToCollapseConverter : IValueConverter
{
    public static readonly NullToCollapseConverter Instance = new();
    public object Convert(object v, Type t, object p, CultureInfo c) =>
        v is null ? Visibility.Collapsed : Visibility.Visible;
    public object ConvertBack(object v, Type t, object p, CultureInfo c) =>
        throw new NotSupportedException();
}
