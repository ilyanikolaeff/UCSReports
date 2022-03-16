using OPCWrapper.HistoricalDataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UCSReports
{
    class EmergencyAlgorithmReportBuilder : ReportBuilderBase
    {
        private TechnologyZoneSettings _tzSettings;
        private int _delta = Settings.GetInstance().Delta;
        private DateTime _algorithmStartTime;
        private DateTime _algorithmEndTime;
        private DateTime _clearAlgStartTime;
        private DateTime _clearAlgEndTime;
        private OpcHdaClient _opcHdaClient;
        public EmergencyAlgorithmReportBuilder(TechnologyZoneSettings tzSettings, OpcHdaClient opcHdaClient)
        {
            _tzSettings = tzSettings;
            _opcHdaClient = opcHdaClient;
        }

        internal override List<Step> GetAlgorithmSteps(DateTime algStartTime, DateTime algEndTime)
        {
            var steps = new List<Step>();
            _clearAlgStartTime = algStartTime;
            _clearAlgEndTime = algEndTime;
            _algorithmStartTime = algStartTime.AddSeconds(_delta);
            _algorithmEndTime = algEndTime.AddSeconds(-_delta);
            // читаем короче все шаги с времени начала до времени окончания
            var historyResults = GetEmergencyStepsHistory(_algorithmStartTime, _algorithmEndTime);
            int increment = 5;
            int stepIndex = 1;
            for (int i = 0; i <= historyResults.Count - increment; i += increment)
            {
                var codeResults = historyResults[i]; // коды 
                var startResults = historyResults[i + 1]; // время начала
                var statusResults = historyResults[i + 2]; // статусы
                var timesResults = historyResults[i + 3]; // накопленное время
                var etResults = historyResults[i + 4]; // контрольное время 

                // status
                var stepStatusResult = statusResults.FilterResults(FilterType.ValueNotNull).LastOrDefault(p => Convert.ToInt32(p.Value) >= 2 && p.Timestamp <= _algorithmEndTime.AddSeconds(_delta));
                if (stepStatusResult == null)
                    continue;

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
                var stepStartTime = stepEndTime.AddSeconds(-stepEstablishedTimeSeconds); // время начала = время окончания - затраченное время на выполнение

                // current step 
                EmergencyStep currentStep = new EmergencyStep
                {
                    // index of step
                    ID = stepIndex++,
                    // code of name (int value)
                    CodeOfName = stepCode,
                    // code of status
                    CodeOfStatus = stepStatusCode,
                    // name (by code)
                    Name = _tzSettings.TZCodes.GetNameOfAlarmStep(stepCode),
                    // status (by code)
                    Status = _tzSettings.TZCodes.GetNameOfStatus(stepStatusCode),
                    // times
                    StartTime = stepStartTime,
                    EndTime = stepEndTime,
                    ControlTime = controlTime
                }; // xD

                steps.Add(currentStep);
            }

            return steps;
        }

        private List<OpcHdaResultsCollection> GetEmergencyStepsHistory(DateTime startTime, DateTime endTime)
        {
            var rootSignal = _tzSettings.Signal;
            var stepsCount = _tzSettings.MaxStepsCount;
            var tagNames = new List<string>();
            for (int i = 1; i <= stepsCount; i++)
            {
                tagNames.Add(rootSignal + $".TASK_ALM_CLOSE.step{i:00}");
                tagNames.Add(rootSignal + $".TASK_ALM_CLOSE.step{i:00}.start");
                tagNames.Add(rootSignal + $".TASK_ALM_CLOSE.step{i:00}.status");
                tagNames.Add(rootSignal + $".TASK_ALM_CLOSE.step{i:00}.time");
                tagNames.Add(rootSignal + $".TASK_ALM_CLOSE.step{i:00}.et");
            }
            var history = _opcHdaClient.ReadRaw(startTime, endTime, tagNames);
            return history;
        }
    }
}
