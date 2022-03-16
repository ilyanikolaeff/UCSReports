using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace UCSReports
{
    class ListOfAlgorithmsViewModel : ViewModelBase, IReportViewModel
    {
        public ICommand OpenAlgorithmReportCommand { get; private set; }
        public bool CanExecuteOpenAlgorithmReportCommand
        {
            get { return GetValue<bool>(); }
            set { SetValue(value); }
        }
        public List<Algorithm> Algorithms { get; set; }

        private Algorithm _selectedAlgorithm;
        public Algorithm SelectedAlgorithm
        {
            get
            {
                return _selectedAlgorithm;
            }
            set
            {
                _selectedAlgorithm = value;
                RaisePropertiesChanged();
                CanExecuteOpenAlgorithmReportCommand = true;
            }
        }
        public string ReportName { get; set; }
        public string TZName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public ListOfAlgorithmsViewModel()
        {
            OpenAlgorithmReportCommand = new DelegateCommand(
                () =>
                {
                    try
                    {
                        if (SelectedAlgorithm != null)
                        {
                            DXSplashScreen.Show<SplashScreenView>();
                            DXSplashScreen.SetState("Загрузка шагов алгоритма...");

                            IReportViewModel currentVM = GetViewModel();

                            DXSplashScreen.Close();

                            var mvvmProvider = new ViewProvider();
                            mvvmProvider.ShowView(currentVM, $"Отчет по шагам алгоритма {SelectedAlgorithm.Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        DXSplashScreen.Close();
                        DXMessageBox.Show(ex.ToString(), "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                },
                () => CanExecuteOpenAlgorithmReportCommand);
        }

        private IReportViewModel GetViewModel()
        {
            if (SelectedAlgorithm.AlgorithmType == AlgorithmType.Emergency)
            {
                var emergencyAlgViewModel = new EmergencyAlgorithmViewModel();
                var emergencyAlgModel = new EmergencyAlgorithmModel();

                emergencyAlgViewModel.CurrentAlgorithm = SelectedAlgorithm;
                emergencyAlgViewModel.CurrentAlgorithm.Steps = new List<Step>(emergencyAlgModel.GetEmergencyAlgorithmSteps(SelectedAlgorithm, TZName));
                emergencyAlgViewModel.TZName = TZName;

                return emergencyAlgViewModel;
            }
            else if (SelectedAlgorithm.AlgorithmType == AlgorithmType.Regular)
            {
                var regularAlgViewModel = new RegularAlgorithmViewModel();
                var regularAlgModel = new RegularAlgorithmModel();

                regularAlgViewModel.CurrentAlgorithm = SelectedAlgorithm;
                regularAlgViewModel.CurrentAlgorithm.Steps = new List<Step>(regularAlgModel.GetRegularAlgorithmSteps(SelectedAlgorithm, TZName));
                regularAlgViewModel.TZName = TZName;

                return regularAlgViewModel;
            }
            else
                return null;
        }

    }
}
