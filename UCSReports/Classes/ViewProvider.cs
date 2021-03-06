using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UCSReports
{
    class ViewProvider
    {
        public void ShowView(IReportViewModel reportViewModel)
        {
            var view = new ReportView()
            {
                DataContext = new ReportViewModel
                {
                    CurrentVM = reportViewModel
                }
            };
            view.Show();
        }
    }

    public enum ReportType
    {
        ListOfAlgorithms,
        RegularAlgorithm,
        EmergencyAlgorithm,
        ProtectionsAlarms
    }
}
