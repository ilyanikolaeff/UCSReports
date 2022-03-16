using OPCWrapper.HistoricalDataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UCSReports
{
    class ListOfAlgorithmsModel
    {
        private int _delta = Settings.GetInstance().Delta;
        private OpcHdaClient _opcHdaClient;
        public List<Algorithm> GetAlgorithms(DateTime startTime, DateTime endTime, TechnologyZoneSettings tzSettings)
        {
            _opcHdaClient = CommAdapter.GetInstance().OpcHdaClient;

            var rootSignal = tzSettings.Signal;
            var tagNames = new List<string>()
            {
                rootSignal + ".TASK_ALM_CLOSE.status",
                rootSignal + ".TASK.status",
            };

            var hdaReadResults = _opcHdaClient.ReadRaw(startTime, endTime, tagNames);

            var listOfAlgorithms = new List<Algorithm>();

            // Противоаварийные алгоритмы
            listOfAlgorithms.AddRange(GetAlgorithms(hdaReadResults[0], tzSettings, AlgorithmType.Emergency));

            // Штатные алгоритмы
            listOfAlgorithms.AddRange(GetAlgorithms(hdaReadResults[1], tzSettings, AlgorithmType.Regular));

            // сортировка и назначение номеров
            int index = 1;
            var sortedListOfAlgorithms = listOfAlgorithms.OrderByDescending(s => s.StartTime).ToList();
            foreach (var alg in sortedListOfAlgorithms)
            {
                alg.ID = index++;
            }
            return sortedListOfAlgorithms;
        }

        private List<Algorithm> GetAlgorithms(IEnumerable<OpcHdaResultItem> statuses, TechnologyZoneSettings tzSettings, AlgorithmType algorithmType)
        {
            var listOfAlgorithms = new List<Algorithm>();
            var filteredStatuses = statuses.FilterResults(FilterType.ValueNotNull).Where(p => Convert.ToInt32(p.Value) >= 2).ToList();

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
                    string tagName = "";
                    if (algorithmType == AlgorithmType.Emergency)
                        tagName = tzSettings.Signal + ".TASK_ALM_CLOSE";
                    if (algorithmType == AlgorithmType.Regular)
                        tagName = tzSettings.Signal + ".TASK";

                    var codesResults = _opcHdaClient.ReadRaw(currentItem.Timestamp.AddSeconds(_delta), nextItem.Timestamp.AddSeconds(-_delta), new List<string> { tagName })[0];
                    var algorithmCodeResult = codesResults.FirstOrDefault();
                    if (algorithmCodeResult == null)
                        continue;

                    var algorithmCode = Convert.ToInt32(algorithmCodeResult.Value);

                    //parseResult = int.TryParse(codes.FindNearestResult(currentItem.Timestamp, false).ToString(), out int algorithmCode);

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
