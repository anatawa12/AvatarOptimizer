using System;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    internal class ToggleLeftAttribute : PropertyAttribute
    {
    }

    internal class AllowMultipleComponent : Attribute
    {
    }

    // Properties marked with this attribute will be drawn with the container
    // FieldDrawer instead of a separate one.
    internal class DrawWithContainerAttribute : PropertyAttribute
    {
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = true)]
    internal class ApiExceptionTypeAttribute : Attribute
    {
        public Type[] ExceptionTypes { get; }

        public ApiExceptionTypeAttribute(params Type[] exceptionTypes)
        {
            ExceptionTypes = exceptionTypes;
        }
    }
}
