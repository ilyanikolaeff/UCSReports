using DevExpress.Xpf.Core;
using System;
using System.IO;
using System.Windows;

namespace UCSReports
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            // проверки настроек и хда клиента
            try
            {
                var settings = Settings.GetInstance();
                var hdaClient = CommAdapter.GetInstance().OpcHdaClient;
                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            }
            catch (Exception ex)
            {
                DXSplashScreen.Close();
                DXMessageBox.Show($"Ошибка инициализации приложения\n{ex}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }

            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            DXSplashScreen.Close();
            Activate();
        }

        private void ThemedWindow_Closed(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
