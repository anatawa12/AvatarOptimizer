using Anatawa12.AvatarOptimizer.ErrorReporting;
using System.Linq;

namespace Anatawa12.AvatarOptimizer.Processors
{
    internal class MakeChildrenProcessor
    {
        private readonly bool _early;

        public MakeChildrenProcessor(bool early)
        {
            _early = early;
        }

        public void Process(OptimizerSession session)
        {
            BuildReport.ReportingObjects(session.GetComponents<MakeChildren>(), makeChildren =>
            {
                if (makeChildren.executeEarly != _early) return;
                foreach (var makeChildrenChild in makeChildren.children.GetAsSet().Where(x => x))
                {
                    makeChildrenChild.parent = makeChildren.transform;
                }
            });
        }
    }
}
