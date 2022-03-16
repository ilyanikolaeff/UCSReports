using DevExpress.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UCSReports
{
    class ReportViewModel : ViewModelBase
    {
        public object CurrentVM
        {
            get { return GetValue<object>(); }
            set { SetValue(value); }
        }
    }
}
