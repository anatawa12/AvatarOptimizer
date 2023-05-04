using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.ErrorReporting
{
    using Validator = Func<IStaticValidated, IEnumerable<ErrorLog>>;

    public static class ComponentValidation
    {
        private static readonly ConditionalWeakTable<Type, Validator> Validators = new ConditionalWeakTable<Type, Validator>();

        internal static List<ErrorLog> ValidateAll(GameObject root)
        {
            return root.GetComponentsInChildren<IStaticValidated>(true)
                .SelectMany(Validate)
                .ToList();
        }

        private static IEnumerable<ErrorLog> Validate(IStaticValidated component) =>
            GetValidator(component.GetType())?.Invoke(component) ?? Array.Empty<ErrorLog>();

        private static Validator GetValidator(Type type)
        {
            // fast path: registered
            if (Validators.TryGetValue(type, out var validator))
                return validator;

            if (typeof(ISelfStaticValidated).IsAssignableFrom(type))
            {
                // if the type is ISelfStaticValidated, use the validator
                validator = x => ((ISelfStaticValidated)x).CheckComponent();
            }
            else
            {
                // if not, find validators
                var finding = type;
                while (finding != null && typeof(IStaticValidated).IsAssignableFrom(finding))
                {
                    if (Validators.TryGetValue(finding, out validator))
                        break;

                    finding = finding.BaseType;
                }
            }

            if (validator == null)
            {
                // if not found, make warning and set empty validator
                Debug.LogWarning($"The StaticValidator for {type} not found. This must be a bug of {type.Assembly}");
                validator = _ => null;
            }

            Validators.Add(type, validator);
            return validator;
        }

        /// <summary>
        /// Registers validator.
        /// </summary>
        /// <param name="validator">The validator to be registered.</param>
        /// <exception cref="ArgumentException">
        /// If the type is invalid. The type is invalid if
        /// <ul>
        /// <li>The type is interface</li>
        /// <li>The type is static class</li>
        /// <li>The type does not implements IStaticValidated, or</li>
        /// <li>The type implements ISelfStaticValidated</li>
        /// </ul>
        /// </exception>
        public static void RegisterValidator<T>(Func<T, IEnumerable<ErrorLog>> validator)
            where T : IStaticValidated
        {
            RegisterValidator(typeof(T), x => validator((T)x));
        }

        /// <summary>
        /// Registers validator for the specified type.
        /// </summary>
        /// <param name="type">The type validator is for.</param>
        /// <param name="validator">The validator to be registered.</param>
        /// <exception cref="ArgumentException">
        /// If the type is invalid. The type is invalid if
        /// <ul>
        /// <li>The type is interface</li>
        /// <li>The type is static class</li>
        /// <li>The type does not implements IStaticValidated, or</li>
        /// <li>The type implements ISelfStaticValidated</li>
        /// </ul>
        /// </exception>
        public static void RegisterValidator([NotNull] Type type, [NotNull] Validator validator)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (validator == null) throw new ArgumentNullException(nameof(validator));
            if (type.IsInterface)
                throw new ArgumentException("You cannot register Validator for interfaces.");
            if (type.IsSealed && type.IsAbstract)
                throw new ArgumentException("You cannot register Validator for static class.");
            if (!typeof(IStaticValidated).IsAssignableFrom(type))
                throw new ArgumentException(
                    "You cannot register Validator for class does not implements IStaticValidated.");
            if (typeof(ISelfStaticValidated).IsAssignableFrom(type))
                throw new ArgumentException(
                    "You cannot register Validator for class does implements ISelfStaticValidated.");

            Validators.Add(type, validator);
        }
    }
}
