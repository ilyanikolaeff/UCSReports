using System;
using System.Collections.Generic;
using System.Linq;

namespace UCSReports
{
    public class HistoryResultsCollection
    {
        public string ItemName { get; set; }
        public Opc.ResultID ResultID { get; set; }
        public List<HistoryResult> Results { get; set; }
        public HistoryResultsCollection()
        {
            Results = new List<HistoryResult>();
        }
    }

    public static class HistoryResultsExtensions
    {
        public static HistoryResult GetLastGoodResult(this IEnumerable<HistoryResult> results)
        {
            foreach (var result in results.Reverse())
            {
                if (result.Quality.GetCode() >= 192)
                    return result;
            }
            return null;
        }

        public static HistoryResult GetFirstGoodResult(this IEnumerable<HistoryResult> results)
        {
            foreach (var result in results)
            {
                if (result.Quality.GetCode() >= 192)
                    return result;
            }
            return null;
        }


        public static List<HistoryResult> FilterResults(this IEnumerable<HistoryResult> results, FilterType filterType)
        {
            var historyResults = new List<HistoryResult>();

            if (filterType == FilterType.ValueNotNull)
                return results.Where(p => p.Value != null).ToList();
            if (filterType == FilterType.QualityGood)
                return results.Where(p => p.Quality.GetCode() >= 192).ToList();
            if (filterType == FilterType.GoodAndNotNull)
                return results.Where(p => (p.Quality.GetCode() >= 192) && (p.Value != null)).ToList();

            return null;
        }
        public static HistoryResult GetResult(this IEnumerable<HistoryResult> results,
            DateTime startTimestamp, DateTime endTimestamp, FindType findType, 
            IntervalChangeType intervalChangeType = IntervalChangeType.None, 
            FilterType resultsFilterType = FilterType.GoodAndNotNull)
        {
            // interval change
            if (intervalChangeType == IntervalChangeType.Constriction)
            {
                startTimestamp = startTimestamp.AddSeconds(Settings.GetInstance().IntervalChange);
                endTimestamp = endTimestamp.AddSeconds(-Settings.GetInstance().IntervalChange);
            }
            if (intervalChangeType == IntervalChangeType.Extension)
            {
                startTimestamp = startTimestamp.AddSeconds(-Settings.GetInstance().IntervalChange);
                endTimestamp = endTimestamp.AddSeconds(Settings.GetInstance().IntervalChange);
            }

            startTimestamp = startTimestamp.Truncate(TimeSpan.TicksPerSecond);
            endTimestamp = endTimestamp.Truncate(TimeSpan.TicksPerSecond);

            // filter
            var listOfResults = results.ToList();
            if (resultsFilterType == FilterType.QualityGood)
                listOfResults = listOfResults.Where(p => p.Quality.GetCode() >= 192).ToList();
            if (resultsFilterType == FilterType.ValueNotNull)
                listOfResults = listOfResults.Where(p => p.Value != null).ToList();
            if (resultsFilterType == FilterType.GoodAndNotNull)
                listOfResults = listOfResults.Where(p => (p.Quality.GetCode() >= 192) && (p.Value != null)).ToList();

            // order by
            listOfResults = listOfResults.OrderBy(ks => ks.Timestamp).ToList();

            if (findType == FindType.Last)
                listOfResults.Reverse();

            HistoryResult areaMethodResult, nearestMethodResult;

            // 1. Ищем в окрестностях результатов (по времени)
            areaMethodResult = listOfResults
                .Where(p => p.Timestamp.Truncate(TimeSpan.TicksPerSecond) >= startTimestamp && p.Timestamp.Truncate(TimeSpan.TicksPerSecond) <= endTimestamp)
                .FirstOrDefault();

            // 2. Ищем ближайший по времени (т.е. первый результат не входящий в крайнюю границу)
            if (findType == FindType.First)
                nearestMethodResult = listOfResults.Where(p => p.Timestamp.Truncate(TimeSpan.TicksPerSecond) <= startTimestamp).LastOrDefault();
            else
                nearestMethodResult = listOfResults.Where(p => p.Timestamp.Truncate(TimeSpan.TicksPerSecond) <= endTimestamp).FirstOrDefault();

            // Сравниваем полученные результаты и вовзращаем ближайший
            if (areaMethodResult != null && nearestMethodResult != null)
            {
                TimeSpan timeDiffArea, timeDiffNear;
                if (findType == FindType.First)
                {
                    timeDiffArea = startTimestamp.Subtract(areaMethodResult.Timestamp);
                    timeDiffNear = startTimestamp.Subtract(nearestMethodResult.Timestamp);
                }
                else
                {
                    timeDiffArea = endTimestamp.Subtract(areaMethodResult.Timestamp);
                    timeDiffNear = endTimestamp.Subtract(nearestMethodResult.Timestamp);
                }

                // приводим к положительному
                if (timeDiffArea < TimeSpan.Zero)
                    timeDiffArea = timeDiffArea.Negate();
                if (timeDiffNear < TimeSpan.Zero)
                    timeDiffNear = timeDiffNear.Negate();

                // ищем ближайшее
                if (timeDiffArea < timeDiffNear)
                    return areaMethodResult;
                else
                    return nearestMethodResult;
            }
            else if (areaMethodResult != null && nearestMethodResult == null)
                return areaMethodResult;
            else if (areaMethodResult == null && nearestMethodResult != null)
                return nearestMethodResult;
            else
                return null;
        }

        public static HistoryResult GetPreviousResult(this List<HistoryResult> values, HistoryResult element)
        {
            int index = values.IndexOf(element);
            if (index == -1)
                return null;
            else if (index == 0)
                return values[0];
            else
                return values[index - 1];
        }
    }
}

public enum FindType
{
    First,
    Last
}

public enum IntervalChangeType
{
    Constriction,
    Extension,
    None
}

public enum FilterType
{
    QualityGood,
    ValueNotNull,
    GoodAndNotNull
}
