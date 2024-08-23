using System;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    /**
     * This struct is to check the API usage of the Avatar Optimizer.
     *
     * This class will generate warning message if the API is used incorrectly.
     */
    internal struct APIChecker
    {
        private int _currentVersion;
        private State _isAPIUsed;

        enum State
        {
            Idle,
            Initialized,
            UsedBeforeInitialization,
        }

        public void OnInitialize(int version, Component component)
        {
            _currentVersion = version;
            switch (_isAPIUsed)
            {
                case State.Idle:
                    _isAPIUsed = State.Initialized;
                    break;
                case State.Initialized:
                    Debug.LogWarning($"The Component {component} is initialized twice. This would cause unexpected behavior!", component);
                    break;
                case State.UsedBeforeInitialization:
                    _isAPIUsed = State.Initialized;
                    Debug.LogWarning($"The Component {component} is initialized after using the API. This will cause unexpected behavior!", component);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public T OnAPIUsage<T>(Component component, T value)
        {
            switch (_isAPIUsed)
            {
                case State.Idle:
                    Debug.LogWarning($"The Component {component} is used before initialization. This would cause unexpected behavior!", component);
                    _isAPIUsed = State.UsedBeforeInitialization;
                    break;
                case State.Initialized:
                case State.UsedBeforeInitialization:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return value;
        }
    }
}
