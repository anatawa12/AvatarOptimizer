using System;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Test.Runtime
{
    public class CheckEnabled : MonoBehaviour
    {
        [NonSerialized] public int onEnableCalled;
        [NonSerialized] public int onDisableCalled;
        [NonSerialized] public int onUpdateCalled;

        private void OnEnable() => onEnableCalled++;
        private void OnDisable() => onDisableCalled++;
        private void Update() => onUpdateCalled++;
    }
}