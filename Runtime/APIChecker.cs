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
                    throw new InvalidOperationException($"Invalid state of APIChecker: {_isAPIUsed}");
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
                    throw new InvalidOperationException($"Invalid state of APIChecker: {_isAPIUsed}");
            }

            return value;
        }

        /// <summary>
        /// Checks the API usage with version, and execute the <paramref name="value"/> if the API is used correctly.
        /// </summary>
        /// <param name="component">The component that uses the API</param>
        /// <param name="minVersion">The minimum version to use</param>
        /// <param name="value">The value to return</param>
        /// <returns>Returns the value of the <paramref name="value"/></returns>
        /// <exception cref="InvalidOperationException">If the API is used before initialization or with unsupported version.</exception>
        public T OnAPIUsageVersioned<T>(Component component, int minVersion, Func<T> value)
        {
            switch (_isAPIUsed)
            {
                case State.Idle:
                case State.UsedBeforeInitialization:
                    throw new InvalidOperationException($"This API (see stacktrace) of {component} is used before initialization, this is unsupported with this API.");
                case State.Initialized:
                    if (_currentVersion < minVersion)
                        throw new InvalidOperationException($"This API (see stacktrace) of {component} is initialized with unsupported version. Initialize with supported version.");
                    break;
                default:
                    throw new InvalidOperationException($"Invalid state of APIChecker: {_isAPIUsed}");
            }

            return value();
        }
    }
}
