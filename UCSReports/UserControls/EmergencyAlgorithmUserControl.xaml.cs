namespace UCSReports
{
    public partial class EmergencyAlgorithmUserControl
    {
        public EmergencyAlgorithmUserControl()
        {
            InitializeComponent();
        }

        private void BestFitColumns()
        {
            TableView.BestFitColumns();
        }

        private void EmergencyAlgorithmUserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            BestFitColumns();
        }
    }
}
