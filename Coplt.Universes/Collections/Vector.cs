using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Coplt.Universes.Utilities;

namespace Coplt.Universes.Collections;

/// <summary>
/// List variant that allows skipping checks and exposing internal arrays
/// </summary>
[StructLayout(LayoutKind.Auto)]
public struct SVector<T> : IList<T>, IReadOnlyList<T>
{
    #region Fields

    public const int DefaultCapacity = 4;

    internal T[] m_items;
    internal int m_size;

    #endregion

    #region Ctor

    public SVector() => m_items = [];

    public SVector(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity, nameof(capacity));
        if (capacity == 0) m_items = [];
        else m_items = new T[capacity];
    }

    public SVector(params ReadOnlySpan<T> span)
    {
        m_items = span.ToArray();
        m_size = span.Length;
    }

    #endregion

    #region IsReadOnly

    public readonly bool IsReadOnly => false;

    #endregion

    #region Count

    public readonly int Count => m_size;

    #endregion

    #region Capacity

    public int Capacity
    {
        readonly get => m_items.Length;
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

    internal int GrowCapacity() => Math.Max(m_items.Length * 2, DefaultCapacity);
    internal void Grow() => Capacity = GrowCapacity();
    internal void Grow(int count) => Capacity = Math.Max(GrowCapacity(), count);
    internal void GrowForInsert(int index, int count = 1)
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

    public readonly ref T GetPinnableReference() => ref MemoryMarshal.GetArrayDataReference(m_items);

    public readonly T[] UnsafeInternalArray => m_items;

    public readonly Span<T> Span => m_items.AsSpan(0, m_size);

    public readonly Memory<T> Memory => m_items.AsMemory(0, m_size);

    T IList<T>.this[int index]
    {
        get => this[index];
        set => this[index] = value;
    }
    T IReadOnlyList<T>.this[int index] => this[index];
    public readonly ref T this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)m_size, nameof(index));
            return ref GetUnchecked(index);
        }
    }

    public readonly ref T this[uint index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, (uint)m_size, nameof(index));
            return ref GetUnchecked(index);
        }
    }

    public readonly ref T GetUnchecked(int index) => ref m_items.GetUnchecked(index);
    public readonly ref T GetUnchecked(uint index) => ref m_items.GetUnchecked(index);

    public readonly Ref<T> ImmAt(int index)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)m_size, nameof(index));
        return ImmAtUnchecked(index);
    }

    public readonly Ref<T> ImmAt(uint index)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, (uint)m_size, nameof(index));
        return ImmAtUnchecked(index);
    }

    public readonly Mut<T> MutAt(int index)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)m_size, nameof(index));
        return MutAtUnchecked(index);
    }

    public readonly Mut<T> MutAt(uint index)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, (uint)m_size, nameof(index));
        return MutAtUnchecked(index);
    }

    public readonly Ref<T> ImmAtUnchecked(int index) => new(m_items, (nuint)index);
    public readonly Ref<T> ImmAtUnchecked(uint index) => new(m_items, index);

    public readonly Mut<T> MutAtUnchecked(int index) => new(m_items, (nuint)index);
    public readonly Mut<T> MutAtUnchecked(uint index) => new(m_items, index);

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

    public void AddRange(params ReadOnlySpan<T> items)
    {
        var count = items.Length;
        if (count == 0) return;
        if (m_items.Length - m_size < count) Grow(checked(m_size + count));
        items.CopyTo(m_items.AsSpan(m_size));
        m_size += count;
    }

    public void AddRange<C>(C items) where C : IEnumerable<T>
    {
        if (items is ICollection<T> c)
        {
            var count = c.Count;
            if (count == 0) return;
            if (m_items.Length - m_size < count) Grow(checked(m_size + count));
            c.CopyTo(m_items, m_size);
            m_size += count;
        }
        else
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }
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

    public void InsertRange(int index, params ReadOnlySpan<T> items)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)index, (uint)m_size, nameof(index));
        InsertRangeUnchecked(index, items);
    }

    public void InsertRangeUnchecked(int index, params ReadOnlySpan<T> items)
    {
        var count = items.Length;
        if (count == 0) return;
        if (m_items.Length - m_size < count) GrowForInsert(index, count);
        else if (index < m_size) Array.Copy(m_items, index, m_items, index + count, m_size - index);
        items.CopyTo(m_items.AsSpan(index));
        m_size += count;
    }

    public void InsertRange<C>(int index, C items) where C : IEnumerable<T>
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)index, (uint)m_size, nameof(index));
        InsertRangeUnchecked(index, items);
    }

    public void InsertRangeUnchecked<C>(int index, C items) where C : IEnumerable<T>
    {
        if (items is ICollection<T> c)
        {
            var count = c.Count;
            if (count == 0) return;
            if (m_items.Length - m_size < count) GrowForInsert(index, count);
            else if (index < m_size) Array.Copy(m_items, index, m_items, index + count, m_size - index);
            c.CopyTo(m_items, index);
            m_size += count;
        }
        else
        {
            foreach (var item in items)
            {
                Insert(index++, item);
            }
        }
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

    public readonly int IndexOf(T item) => Array.IndexOf(m_items, item, 0, m_size);

    public readonly bool Contains(T item) => IndexOf(item) >= 0;

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

    public void RemoveLast()
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(m_size, 1);
        RemoveLastUnchecked();
    }

    public void RemoveLastUnchecked()
    {
        m_size--;
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            GetUnchecked(m_size) = default!;
        }
    }
    
    public T RemoveAndGetLast()
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(m_size, 1);
        return RemoveAndGetLastUnchecked();
    }

    public T RemoveAndGetLastUnchecked()
    {
        m_size--;
        ref var slot = ref GetUnchecked(m_size);
        var r = slot;
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            slot = default!;
        }
        return r;
    }

    public void RemoveLastUncheckedSkipReset()
    {
        m_size--;
    }

    #endregion

    #region CopyTo

    public readonly void CopyTo(Span<T> span) => Span.CopyTo(span);

    public readonly void CopyTo(T[] array, int arrayIndex) => Array.Copy(m_items, 0, array, arrayIndex, m_size);

    #endregion

    #region Enumerator

    public readonly Enumerator GetEnumerator() => new(in this);
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => new EnumeratorClass(in this);
    IEnumerator IEnumerable.GetEnumerator() => new EnumeratorClass(in this);

    [StructLayout(LayoutKind.Auto)]
    public ref struct Enumerator(scoped in SVector<T> self) : IEnumerator<T>
    {
        private readonly T[] m_items = self.m_items;
        private ref T m_cur = ref self.m_items.GetUnchecked(-1);
        private readonly ref T m_end = ref self.m_items.GetUnchecked(self.m_size);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() => m_cur = ref m_items.GetUnchecked(-1);

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

        public readonly ref T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref m_cur;
        }
        readonly T IEnumerator<T>.Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Current;
        }
        readonly object? IEnumerator.Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Current;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly void IDisposable.Dispose() { }
    }

    public class EnumeratorClass(scoped in SVector<T> self) : IEnumerator<T>
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

