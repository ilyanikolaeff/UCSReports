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
    class ProtectionsAlarmsViewModel : ViewModelBase, IReportViewModel
    {
        public List<Protection> ProtectionsList { get; set; }

        public string ReportName { get; set; }

        public string TZName { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public ICommand SaveReportCommand { get; private set; }

        public ProtectionsAlarmsViewModel()
        {
            SaveReportCommand = new DelegateCommand(() =>
            {
                try
                {
                    DXSplashScreen.Show<SplashScreenView>();
                    DXSplashScreen.SetState("Экспорт истории срабатывания защит в файл...");
                    string fileName = $"{TZName}. {ReportName}";
                    var exportHelper = new ExportHelper();

                    var data = new List<string>
                        {
                            ReportName,
                            TZName,
                            $"{StartTime} - {EndTime}",
                            fileName
                        };

                    exportHelper.ExportProtections(data, ProtectionsList, out string savedFileName);
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
