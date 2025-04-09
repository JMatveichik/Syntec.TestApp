using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Syntec.TestApp.WPF.Converters
{
    public class IndexToNumberConverter : IValueConverter
    {
        public static readonly IndexToNumberConverter Instance = new IndexToNumberConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is int index ? index + 1 : value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
