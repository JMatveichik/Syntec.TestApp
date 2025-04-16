using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Syntec.TestApp.WPF.Converters
{
    public class CheckBoxListConverter : IMultiValueConverter
    {
        public static readonly CheckBoxListConverter Instance = new CheckBoxListConverter();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is ICollection<string> selectedItems && values[1] is string currentItem)
            {
                return selectedItems.Contains(currentItem);
            }
            
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
