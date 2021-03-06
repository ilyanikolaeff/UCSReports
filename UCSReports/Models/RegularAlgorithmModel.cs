using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UCSReports
{
    class RegularAlgorithmModel
    {
        public List<RegularStep> GetNormalSteps(Algorithm selectedAlgorithm, string selectedTZ)
        {
            var opcHdaClient = OPCHDAClient.GetInstance();

            // формируем теги для считывания (ТУ-1) 
            var tzSettings = Settings.GetInstance().TZSettings.Where(p => p.Name == selectedTZ).FirstOrDefault();

            var tagNames = new List<string>();
            var rootSignal = tzSettings.Signal;
            var maxActsCount = tzSettings.MaxActsCount;
            for (int i = 1; i <= maxActsCount; i++)
            {
                tagNames.Add(rootSignal + $".TASK.STEP{i:00}");
                tagNames.Add(rootSignal + $".TASK.STEP{i:00}.status");
                tagNames.Add(rootSignal + $".TASK.STEP{i:00}.time");
                tagNames.Add(rootSignal + $".TASK.STEP{i:00}.et");
                tagNames.Add(rootSignal + $".TASK.STEP{i:00}.start_act");
                tagNames.Add(rootSignal + $".TASK.STEP{i:00}.count_act");
            }

            // Поджимаем время начала и окончания слева и справа
            var algStartTime = selectedAlgorithm.StartTime;
            var algEndTime = selectedAlgorithm.EndTime;

            var readResults = opcHdaClient.ReadRaw(algStartTime, algEndTime, 0, true, tagNames);

            var steps = ReportObject.GetAlgorithmSteps(readResults, tzSettings, AlgorithmType.Regular, algStartTime, algEndTime);

            return steps.Cast<RegularStep>().ToList();
        }
    }
}
