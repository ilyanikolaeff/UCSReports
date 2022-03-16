using System.Collections.Generic;
using System.Linq;

namespace UCSReports
{
    public class Codes
    {
        private List<CodeItem> _algorithms;
        private List<CodeItem> _statuses;
        private List<CodeItem> _steps;
        private List<CodeItem> _acts;
        private List<CodeItem> _commands;
        private List<CodeItem> _almSteps;
        public Codes(IEnumerable<CodeItem> algs, IEnumerable<CodeItem> sts, IEnumerable<CodeItem> steps,
                     IEnumerable<CodeItem> acts, IEnumerable<CodeItem> cmds, IEnumerable<CodeItem> almSteps)
        {
            _algorithms = algs.ToList();
            _statuses = sts.ToList();
            _steps = steps.ToList();
            _acts = acts.ToList();
            _commands = cmds.ToList();
            _almSteps = almSteps.ToList();
        }

        public string GetNameOfAlgorithm(int code)
        {
            return _algorithms.Where(p => p.Code == code).Select(s => s.Name).FirstOrDefault() is null ? 
                $"No algorithm with code {code}" : _algorithms.Where(p => p.Code == code).Select(s => s.Name).FirstOrDefault();
        }

        public string GetNameOfStatus(int code)
        {
            return _statuses.Where(p => p.Code == code).Select(s => s.Name).FirstOrDefault() is null ?
                $"No status with code {code}" : _statuses.Where(p => p.Code == code).Select(s => s.Name).FirstOrDefault();
        }

        public string GetNameOfStep(int code)
        {
            return _steps.Where(p => p.Code == code).Select(s => s.Name).FirstOrDefault() is null ?
                $"No step with code {code}" : _steps.Where(p => p.Code == code).Select(s => s.Name).FirstOrDefault();
        }

        public string GetNameOfAct(int code)
        {
            return _acts.Where(p => p.Code == code).Select(s => s.Name).FirstOrDefault() is null ?
                $"No act with code {code}" : _acts.Where(p => p.Code == code).Select(s => s.Name).FirstOrDefault();
        }

        public string GetNameOfCommand(int code)
        {
            return _commands.Where(p => p.Code == code).Select(s => s.Name).FirstOrDefault() is null ?
                $"No command with code {code}" : _commands.Where(p => p.Code == code).Select(s => s.Name).FirstOrDefault();
        }

        public string GetNameOfAlarmStep(int code)
        {
            return _almSteps.Where(p => p.Code == code).Select(s => s.Name).FirstOrDefault() is null ?
                $"No alarm step with code {code}" : _almSteps.Where(p => p.Code == code).Select(s => s.Name).FirstOrDefault();
        }

        public CodeItem GetAct(int code)
        {
            return _acts.Where(p => p.Code == code).FirstOrDefault();
        }
    }
}
