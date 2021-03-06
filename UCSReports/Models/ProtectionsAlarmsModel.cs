using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UCSReports
{
    class ProtectionsAlarmsModel
    {
        public delegate void ProgressNotify(string message, double progressValue);
        public event ProgressNotify NotifyProgress;
        public List<FirstLevelProtection> GetFirstLevelProtections(DateTime startTime, DateTime endTime, TechnologyZoneSettings tzSettings)
        {
            var hdaClient = OPCHDAClient.GetInstance();
            var protList = new List<FirstLevelProtection>();

            // 1 уровень
            int i = 0;
            foreach (var protFirstLevel in tzSettings.FirstLevelProtection)
            {
                NotifyProgress?.Invoke($"1 ур.: {protFirstLevel.Value}", i / tzSettings.FirstLevelProtection.Count * 100);

                var historyResults = hdaClient.ReadRaw(startTime, endTime, 0, true, new List<string>() { protFirstLevel.Key })[0];
                foreach (var result in historyResults.Results)
                {
                    bool currentValue = Convert.ToBoolean(result.Value);
                    if (currentValue)
                    {
                        protList.Add(new FirstLevelProtection()
                        {
                            Name = protFirstLevel.Value,
                            ActivationTime = result.Timestamp,
                            Tag = protFirstLevel.Key
                        });
                    }
                    if (!currentValue)
                    {
                        // защита от нескольких флагов защиты без времени окончания
                        //var prevProts = protList.Where(s => s.DeactivationTime == DateTime.MinValue).ToList();
                        //if (prevProts.Count() > 0)
                        //{
                        //    prevProts[0].DeactivationTime = result.Timestamp;
                        //    foreach (var prot in prevProts.Skip(1))
                        //    {
                        //        protList.Remove(prot);
                        //    }
                        //}
                    }
                }
                i++;
            }
            return protList;
        }

        public List<SecondLevelProtection> GetSecondLevelProtections(DateTime startTime, DateTime endTime, TechnologyZoneSettings tzSettings)
        {
            var hdaClient = OPCHDAClient.GetInstance();
            var protList = new List<SecondLevelProtection>();
            // 2 уровень

            for (int i = 0; i < tzSettings.Flags.Count; i++)
            {
                NotifyProgress?.Invoke($"2 ур.", i / tzSettings.FirstLevelProtection.Count * 100);

                List<string> tagNames = new List<string>
                {
                    tzSettings.Signal + $".LCK_SECOND_LEVEL.source.source_mask{i}",
                    tzSettings.Signal + $".LCK_SECOND_LEVEL.source.source_not_mask{i}"
                };

                var readResults = hdaClient.ReadRaw(startTime, endTime, 0, true, tagNames);
                foreach (var result in readResults)
                {
                    var currentFlagNumber = int.Parse(string.Join("", result.ItemName.Split('.').LastOrDefault().Where(c => char.IsDigit(c))));
                    foreach (var flag in result.Results)
                    {
                        if (flag.Value == null)
                            continue;

                        var currentFlagValue = Convert.ToInt32(flag.Value);
                        var bits = CheckBits(currentFlagValue);

                        foreach (var bit in bits)
                        {
                            if (bit.Value)
                            {
                                protList.Add(new SecondLevelProtection()
                                {
                                    Name = tzSettings.Flags.FirstOrDefault(p => p.Number == currentFlagNumber).Bits.FirstOrDefault(p => p.Key == bit.Key).Value,
                                    FlagNum = currentFlagNumber,
                                    BitNum = bit.Key,
                                    ActivationTime = flag.Timestamp
                                });
                            }
                            if (!bit.Value)
                            {
                                // защита от нескольких флагов защиты без времени окончания
                                //var prevProts = protList.Where(p => p.ProtectionNum == currentFlagNumber * 32 + bit.Key).ToList();
                                //if (prevProts.Count > 0)
                                //{
                                //    prevProts[0].DeactivationTime = flag.Timestamp;
                                //    foreach (var prot in prevProts.Skip(1))
                                //        protList.Remove(prot);
                                //}
                            }
                        }
                    }
                }
            }

            return protList;
        }

        private static Dictionary<int, bool> CheckBits(int flgValue)
        {
            var bits = new Dictionary<int, bool>();
            // Конвертим в строку, слева добавляем нулевые биты до 32 битов
            var flgBinary = Convert.ToString(flgValue, 2).PadLeft(32, '0');
            // Читаем с конца (от младщего к старшему биту)
            int bitsCount = 0;
            for (int i = flgBinary.Length - 1; i >= 0; i--)
            {
                bits.Add(bitsCount, flgBinary[i] == '1');
                bitsCount++;
            }
            return bits;
        }
    }
}
