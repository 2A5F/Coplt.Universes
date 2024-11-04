using System.Diagnostics.CodeAnalysis;

namespace Coplt.Universes.Collections;

public readonly struct ValueTypeEqualityComparer<T> : IEqualityComparer<T>
{
    public bool Equals(T? x, T? y) => EqualityComparer<T>.Default.Equals(x, y);

    public int GetHashCode([DisallowNull] T obj) => obj.GetHashCode();
}