/// <summary>
/// List variant that allows skipping checks and exposing internal arrays
/// </summary>
public class Vector<T> : IList<T>, IReadOnlyList<T>
{
    #region Fields

    public const int DefaultCapacity = SVector<T>.DefaultCapacity;

    internal SVector<T> m_inner;

    #endregion

    #region Ctor

    public Vector() => m_inner = new();

    public Vector(int capacity) => m_inner = new(capacity);

    public Vector(params ReadOnlySpan<T> span) => m_inner = new(span);

    #endregion

    #region IsReadOnly

    public bool IsReadOnly => false;

    #endregion

    #region Count

    public int Count => m_inner.Count;

    #endregion

    #region Capacity

    public int Capacity
    {
        get => m_inner.Capacity;
        set => m_inner.Capacity = value;
    }

    #endregion

    #region Get

    public ref T GetPinnableReference() => ref m_inner.GetPinnableReference();

    public T[] UnsafeInternalArray => m_inner.UnsafeInternalArray;

    public Span<T> Span => m_inner.Span;

    public Memory<T> Memory => m_inner.Memory;

    T IList<T>.this[int index]
    {
        get => this[index];
        set => this[index] = value;
    }
    T IReadOnlyList<T>.this[int index] => this[index];
    public ref T this[int index] => ref m_inner[index];

