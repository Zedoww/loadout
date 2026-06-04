using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Opti.App.Converters;

/// <summary>true → Visible, false → Collapsed.</summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility.Visible;
}

/// <summary>Inverse un booléen (utile pour IsEnabled = !IsBusy).</summary>
public sealed class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is not true;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is not true;
}

/// <summary>Convertit un nombre d'octets en chaîne lisible (Ko/Mo/Go).</summary>
public sealed class BytesToReadableConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        long bytes = value switch
        {
            long l => l,
            int i => i,
            double d => (long)d,
            _ => 0,
        };
        return Format(bytes);
    }

    public static string Format(long bytes)
    {
        string[] units = { "o", "Ko", "Mo", "Go", "To" };
        double size = bytes;
        int unit = 0;
        while (size >= 1024 && unit < units.Length - 1) { size /= 1024; unit++; }
        return $"{size:0.#} {units[unit]}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Affiche une valeur float? avec un suffixe, ou « — » si null.</summary>
public sealed class NullableFloatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is float f)
            return $"{f:0}{parameter as string}";
        if (value is double d)
            return $"{d:0}{parameter as string}";
        return "—";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
