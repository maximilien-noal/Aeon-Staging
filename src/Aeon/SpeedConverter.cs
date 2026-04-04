using System.Globalization;
using Avalonia.Data.Converters;

namespace Aeon.Emulator.Launcher;

internal sealed class SpeedConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int speed)
            return targetType == typeof(string) ? string.Empty : 0;

        var mhz = (decimal)speed / 1_000_000;
        if (targetType == typeof(string))
            return mhz.ToString("0.#", culture) + "MHz";

        return (int)Math.Round(mhz, MidpointRounding.AwayFromZero);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int mhz)
            return mhz * 1_000_000;

        if (value is string s && int.TryParse(s, NumberStyles.Integer, culture, out int parsed))
            return parsed * 1_000_000;

        return 0;
    }
}
