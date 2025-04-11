using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Syntec.TestApp.WPF.ViewModels;

namespace Syntec.TestApp.WPF.Converters
{
    public class LogTypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is NotificationType type)
            {
                switch(type)
                {
                    case NotificationType.Error:
                        return Brushes.Red;

                    case NotificationType.Warning:
                        return Brushes.Orange;

                    case NotificationType.Info:
                        return Brushes.Black;

                    case NotificationType.Debug:
                        return Brushes.Gray;

                    default:
                        return Brushes.Black;                };
            }

            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}