using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using NUnit.Framework;
using Debug = System.Diagnostics.Debug;

namespace Anatawa12.AvatarOptimizer.Test
{
    public class PublicApiCheck
    {
        [Test]
        [TestCaseSource(nameof(AllTypes))]
        public void CheckPublicApi(Type type)
        {
            UnityEngine.Debug.Log(type.Name  + " is " + type.Attributes);

            var parsed = new TypeInfo(type);

            if (!parsed.PubliclyAccessible)
            {
                Assert.That(type.GetCustomAttribute<PublicAPIAttribute>(), Is.Null,
                    $"{type} is not publicly accessible but marked as PublicAPIAttribute");
                return;
            }

            // Anatawa12.AvatarOptimizer.APIInternal is special namespace for internal use.
            if (!IsInternalNamespaceInApiModule(type))
                Assert.That(type.GetCustomAttribute<PublicAPIAttribute>(), Is.Not.Null,
                    $"{type} is publicly accessible but not marked as PublicAPIAttribute");

            var allowInherit = type.GetCustomAttributes().Any(x => x.GetType().Name == "AllowInheritAttribute");

            if (type.IsInterface)
            {
                Assert.That(type.GetInterfaces().All(IsAAOApi),
                    "interface must not inherit from non-AAO api");
                Assert.That(allowInherit, Is.True, "public interface must be inheritable");
                return;
            }

            Debug.Assert(type.IsClass);

            // check base type
            Assert.That(IsAAOApi(type.BaseType), "base type must be AAO api");

            // check inherit ability
            if (type.IsSealed)
            {
                // sealed: impossible to inherit
            }
            else if (allowInherit)
            {
                // allowed to inherit
            }
            else
            {
                // not allowed to inherit and not sealed so we have to seal with internal constructor
                // actually, we can seal with internal or private protected abstract members but we choose internal constructor
                Assert.That(!parsed.AllowInherit, "non-sealed class must not have public constructor");
            }

            // process members

            var publicMethods = new HashSet<MethodInfo>();
            // PublicAPIAttribute on properties and events
            foreach (var eventInfo in type.GetEvents())
            {
                if (eventInfo.GetCustomAttribute<PublicAPIAttribute>() != null)
                {
                    if (eventInfo.AddMethod != null) publicMethods.Add(eventInfo.AddMethod);
                    if (eventInfo.RemoveMethod != null) publicMethods.Add(eventInfo.RemoveMethod);
                }
            }
            foreach (var propertyInfo in type.GetProperties())
            {
                if (propertyInfo.GetCustomAttribute<PublicAPIAttribute>() != null)
                {
                    if (propertyInfo.GetMethod != null) publicMethods.Add(propertyInfo.GetMethod);
                    if (propertyInfo.SetMethod != null) publicMethods.Add(propertyInfo.SetMethod);
                }
            }

            // check method
            foreach (var methodInfo in type.GetMethods())
            {
                // for inherited methods from external assembly, we do not check PublicAPIAttribute
                // ReSharper disable once PossibleNullReferenceException
                if (!IsAAOPublicAssembly(methodInfo.DeclaringType.Assembly)) continue;

                if (IsPubliclyAccessible(methodInfo.Attributes, allowInherit))
                {
                    var attribute = methodInfo.GetCustomAttribute<PublicAPIAttribute>();
                    Assert.That(attribute != null || publicMethods.Contains(methodInfo), Is.True,
                        $"{methodInfo} is publicly accessible but not marked as PublicAPIAttribute");

                    Assert.That(IsAAOApi(methodInfo.ReturnType), Is.True,
                        $"{methodInfo} is publicly accessible but return type is not AAO api");
                    Assert.That(methodInfo.GetParameters().All(x => IsAAOApi(x.ParameterType)), Is.True,
                        $"{methodInfo} is publicly accessible but return type is not AAO api");
                }
            }

            // fields are not allowed to be public
            foreach (var fieldInfo in type.GetFields())
            {
                Assert.That(IsPubliclyAccessible(fieldInfo.Attributes, allowInherit), Is.False,
                    $"{fieldInfo} is publicly accessible but not marked as PublicAPIAttribute");
            }
        }

