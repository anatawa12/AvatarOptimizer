using System.Collections.Generic;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.Processors.TraceAndOptimizes
{
    internal class ActivenessCache
    {
        private readonly ImmutableModificationsContainer _modifications;
        private readonly Dictionary<Component, bool?> _activeNessCache;
        private Transform _avatarRoot;

        public ActivenessCache(ImmutableModificationsContainer modifications, Transform avatarRoot)
        {
            _modifications = modifications;
            _avatarRoot = avatarRoot;
            _activeNessCache = new Dictionary<Component, bool?>();
        }

        public bool? GetActiveness(Component component)
        {
            if (_activeNessCache.TryGetValue(component, out var activeness))
                return activeness;
            activeness = ComputeActiveness(component);
            _activeNessCache.Add(component, activeness);
            return activeness;
        }

        private bool? ComputeActiveness(Component component)
        {
            if (_avatarRoot == component) return true;
            bool? parentActiveness;
            if (component is Transform t)
                parentActiveness = t.parent == null ? true : GetActiveness(t.parent);
            else
                parentActiveness = GetActiveness(component.transform);
            if (parentActiveness == false) return false;

            bool? activeness;
            switch (component)
            {
                case Transform transform:
                    var gameObject = transform.gameObject;
                    activeness = _modifications.GetConstantValue(gameObject, "m_IsActive", gameObject.activeSelf);
                    break;
                case Behaviour behaviour:
                    activeness = _modifications.GetConstantValue(behaviour, "m_Enabled", behaviour.enabled);
                    break;
                case Cloth cloth:
                    activeness = _modifications.GetConstantValue(cloth, "m_Enabled", cloth.enabled);
                    break;
                case Collider collider:
                    activeness = _modifications.GetConstantValue(collider, "m_Enabled", collider.enabled);
                    break;
                case LODGroup lodGroup:
                    activeness = _modifications.GetConstantValue(lodGroup, "m_Enabled", lodGroup.enabled);
                    break;
                case Renderer renderer:
                    activeness = _modifications.GetConstantValue(renderer, "m_Enabled", renderer.enabled);
                    break;
                // components without isEnable
                case CanvasRenderer _:
                case Joint _:
                case MeshFilter _:
                case OcclusionArea _:
                case OcclusionPortal _:
                case ParticleSystem _:
#if !UNITY_2021_3_OR_NEWER
                case ParticleSystemForceField _:
#endif
                case Rigidbody _:
                case Rigidbody2D _:
                case TextMesh _:
                case Tree _:
                case WindZone _:
#if !UNITY_2020_2_OR_NEWER
                case UnityEngine.XR.WSA.WorldAnchor _:
#endif
                    activeness = true;
                    break;
                case Component _:
                case null:
                    // fallback: all components type should be proceed with above switch
                    activeness = null;
                    break;
            }

            if (activeness == false) return false;
            if (parentActiveness == true && activeness == true) return true;

            return null;
        }
    }
    
}