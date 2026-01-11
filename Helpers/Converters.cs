using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

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
                    return (SolidColorBrush)new BrushConverter().ConvertFromString(colorStr)!;
                }
                catch { }
            }
            return System.Windows.Media.Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
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
