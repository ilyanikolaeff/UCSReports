using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace UCSReports
{
    /// <summary>
    /// Interaction logic for AlgorithmsView.xaml
    /// </summary>
    public partial class ListOfAlgorithmsUserControl
    {
        public ListOfAlgorithmsUserControl()
        {
            InitializeComponent();
        }

        private void BestFitColumns()
        {
            TableView.BestFitColumns();
        }

        private void ListOfAlgorithmView_Loaded(object sender, RoutedEventArgs e)
        {
            BestFitColumns();
        }
    }
}
