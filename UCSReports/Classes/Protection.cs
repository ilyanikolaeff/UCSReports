using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UCSReports
{
    abstract class Protection
    {
        public string Name { get; set; }
        public DateTime ActivationTime { get; set; }
        public DateTime DeactivationTime { get; set; }
        public TimeSpan Duration
        {
            get
            {
                return DeactivationTime - ActivationTime;
            }
        }
    }

    class FirstLevelProtection : Protection
    {
        public string Tag { get; set; }
    }

    class SecondLevelProtection : Protection
    {
        public int FlagNum { get; set; }
        public int BitNum { get; set; }

        public int ProtectionNum 
        {
            get { return FlagNum * 32 + BitNum; }
        }
    }

    public class Flag
    {
        public int Number { get; set; }
        public Dictionary<int, string> Bits { get; set; }
        public Flag()
        {
            Bits = new Dictionary<int, string>();
        }
    }

    public enum ProtectionLevel
    {
        First,
        Second
    }
}
