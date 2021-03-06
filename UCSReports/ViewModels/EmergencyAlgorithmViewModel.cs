using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using System.Windows.Input;
using DevExpress.Xpf.Core;

namespace UCSReports
{
    class EmergencyAlgorithmViewModel : ViewModelBase, IReportViewModel
    {
        public string ReportName { get { return CurrentAlgorithm.Name; } }
        public string TZName { get; set; }
        public string Status { get { return CurrentAlgorithm.Status; } }
        public DateTime StartTime { get { return CurrentAlgorithm.StartTime; } }
        public DateTime EndTime { get { return CurrentAlgorithm.EndTime; } }
        public Algorithm CurrentAlgorithm { get; set; }
        public ICommand SaveReportCommand { get; private set; }
        public EmergencyAlgorithmViewModel()
        {
            SaveReportCommand = new DelegateCommand(() =>
            {
                try
                {
                    DXSplashScreen.Show<SplashScreenView>();
                    DXSplashScreen.SetState("Экспорт шагов алгоритма в файл...");
                    string fileName = $"{StartTime:yyyy_MM_dd} {ReportName}";
                    var exportHelper = new ExportHelper();

                    var data = new List<string>
                        {
                            TZName,
                            ReportName,
                            $"{StartTime} - {EndTime}",
                            Status,
                            fileName
                        };

                    exportHelper.ExportAlgorithm(data, CurrentAlgorithm.Steps, AlgorithmType.Emergency, out string savedFileName);
                    DXSplashScreen.Close();
                    DXMessageBox.Show($"Экспорт завершен!\nСохраненный файл: {savedFileName}", "Информация", System.Windows.MessageBoxButton.OK);
                }
                catch (Exception ex)
                {
                    DXSplashScreen.Close();
                    DXMessageBox.Show(ex.ToString(), "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            });
        }
    }
}
