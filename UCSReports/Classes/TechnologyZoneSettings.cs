using System.Collections.Generic;

namespace UCSReports
{
    public class TechnologyZoneSettings
    {
        public string Name { get; set; }
        public int Number { get; set; }
        public string Signal { get; set; }
        public int MaxStepsCount { get; set; }
        public int MaxActsCount { get; set; }
        public Codes TZCodes { get; set; }
        public Dictionary<string, string> FirstLevelProtection { get; set; }
        public List<Flag> Flags { get; set; }
        public bool IsExistProtSettings { get; set; }
        public bool IsExistRepSettings { get; set; }
        public TechnologyZoneSettings()
        {
            FirstLevelProtection = new Dictionary<string, string>();
            Flags = new List<Flag>();
        }
    }
}
