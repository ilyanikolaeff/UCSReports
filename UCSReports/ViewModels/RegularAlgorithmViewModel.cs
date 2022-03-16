using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace UCSReports
{
    class RegularAlgorithmViewModel : ViewModelBase, IReportViewModel
    {
        public string ReportName { get { return CurrentAlgorithm.Name; } }
        public string TZName { get; set; }
        public string Status { get { return CurrentAlgorithm.Status; } }
        public DateTime StartTime { get { return CurrentAlgorithm.StartTime; } }
        public DateTime EndTime { get { return CurrentAlgorithm.EndTime; } }
        public Algorithm CurrentAlgorithm { get; set; }
        public RegularAlgorithmViewModel()
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
                        exportHelper.ExportAlgorithm(data, CurrentAlgorithm.Steps, AlgorithmType.Regular, out string savedFileName);
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

        public ICommand SaveReportCommand { get; private set; }
    }
}
