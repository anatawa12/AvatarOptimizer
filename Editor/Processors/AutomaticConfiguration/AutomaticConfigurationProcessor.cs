using System;
using System.Collections.Generic;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors
{
    internal partial class AutomaticConfigurationProcessor
    {
        private AutomaticConfiguration _config;
        private OptimizerSession _session;

        private Dictionary<Component, HashSet<string>> _modifiedProperties =
            new Dictionary<Component, HashSet<string>>();

        public void Process(OptimizerSession session)
        {
            _session = session;
            _config = session.GetRootComponent<AutomaticConfiguration>();
            if (!_config) return;

            // TODO: implement
            GatherAnimationModifications();
            AutoFreezeBlendShape();
        }

        private IReadOnlyCollection<string> GetModifiedProperties(Component component)
        {
            return _modifiedProperties.TryGetValue(component, out var value)
                ? (IReadOnlyCollection<string>)value
                : Array.Empty<string>();
        }
    }
}
