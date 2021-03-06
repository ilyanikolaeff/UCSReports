using System;

namespace UCSReports
{
    public class HistoryResult
    {
        public object Value { get; set; }
        public DateTime Timestamp { get; set; }
        public Opc.Da.Quality Quality { get; set; }

        public bool Compare(HistoryResult other)
        {
            return (Value.ToString() == other.Value.ToString()) && (Timestamp == other.Timestamp) && (Quality == other.Quality);  
        }

        public override bool Equals(object obj)
        {
            var otherObj = obj as HistoryResult;
            return (Value.ToString() == otherObj.Value.ToString()) && (Timestamp == otherObj.Timestamp) && (Quality == otherObj.Quality);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
