using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Coplt.Universes.Utilities;

namespace Coplt.Universes.Core;

[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly unsafe struct ChunkSlice<T>(T* ptr, int length) : IEnumerable<T>
{
    #region Getter

    public T* Ptr
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ptr;
    }
    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => length;
    }
    public ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (index < 0 || index >= Length) throw new IndexOutOfRangeException();
            return ref *UncheckedGet(index);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T* UncheckedGet(int index) => (T*)((byte*)Ptr + TypeUtils.AlignedSizeOf<T>() * index);

    public Ref<T> ImmRefAt(int index)
    {
        if (index < 0 || index >= Length) throw new IndexOutOfRangeException();
        return new(UncheckedGet(index));
    }

    public Mut<T> MutRefAt(int index)
    {
        if (index < 0 || index >= Length) throw new IndexOutOfRangeException();
        return new(UncheckedGet(index));
    }

    #endregion

    #region Span

    /// <returns>If it cannot be converted to Span, the empty Span will be returned.</returns>
    public Span<T> Span => TryAsSpan(out _);

    public Span<T> TryAsSpan(out bool success)
    {
        if (Unsafe.SizeOf<T>() != TypeUtils.AlignedSizeOf<T>())
        {
            success = false;
            return default;
        }
        success = true;
        return new(ptr, length);
    }

    #endregion

    #region Enumerator

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(this);
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => new EnumeratorClass(this);
    IEnumerator IEnumerable.GetEnumerator() => new EnumeratorClass(this);

    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    public struct Enumerator(ChunkSlice<T> self)
    {
        private T* m_cur = self.UncheckedGet(-1);
        private readonly T* m_end = self.UncheckedGet(self.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var cur = (T*)((byte*)m_cur + TypeUtils.AlignedSizeOf<T>());
            if (cur >= m_end) return false;
            m_cur = cur;
            return true;
        }

        public ref T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref *m_cur;
        }
    }

    internal sealed class EnumeratorClass(ChunkSlice<T> self) : IEnumerator<T>
    {
        private T* m_cur = self.UncheckedGet(-1);
        private readonly T* m_end = self.UncheckedGet(self.Length);
        public void Reset() => m_cur = self.UncheckedGet(-1);
        public bool MoveNext()
        {
            var cur = (T*)((byte*)m_cur + TypeUtils.AlignedSizeOf<T>());
            if (cur >= m_end) return false;
            m_cur = cur;
            return true;
        }
        public T Current => *m_cur;
        object? IEnumerator.Current => Current;
        public void Dispose() { }
    }

    public ImmRefEnumerable ImmRefs => new(this);
    public MutRefEnumerable MutRefs => new(this);

    public readonly struct ImmRefEnumerable(ChunkSlice<T> self) : IEnumerable<Ref<T>>
    {
        public int Length => self.Length;
        public Ref<T> this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => self.ImmRefAt(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ImmRefEnumerator GetEnumerator() => new(self);
        IEnumerator<Ref<T>> IEnumerable<Ref<T>>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public readonly struct MutRefEnumerable(ChunkSlice<T> self) : IEnumerable<Mut<T>>
    {
        public int Length => self.Length;
        public Mut<T> this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => self.MutRefAt(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MutRefEnumerator GetEnumerator() => new(self);
        IEnumerator<Mut<T>> IEnumerable<Mut<T>>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public struct ImmRefEnumerator(ChunkSlice<T> self) : IEnumerator<Ref<T>>
    {
        private T* m_cur = self.UncheckedGet(-1);
        private readonly T* m_end = self.UncheckedGet(self.Length);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() => m_cur = self.UncheckedGet(-1);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var cur = (T*)((byte*)m_cur + TypeUtils.AlignedSizeOf<T>());
            if (cur >= m_end) return false;
            m_cur = cur;
            return true;
        }
        public Ref<T> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(m_cur);
        }
        object IEnumerator.Current => Current;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() { }
    }

    public struct MutRefEnumerator(ChunkSlice<T> self) : IEnumerator<Mut<T>>
    {
        private T* m_cur = self.UncheckedGet(-1);
        private readonly T* m_end = self.UncheckedGet(self.Length);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() => m_cur = self.UncheckedGet(-1);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var cur = (T*)((byte*)m_cur + TypeUtils.AlignedSizeOf<T>());
            if (cur >= m_end) return false;
            m_cur = cur;
            return true;
        }
        public Mut<T> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(m_cur);
        }
        object IEnumerator.Current => Current;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() { }
    }

    #endregion
}

