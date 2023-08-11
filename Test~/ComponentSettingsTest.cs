using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Test
{
    public class ComponentSettingsTest
    {
        [Test]
        [TestCaseSource(nameof(ComponentTypes))]
        public void CheckAddComponentMenuIsInAvatarOptimizer(Type type)
        {
            var addComponentMenu = type.GetCustomAttribute<AddComponentMenu>();
            Assert.That(addComponentMenu, Is.Not.Null);
            Assert.That(addComponentMenu.componentMenu, Does.StartWith("Avatar Optimizer/AAO ").Or.Empty);
        }

        static IEnumerable<Type> ComponentTypes()
        {
            return 
                typeof(AvatarTagComponent).Assembly
                .GetTypes()
                .Where(x => x.IsClass && !x.IsAbstract)
                .Where(x => typeof(MonoBehaviour).IsAssignableFrom(x));
        }
    }
}
