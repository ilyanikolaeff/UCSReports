using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UCSReports
{
    class Algorithm : ReportObject
    {
        public TimeSpan Duration
        {
            get => EndTime - StartTime;
        }
        public AlgorithmType AlgorithmType { get; set; }

        public List<Step> Steps { get; set; }
    }

    class EmergencyAlgorithm : Algorithm
    {
    }

    class RegularAlgorithm : Algorithm
    {
    }

    public enum AlgorithmType
    {
        Emergency,
        Regular
    }
}