public readonly ref struct ChunkSpan<T>
{
    #region Getter

    public readonly ref T Ref;
    private readonly int m_length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ChunkSpan(ref T @ref, int length)
    {
        Ref = ref @ref;
        m_length = length;
    }

    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => m_length;
    }
    public ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (index < 0 || index >= Length) throw new IndexOutOfRangeException();
            return ref UncheckedGet(index);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T UncheckedGet(int index) =>
        ref Unsafe.As<byte, T>(ref Unsafe.Add(ref Unsafe.As<T, byte>(ref Ref),
            (nuint)(TypeUtils.AlignedSizeOf<T>() * index)));

    #endregion

    #region Span

    /// <returns>If it cannot be converted to Span, the empty Span will be returned.</returns>
    public Span<T> Span => TryAsSpan(out _);

    public Span<T> TryAsSpan(out bool success)
    {
        if (Unsafe.SizeOf<T>() != TypeUtils.AlignedSizeOf<T>())
        {
            success = false;
            return default;
        }
        success = true;
        return MemoryMarshal.CreateSpan(ref Ref, m_length);
    }

    #endregion

    #region Enumerator

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(this);

    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref struct Enumerator(ChunkSpan<T> self)
    {
        private readonly ChunkSpan<T> self = self;
        private int m_index = -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() => m_index = -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var index = m_index + 1;
            if (index >= self.Length) return false;
            m_index = index;
            return true;
        }

        public ref T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref self.UncheckedGet(m_index);
        }
    }

    #endregion
}

