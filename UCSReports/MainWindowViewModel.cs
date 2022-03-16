using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Input;

namespace UCSReports
{
    class MainWindowViewModel : ViewModelBase, INotifyPropertyChanged
    {
        public ICommand OpenAlgsReportsCommand { get; private set; }
        public bool CanExecuteOpenAlgsReportsCommand
        {
            get { return GetValue<bool>(); }
            set { SetValue(value); }
        }

        public ICommand OpenProtsReportsCommand { get; private set; }
        public bool CanExecuteOpenProtsReportsCommand
        {
            //get { return GetValue<bool>(); }
            get { return false; }
            set { SetValue(value); }
        }

        public ICommand OpenMasksReportsCommand { get; private set; }
        public bool CanExecuteOpenMasksReportsCommand
        {
            //get { return GetValue<bool>(); }
            get { return false; }
            set { SetValue(value); }
        }

        public ICommand OpenSettingsCommand { get; private set; }
        public bool CanExecuteOpenSettingsCommand
        {
            get { return GetValue<bool>(); }
            set { SetValue(value); }
        }

        public List<string> TZNames { get; set; }

        private DateTime _pickedStartTime;
        public DateTime PickedStartTime
        {
            get
            {
                return _pickedStartTime;
            }
            set
            {
                _pickedStartTime = value;
                RaisePropertyChanged();
            }
        }

        private DateTime _pickedEndTime;
        public DateTime PickedEndTime
        {
            get
            {
                return _pickedEndTime;
            }
            set
            {
                _pickedEndTime = value;
                RaisePropertyChanged();
            }
        }

        private string _tzName;
        public string TZName
        {
            get
            {
                return _tzName;
            }
            set
            {
                _tzName = value;
                CanExecuteOpenAlgsReportsCommand = Settings.GetInstance().TZSettings.FirstOrDefault(p => p.Name == value).IsExistRepSettings;
                CanExecuteOpenProtsReportsCommand = Settings.GetInstance().TZSettings.FirstOrDefault(p => p.Name == value).IsExistProtSettings;
                CanExecuteOpenMasksReportsCommand = Settings.GetInstance().TZSettings.FirstOrDefault(p => p.Name == value).IsExistProtSettings;
            }
        }

        public bool ConnectionStatus
        {
            get { return GetValue<bool>(); }
            set
            {
                SetValue(value);
            }
        }

        public string ConnectionString
        {
            get
            {
                if (ConnectionStatus)
                    return $"Подключен к {Settings.GetInstance().HdaConnectionSettings.IPAddress}/{Settings.GetInstance().HdaConnectionSettings.ServerName}";
                else
                    return $"Ошибка подключения к {Settings.GetInstance().HdaConnectionSettings.IPAddress}/{Settings.GetInstance().HdaConnectionSettings.ServerName}";
            }
        }

        public MainWindowViewModel()
        {
            PickedStartTime = DateTime.Now.AddDays(-1);
            PickedEndTime = DateTime.Now;

            TZNames = Settings.GetInstance().TZSettings.Select(s => s.Name).ToList();

            // скрываем кнопки защиты и маскирование
            CanExecuteOpenAlgsReportsCommand = false;
            CanExecuteOpenProtsReportsCommand = false;
            CanExecuteOpenMasksReportsCommand = false;

            // 
            CanExecuteOpenSettingsCommand = true;

            OpenAlgsReportsCommand = new DelegateCommand(
                () =>
                    {
                        try
                        {
                            if (!IsCommandAvailable())
                                return;

                            DXSplashScreen.Show<SplashScreenView>();
                            DXSplashScreen.SetState("Загрузка списка алгоритмов...");

                            var vm = new ListOfAlgorithmsViewModel(); // view - model
                            var model = new ListOfAlgorithmsModel(); // model

                            // Заполняем ViewModel
                            vm.ReportName = $"Отчет по выполненным алгоритмам";
                            vm.TZName = TZName;
                            vm.StartTime = PickedStartTime;
                            vm.EndTime = PickedEndTime;
                            var algs = model.GetAlgorithms(PickedStartTime, PickedEndTime, Settings.GetInstance().TZSettings.Where(p => p.Name == TZName).FirstOrDefault());
                            vm.Algorithms = new List<Algorithm>(algs);

                            DXSplashScreen.Close();

                            var mvvmProvider = new ViewProvider();
                            mvvmProvider.ShowView(vm, $"Список выполненных алгоритмов");
                        }
                        catch (Exception ex)
                        {
                            DXSplashScreen.Close();
                            DXMessageBox.Show(ex.ToString(), "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        }
                    },
                () => CanExecuteOpenAlgsReportsCommand);



            OpenSettingsCommand = new DelegateCommand(
                () =>
                    {
                        try
                        {
                            Process.Start("notepad.exe", AppDomain.CurrentDomain.BaseDirectory + @"\Settings.xml");
                        }
                        catch (Exception ex)
                        {
                            DXSplashScreen.Close();
                            DXMessageBox.Show(ex.ToString(), "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        }
                    },
                () => CanExecuteOpenSettingsCommand);

            var monitoringThread = new Thread(StartConnStatusMonitoring);
            monitoringThread.Start();
        }

        private void Model_NotifyProgress(string message, double progressValue)
        {
            DXSplashScreen.SetState(message);
            DXSplashScreen.Progress(progressValue);
        }

        private void StartConnStatusMonitoring()
        {
            // initial check
            ConnectionStatus = CommAdapter.GetInstance().OpcHdaClient.IsConnected;

            var timer = new System.Timers.Timer(5000)
            {
                AutoReset = true,
                Enabled = true
            };
            timer.Elapsed += (sender, e) => { ConnectionStatus = CommAdapter.GetInstance().OpcHdaClient.IsConnected; };
        }

        private bool IsCommandAvailable()
        {
            if (!ConnectionStatus)
            {
                DXMessageBox.Show("Отсутствует подключение к OPC HDA серверу!",
                                "Ошибка", System.Windows.MessageBoxButton.OK,
                                System.Windows.MessageBoxImage.Warning);
                return false;
            }

            if (PickedStartTime > PickedEndTime)
            {
                DXMessageBox.Show("Неправильно указан интервал времени (время начала больше времени окончания)",
                    "Ошибка", System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        //private void ShowReportWindow<T, U>()
        //{
        //}     

    }
}
