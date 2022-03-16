using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UCSReports
{
    interface IReportViewModel
    {
        string ReportName { get; }
        string TZName { get; set; }
        DateTime StartTime { get; }
        DateTime EndTime { get; }
    }
}
