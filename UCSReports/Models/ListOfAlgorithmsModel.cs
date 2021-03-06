using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UCSReports
{
    class ListOfAlgorithmsModel
    {
        public List<Algorithm> GetAlgorithms(DateTime startTime, DateTime endTime, TechnologyZoneSettings tzSettings)
        {
            var opcHdaClient = OPCHDAClient.GetInstance();

            var rootSignal = tzSettings.Signal;
            var tagNames = new List<string>()
            {
                rootSignal + ".TASK_ALM_CLOSE",
                rootSignal + ".TASK_ALM_CLOSE.status",
                rootSignal + ".TASK",
                rootSignal + ".TASK.status"
            };

            var hdaReadResults = opcHdaClient.ReadRaw(startTime, endTime, 0, true, tagNames);

            var listOfAlgorithms = new List<Algorithm>();

            // Противоаварийные алгоритмы
            listOfAlgorithms.AddRange(GetAlgorithms(hdaReadResults[0].Results, hdaReadResults[1].Results, tzSettings, AlgorithmType.Emergency));

            // Штатные алгоритмы
            listOfAlgorithms.AddRange(GetAlgorithms(hdaReadResults[2].Results, hdaReadResults[3].Results, tzSettings, AlgorithmType.Regular));

            // сортировка и назначение номеров
            int index = 1;
            var sortedListOfAlgorithms = listOfAlgorithms.OrderByDescending(s => s.StartTime).ToList();
            foreach (var alg in sortedListOfAlgorithms)
            {
                alg.ID = index;
                index++;
            }
            return sortedListOfAlgorithms;
        }

        private List<Algorithm> GetAlgorithms(IEnumerable<HistoryResult> codes, IEnumerable<HistoryResult> statuses, TechnologyZoneSettings tzSettings, AlgorithmType algorithmType)
        {
            var listOfAlgorithms = new List<Algorithm>();
            var filteredStatuses = statuses.Where(p => p.Value != null).Where(p => int.Parse(p.Value.ToString()) >= 2).ToList();

            for (int i = 0; i < filteredStatuses.Count - 1; i++)
            {
                var currentItem = filteredStatuses[i];
                var nextItem = filteredStatuses[i + 1];

                bool parseResult;
                parseResult = int.TryParse(currentItem.Value.ToString(), out int currentValue);
                parseResult = int.TryParse(nextItem.Value.ToString(), out int nextValue);

                nextValue = nextValue == 2 ? 5 : nextValue; // проверка на два старта без окончания

                if (currentValue == 2)
                {
                    // код алгоритма
                    parseResult = int.TryParse(codes.GetResult(nextItem.Timestamp, nextItem.Timestamp,
                                                                FindType.Last,
                                                                IntervalChangeType.Extension,
                                                                FilterType.ValueNotNull).Value.ToString(),
                                                                out int algorithmCode);


                    listOfAlgorithms.Add(new Algorithm()
                    {
                        StartTime = currentItem.Timestamp,
                        EndTime = nextItem.Timestamp,
                        CodeOfStatus = nextValue,
                        Status = tzSettings.TZCodes.GetNameOfStatus(nextValue),
                        CodeOfName = algorithmCode,
                        Name = tzSettings.TZCodes.GetNameOfAlgorithm(algorithmCode),
                        AlgorithmType = algorithmType
                    });
                }
            }

            return listOfAlgorithms;
        }
    }
}
