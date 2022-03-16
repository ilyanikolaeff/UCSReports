using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace UCSReports
{
    /// <summary>
    /// Базовый класс шага
    /// </summary>
    internal abstract class Step : ReportObject
    {

    }

    /// <summary>
    /// Шаг алгоритма
    /// </summary>
    class RegularStep : Step
    {
        public List<Act> Acts { get; set; }
        public RegularStep()
        {
            Acts = new List<Act>();
        }
    }

    /// <summary>
    /// Шаг алгоритма противоаварийной остановки
    /// </summary>
    class EmergencyStep : Step
    { }
}
