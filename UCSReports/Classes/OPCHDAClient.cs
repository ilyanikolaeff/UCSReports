using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Opc.Hda;
using UCSReports;

namespace UCSReports
{
    public class OPCHDAClient
    {
        private static Lazy<OPCHDAClient> _instance = new Lazy<OPCHDAClient>(() => new OPCHDAClient());
        public static OPCHDAClient GetInstance()
        {
            return _instance.Value;
        }

        private Server _hdaServer;
        private OPCHDAClient()
        {
        }
        public void Connect(HdaConnection connectionSettings)
        {
            var url = new Opc.URL($"opchda://{connectionSettings.IPAddress}/{connectionSettings.ServerName}");
            var opcFactory = new OpcCom.Factory();
            _hdaServer = new Server(opcFactory, url);
            _hdaServer.Connect();
        }

        public bool IsConnected
        {
            get
            {
                if (_hdaServer != null)
                {
                    return _hdaServer.IsConnected;
                }
                else
                {
                    return false;
                }
            }
        }

        public List<HistoryResultsCollection> ReadRaw(DateTime startTime, DateTime endTime, int maxValues, bool includeBounds, IEnumerable<string> tagNames)
        {
            var items = new Item[tagNames.Count()];

            int index = 0;
            foreach (var tagName in tagNames)
            {
                items[index] = new Item(new Opc.ItemIdentifier(tagName));
                index++;
            }

            var identifiedResultsArray = _hdaServer.CreateItems(items);
            var historyRawValues = _hdaServer.ReadRaw(new Time(startTime), new Time(endTime), maxValues, includeBounds, identifiedResultsArray);

            var results = new List<HistoryResultsCollection>();
            foreach (var historyValuesCollection in historyRawValues)
            {
                var resultsCollection = new HistoryResultsCollection();
                resultsCollection.ItemName = historyValuesCollection.ItemName;
                resultsCollection.ResultID = historyValuesCollection.ResultID;
                foreach (ItemValue historyValue in historyValuesCollection)
                {
                    resultsCollection.Results.Add(new HistoryResult() { Value = historyValue.Value, Quality = historyValue.Quality, Timestamp = historyValue.Timestamp });
                }
                results.Add(resultsCollection);
            }
            return results;
        }
    }
}
