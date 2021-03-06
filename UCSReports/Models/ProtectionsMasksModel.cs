using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace UCSReports
{
    class ProtectionsMasksModel
    {
        public List<Mask> GetMasks(DateTime startTime, DateTime endTime, TechnologyZoneSettings tzSettings)
        {
            var masks = new List<Mask>();
            var hdaClient = OPCHDAClient.GetInstance();
            foreach (var prot in tzSettings.FirstLevelProtection)
            {
                var tags = new List<string>
                    {
                        prot.Key.Replace(".source.", ".mode."),
                        prot.Key.Replace(".source.", ".tor.")
                    };

                var historyResults = hdaClient.ReadRaw(startTime, endTime, 0, true, tags);
                for (int i = 0; i < historyResults.Count; i++)
                {
                    foreach (var result in historyResults[i].Results.FilterResults(FilterType.ValueNotNull))
                    {
                        var currentValue = Convert.ToBoolean(result.Value);
                        if (currentValue)
                        {
                            masks.Add(new Mask()
                            {
                                Name = prot.Value,
                                TimeSet = result.Timestamp,
                                TimeUnset = DateTime.MaxValue,
                                IsTorMask = i == 1
                            }); ;
                        }
                        else
                        {
                            // устанавливаем время снятия
                            var prevMask = masks.LastOrDefault();
                            if (prevMask != null)
                            {
                                if (result.Timestamp >= prevMask.TimeSet)
                                {
                                    prevMask.TimeUnset = result.Timestamp;
                                }
                            }
                        }
                    }
                }
            }

            return masks.Where(p => p.Duration != TimeSpan.Zero).ToList();
        }
    }
}
