using System.Linq;
using nadena.dev.ndmf;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors
{
    internal class MakeChildrenProcessor
    {
        private readonly bool _early;

        public MakeChildrenProcessor(bool early)
        {
            _early = early;
        }

        public void Process(BuildContext context)
        {
            foreach (var makeChildren in context.GetComponents<MakeChildren>())
            {
                using (ErrorReport.WithContextObject(makeChildren))
                {
                    if (makeChildren.executeEarly != _early) continue;
                    foreach (var makeChildrenChild in makeChildren.children.GetAsSet().Where(x => x))
                        context.Extension<GCComponentInfoContext>()
                            .SetParent(makeChildrenChild, makeChildren.transform);
                    DestroyTracker.DestroyImmediate(makeChildren);
                }
            }
        }
    }
}
