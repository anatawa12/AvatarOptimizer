using System.Collections.Generic;
using Anatawa12.AvatarOptimizer.API;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Test
{
    public class DependencyTestComponent : MonoBehaviour
    {
        public bool isEntrypoint;
        public bool isBehavior;
        public bool isHeavyBehavior;
        public List<Component> dependency = new List<Component>();
        public List<Component> dependant = new List<Component>();
    }
}