using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anatawa12.AvatarOptimizer
{
    public static class UtilsUsingUnsafe
    {
        public static unsafe Span<T> AsSpan<T>(this NativeArray<T> self) where T : unmanaged =>
            new Span<T>(self.GetUnsafePtr(), self.Length);
        public static unsafe ReadOnlySpan<T> AsReadOnlySpan<T>(this NativeArray<T> self) where T : unmanaged => 
            new ReadOnlySpan<T>(self.GetUnsafeReadOnlyPtr(), self.Length);
    }
}
