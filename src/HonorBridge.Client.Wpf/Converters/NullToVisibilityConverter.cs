using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HonorBridge.Client.Wpf.Converters;

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        return value == null ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // One-way converter; cannot restore original object from Visibility.
        return Binding.DoNothing;
    }
}
