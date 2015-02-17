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
                string strValue = value.ToString();
                bool boolValue = false;
                if (bool.TryParse(strValue,out boolValue))
                {
                    return boolValue ? Visibility.Collapsed : Visibility.Visible;
                }
                else
                {
                    return string.IsNullOrEmpty(strValue) ? Visibility.Collapsed : Visibility.Visible;
                }
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
