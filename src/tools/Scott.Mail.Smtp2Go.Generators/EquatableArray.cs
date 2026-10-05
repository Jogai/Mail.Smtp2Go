using System.Collections;
using System.Collections.Immutable;

namespace Scott.Mail.Smtp2Go.Generators;

/// <summary>
/// An immutable array with value equality, so records holding one stay cacheable in an incremental pipeline
/// (<see cref="ImmutableArray{T}"/> compares by reference).
/// </summary>
internal readonly struct EquatableArray<T>(ImmutableArray<T> values) : IEquatable<EquatableArray<T>>, IReadOnlyList<T>
    where T : IEquatable<T>
{
    private readonly ImmutableArray<T> _values = values.IsDefault ? ImmutableArray<T>.Empty : values;

    public static EquatableArray<T> Empty => new(ImmutableArray<T>.Empty);

    public int Count => _values.Length;

    public T this[int index] => _values[index];

    public bool Equals(EquatableArray<T> other)
    {
        return _values.AsSpan().SequenceEqual(other._values.AsSpan());
    }

    public override bool Equals(object? obj)
    {
        return obj is EquatableArray<T> other && Equals(other);
    }

    public override int GetHashCode()
    {
        int hash = 17;
        foreach (T value in _values)
        {
            hash = unchecked((hash * 31) + (value?.GetHashCode() ?? 0));
        }

        return hash;
    }

    public IEnumerator<T> GetEnumerator()
    {
        return ((IEnumerable<T>)_values).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
