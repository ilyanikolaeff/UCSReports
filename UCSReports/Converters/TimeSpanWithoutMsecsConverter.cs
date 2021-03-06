using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace UCSReports
{
    class TimeSpanWithoutMsecsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var timeSpanValue = (TimeSpan)value;
            if (timeSpanValue > TimeSpan.Zero)
            {
                if (timeSpanValue.Days > 0)
                    return new TimeSpan(timeSpanValue.Days, timeSpanValue.Hours, timeSpanValue.Minutes, timeSpanValue.Seconds);
                else
                    return new TimeSpan(timeSpanValue.Hours, timeSpanValue.Minutes, timeSpanValue.Seconds);
            }
            else
                return "---";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
