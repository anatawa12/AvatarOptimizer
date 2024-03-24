using System.Diagnostics.CodeAnalysis;

namespace Anatawa12.AvatarOptimizer.Processors.AnimatorOptimizer
{
    static class KnownParameterValues
    {
        public static bool GetIntValues(string param, [NotNullWhen(true)] out int[]? values)
        {
            switch (param)
            {
                case "GestureLeft":
                case "GestureRight":
                    values = new[] { 0, 1, 2, 3, 4, 5, 6, 7 };
                    return true;
                default:
                    values = null;
                    return false;
            }
        }
    }
}
