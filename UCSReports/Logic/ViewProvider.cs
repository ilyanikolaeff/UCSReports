using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UCSReports
{
    class ViewProvider
    {
        public void ShowView(IReportViewModel reportViewModel, string title)
        {
            var view = new ReportView()
            {
                DataContext = new ReportViewModel
                {
                    CurrentVM = reportViewModel
                }
            };
            view.Title = title;
            view.Show();
        }
    }
}
