using System;
using System.Windows;
using System.Windows.Data;

namespace RecipeHubApp
{
    /// <summary>
    /// Class to convert a boolean property to a boolean 
    /// This control let enabled or disable command base on if the view has changes
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null)
            {
                if ((bool)value)
                    return Visibility.Collapsed;
                else
                    return Visibility.Visible;
            }
            else
            {
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }
}
