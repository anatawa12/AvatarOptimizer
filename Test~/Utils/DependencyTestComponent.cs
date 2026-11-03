using Anatawa12.AvatarOptimizer.API;

namespace Anatawa12.AvatarOptimizer.Test
{
    [ComponentInformation(typeof(DependencyTestComponent))]
    internal class DependencyTestComponentInformation : ComponentInformation<DependencyTestComponent>
    {
        protected override void CollectDependency(DependencyTestComponent component, ComponentDependencyCollector collector)
        {
            if (component.isEntrypoint) collector.MarkEntrypoint();
            if (component.isBehavior) collector.MarkBehaviour();
            if (component.isHeavyBehavior) collector.MarkHeavyBehaviour();

            foreach (var dep in component.dependency)
                collector.AddDependency(dep);
            foreach (var dep in component.dependant)
                collector.AddDependency(dep, component);
        }
    }
}