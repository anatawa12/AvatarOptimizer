using System.Linq;

namespace Anatawa12.AvatarOptimizer.Processors
{
    internal class MakeChildrenProcessor
    {
        public void Process(OptimizerSession session)
        {
            foreach (var makeChildren in session.GetComponents<MakeChildren>())
            {
                foreach (var makeChildrenChild in makeChildren.children.GetAsSet().Where(x => x))
                {
                    session.MappingBuilder.RecordMoveObject(makeChildrenChild.gameObject, makeChildren.gameObject);
                    makeChildrenChild.parent = makeChildren.transform;
                }
            }
        }
    }
}
