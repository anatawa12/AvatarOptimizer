using System;

namespace Anatawa12.AvatarOptimizer;

public interface IReferenceCount : IDisposable
{
    ReferenceCount ReferenceCount { get; }
}

public static class RefCount
{
    public static void Increment<T>(T value)
        where T : IReferenceCount?
    {
        if (value != null)
            value.ReferenceCount.Count++;
    }
}

public class ReferenceCount
{
    internal int Count;
}

public struct RefCountField<T> : IDisposable
    where T : IReferenceCount?
{
    private T value;

    public RefCountField(T value)
    {
        this.value = value;
        if (value != null)
            value.ReferenceCount.Count++;
    }

    public T Value
    {
        get => value;
        set
        {
            var old = this.value;
            this.value = value;
            if (value != null)
            {
                value.ReferenceCount.Count++;
            }
            if (old != null)
            {
                var afterCount = --old.ReferenceCount.Count;
                if (afterCount == 0) old.Dispose();
            }
        }
    }

    public void Dispose()
    {
        if (value != null)
        {
            var afterCount = --value.ReferenceCount.Count;
            if (afterCount == 0) value.Dispose();
        }
    }

    public static implicit operator RefCountField<T>(T value) => new(value);
    public static implicit operator T(RefCountField<T> value) => value.Value;
}
