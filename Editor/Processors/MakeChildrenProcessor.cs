namespace Anatawa12.AvatarOptimizer.Processors
{
    internal class MakeChildrenProcessor
    {
        public void Process(OptimizerSession session)
        {
            foreach (var makeChildren in session.GetComponents<MakeChildren>())
            {
                foreach (var makeChildrenChild in makeChildren.children.GetAsSet())
                    if (makeChildrenChild)
                        makeChildrenChild.parent = makeChildren.transform;
            }
        }
    }
}
