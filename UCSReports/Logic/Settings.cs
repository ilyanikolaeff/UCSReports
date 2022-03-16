using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using OPCWrapper;

namespace UCSReports
{
    public class Settings
    {
        private static readonly Lazy<Settings> _instance = new Lazy<Settings>(() => new Settings());
        public static Settings GetInstance()
        {
            return _instance.Value;
        }

        public List<TechnologyZoneSettings> TZSettings { get; set; }
        public ConnectionSettings HdaConnectionSettings { get; set; }
        public int Delta { get; set; } = 2;
        private Settings()
        {
            LoadSettings();
        }
        public void LoadSettings()
        {
            var xRoot = XDocument.Load(AppDomain.CurrentDomain.BaseDirectory + @"\Settings.xml").Root;

            // hda conn settings
            HdaConnectionSettings = new ConnectionSettings(xRoot.Element("HDAServer").Attribute("IP").Value, xRoot.Element("HDAServer").Attribute("Name").Value);

            // tz settings
            TZSettings = new List<TechnologyZoneSettings>();
            var xZones = xRoot.Element("TZSettings").Elements("TZ");
            foreach (var xZone in xZones)
            {
                // ====ALGORITHMS SETTINGS (CODES)====
                var currentSettings = new TechnologyZoneSettings
                {
                    Name = xZone.Attribute("Name").Value,
                    MaxActsCount = int.Parse(xZone.Attribute("Acts").Value),
                    Number = int.Parse(xZone.Attribute("Number").Value),
                    Signal = xZone.Attribute("Signal").Value,
                    MaxStepsCount = int.Parse(xZone.Attribute("Steps").Value)
                };

                if (xZone.Attribute("Reports")?.Value != null)
                {
                    currentSettings.IsExistRepSettings = true;
                    var xReportsSettings = XDocument.Load(CheckPath(xZone.Attribute("Reports").Value)).Root.Element("ReportsSettings");
                    currentSettings.TZCodes = new Codes(GetCodes("algorithms", "algorithm", xReportsSettings),
                                                        GetCodes("statuses", "status", xReportsSettings),
                                                        GetCodes("steps", "step", xReportsSettings),
                                                        GetCodes("acts", "act", xReportsSettings),
                                                        GetCodes("commands", "command", xReportsSettings),
                                                        GetCodes("alm_steps", "step", xReportsSettings));
                }

                TZSettings.Add(currentSettings);
            }
        }

        private static List<CodeItem> GetCodes(string elementName, string elementsName, XElement xZone)
        {
            var codes = new List<CodeItem>();

            foreach (var codeItem in xZone.Element(elementName).Elements(elementsName))
            {
                var currentCode = new CodeItem
                {
                    Code = int.Parse(codeItem.Attribute("Code").Value),
                    Name = codeItem.Attribute("Name").Value
                };

                if (codeItem.Attribute("Type") != null)
                    currentCode.Type = codeItem.Attribute("Type").Value;
                if (codeItem.Attribute("EUnit") != null)
                    currentCode.EUnit = codeItem.Attribute("EUnit").Value;

                codes.Add(currentCode);
            }

            return codes;
        }

        private string CheckPath(string path)
        {
            if (!path.Contains(@":\"))
                path = AppDomain.CurrentDomain.BaseDirectory + path;
            return path;
        }
    }
}
