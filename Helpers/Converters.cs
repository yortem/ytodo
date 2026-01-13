using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

// Alias to avoid ambiguity with System.Drawing.Color
using MediaColor = System.Windows.Media.Color;

namespace yTodo.Helpers
{
    public class HeaderWeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isHeader && isHeader)
                return FontWeights.Bold;
            return FontWeights.Normal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class ColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string colorStr)
            {
                try
                {
                    var color = (MediaColor)System.Windows.Media.ColorConverter.ConvertFromString(colorStr);
                    
                    // Check for parameter
                    if (parameter is string param)
                    {
                        if (param.StartsWith("Darken"))
                        {
                            double factor = 0.8;
                            if (double.TryParse(param.Substring(6), out double customFactor)) factor = customFactor;

                            color = MediaColor.FromRgb(
                                (byte)(color.R * factor),
                                (byte)(color.G * factor),
                                (byte)(color.B * factor));
                        }
                        else if (param.StartsWith("Lighten"))
                        {
                            double factor = 1.2;
                            if (double.TryParse(param.Substring(7), out double customFactor)) factor = customFactor;

                            color = MediaColor.FromRgb(
                                (byte)Math.Min(255, color.R * factor),
                                (byte)Math.Min(255, color.G * factor),
                                (byte)Math.Min(255, color.B * factor));
                        }
                    }

                    return new SolidColorBrush(color);
                }
                catch { }
            }
            else if (value is MediaColor c)
            {
                return new SolidColorBrush(c);
            }
            return System.Windows.Media.Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
        
        public static MediaColor ColorFromHex(string hex)
        {
            return (MediaColor)System.Windows.Media.ColorConverter.ConvertFromString(hex);
        }
    }

    public class ThicknessConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double horizontal = 10;
            double vertical = 12;

            try
            {
                if (values.Length >= 1 && values[0] != null && values[0] != DependencyProperty.UnsetValue)
                    double.TryParse(values[0].ToString(), out horizontal);
                if (values.Length >= 2 && values[1] != null && values[1] != DependencyProperty.UnsetValue)
                    double.TryParse(values[1].ToString(), out vertical);
            }
            catch { }

            return new Thickness(horizontal, vertical, horizontal, vertical);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}