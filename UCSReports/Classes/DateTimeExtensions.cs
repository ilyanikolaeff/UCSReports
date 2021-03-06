using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UCSReports
{
    public static class DateTimeExtensions
    {
        public static DateTime Truncate(this DateTime dateTime, long resolution)
        {
            return new DateTime(dateTime.Ticks - (dateTime.Ticks % resolution), dateTime.Kind);
        }
    }
}
