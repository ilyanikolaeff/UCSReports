using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace UCSReports
{
    class ProtectionsMasksViewModel : ViewModelBase, IReportViewModel
    {
        public List<Mask> MasksList { get; set; }

        public string ReportName { get; set; }

        public string TZName { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public ICommand SaveReportCommand { get; private set; }

        public ProtectionsMasksViewModel()
        {
            SaveReportCommand = new DelegateCommand(() =>
            {
                try
                {
                    DXSplashScreen.Show<SplashScreenView>();
                    DXSplashScreen.SetState("Экспорт истории маскирования защит в файл...");
                    string fileName = $"{TZName}. {ReportName}";
                    var exportHelper = new ExportHelper();

                    var data = new List<string>
                        {
                            ReportName,
                            TZName,
                            $"{StartTime} - {EndTime}",
                            fileName
                        };

                    exportHelper.ExportMasks(data, MasksList, out string savedFileName);
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
