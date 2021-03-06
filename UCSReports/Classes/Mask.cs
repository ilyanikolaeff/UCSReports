using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UCSReports
{
    class Mask
    {
        public string Name { get; set; }
        public DateTime TimeSet { get; set; }
        public DateTime TimeUnset { get; set; }
        public TimeSpan Duration { get { return TimeUnset - TimeSet; } }
        public bool IsTorMask { get; set; }
    }
}
