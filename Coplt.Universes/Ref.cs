using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Coplt.Universes;

public readonly unsafe struct Ref<T> : IEquatable<Ref<T>>
{
    private readonly T[]? m_array;
    private readonly T* m_ptr_or_index;

    public Ref(T* ptr)
    {
        m_ptr_or_index = ptr;
    }

    public Ref(T[] array, nuint index)
    {
        Debug.Assert(index < (nuint)array.Length);

        m_array = array;
        m_ptr_or_index = (T*)index;
    }

    public static Ref<T> Null => default;

    public bool IsNull => m_array is null && m_ptr_or_index is null;

    public ref readonly T V
    {
        get
        {
            if (m_array == null) return ref *m_ptr_or_index;
            return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(m_array), (nuint)m_ptr_or_index);
        }
    }

    public ref readonly T GetPinnableReference() => ref V;

    public override string ToString() => $"{V}";

    #region Equals

    public bool Equals(Ref<T> other) =>
        ReferenceEquals(m_array, other.m_array) && m_ptr_or_index == other.m_ptr_or_index;
    public override bool Equals(object? obj) => obj is Ref<T> other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(m_array, (nuint)m_ptr_or_index);
    public static bool operator ==(Ref<T> left, Ref<T> right) => left.Equals(right);
    public static bool operator !=(Ref<T> left, Ref<T> right) => !left.Equals(right);

    #endregion
}

public readonly unsafe struct Mut<T>
{
    private readonly T[]? m_array;
    private readonly T* m_ptr_or_index;

    public Mut(T* ptr)
    {
        m_ptr_or_index = ptr;
    }

    public Mut(T[] array, nuint index)
    {
        Debug.Assert(index < (nuint)array.Length);

        m_array = array;
        m_ptr_or_index = (T*)index;
    }

    public static Mut<T> Null => default;

    public bool IsNull => m_array is null && m_ptr_or_index is null;

    public ref T V
    {
        get
        {
            if (m_array == null) return ref *m_ptr_or_index;
            return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(m_array), (nuint)m_ptr_or_index);
        }
    }

    public ref T GetPinnableReference() => ref V;

    public override string ToString() => $"{V}";

    #region Equals

    public bool Equals(Mut<T> other) =>
        ReferenceEquals(m_array, other.m_array) && m_ptr_or_index == other.m_ptr_or_index;
    public override bool Equals(object? obj) => obj is Mut<T> other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(m_array, (nuint)m_ptr_or_index);
    public static bool operator ==(Mut<T> left, Mut<T> right) => left.Equals(right);
    public static bool operator !=(Mut<T> left, Mut<T> right) => !left.Equals(right);

    #endregion
}
