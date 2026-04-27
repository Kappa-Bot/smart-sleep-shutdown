using System.Globalization;
using System.Windows.Data;

namespace SmartSleepShutdown.App.ViewModels;

public static class BooleanBoxes
{
    public static IValueConverter OnOffConverter { get; } = new OnOffValueConverter();

    private sealed class OnOffValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool enabled && enabled ? "Activado" : "Desactivado";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is string text && string.Equals(text, "Activado", StringComparison.OrdinalIgnoreCase);
        }
    }
}
