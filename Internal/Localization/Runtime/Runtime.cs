using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    public sealed class AAOLocalizedAttribute : PropertyAttribute
    {
        [NotNull] public string LocalizationKey { get; }
        [CanBeNull] public string TooltipKey { get; }

        public AAOLocalizedAttribute([NotNull] string localizationKey) : this(localizationKey, null) {}

        public AAOLocalizedAttribute([NotNull] string localizationKey, [CanBeNull] string tooltipKey)
        {
            LocalizationKey = localizationKey ?? throw new ArgumentNullException(nameof(localizationKey));
            TooltipKey = tooltipKey;
        }
    } 
}
