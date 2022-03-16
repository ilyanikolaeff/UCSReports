using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace UCSReports
{
    class StatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int.TryParse(value.ToString(), out int statusCode);
            SolidColorBrush statusBrush;

            if (statusCode == 3)
                statusBrush = new SolidColorBrush(Colors.LightGreen);
            else if (statusCode == 5)
                statusBrush = new SolidColorBrush(Colors.LightPink);
            else if (statusCode == 7)
                statusBrush = new SolidColorBrush(Colors.Yellow);
            else if (statusCode == 8 || statusCode == 9)
                statusBrush = new SolidColorBrush(Colors.LightGray);
            else
                statusBrush = new SolidColorBrush(Colors.White);

            return statusBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
