using System.Collections.ObjectModel;

namespace UCSReports
{
    abstract class Step : ReportObject
    {
    }

    class RegularStep : EmergencyStep
    {
        public ObservableCollection<Act> Acts { get; set; }
        public RegularStep()
        {
            Acts = new ObservableCollection<Act>();
        }
    }
    class EmergencyStep : Step
    { }

    abstract class StepCreator
    {
        public abstract Step CreateStep();
    }

    class RegularStepCreator : StepCreator
    {
        public override Step CreateStep()
        {
            return new RegularStep();
        }
    }

    class EmergencyStepCreator : StepCreator
    {
        public override Step CreateStep()
        {
            return new EmergencyStep();
        }
    }
}
