using System;
using System.Globalization;
using System.Windows.Data;

namespace Lazarus.Desktop.Converters
{
    public class ViewToTagConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string currentView && parameter is string targetView)
            {
                return currentView == targetView ? "Selected" : "Unselected";
            }
            return "Unselected";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}