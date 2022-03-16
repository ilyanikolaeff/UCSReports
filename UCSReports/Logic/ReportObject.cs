using System;
using System.Collections.Generic;
using System.Linq;

namespace UCSReports
{
    class ReportObject
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int CodeOfName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan ControlTime { get; set; }
        public int CodeOfStatus { get; set; }
        public string Status { get; set; }
    }
}
