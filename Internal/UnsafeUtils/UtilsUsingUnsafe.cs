using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    public static class UtilsUsingUnsafe
    {
        public static unsafe Span<T> AsSpan<T>(this NativeArray<T> self) where T : unmanaged =>
            new Span<T>(self.GetUnsafePtr(), self.Length);
        public static unsafe ReadOnlySpan<T> AsReadOnlySpan<T>(this NativeArray<T> self) where T : unmanaged => 
            new ReadOnlySpan<T>(self.GetUnsafeReadOnlyPtr(), self.Length);

        public static unsafe void LoadRawTextureData<T>(this Texture2D texture2D, Span<T> data) where T : unmanaged
            => LoadRawTextureData(texture2D, (ReadOnlySpan<T>)data);

        public static unsafe void LoadRawTextureData<T>(this Texture2D texture2D, ReadOnlySpan<T> data) where T : unmanaged
        {
            var sizeInBytes = Unsafe.SizeOf<T>() * data.Length;
            fixed (T* ptr = data)
            {
                texture2D.LoadRawTextureData((IntPtr)ptr, sizeInBytes);
            }
        }
    }
}
