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
        public List<EmergencyStep> GetEmergencyAlgorithmSteps(Algorithm selectedAlg, string selectedTZ)
        {
            // Запрашиваем все шаги
            var opcHdaClient = CommAdapter.GetInstance().OpcHdaClient;
            var tzSettings = Settings.GetInstance().TZSettings.Where(p => p.Name == selectedTZ).FirstOrDefault();

            var algStartTime = selectedAlg.StartTime;
            var algEndTime = selectedAlg.EndTime;


            var steps = new EmergencyAlgorithmReportBuilder(tzSettings, opcHdaClient).GetAlgorithmSteps(algStartTime, algEndTime);
            return steps.Cast<EmergencyStep>().ToList();
        }
    }
}
