using OPCWrapper.HistoricalDataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UCSReports
{
    public abstract class ReportBuilderBase
    {
        internal abstract List<Step> GetAlgorithmSteps(DateTime start, DateTime end);
    }

    enum EquipmentStatusType
    {
        Start,
        End,
        Control
    }
}
