using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Anatawa12.ApplyOnPlay;
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

        [Test]
        [TestCaseSource(nameof(ComponentTypes))]
        public void CheckHelpURLAttribute(Type type)
        {
            if (type == typeof(Activator)) return;
            if (type == typeof(AvatarActivator)) return;
            if (type == typeof(InternalAutoFreezeMeaninglessBlendShape)) return;
            if (type == typeof(GlobalActivator)) return;
            var addComponentMenu = type.GetCustomAttribute<HelpURLAttribute>();
            Assert.That(addComponentMenu, Is.Not.Null);
            Assert.That(addComponentMenu.URL, Does.StartWith("https://vpm.anatawa12.com/avatar-optimizer/ja/"));
        }

        [Test]
        [TestCaseSource(nameof(ComponentTypes))]
        public void CheckDisallowMultipleComponentIsSpecified(Type type)
        {
            var addComponentMenu = type.GetCustomAttribute<DisallowMultipleComponent>();
            Assert.That(addComponentMenu, Is.Not.Null);
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
