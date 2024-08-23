using System;
using Unity.Burst;
using Unity.Collections;

namespace Anatawa12.AvatarOptimizer
{
    [BurstCompile]
    public struct NativeArray2<T> : IDisposable where T : unmanaged
    {
        private NativeArray<T> _array;
        private readonly int _firstDimension;
        private readonly int _secondDimension;

        public NativeArray2(int firstDimension, int secondDimension, Allocator allocator)
        {
            _array = new NativeArray<T>(firstDimension * secondDimension, allocator);
            _firstDimension = firstDimension;
            _secondDimension = secondDimension;
        }

        public T this[int first, int second]
        {
            get => _array[first * _secondDimension + second];
            set => _array[first * _secondDimension + second] = value;
        }

        public void Dispose() => _array.Dispose();
    }
    
    [BurstCompile]
    public struct NativeArray3<T> : IDisposable where T : unmanaged
    {
        private NativeArray<T> _array;
        private readonly int _firstDimension;
        private readonly int _secondDimension;
        private readonly int _thirdDimension;

        public NativeArray3(int firstDimension, int secondDimension, int thirdDimension, Allocator allocator)
        {
            _array = new NativeArray<T>(firstDimension * secondDimension * thirdDimension, allocator);
            _firstDimension = firstDimension;
            _secondDimension = secondDimension;
            _thirdDimension = thirdDimension;
        }

        public T this[int first, int second, int third]
        {
            get => _array[(first * _secondDimension + second) * _thirdDimension + third];
            set => _array[(first * _secondDimension + second) * _thirdDimension + third] = value;
        }

        public NativeSlice<T> this[int first, int second] =>
            _array.Slice((first * _secondDimension + second) * _thirdDimension, _thirdDimension);

        public void Dispose() => _array.Dispose();
    }
}
