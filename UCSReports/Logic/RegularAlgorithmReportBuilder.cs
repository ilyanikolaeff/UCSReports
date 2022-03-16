using OPCWrapper.HistoricalDataAccess;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UCSReports
{
    internal class RegularAlgorithmReportBuilder : ReportBuilderBase
    {
        private TechnologyZoneSettings _tzSettings;
        private int _delta = Settings.GetInstance().Delta;
        private DateTime _algorithmStartTime;
        private DateTime _algorithmEndTime;
        private DateTime _clearAlgStartTime;
        private DateTime _clearAlgEndTime;
        private OpcHdaClient _opcHdaClient;
        public RegularAlgorithmReportBuilder(OpcHdaClient opcHdaClient, TechnologyZoneSettings tzSettings)
        {
            _opcHdaClient = opcHdaClient;
            _tzSettings = tzSettings;
        }

        internal override List<Step> GetAlgorithmSteps(DateTime algStartTime, DateTime algEndTime)
        {
            var steps = new List<Step>();
            _clearAlgStartTime = algStartTime;
            _clearAlgEndTime = algEndTime;
            _algorithmStartTime = algStartTime.AddSeconds(_delta);
            _algorithmEndTime = algEndTime.AddSeconds(-_delta);
            // читаем короче все шаги с времени начала до времени окончания
            var historyResults = GetRegularStepsHistory(_algorithmStartTime, _algorithmEndTime);
            int increment = 7;
            int stepIndex = 1;
            for (int i = 0; i <= historyResults.Count - increment; i += increment)
            {
                var codeResults = historyResults[i]; // коды 
                var startResults = historyResults[i + 1]; // время начала
                var statusResults = historyResults[i + 2]; // статусы
                var timesResults = historyResults[i + 3]; // накопленное время
                var etResults = historyResults[i + 4]; // контрольное время 
                var startActResults = historyResults[i + 5]; // начальный номер акта в массиве актов
                var actsCountResults = historyResults[i + 6]; // количество актов начиная с начального

                // status
                var stepStatusResult = statusResults.FilterResults(FilterType.ValueNotNull).LastOrDefault(p => Convert.ToInt32(p.Value) >= 2 && p.Timestamp <= _algorithmEndTime.AddSeconds(_delta * 2));
                if (stepStatusResult == null)
                    continue;

                var stepStartStatusResult = statusResults.FilterResults(FilterType.ValueNotNull).FirstOrDefault(p => Convert.ToInt32(p.Value) == 2 && p.Timestamp >= _algorithmStartTime.AddSeconds(_delta * 2));

                // определяем время начала шага и время окончания шага 
                var stepStartResult = startResults.FilterResults(FilterType.ValueNotNull).LastOrDefault(p => p.Timestamp <= _algorithmEndTime);
                if (stepStartResult == null)
                    continue;

                var stepCodeResult = codeResults.FilterResults(FilterType.ValueNotNull).FirstOrDefault();
                if (stepCodeResult == null)
                    continue;

                var stepTimeResult = timesResults.FilterResults(FilterType.ValueNotNull).LastOrDefault(p => p.Timestamp <= _algorithmEndTime);
                if (stepTimeResult == null)
                    continue;

                var stepEtResult = etResults.FilterResults(FilterType.ValueNotNull).LastOrDefault(p => p.Timestamp <= _algorithmEndTime);
                if (stepEtResult == null)
                    continue;

                var stepCode = Convert.ToInt32(stepCodeResult.Value);
                if (stepCode == -1)
                    continue; // no valid step
                var stepStatusCode = Convert.ToInt32(stepStatusResult.Value);
                var stepStartTimeSeconds = Convert.ToInt32(stepStartResult.Value);
                var stepEstablishedTimeSeconds = Convert.ToInt32(stepEtResult.Value);
                var stepControlTimeSeconds = Convert.ToInt32(stepTimeResult.Value);
                TimeSpan controlTime = TimeSpan.FromSeconds(stepControlTimeSeconds - stepEstablishedTimeSeconds);
                if (controlTime < TimeSpan.Zero)
                    controlTime = TimeSpan.Zero;

                var stepEndTime = stepStatusResult.Timestamp; // время окончания - всегда время статуса

                var possibleStartTimes = new List<DateTime>()
                {
                    stepStartStatusResult != null ? stepStartStatusResult.Timestamp : DateTime.MaxValue, // by status
                    _clearAlgStartTime.AddSeconds(stepStartTimeSeconds), // by start of step from start of algorithm
                    stepEndTime.AddSeconds(-stepEstablishedTimeSeconds) // by end - established time
                };
                var stepStartTime = possibleStartTimes.Min();


                // current step 
                RegularStep currentStep = new RegularStep
                {
                    ID = stepIndex++,
                    CodeOfName = Convert.ToInt32(stepCodeResult.Value),
                    CodeOfStatus = stepStatusCode,
                    Name = _tzSettings.TZCodes.GetNameOfStep(stepCode),
                    Status = _tzSettings.TZCodes.GetNameOfStatus(stepStatusCode),
                    StartTime = stepStartTime,
                    EndTime = stepEndTime,
                    ControlTime = controlTime
                };

                steps.Add(currentStep);


                // acts
                if (stepStatusCode > 7)
                    continue; // если пропущено - акты не собираем

                var startActNumberResult = startActResults.FilterResults(FilterType.ValueNotNull).LastOrDefault(p => p.Timestamp <= _algorithmEndTime);
                var actsCountResult = actsCountResults.FilterResults(FilterType.ValueNotNull).LastOrDefault(p => p.Timestamp <= _algorithmEndTime && Convert.ToInt32(p.Value) > 0);

                if (actsCountResult == null || startActNumberResult == null)
                    continue;

                var acts = GetActs(Convert.ToInt32(startActNumberResult.Value), Convert.ToInt32(actsCountResult.Value), stepStartTime, stepEndTime);
                currentStep.Acts = acts;
            }
            return steps;
        }

        private int GetPmpNumber(object val)
        {
            bool parseResult = int.TryParse(val.ToString(), out int intValue);
            if (parseResult && intValue >= 1000)
                return intValue - (intValue % 1000);
            else
                return -1;
        }

        private List<OpcHdaResultsCollection> GetRegularStepsHistory(DateTime startTime, DateTime endTime)
        {
            var tagNames = new List<string>();
            var rootSignal = _tzSettings.Signal;
            var maxStepsCount = _tzSettings.MaxStepsCount;
            for (int i = 1; i <= maxStepsCount; i++)
            {
                tagNames.Add(rootSignal + $".TASK.STEP{i:00}");
                tagNames.Add(rootSignal + $".TASK.STEP{i:00}.start");
                tagNames.Add(rootSignal + $".TASK.STEP{i:00}.status");
                tagNames.Add(rootSignal + $".TASK.STEP{i:00}.time");
                tagNames.Add(rootSignal + $".TASK.STEP{i:00}.et");
                tagNames.Add(rootSignal + $".TASK.STEP{i:00}.start_act");
                tagNames.Add(rootSignal + $".TASK.STEP{i:00}.count_act");
            }

            var history = _opcHdaClient.ReadRaw(startTime, endTime, tagNames, 0, true);
            return history;
        }

        private List<OpcHdaResultsCollection> GetActsHistory(int actNumber, DateTime stepStartTime, DateTime stepEndTime)
        {
            // формируем массив тегов для запроса 
            var tagNames = new List<string>()
            {
                _tzSettings.Signal + $".ACTS.act{actNumber:000}",
                _tzSettings.Signal + $".ACTS.act{actNumber:000}.start",
                _tzSettings.Signal + $".ACTS.act{actNumber:000}.status",
                _tzSettings.Signal + $".ACTS.act{actNumber:000}.time",
                _tzSettings.Signal + $".ACTS.act{actNumber:000}.et",
                _tzSettings.Signal + $".ACTS.act{actNumber:000}.value",
                _tzSettings.Signal + $".ACTS.act{actNumber:000}.target"
             };

            var history = _opcHdaClient.ReadRaw(stepStartTime, stepEndTime, tagNames);
            return history;
        }

        private List<Act> GetActs(int startActNumber, int actsCount, DateTime stepStartTime, DateTime stepEndTime)
        {
            var acts = new List<Act>();

            // время начала и конца шага
            var clearStepStartTime = stepStartTime;
            var clearStepEndTime = stepEndTime;

            if (Math.Abs(TimeSpan.FromTicks(stepEndTime.Ticks).TotalSeconds - TimeSpan.FromTicks(stepStartTime.Ticks).TotalSeconds) > _delta * 2)
            {
                stepStartTime = stepStartTime.AddSeconds(_delta);
                stepEndTime = stepEndTime.AddSeconds(-_delta); // ужались
            }
            else if (Math.Abs(TimeSpan.FromTicks(stepEndTime.Ticks).TotalSeconds - TimeSpan.FromTicks(stepStartTime.Ticks).TotalSeconds) == 0)
            {
                stepStartTime = stepStartTime.AddSeconds(-_delta / 2);
                stepEndTime = stepEndTime.AddSeconds(_delta / 2); // ужались
            }

            int actIndex = 1;
            for (int j = ++startActNumber; j < startActNumber + actsCount; j++)
            {
                try
                {
                    var actsReadResults = GetActsHistory(j, stepStartTime, stepEndTime);

                    var actCodeResults = actsReadResults[0];
                    var actStartResults = actsReadResults[1];
                    var actStatusResults = actsReadResults[2];
                    var actTimeResults = actsReadResults[3];
                    var actEtResults = actsReadResults[4];
                    var actValueResults = actsReadResults[5];
                    var actTargetResults = actsReadResults[6];

                    // act status
                    GetActStatus(actStatusResults, out int? actStatus, out DateTime actEndTime);
                    if (!actStatus.HasValue) continue;
                    if (actStatus.Value == 2)
                        actStatus = 3;
                    if (actStatus.Value > 7) continue;

                    // act start time
                    var actStartTimeSeconds = GetActStart(actStartResults, clearStepEndTime);
                    if (!actStartTimeSeconds.HasValue) continue;
                    var actStartTime = clearStepStartTime.AddSeconds(actStartTimeSeconds.Value);

                    var actEstablishedTimeSeconds = GetActEstablishedTime(actEtResults, clearStepEndTime);
                    if (!actEstablishedTimeSeconds.HasValue) continue;

                    var actControlTimeSeconds = GetActTime(actTimeResults, clearStepEndTime);
                    if (!actControlTimeSeconds.HasValue) continue;

                    TimeSpan actControlTime = TimeSpan.FromSeconds(actControlTimeSeconds.Value - actEstablishedTimeSeconds.Value);
                    if (actControlTime < TimeSpan.Zero)
                        actControlTime = TimeSpan.Zero;

                    // get act code
                    var actCode = GetActCode(actCodeResults, clearStepStartTime, clearStepEndTime, actStartTime);
                    if (!actCode.HasValue) continue;
                    if (actCode == -1) continue;

                    var actCodeItem = _tzSettings.TZCodes.GetAct(actCode.Value);

                    GetEquipmentsStatuses(actTargetResults, actStartTime, actEndTime, clearStepStartTime, clearStepEndTime, actCodeItem, j,
                        out string equipmentStart, out string equipmentEnd, out string equipmentControl, out string actAddName);


                    acts.Add(new Act
                    {
                        ID = actIndex++,
                        Name = _tzSettings.TZCodes.GetNameOfAct(actCode.Value) + " " + actAddName,
                        CodeOfName = actCode.Value,
                        StartTime = actStartTime,
                        EndTime = actEndTime,
                        CodeOfStatus = actStatus.Value,
                        ControlTime = actControlTime,
                        EquipmentStart = equipmentStart,
                        EquipmentEnd = equipmentEnd,
                        EquipmentControl = equipmentControl,
                        Status = _tzSettings.TZCodes.GetNameOfStatus(actStatus.Value)
                    });
                }
                catch (Exception ex)
                {
                    LogMessage($"Exception on j = {j}. Exception: {ex}");
                }
            }
            return acts;
        }

        private void GetActStatus(OpcHdaResultsCollection resultsCollection, out int? statusValue, out DateTime timeStamp)
        {
            statusValue = null;
            timeStamp = DateTime.MinValue;
            var actStatusResult = resultsCollection.FilterResults(FilterType.ValueNotNull).LastOrDefault(p => Convert.ToInt32(p.Value) >= 2 && p.Timestamp <= _clearAlgEndTime.AddSeconds(_delta * 2));
            if (actStatusResult != null)
            {
                statusValue = Convert.ToInt32(actStatusResult.Value);
                timeStamp = actStatusResult.Timestamp;
            }
        }

        private int? GetActCode(OpcHdaResultsCollection resultsCollection, DateTime clearStepStartTime, DateTime clearStepEndTime, DateTime actStartTime)
        {
            int? codeValue = null;
            var actCodeResult1 = resultsCollection.FilterResults(FilterType.ValueNotNull).FirstOrDefault(); // try get just first result
            var actCodeResult2 = resultsCollection.FilterResults(FilterType.ValueNotNull).FirstOrDefault(p => p.Timestamp >= clearStepStartTime && p.Timestamp <= clearStepEndTime);
            OpcHdaResultItem actCodeResult = CompareOpcHdaResultItems(actCodeResult1, actCodeResult2, actStartTime);
            if (actCodeResult != null)
                codeValue = Convert.ToInt32(actCodeResult.Value);
            return codeValue;
        }

        private int? GetActStart(OpcHdaResultsCollection resultsCollection, DateTime timestamp)
        {
            return GetLastNullableIntValueFromResultsCollection(resultsCollection, p => p.Timestamp <= timestamp.AddSeconds(_delta));
        }

        private int? GetActTime(OpcHdaResultsCollection resultsCollection, DateTime timestamp)
        {
            return GetLastNullableIntValueFromResultsCollection(resultsCollection, p => p.Timestamp <= timestamp.AddSeconds(_delta));
        }

        private int? GetActEstablishedTime(OpcHdaResultsCollection resultsCollection, DateTime timestamp)
        {
            return GetLastNullableIntValueFromResultsCollection(resultsCollection, p => p.Timestamp <= timestamp.AddSeconds(_delta));
        }

        private int? GetLastNullableIntValueFromResultsCollection(OpcHdaResultsCollection resultsCollection, Func<OpcHdaResultItem, bool> predicate)
        {
            int? findValue = null;
            var resultItem = resultsCollection.FilterResults(FilterType.ValueNotNull).LastOrDefault(predicate);
            if (resultItem != null)
                findValue = Convert.ToInt32(resultItem.Value);
            return findValue;
        }

        private void GetEquipmentsStatuses(OpcHdaResultsCollection actTargetResults, DateTime actStartTime, DateTime actEndTime, DateTime clearStepStartTime, DateTime clearStepEndTime, CodeItem actCodeItem, int actNumber,
            out string equipmentStart, out string equipmentEnd, out string equipmentControl, out string actAddName)
        {
            equipmentStart = "---"; equipmentEnd = "---"; equipmentControl = "---"; actAddName = "";
            try
            {
                var queryStartTime = actStartTime.AddMilliseconds(_delta * 100);
                if (actCodeItem.Type.Equals("IPR"))
                    queryStartTime = clearStepStartTime.AddSeconds(_delta);

                var queryEndTime = actEndTime.AddSeconds(_delta);

                var actValueResults = _opcHdaClient.ReadRaw(queryStartTime, queryEndTime, new List<string> { _tzSettings.Signal + $".ACTS.act{actNumber:000}.value" })[0];

                var actValueStart = GetActValueStart(actValueResults, actCodeItem, actStartTime);
                if (!actValueStart.HasValue) return;
                var actValueEnd = GetActValueEnd(actValueResults, clearStepEndTime);
                if (!actValueEnd.HasValue) return;

                var actTarget = GetActTarget(actTargetResults, clearStepEndTime);
                if (!actTarget.HasValue) return;

                // equipment params
                if (actCodeItem != null)
                {

                    equipmentStart = GetEquipmentStatus(actCodeItem.Type, actCodeItem.EUnit, actValueStart, EquipmentStatusType.Start);
                    equipmentEnd = GetEquipmentStatus(actCodeItem.Type, actCodeItem.EUnit, actValueEnd, EquipmentStatusType.End);
                    equipmentControl = GetEquipmentStatus(actCodeItem.Type, actCodeItem.EUnit, actTarget, EquipmentStatusType.Control);

                    // если команда, то нужно добавить к имени еще код комадны
                    if (actCodeItem.Type == "CMD")
                    {
                        var cmdCodeValue = Convert.ToInt32(actTarget);
                        // Если пуск
                        if (cmdCodeValue >= 100)
                        {
                            actAddName = _tzSettings.TZCodes.GetNameOfCommand(cmdCodeValue);
                        }
                    }

                    // если агрегат, то нужно еще добавить номер агрегата
                    if (actCodeItem.Type == "PMP")
                    {
                        var pmpNumberCmd = GetPmpNumber(actTarget);
                        if (pmpNumberCmd > 0)
                        {
                            actAddName += _tzSettings.TZCodes.GetNameOfCommand(pmpNumberCmd);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Exception: {ex}");
            }
        }

        private double? GetActValueStart(OpcHdaResultsCollection resultsCollection, CodeItem actCodeItem, DateTime actStartTime)
        {
            double? actValueStart = null;

            var actValueStartResult = resultsCollection.FilterResults(FilterType.ValueNotNull).FirstOrDefault();

            if ((actCodeItem.Type.Equals("PMP") || actCodeItem.Type.StartsWith("VLV")) && Convert.ToInt32(actValueStartResult.Value) == 0)
                actValueStartResult = resultsCollection.FilterResults(FilterType.ValueNotNull).FirstOrDefault(p => Convert.ToInt32(p.Value) > 0);

            if (actCodeItem.Type.StartsWith("IP") && (Convert.ToDouble(actValueStartResult.Value) > 10 || Convert.ToDouble(actValueStartResult.Value) == 0))
            {
                // firstTry
                var actValueStartResult1 = resultsCollection.FilterResults(FilterType.ValueNotNull).FirstOrDefault(p => Convert.ToDouble(p.Value) <= 10 && Convert.ToDouble(p.Value) > 0);
                // secondTry
                var actValueStartResult2 = resultsCollection.FilterResults(FilterType.ValueNotNull).FirstOrDefault(p => Convert.ToDouble(p.Value) <= 10);

                if (actValueStartResult1 != null || actValueStartResult2 != null)
                {
                    if (actValueStartResult1 == null)
                        actValueStartResult = actValueStartResult2;
                    if (actValueStartResult2 == null)
                        actValueStartResult = actValueStartResult1;

                    if (actValueStartResult1 != null && actValueStartResult2 != null)
                    {
                        if (GetDateTimeDiff(actValueStartResult1.Timestamp, actStartTime) <= GetDateTimeDiff(actValueStartResult2.Timestamp, actStartTime))
                            actValueStartResult = actValueStartResult1;
                        else
                            actValueStartResult = actValueStartResult2;
                    }
                }
            }

            if (actValueStartResult != null)
                actValueStart = Convert.ToDouble(actValueStartResult.Value);

            return actValueStart;
        }

        private double? GetActTarget(OpcHdaResultsCollection resultsCollection, DateTime clearStepEndTime)
        {
            double? actTargetValue = null;
            var actTargetResult = resultsCollection.FilterResults(FilterType.ValueNotNull).LastOrDefault(p => p.Timestamp <= clearStepEndTime.AddSeconds(_delta));
            //LogSelectedResult(actTargetResults, actTargetResult);
            if (actTargetResult != null)
                actTargetValue = Convert.ToDouble(actTargetResult.Value);

            return actTargetValue;
        }

        private double? GetActValueEnd(OpcHdaResultsCollection resultsCollection, DateTime clearStepEndTime)
        {
            double? actValueEnd = null;
            var actValueEndResult = resultsCollection.FilterResults(FilterType.ValueNotNull).LastOrDefault(p => p.Timestamp <= clearStepEndTime.AddSeconds(_delta));

            if (actValueEndResult != null)
                actValueEnd = Convert.ToDouble(actValueEndResult.Value);

            return actValueEnd;
        }

        private double GetDateTimeDiff(DateTime dt1, DateTime dt2)
        {
            return Math.Abs(TimeSpan.FromTicks(dt1.Ticks).TotalSeconds - TimeSpan.FromTicks(dt2.Ticks).TotalSeconds);
        }

        private OpcHdaResultItem CompareOpcHdaResultItems(OpcHdaResultItem item1, OpcHdaResultItem item2, DateTime searchTimestamp)
        {
            // null compare
            if (item1 == null && item2 == null)
                return null;
            if (item1 == null)
                return item2;
            if (item2 == null)
                return item1;

            if (GetDateTimeDiff(item1.Timestamp, searchTimestamp) > GetDateTimeDiff(item2.Timestamp, searchTimestamp))
                return item2;
            else
                return item1;
        }

        private string GetEquipmentStatus(string actType, string actEunit, object val, EquipmentStatusType equipmentStatusType)
        {
            // null checking
            if (actType == null)
                return "---";

            // eq start and without begin => ---
            if (equipmentStatusType == EquipmentStatusType.Start && actType.EndsWith("WITHOUT_BEGIN"))
                return "---";

            string equipmentStatus = "";
            if (actType == "VLV")
            {
                switch (Convert.ToInt32(val))
                {
                    case 0:
                        equipmentStatus = "Не определен";
                        break;
                    case 1:
                        equipmentStatus = "Открыта";
                        break;
                    case 2:
                        equipmentStatus = "Закрывается";
                        break;
                    case 3:
                        equipmentStatus = "Закрыта";
                        break;
                    case 4:
                        equipmentStatus = "Открывается";
                        break;
                }
            }
            else if (actType == "VLV_GRP")
            {
                switch (Convert.ToInt32(val))
                {
                    case 0:
                        equipmentStatus = "Не определен";
                        break;
                    case 1:
                        equipmentStatus = "Открыт";
                        break;
                    case 2:
                        equipmentStatus = "Закрыт";
                        break;
                    case 3:
                        equipmentStatus = "Закрывается";
                        break;
                    case 4:
                        equipmentStatus = "Открывается";
                        break;
                }
            }
            else if (actType == "PMP")
            {
                int int32 = Convert.ToInt32(val);
                if (int32 > 1000)
                    int32 %= 1000;
                switch (int32)
                {
                    case 1:
                        equipmentStatus = "Включен";
                        break;
                    case 2:
                        equipmentStatus = "Пуск";
                        break;
                    case 5:
                        equipmentStatus = "Выключен";
                        break;
                    case 8:
                        equipmentStatus = "Останов";
                        break;
                }
            }
            else if (actType.StartsWith("IP"))
                equipmentStatus = Convert.ToDouble(val).ToString("0.000") + " " + actEunit;
            else if (actType.StartsWith("OUT"))
                equipmentStatus = Convert.ToInt32(val).ToString("0") + " " + actEunit;
            else
                equipmentStatus = "---";

            return equipmentStatus;
        }

        private void LogSelectedResult(OpcHdaResultsCollection results, OpcHdaResultItem selectedItem)
        {
            LogResult($"Selected result: ", selectedItem);
            LogResults(results);
        }

        private void LogResults(OpcHdaResultsCollection resultsCollection)
        {
            using (var logger = new StreamWriter("Log.log", true))
            {
                logger.WriteLine($"{resultsCollection.ItemName} from {resultsCollection.StartTime} to {resultsCollection.EndTime}");
                foreach (OpcHdaResultItem result in resultsCollection)
                {
                    logger.WriteLine($"\t{result.Value} | {result.Timestamp} | {result.Quality}");
                }
            }
        }

        private void LogResult(string message, OpcHdaResultItem result)
        {
            using (var logger = new StreamWriter("Log.log", true))
            {
                if (result != null)
                    logger.WriteLine($"{message}\t{ result.Value} | { result.Timestamp} | { result.Quality}");
                else
                    logger.WriteLine($"{message}\tnull");
            }
        }

        private void LogMessage(string message)
        {
            using (var logger = new StreamWriter("Log.log", true))
            {
                logger.WriteLine($"{message}");
            }
        }
    }

    public static class HdaResultsExtensions
    {
        public static OpcHdaResultItem GetFirstResult(this IEnumerable<OpcHdaResultItem> items, DateTime startTimestamp, DateTime endTimestamp)
        {
            var range = items.Where(p => p.Timestamp >= startTimestamp && p.Timestamp <= endTimestamp);
            if (range.Count() > 0)
                return range.First();
            else
                return items.LastOrDefault(p => p.Timestamp <= startTimestamp);
        }


        public static OpcHdaResultItem GetLastResult(this IEnumerable<OpcHdaResultItem> items, DateTime startTimestamp, DateTime endTimestamp)
        {
            var range = items.Where(p => p.Timestamp >= startTimestamp && p.Timestamp <= endTimestamp);
            if (range.Count() > 0)
                return range.Last();
            else
                return items.FirstOrDefault(p => p.Timestamp >= endTimestamp);
        }

        public static IEnumerable<OpcHdaResultItem> GetResultsRange(this IEnumerable<OpcHdaResultItem> items, DateTime startTimestamp, DateTime endTimestamp, bool includeBoundValues)
        {
            var resultsCollection = new List<OpcHdaResultItem>();
            if (includeBoundValues && items.LastOrDefault(p => p.Timestamp <= startTimestamp) != null)
                resultsCollection.Add(items.LastOrDefault(p => p.Timestamp <= startTimestamp));
            resultsCollection.AddRange(items.Where(p => p.Timestamp >= startTimestamp && p.Timestamp <= endTimestamp));
            if (includeBoundValues && items.FirstOrDefault(p => p.Timestamp >= endTimestamp) != null)
                resultsCollection.Add(items.FirstOrDefault(p => p.Timestamp >= endTimestamp));

            return resultsCollection;
        }
    }
}
