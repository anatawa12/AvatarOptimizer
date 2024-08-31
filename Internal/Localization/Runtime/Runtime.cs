using System;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    public sealed class AAOLocalizedAttribute : PropertyAttribute
    {
        public string LocalizationKey { get; }
        public string? TooltipKey { get; }

        public AAOLocalizedAttribute(string localizationKey) : this(localizationKey, null) {}

        public AAOLocalizedAttribute(string localizationKey, string? tooltipKey)
        {
            LocalizationKey = localizationKey ?? throw new ArgumentNullException(nameof(localizationKey));
            TooltipKey = tooltipKey;
        }
    } 
}