        private const BindingFlags AllMembers =
            BindingFlags.Instance | BindingFlags.Static
                                  | BindingFlags.Public | BindingFlags.NonPublic
                                  | BindingFlags.FlattenHierarchy;

        class TypeInfo
        {
            public bool PubliclyAccessible;
            public readonly bool AllowInherit;

            public TypeInfo(Type type)
            {
                if (type.IsNested)
                {
                    var declaringType = new TypeInfo(type.DeclaringType);
                    PubliclyAccessible = declaringType.PubliclyAccessible &&
                                         IsPubliclyAccessible(type.Attributes, declaringType.AllowInherit);
                }
                else
                {
                    PubliclyAccessible = IsPubliclyAccessible(type.Attributes);
                }
                AllowInherit = !type.IsSealed && type.GetConstructors().Any(x => IsPubliclyAccessible(x.Attributes));
            }
        }

        static IEnumerable<Type> AllTypes()
        {
            var aaoAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(IsAAOPublicAssembly);
            return aaoAssemblies.SelectMany(x => x.GetTypes());
        }

        static bool IsAAOPublicAssembly(Assembly assembly)
        {
            var name = assembly.GetName().Name;
            if (!name.StartsWith("com.anatawa12.avatar-optimizer")) return false;
            if (name.StartsWith("com.anatawa12.avatar-optimizer.test")) return false;
            if (name.StartsWith("com.anatawa12.avatar-optimizer.internal")) return false;
            return true;
        }

        // returns true if the assembly is allowed to be used by AAO api.
        // for example, NDMF is a implementation details of AAO so it's not allowed to expose to AAO api.
        static bool IsAAOApiAssembly(Assembly assembly)
        {
            if (IsAAOPublicAssembly(assembly)) return true;
            if (assembly == typeof(object).Assembly) return true;
            var name = assembly.GetName().Name;
            if (name.StartsWith("UnityEditor.")) return true;
            if (name.StartsWith("UnityEngine.")) return true;
            return false;
        }

        static bool IsAAOApi(Type type) => IsAAOApiAssembly(type.Assembly);
        
        bool IsInternalNamespaceInApiModule(Type type) =>
            type.Namespace == "Anatawa12.AvatarOptimizer.APIInternal" &&
            type.Assembly.GetName().Name == "com.anatawa12.avatar-optimizer.api.editor";

        #region Infracture

        private static bool IsPubliclyAccessible(TypeAttributes attributes, bool inherit = true)
        {
            var masked = attributes & TypeAttributes.VisibilityMask;
            switch (masked)
            {
                case TypeAttributes.NotPublic:
                case TypeAttributes.NestedPrivate:
                case TypeAttributes.NestedFamANDAssem:
                case TypeAttributes.NestedAssembly:
                    return false;
                case TypeAttributes.NestedFamily:
                case TypeAttributes.NestedFamORAssem:
                    return inherit;
                case TypeAttributes.Public:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static bool IsPubliclyAccessible(MethodAttributes attributes, bool inherit = true)
        {
            var masked = attributes & MethodAttributes.MemberAccessMask;
            switch (masked)
            {
                case MethodAttributes.PrivateScope:
                case MethodAttributes.Private:
                case MethodAttributes.FamANDAssem:
                case MethodAttributes.Assembly:
                    return false;
                case MethodAttributes.Family:
                case MethodAttributes.FamORAssem:
                    return inherit;
                case MethodAttributes.Public:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static bool IsPubliclyAccessible(FieldAttributes attributes, bool inherit = true)
        {
            var masked = attributes & FieldAttributes.FieldAccessMask;
            switch (masked)
            {
                case FieldAttributes.PrivateScope:
                case FieldAttributes.Private:
                case FieldAttributes.FamANDAssem:
                case FieldAttributes.Assembly:
                    return false;
                case FieldAttributes.Family:
                case FieldAttributes.FamORAssem:
                    return inherit;
                case FieldAttributes.Public:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion
    }
}
