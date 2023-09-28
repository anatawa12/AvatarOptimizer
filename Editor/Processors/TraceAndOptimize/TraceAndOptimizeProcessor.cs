using System.Collections.Generic;
using Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes;
using JetBrains.Annotations;
using nadena.dev.ndmf;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors
{
    internal class TraceAndOptimizeProcessor
    {
        private ImmutableModificationsContainer _modifications;
        [CanBeNull] private TraceAndOptimize _config;
        private HashSet<GameObject> _exclusions;

        public void Process(BuildContext context)
        {
            _config = context.AvatarRootObject.GetComponent<TraceAndOptimize>();
            if (_config is null) return;
            Object.DestroyImmediate(_config);
            _exclusions = new HashSet<GameObject>(_config.advancedSettings.exclusions);

            _modifications = new AnimatorParser(_config).GatherAnimationModifications(context);
            if (_config.freezeBlendShape)
                new AutoFreezeBlendShape(_modifications, context, _exclusions).Process();
        }

        public void ProcessLater(BuildContext context)
        {
            if (_config is null) return;

            if (_config.removeUnusedObjects)
                new FindUnusedObjectsProcessor(_modifications, context, 
                    preserveEndBone: _config.preserveEndBone,
                    useLegacyGC: _config.advancedSettings.useLegacyGc,
                    noConfigureMergeBone: _config.advancedSettings.noConfigureMergeBone,
                    gcDebug: _config.advancedSettings.gcDebug,
                    exclusions: _exclusions).Process();

        }
    }
}