public readonly unsafe struct ChunkView<T> : IEnumerable<T>
{
    #region Fields

    private readonly object m_object;
    private readonly T* m_ptr;

    #endregion

    #region Ctor

    public ChunkView(ArcheType.Chunk chunk, T* ptr)
    {
        m_object = chunk;
        m_ptr = ptr;
    }

    public ChunkView(T[] array)
    {
        m_object = array;
        m_ptr = null;
    }

    #endregion

    #region Getter

    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => m_ptr == null ? ((T[])m_object).Length : ((ArcheType.Chunk)m_object).Count;
    }

    public ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (index < 0 || index >= Length) throw new IndexOutOfRangeException();
            return ref UncheckedGet(index);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T UncheckedGet(int index)
    {
        if (m_ptr == null) return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference((T[])m_object), index);
        return ref *(T*)((byte*)m_ptr + TypeUtils.AlignedSizeOf<T>() * index);
    }

    public Ref<T> ImmRefAt(int index)
    {
        if (index < 0 || index >= Length) throw new IndexOutOfRangeException();
        if (m_ptr == null) return new((T[])m_object, (nuint)index);
        return new((T*)((byte*)m_ptr + TypeUtils.AlignedSizeOf<T>() * index));
    }

    public Mut<T> MutRefAt(int index)
    {
        if (index < 0 || index >= Length) throw new IndexOutOfRangeException();
        if (m_ptr == null) return new((T[])m_object, (nuint)index);
        return new((T*)((byte*)m_ptr + TypeUtils.AlignedSizeOf<T>() * index));
    }

    #endregion

    #region Span

    /// <returns>If it cannot be converted to Span, the empty Span will be returned.</returns>
    public Span<T> Span => TryAsSpan(out _);

    public Span<T> TryAsSpan(out bool success)
    {
        if (Unsafe.SizeOf<T>() != TypeUtils.AlignedSizeOf<T>())
        {
            success = false;
            return default;
        }
        success = true;
        if (m_ptr == null) return ((T[])m_object).AsSpan();
        return new(m_ptr, ((ArcheType.Chunk)m_object).Count);
    }

    #endregion

    #region Memory

    /// <returns>If it cannot be converted to Memory, the empty Memory will be returned.</returns>
    public Memory<T> Memory => TryAsMemory(out _);

    public Memory<T> TryAsMemory(out bool success)
    {
        if (Unsafe.SizeOf<T>() != TypeUtils.AlignedSizeOf<T>() || m_ptr != null)
        {
            success = false;
            return default;
        }
        success = true;
        return ((T[])m_object).AsMemory();
    }

    #endregion

    #region Enumerator

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(this);
    private IEnumerator<T> GetEnumeratorClass() =>
        m_ptr == null
            ? new EnumeratorClassArray((T[])m_object)
            : new ChunkSlice<T>.EnumeratorClass(new(m_ptr, ((ArcheType.Chunk)m_object).Count));
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumeratorClass();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumeratorClass();

    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref struct Enumerator(ChunkView<T> self)
    {
        private ref T m_cur = ref self.UncheckedGet(-1);
        private readonly ref T m_end = ref self.UncheckedGet(self.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            ref var cur = ref Unsafe.AddByteOffset(ref m_cur, TypeUtils.AlignedSizeOf<T>());
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

    private sealed class EnumeratorClassArray(T[] array) : IEnumerator<T>
    {
        private int m_index = -1;
        public void Reset() => m_index = -1;
        public bool MoveNext()
        {
            var index = m_index + 1;
            if (index >= array.Length) return false;
            m_index = index;
            return true;
        }
        public T Current => array[m_index];
        object? IEnumerator.Current => Current;
        public void Dispose() { }
    }

    #endregion

    #region Refs

    public ImmRefEnumerable ImmRefs => new(this);
    public MutRefEnumerable MutRefs => new(this);

    public readonly struct ImmRefEnumerable(ChunkView<T> self) : IEnumerable<Ref<T>>
    {
        public int Length => self.Length;
        public Ref<T> this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => self.ImmRefAt(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ImmRefEnumerator GetEnumerator() => new(self);
        private IEnumerator<Ref<T>> GetEnumeratorClass() =>
            self.m_ptr == null
                ? new ImmRefEnumeratorArray((T[])self.m_object)
                : new ChunkSlice<T>.ImmRefEnumerator(new(self.m_ptr, ((ArcheType.Chunk)self.m_object).Count));
        IEnumerator<Ref<T>> IEnumerable<Ref<T>>.GetEnumerator() => GetEnumeratorClass();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumeratorClass();
    }

    public readonly struct MutRefEnumerable(ChunkView<T> self) : IEnumerable<Mut<T>>
    {
        public int Length => self.Length;
        public Mut<T> this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => self.MutRefAt(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MutRefEnumerator GetEnumerator() => new(self);
        private IEnumerator<Mut<T>> GetEnumeratorClass() =>
            self.m_ptr == null
                ? new MutRefEnumeratorArray((T[])self.m_object)
                : new ChunkSlice<T>.MutRefEnumerator(new(self.m_ptr, ((ArcheType.Chunk)self.m_object).Count));
        IEnumerator<Mut<T>> IEnumerable<Mut<T>>.GetEnumerator() => GetEnumeratorClass();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumeratorClass();
    }

    public struct ImmRefEnumerator(ChunkView<T> self) : IEnumerator<Ref<T>>
    {
        private int m_index = -1;
        private readonly int m_length = self.Length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() => m_index = -1;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var index = m_index + 1;
            if (index >= m_length) return false;
            m_index = index;
            return true;
        }
        public Ref<T> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => self.ImmRefAt(m_index);
        }
        object IEnumerator.Current => Current;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() { }
    }

    public struct MutRefEnumerator(ChunkView<T> self) : IEnumerator<Mut<T>>
    {
        private int m_index = -1;
        private readonly int m_length = self.Length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() => m_index = -1;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var index = m_index + 1;
            if (index >= m_length) return false;
            m_index = index;
            return true;
        }
        public Mut<T> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => self.MutRefAt(m_index);
        }
        object IEnumerator.Current => Current;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() { }
    }

    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal sealed class ImmRefEnumeratorArray(T[] array) : IEnumerator<Ref<T>>
    {
        private int m_index = -1;
        public void Reset() => m_index = -1;
        public bool MoveNext()
        {
            var index = m_index + 1;
            if (index >= array.Length) return false;
            m_index = index;
            return true;
        }
        public Ref<T> Current => new(array, (nuint)m_index);
        object IEnumerator.Current => Current;
        public void Dispose() { }
    }

    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal sealed class MutRefEnumeratorArray(T[] array) : IEnumerator<Mut<T>>
    {
        private int m_index = -1;
        public void Reset() => m_index = -1;
        public bool MoveNext()
        {
            var index = m_index + 1;
            if (index >= array.Length) return false;
            m_index = index;
            return true;
        }
        public Mut<T> Current => new(array, (nuint)m_index);
        object IEnumerator.Current => Current;
        public void Dispose() { }
    }

    #endregion
}
