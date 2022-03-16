using OPCWrapper.HistoricalDataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UCSReports
{
    class CommAdapter
    {
        private static CommAdapter _instance;
        public static CommAdapter GetInstance()
        {
            if (_instance == null)
                _instance = new CommAdapter();
            return _instance;
        }

        public OpcHdaClient OpcHdaClient { get; set; }

        private CommAdapter()
        {
            var hdaConnSettings = Settings.GetInstance().HdaConnectionSettings;
            OpcHdaClient = new OpcHdaClient(new OPCWrapper.ConnectionSettings(hdaConnSettings.IPAddress, hdaConnSettings.ServerName));
            OpcHdaClient.Connect();
        }
    }
}
