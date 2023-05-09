using Anatawa12.AvatarOptimizer.ErrorReporting;

namespace Anatawa12.AvatarOptimizer.Processors
{
    internal class MakeChildrenProcessor
    {
        public void Process(OptimizerSession session)
        {
            BuildReport.ReportingObjects(session.GetComponents<MakeChildren>(), makeChildren =>
            {
                foreach (var makeChildrenChild in makeChildren.children.GetAsSet())
                    if (makeChildrenChild)
                        makeChildrenChild.parent = makeChildren.transform;
            });
        }
    }
}
