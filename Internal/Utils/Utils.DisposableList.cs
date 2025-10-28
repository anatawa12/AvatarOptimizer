using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace Anatawa12.AvatarOptimizer;

partial class Utils
{
    public static void DisposeAll<T>(IEnumerable<T> enumerable, Exception? exception = null)
        where T : IDisposable
    {
        ExceptionDispatchInfo? dispatchInfo = null;
        var exceptions = new List<Exception>();
        foreach (var item in enumerable)
        {
            try
            {
                item.Dispose();
            }
            catch (Exception e)
            {
                if (dispatchInfo == null) dispatchInfo = ExceptionDispatchInfo.Capture(e);
                exceptions.Add(e);
            }
        }
        if (exceptions.Count >= 1)
        {
            if (exception != null)
            {
                exceptions.Insert(0, exception);
            }
            else
            {
                if (exceptions.Count == 1) dispatchInfo?.Throw();
            }

            throw new AggregateException(exceptions);
        }
    }

    public static DisposableList<T> ToDisposableList<T>(this IEnumerable<T> enumerable)
        where T : IDisposable
    {
        var list = new List<T>();
        try
        {
            using var enumerator = enumerable.GetEnumerator();
            while (enumerator.MoveNext())
                list.Add(enumerator.Current);
        }
        catch (Exception e)
        {
            DisposeAll(list, e);
            throw;
        }
        return new DisposableList<T>(list);
    }

    public static MultiDisposable<T> NewMultiDisposable<T>(Func<IEnumerable<T>> disposables) where T : IDisposable =>
        new(disposables);
}

public readonly struct MultiDisposable<T> : IDisposable
    where T : IDisposable
{
    private readonly Func<IEnumerable<T>> disposables;

    public MultiDisposable(Func<IEnumerable<T>> disposables)
    {
        this.disposables = disposables;
    }

    public void Dispose()
    {
        Utils.DisposeAll(disposables());
    }
}

public readonly struct DisposableList<T> : IDisposable, IList<T>
    where T : IDisposable
{
    public readonly List<T> list;

    public DisposableList(List<T> list) => this.list = list;

    public void Dispose()
    {
        Utils.DisposeAll(list);
    }

    public List<T>.Enumerator GetEnumerator() => list.GetEnumerator();
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => list.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();
    public void Add(T item) => list.Add(item);
    public void Clear() => list.Clear();
    public bool Contains(T item) => list.Contains(item);
    public void CopyTo(T[] array, int arrayIndex) => list.CopyTo(array, arrayIndex);
    public bool Remove(T item) => list.Remove(item);
    public int Count => list.Count;
    bool ICollection<T>.IsReadOnly => ((ICollection<T>)list).IsReadOnly;
    public int IndexOf(T item) => list.IndexOf(item);
    public void Insert(int index, T item) => list.Insert(index, item);
    public void RemoveAt(int index) => list.RemoveAt(index);

    public T this[int index]
    {
        get => list[index];
        set => list[index] = value;
    }
}
