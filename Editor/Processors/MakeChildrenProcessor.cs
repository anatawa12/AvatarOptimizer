using Anatawa12.AvatarOptimizer.ErrorReporting;
using System.Linq;

namespace Anatawa12.AvatarOptimizer.Processors
{
    internal class MakeChildrenProcessor
    {
        public void Process(OptimizerSession session)
        {
            BuildReport.ReportingObjects(session.GetComponents<MakeChildren>(), makeChildren =>
            {
                foreach (var makeChildrenChild in makeChildren.children.GetAsSet().Where(x => x))
                {
                    session.MappingBuilder.RecordMoveObject(makeChildrenChild.gameObject, makeChildren.gameObject);
                    makeChildrenChild.parent = makeChildren.transform;
                }
            });
        }
    }
}
