using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UCSReports
{
    class EmergencyAlgorithmModel
    {
        public List<EmergencyStep> GetEmergencySteps(Algorithm selectedAlg, string selectedTZ)
        {
            // Запрашиваем все шаги
            var opcHdaClient = OPCHDAClient.GetInstance();

            var tzSettings = Settings.GetInstance().TZSettings.Where(p => p.Name == selectedTZ).FirstOrDefault();

            var rootSignal = tzSettings.Signal;
            var stepsCount = tzSettings.MaxStepsCount;
            var tagNames = new List<string>();
            for (int i = 1; i <= stepsCount; i++)
            {
                tagNames.Add(rootSignal + $".TASK_ALM_CLOSE.step{i:00}");
                tagNames.Add(rootSignal + $".TASK_ALM_CLOSE.step{i:00}.status");
                tagNames.Add(rootSignal + $".TASK_ALM_CLOSE.step{i:00}.time");
                tagNames.Add(rootSignal + $".TASK_ALM_CLOSE.step{i:00}.et");
            }

            // Поджимаем время внутрь
            var algStartTime = selectedAlg.StartTime;
            var algEndTime = selectedAlg.EndTime;

            var readResults = opcHdaClient.ReadRaw(algStartTime, algEndTime, 0, true, tagNames);

            var steps = ReportObject.GetAlgorithmSteps(readResults, tzSettings, AlgorithmType.Emergency, algStartTime, algEndTime);
            return steps.Cast<EmergencyStep>().ToList();
        }
    }
}