    public ref T this[uint index] => ref m_inner[index];

    public ref T GetUnchecked(int index) => ref m_inner.GetUnchecked(index);
    public ref T GetUnchecked(uint index) => ref m_inner.GetUnchecked(index);

    public Ref<T> ImmAt(int index) => m_inner.ImmAt(index);

    public Ref<T> ImmAt(uint index) => m_inner.ImmAt(index);

    public Mut<T> MutAt(int index) => m_inner.MutAt(index);

    public Mut<T> MutAt(uint index) => m_inner.MutAt(index);

    public Ref<T> ImmAtUnchecked(int index) => m_inner.ImmAtUnchecked(index);
    public Ref<T> ImmAtUnchecked(uint index) => m_inner.ImmAtUnchecked(index);

    public Mut<T> MutAtUnchecked(int index) => m_inner.MutAtUnchecked(index);
    public Mut<T> MutAtUnchecked(uint index) => m_inner.MutAtUnchecked(index);

    #endregion

    #region Add

    public void Add(T item) => m_inner.Add(item);

    public ref T UnsafeAdd() => ref m_inner.UnsafeAdd();

    public void AddRange(params ReadOnlySpan<T> items) => m_inner.AddRange(items);

    public void AddRange<C>(C items) where C : IEnumerable<T> => m_inner.AddRange(items);

    #endregion

    #region Insert

    public void Insert(int index, T item) => m_inner.Insert(index, item);

    public ref T UnsafeInsert(int index) => ref m_inner.UnsafeInsert(index);

    public void InsertUnchecked(int index, T item) => m_inner.InsertUnchecked(index, item);

    public ref T UnsafeInsertUnchecked(int index) => ref m_inner.UnsafeInsertUnchecked(index);

    public void InsertRange(int index, params ReadOnlySpan<T> items) => m_inner.InsertRange(index, items);

    public void InsertRangeUnchecked(int index, params ReadOnlySpan<T> items) =>
        m_inner.InsertRangeUnchecked(index, items);

    public void InsertRange<C>(int index, C items) where C : IEnumerable<T>
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)index, (uint)m_inner.m_size, nameof(index));
        InsertRangeUnchecked(index, items);
    }

    public void InsertRangeUnchecked<C>(int index, C items) where C : IEnumerable<T>
    {
        if (items is ICollection<T> c)
        {
            var count = c.Count;
            if (count == 0) return;
            if (m_inner.m_items.Length - m_inner.m_size < count) m_inner.GrowForInsert(index, count);
            else if (index < m_inner.m_size)
                Array.Copy(m_inner.m_items, index, m_inner.m_items, index + count, m_inner.m_size - index);
            if (ReferenceEquals(this, c))
            {
                Array.Copy(m_inner.m_items, 0, m_inner.m_items, index, index);
                Array.Copy(m_inner.m_items, index + count, m_inner.m_items, index * 2, m_inner.m_size - index);
            }
            else c.CopyTo(m_inner.m_items, index);
            m_inner.m_size += count;
        }
        else
        {
            foreach (var item in items)
            {
                Insert(index++, item);
            }
        }
    }

    #endregion

    #region Clear

    public void Clear() => m_inner.Clear();

    #endregion

    #region Find

    public int IndexOf(T item) => m_inner.IndexOf(item);

    public bool Contains(T item) => m_inner.Contains(item);

    #endregion

    #region Remove

    public bool Remove(T item) => m_inner.Remove(item);

    public void RemoveAt(int index) => m_inner.RemoveAt(index);

    public void RemoveAtUnchecked(int index) => m_inner.RemoveAtUnchecked(index);

    #endregion

    #region CopyTo

    public void CopyTo(Span<T> span) => m_inner.CopyTo(span);

    public void CopyTo(T[] array, int arrayIndex) => m_inner.CopyTo(array, arrayIndex);

    #endregion

    #region Enumerator

    public SVector<T>.Enumerator GetEnumerator() => new(in m_inner);
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => new SVector<T>.EnumeratorClass(in m_inner);
    IEnumerator IEnumerable.GetEnumerator() => new SVector<T>.EnumeratorClass(in m_inner);

    #endregion
}
