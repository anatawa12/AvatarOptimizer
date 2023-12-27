using System;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using nadena.dev.ndmf;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.ErrorReporting
{
    using Validator = Action<IStaticValidated>;

    internal static class ComponentValidation
    {
        private static readonly ConditionalWeakTable<Type, Validator> Validators = new ConditionalWeakTable<Type, Validator>();

        // TODO: make internal
        public static void ValidateAll(GameObject root)
        {
            foreach (var component in root.GetComponentsInChildren<IStaticValidated>(true))
                using (ErrorReport.WithContextObject((Object)component))
                    GetValidator(component.GetType())?.Invoke(component);
        }

        private static Validator GetValidator(Type type)
        {
            // fast path: registered
            if (Validators.TryGetValue(type, out var validator))
                return validator;

            // find validators
            var finding = type;
            while (finding != null && typeof(IStaticValidated).IsAssignableFrom(finding))
            {
                if (Validators.TryGetValue(finding, out validator))
                    break;

                finding = finding.BaseType;
            }

            if (validator == null)
            {
                // if not found, make warning and set empty validator
                Debug.LogWarning($"The StaticValidator for {type} not found. This must be a bug of {type.Assembly}");
                validator = _ => { };
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
        public static void RegisterValidator<T>(Action<T> validator)
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

            Validators.Add(type, validator);
        }
    }
}
