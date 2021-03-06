using System;
using System.Collections.Generic;
using System.Linq;
using ResultList = UCSReports.HistoryResultsCollection;

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

        private static int GetPmpNumber(object val)
        {
            bool parseResult = int.TryParse(val.ToString(), out int intValue);
            if (parseResult && intValue >= 1000)
                return intValue - (intValue % 1000);
            else
                return -1;
        }

        private static string GetEquipmentStatus(string actType, string actEunit, object val, EquipmentStatusType equipmentStatusType)
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

        public static List<Step> GetAlgorithmSteps(List<ResultList> readResults, TechnologyZoneSettings tzSettings, AlgorithmType algorithmType, DateTime start, DateTime end)
        {
            var steps = new List<Step>();

            int increment = algorithmType == AlgorithmType.Emergency ? 4 : 6;

            int stepIndex = 1;
            for (int i = 0; i <= readResults.Count - increment; i += increment)
            {
                var codes = readResults[i].Results; // коды 
                var statuses = readResults[i + 1].Results; // статусы
                var times = readResults[i + 2].Results; // накопленное время
                var ets = readResults[i + 3].Results; // контрольное время 

                // step code
                var stepCodeResult = codes.GetResult(start, end, FindType.Last, IntervalChangeType.None, FilterType.QualityGood);
                // step status start
                var stepStatusStartResult = statuses.GetResult(start, end, FindType.First, IntervalChangeType.None, FilterType.QualityGood);
                // step status end
                var stepStatusEndResult = statuses.GetResult(start, end, FindType.Last, IntervalChangeType.None, FilterType.QualityGood);
                // step time
                var stepTimeResult = times.GetResult(start, end, FindType.First, IntervalChangeType.None, FilterType.QualityGood);
                // step established time
                var stepEtResult = ets.GetResult(start, end, FindType.Last, IntervalChangeType.None, FilterType.QualityGood);

                if (stepCodeResult == null || stepStatusStartResult == null || stepStatusEndResult == null || stepTimeResult == null || stepEtResult == null)
                    continue;

                // code name
                int codeName = int.Parse(stepCodeResult.Value.ToString());
                // code status
                int codeStatus = int.Parse(stepStatusEndResult.Value.ToString());
                if (codeStatus < 2)
                    continue;
                // time start
                DateTime startTime = stepStatusStartResult.Timestamp;
                int establishedTimeSeconds = int.Parse(stepEtResult.Value.ToString());
                // time end
                DateTime endTime = stepStatusEndResult.Timestamp;
                //endTime = startTime.AddSeconds(establishedTimeSeconds);
                // control time
                int controlTimeSeconds = int.Parse(stepTimeResult.Value.ToString());
                TimeSpan controlTime = TimeSpan.FromSeconds(controlTimeSeconds - establishedTimeSeconds);
                if (controlTime < TimeSpan.Zero)
                    controlTime = TimeSpan.Zero;

                // current step 
                Step currentStep = algorithmType == AlgorithmType.Regular ? new RegularStepCreator().CreateStep() : new EmergencyStepCreator().CreateStep();
                // index of step
                currentStep.ID = stepIndex;
                // code of name (int value)
                currentStep.CodeOfName = codeName;
                // code of status
                currentStep.CodeOfStatus = codeStatus;
                // name (by code)
                currentStep.Name = algorithmType == AlgorithmType.Emergency ? tzSettings.TZCodes.GetNameOfAlarmStep(codeName) : tzSettings.TZCodes.GetNameOfStep(codeName);
                // status (by code)
                currentStep.Status = tzSettings.TZCodes.GetNameOfStatus(codeStatus);
                // times
                currentStep.StartTime = startTime;
                currentStep.EndTime = endTime;
                currentStep.ControlTime = controlTime;

                steps.Add(currentStep);
                stepIndex++;

                if (algorithmType == AlgorithmType.Regular)
                {
                    if (codeStatus > 7)
                        continue;

                    var startAct = readResults[i + 4].Results;
                    var countsActs = readResults[i + 5].Results;

                    // start act number
                    var startActNumberResult = startAct.GetResult(startTime, endTime, FindType.Last, IntervalChangeType.Extension, FilterType.QualityGood);
                    // count of acts (from start number)
                    var countOfActsResult = countsActs.GetResult(startTime, endTime, FindType.Last, IntervalChangeType.Extension, FilterType.QualityGood);

                    if (countOfActsResult == null || startActNumberResult == null)
                        continue;

                    var opcHdaClient = OPCHDAClient.GetInstance();

                    int countOfActs = int.Parse(countOfActsResult.Value.ToString());
                    if (countOfActs > 0)
                    {
                        int actIndex = 1;
                        int.TryParse(startActNumberResult.Value.ToString(), out int startActNumber);
                        startActNumber++;
                        for (int j = startActNumber; j < startActNumber + countOfActs; j++)
                        {
                            // формируем массив тегов для запроса 
                            var tagNames = new List<string>()
                            {
                                tzSettings.Signal + $".ACTS.act{j:000}",
                                tzSettings.Signal + $".ACTS.act{j:000}.status",
                                tzSettings.Signal + $".ACTS.act{j:000}.time",
                                tzSettings.Signal + $".ACTS.act{j:000}.et",
                                tzSettings.Signal + $".ACTS.act{j:000}.value",
                                tzSettings.Signal + $".ACTS.act{j:000}.target"
                            };

                            var actsReadResults = opcHdaClient.ReadRaw(startTime, endTime, 0, true, tagNames);

                            var actCodes = actsReadResults[0].Results;
                            var actStatuses = actsReadResults[1].Results;
                            var actTimes = actsReadResults[2].Results;
                            var actEts = actsReadResults[3].Results;
                            var actValues = actsReadResults[4].Results;
                            var actTargets = actsReadResults[5].Results;

                            // code of act
                            var actCodeResult = actCodes.GetResult(startTime, endTime, FindType.Last, IntervalChangeType.Extension, FilterType.QualityGood);
                            // act status start
                            var actStatusStartResult = actStatuses
                                .FilterResults(FilterType.ValueNotNull)
                                .Where(p => int.Parse(p.Value.ToString()) >= 2)
                                .GetResult(startTime, endTime, FindType.First, IntervalChangeType.Extension, FilterType.QualityGood);
                            // act status end
                            var actStatusEndResult = actStatuses
                                .FilterResults(FilterType.ValueNotNull)
                                .Where(p => int.Parse(p.Value.ToString()) >= 2)
                                .GetResult(startTime, endTime, FindType.Last, IntervalChangeType.Extension, FilterType.QualityGood);
                            //GetActStatusesResults(actStatuses, out HistoryResult actStatusStartResult, out HistoryResult actStatusEndResult);
                            // act time
                            var actTimeResult = actTimes.GetResult(startTime, endTime, FindType.Last, IntervalChangeType.Extension, FilterType.QualityGood);
                            // act established time
                            var actEtResult = actEts.GetResult(startTime, endTime, FindType.Last, IntervalChangeType.Extension, FilterType.QualityGood);
                            // act value start
                            var actValueResultStart = actValues.GetResult(startTime, endTime, FindType.First, IntervalChangeType.Constriction, FilterType.QualityGood);
                            // act value end
                            var actValueResultEnd = actValues.GetResult(startTime, endTime, FindType.Last, IntervalChangeType.Extension, FilterType.QualityGood);
                            // act target value end
                            var actTargetResultEnd = actTargets.GetResult(startTime, endTime, FindType.Last, IntervalChangeType.Extension, FilterType.QualityGood);
                            // act target value start
                            var actTargetResultStart = actTargets.GetResult(startTime, endTime, FindType.First, IntervalChangeType.Extension, FilterType.QualityGood);

                            if (actCodeResult == null || actStatusStartResult == null || actStatusEndResult == null || actTimeResult == null 
                                || actEtResult == null || actValueResultStart == null || actValueResultEnd == null || actTargetResultEnd == null)
                                continue;

                            // code of act
                            int.TryParse(actCodeResult.Value.ToString(), out int actCode);
                            if (actCode < 0)
                                continue;
                            // code of status
                            int.TryParse(actStatusEndResult.Value.ToString(), out int actStatusCode);
                            if (actStatusCode < 2 || actStatusCode > 7)
                                continue;
                            // start time
                            DateTime actStartTime = actStatusStartResult.Timestamp;
                            // established time (seconds)
                            int.TryParse(actEtResult.Value.ToString(), out int actEtSeconds);
                            // end time = startTime + establishedTime
                            var actEndTime = actStartTime.AddSeconds(actEtSeconds);
                            // control time (seconds)
                            int.TryParse(actTimeResult.Value.ToString(), out int actCtrlSeconds);
                            TimeSpan actControlTime = TimeSpan.FromSeconds(actCtrlSeconds - actEtSeconds);
                            if (actControlTime < TimeSpan.Zero)
                                actControlTime = TimeSpan.Zero;

                            // name
                            var actName = tzSettings.TZCodes.GetNameOfAct(actCode);

                            // equipment params
                            string equipmentStart = "---", equipmentEnd = "---", equipmentControl = "---";
                            var actSettings = tzSettings.TZCodes.GetAct(actCode);
                            if (actSettings != null)
                            {
                                if (actSettings.Type == "IP")
                                    actValueResultStart = actValues.GetPreviousResult(actValueResultEnd);

                                equipmentStart = GetEquipmentStatus(actSettings.Type, actSettings.EUnit, actValueResultStart.Value, EquipmentStatusType.Start);
                                equipmentEnd = GetEquipmentStatus(actSettings.Type, actSettings.EUnit, actValueResultEnd.Value, EquipmentStatusType.End);
                                equipmentControl = GetEquipmentStatus(actSettings.Type, actSettings.EUnit, actTargetResultEnd.Value, EquipmentStatusType.Control);

                                // если команда, то нужно добавить к имени еще код комадны
                                if (actSettings.Type == "CMD")
                                {
                                    var cmdCodeValue = Convert.ToInt32(actValueResultEnd.Value);
                                    if (cmdCodeValue <= 0)
                                    {
                                        cmdCodeValue = Convert.ToInt32(actValueResultStart.Value);
                                        if (cmdCodeValue <= 0)
                                        {
                                            cmdCodeValue = Convert.ToInt32(actTargetResultEnd.Value);
                                            if (cmdCodeValue <= 0)
                                                cmdCodeValue = Convert.ToInt32(actTargetResultStart.Value);
                                        }
                                    }

                                    if (cmdCodeValue > 0)
                                    {
                                        var cmdName = tzSettings.TZCodes.GetNameOfCommand(cmdCodeValue);
                                        if (!actName.ToLower().Contains("команда ту")) // чисто костыль
                                            actName += $"{cmdName}";
                                    }
                                }

                                // если агрегат, то нужно еще добавить номер агрегата
                                if (actSettings.Type == "PMP")
                                {
                                    var pmpNumberCmd = GetPmpNumber(actTargetResultEnd.Value);
                                    if (pmpNumberCmd > 0)
                                    {
                                        var cmdName = tzSettings.TZCodes.GetNameOfCommand(pmpNumberCmd);
                                        actName += $"{cmdName}";
                                    }
                                }
                            }


                            var act = new Act
                            {
                                ID = actIndex,
                                Name = actName,
                                CodeOfName = actCode,
                                StartTime = actStartTime,
                                EndTime = actEndTime,
                                CodeOfStatus = actStatusCode,
                                ControlTime = actControlTime,
                                EquipmentStart = equipmentStart,
                                EquipmentEnd = equipmentEnd,
                                EquipmentControl = equipmentControl,
                                Status = tzSettings.TZCodes.GetNameOfStatus(actStatusCode)
                            };
                            ((RegularStep)currentStep).Acts.Add(act);
                            actIndex++;
                        }
                    }
                }
            }
            return steps;
        }


        public static void GetActStatusesResults(List<HistoryResult> statuses, out HistoryResult statusStart, out HistoryResult statusEnd)
        {
            statusStart = null;
            statusEnd = null;
            var listOfStatuses = statuses
                .Where(p => p.Quality.GetCode() >= 192 && p.Value != null)
                .OrderBy(ks => ks.Timestamp)
                .Where(p => int.Parse(p.Value.ToString()) >= 2)
                .ToList();
            
            // ищем сначала первый нормальный статус
            for (int i = 0; i < listOfStatuses.Count - 1; i++)
            {
                var currentValue = int.Parse(listOfStatuses[i].Value.ToString());
                var nextValue = int.Parse(listOfStatuses[i + 1].Value.ToString());
                if (currentValue >= 2 && nextValue >= 2)
                {
                    statusStart = listOfStatuses[i];
                    statusEnd = listOfStatuses[i + 1];
                    return;
                }
            }

            // возможно у нас шаг пропущен, поэтому там будет только 1 значение (для аварийного)
            if (listOfStatuses.Count == 1)
            {
                statusStart = listOfStatuses[0];
                statusEnd = listOfStatuses[0];
            }       
        }

        enum EquipmentStatusType
        {
            Start,
            End,
            Control
        }
    }
}
