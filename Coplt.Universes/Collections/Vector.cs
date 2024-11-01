using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Coplt.Universes.Utilities;

namespace Coplt.Universes.Collections;

/// <summary>
/// List variant that allows skipping checks and exposing internal arrays
/// </summary>
public class Vector<T> : IList<T>, IReadOnlyList<T>
{
    #region Fields

    public const int DefaultCapacity = 4;

    internal T[] m_items;
    internal int m_size;

    #endregion

    #region Ctor

    public Vector() => m_items = [];

    public Vector(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity, nameof(capacity));
        if (capacity == 0) m_items = [];
        else m_items = new T[capacity];
    }

    public Vector(ReadOnlySpan<T> span)
    {
        m_items = span.ToArray();
        m_size = span.Length;
    }

    #endregion

    #region IsReadOnly

    public bool IsReadOnly => false;

    #endregion

    #region Count

    public int Count => m_size;

    #endregion

    #region Capacity

    public int Capacity
    {
        get => m_items.Length;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, m_size, nameof(Capacity));
            if (value == m_items.Length) return;
            if (value == 0) m_items = [];
            else
            {
                var new_items = new T[value];
                if (m_size > 0)
                {
                    Array.Copy(m_items, new_items, m_size);
                }
                m_items = new_items;
            }
        }
    }

    private int GrowCapacity() => Math.Max(m_items.Length * 2, DefaultCapacity);
    private void Grow() => Capacity = GrowCapacity();
    private void GrowForInsert(int index, int count = 1)
    {
        Debug.Assert(count >= 0);

        var required_capacity = checked(m_size + count);
        var new_capacity = Math.Max(required_capacity, GrowCapacity());

        var new_items = new T[new_capacity];
        if (index != 0)
        {
            Array.Copy(m_items, new_items, length: index);
        }
        if (m_size != index)
        {
            Array.Copy(m_items, index, new_items, index + count, m_size - index);
        }
        m_items = new_items;
    }

    #endregion

    #region Get

    public ref T GetPinnableReference() => ref MemoryMarshal.GetArrayDataReference(m_items);

    public T[] UnsafeInternalArray => m_items;

    public Span<T> Span => m_items.AsSpan(0, m_size);

    public Memory<T> Memory => m_items.AsMemory(0, m_size);

    T IList<T>.this[int index]
    {
        get => this[index];
        set => this[index] = value;
    }
    T IReadOnlyList<T>.this[int index] => this[index];
    public ref T this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)m_size, nameof(index));
            return ref GetUnchecked(index);
        }
    }

    public ref T this[uint index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, (uint)m_size, nameof(index));
            return ref GetUnchecked(index);
        }
    }

    public ref T GetUnchecked(int index) => ref m_items.GetUnchecked(index);
    public ref T GetUnchecked(uint index) => ref m_items.GetUnchecked(index);

    public Ref<T> ImmAt(int index)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)m_size, nameof(index));
        return ImmAtUnchecked(index);
    }

    public Ref<T> ImmAt(uint index)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, (uint)m_size, nameof(index));
        return ImmAtUnchecked(index);
    }

    public Mut<T> MutAt(int index)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)m_size, nameof(index));
        return MutAtUnchecked(index);
    }

    public Mut<T> MutAt(uint index)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, (uint)m_size, nameof(index));
        return MutAtUnchecked(index);
    }

    public Ref<T> ImmAtUnchecked(int index) => new(m_items, (nuint)index);
    public Ref<T> ImmAtUnchecked(uint index) => new(m_items, index);

    public Mut<T> MutAtUnchecked(int index) => new(m_items, (nuint)index);
    public Mut<T> MutAtUnchecked(uint index) => new(m_items, index);

    #endregion

    #region Add

    public void Add(T item) => UnsafeAdd() = item;

    public ref T UnsafeAdd()
    {
        var index = m_size;
        if (index >= m_items.Length) Grow();
        m_size++;
        return ref GetUnchecked(index);
    }

    #endregion

    #region Insert

    public void Insert(int index, T item)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)index, (uint)m_size, nameof(index));
        InsertUnchecked(index, item);
    }

    public ref T UnsafeInsert(int index)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)index, (uint)m_size, nameof(index));
        return ref UnsafeInsertUnchecked(index);
    }

    public void InsertUnchecked(int index, T item) => UnsafeInsertUnchecked(index) = item;

    public ref T UnsafeInsertUnchecked(int index)
    {
        if (m_size == m_items.Length) GrowForInsert(index);
        else if (index < m_size) Array.Copy(m_items, index, m_items, index + 1, m_size - index);
        m_size++;
        return ref GetUnchecked(index);
    }

    #endregion

    #region Clear

    public void Clear()
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            var size = m_size;
            m_size = 0;
            if (size > 0) Array.Clear(m_items, 0, size);
        }
        else
        {
            m_size = 0;
        }
    }

    #endregion

    #region Find

    public int IndexOf(T item) => Array.IndexOf(m_items, item, 0, m_size);

    public bool Contains(T item) => IndexOf(item) >= 0;

    #endregion

    #region Remove

    public bool Remove(T item)
    {
        var index = IndexOf(item);
        if (index < 0) return false;
        RemoveAt(index);
        return true;
    }

    public void RemoveAt(int index)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)m_size, nameof(index));
        RemoveAtUnchecked(index);
    }

    public void RemoveAtUnchecked(int index)
    {
        m_size--;
        if (index < m_size)
        {
            Array.Copy(m_items, index + 1, m_items, index, m_size - index);
        }
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            GetUnchecked(index) = default!;
        }
    }

    #endregion

    #region CopyTo

    public void CopyTo(Span<T> span) => Span.CopyTo(span);

    public void CopyTo(T[] array, int arrayIndex) => Array.Copy(m_items, 0, array, arrayIndex, m_size);

    #endregion

    #region Enumerator

    public Enumerator GetEnumerator() => new(this);
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => new EnumeratorClass(this);
    IEnumerator IEnumerable.GetEnumerator() => new EnumeratorClass(this);

    public ref struct Enumerator(Vector<T> self)
    {
        private ref T m_cur = ref self.GetUnchecked(-1);
        private readonly ref T m_end = ref self.GetUnchecked(self.m_size);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() => m_cur = ref self.GetUnchecked(-1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            ref var cur = ref Unsafe.Add(ref m_cur, 1);
            if (!Unsafe.IsAddressLessThan(ref cur, ref m_end)) return false;
#pragma warning disable CS8619
            m_cur = ref cur;
#pragma warning restore CS8619
            return true;
        }

        public ref T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref m_cur;
        }
    }

    public class EnumeratorClass(Vector<T> self) : IEnumerator<T>
    {
        private readonly T[] m_items = self.m_items;
        private int m_index = -1;
        private readonly int m_size = self.m_size;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() => m_index = -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var index = m_index + 1;
            if (index >= m_size) return false;
            m_index = index;
            return true;
        }
        public T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_items.GetUnchecked(m_index);
        }
        object? IEnumerator.Current => Current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() { }
    }

    #endregion
}
