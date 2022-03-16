using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UCSReports
{
    class RegularAlgorithmModel
    {
        public List<RegularStep> GetRegularAlgorithmSteps(Algorithm selectedAlgorithm, string selectedTZ)
        {
            var hdaClient = CommAdapter.GetInstance().OpcHdaClient;
            var tzSettings = Settings.GetInstance().TZSettings.Where(p => p.Name == selectedTZ).FirstOrDefault();

            // Поджимаем время начала и окончания слева и справа
            var algStartTime = selectedAlgorithm.StartTime;
            var algEndTime = selectedAlgorithm.EndTime;

            var steps = new RegularAlgorithmReportBuilder(hdaClient, tzSettings).GetAlgorithmSteps(algStartTime, algEndTime);

            return steps.Cast<RegularStep>().ToList();
        }
    }
}
