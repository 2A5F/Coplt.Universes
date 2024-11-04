using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Coplt.Universes.Collections.Magics;
using Coplt.Universes.Utilities;
using static Coplt.Universes.Collections.ChunkedVector;

namespace Coplt.Universes.Collections;

public static class ChunkedVector
{
    public const int MinChunkSize = 4;

    public enum Behavior
    {
        /// <summary>
        /// No index stability on deletes and inserts, but fast
        /// </summary>
        SwapToEnd,
        /// <summary>
        /// Index stability
        /// </summary>
        MoveItems,
    }

    public readonly record struct BehaviorSwapToEnd : IConst<Behavior>
    {
        public static Behavior Value => Behavior.SwapToEnd;
    }

    public readonly record struct BehaviorMoveItems : IConst<Behavior>
    {
        public static Behavior Value => Behavior.MoveItems;
    }
}

/// <summary>
/// Chunked List, extremely fast expansion speed (no need to copy old items)
/// <br/>
/// <br/>
/// Note: Reference stability when only add new items
/// </summary>
[StructLayout(LayoutKind.Auto)]
public struct SChunkedVector<T, TChunkSize, TBehavior>() : IList<T>, IReadOnlyList<T>
    where TChunkSize : struct, IConst<int>
    where TBehavior : struct, IConst<Behavior>
{
    #region Static

    private static int Shift => BitOperations.TrailingZeroCount((uint)TChunkSize.Value);
    private static uint Mask => (uint)TChunkSize.Value - 1u;

    static SChunkedVector()
    {
        if (!int.IsPow2(TChunkSize.Value))
            throw new ArgumentException("ChunkSize must be pow of 2", nameof(TChunkSize));
        if (TChunkSize.Value < MinChunkSize)
            throw new ArgumentException($"ChunkSize must >= {MinChunkSize}", nameof(TChunkSize));
        if (TBehavior.Value is not (Behavior.SwapToEnd or Behavior.MoveItems))
            throw new ArgumentOutOfRangeException(nameof(TBehavior));
    }

    #endregion

    #region Fields

    private SVector<T[]> m_chunks = new();
    private int m_cur_chunk;
    private int m_size_in_chunk;

    #endregion

    #region IsReadOnly

    public readonly bool IsReadOnly => false;

    #endregion

    #region Count

    public readonly int Count => m_cur_chunk * TChunkSize.Value + m_size_in_chunk;

    public readonly int ChunkCount => m_chunks.Count;

    #endregion

    #region Capacity

    public readonly int Capacity => m_chunks.Count * TChunkSize.Value;

    #endregion

    #region Grow

    internal void TryGrow()
    {
        if (m_size_in_chunk >= TChunkSize.Value)
        {
            m_cur_chunk++;
            m_size_in_chunk = 0;
        }
        if (m_cur_chunk >= m_chunks.Count) Grow();
    }

    internal void Grow()
    {
        m_chunks.Add(new T[TChunkSize.Value]);
    }

    #endregion

    #region Get

    private readonly void CheckRange(uint chunk, uint at)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(chunk, (uint)m_cur_chunk, nameof(chunk));
        if (chunk == m_cur_chunk)
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(at, (uint)m_size_in_chunk, nameof(at));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private readonly (uint chunk, uint at) CalcIndex(uint index)
    {
        var chunk = index >> Shift;
        var at = index & Mask;
        return (chunk, at);
    }

    T IList<T>.this[int index]
    {
        get => this[index];
        set => this[index] = value;
    }
    T IReadOnlyList<T>.this[int index] => this[index];

    public readonly ref T this[int index] => ref this[(uint)index];

    public readonly ref T this[uint index]
    {
        get
        {
            var (chunk, at) = CalcIndex(index);
            CheckRange(chunk, at);
            return ref GetUnchecked(chunk, at);
        }
    }

    public readonly ref T this[int chunk, int at] => ref this[(uint)chunk, (uint)at];

    public readonly ref T this[uint chunk, uint at]
    {
        get
        {
            CheckRange(chunk, at);
            return ref GetUnchecked(chunk, at);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref T GetUnchecked(int index) => ref GetUnchecked((uint)index);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref T GetUnchecked(uint index)
    {
        var (chunk, at) = CalcIndex(index);
        return ref GetUnchecked(chunk, at);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref T GetUnchecked(uint chunk, uint index) =>
        ref m_chunks
            .GetUnchecked(chunk)
            .GetUnchecked(index);

    #endregion

    #region Add

    public void Add(T item) => UnsafeAdd() = item;

    public ref T UnsafeAdd()
    {
        TryGrow();
        var chunk = m_cur_chunk;
        var index = m_size_in_chunk;
        m_size_in_chunk++;
        return ref GetUnchecked((uint)chunk, (uint)index);
    }

    #endregion

    #region Insert

    public void Insert(int index, T value) => Insert((uint)index, value);
    public void Insert(uint index, T value) => UnsafeInsert(index) = value;

    public ref T UnsafeInsert(int index) => ref UnsafeInsert((uint)index);
    public ref T UnsafeInsert(uint index)
    {
        var size = Count;
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, (uint)size, nameof(index));
        var (chunk, at) = CalcIndex(index);
        return ref UnsafeInsertUnchecked(chunk, at);
    }

    public void InsertUnchecked(int index, T value) => InsertUnchecked((uint)index, value);
    public void InsertUnchecked(uint index, T value) => UnsafeInsertUnchecked(index) = value;

    public ref T UnsafeInsertUnchecked(int index) => ref UnsafeInsertUnchecked((uint)index);
    public ref T UnsafeInsertUnchecked(uint index)
    {
        var (chunk, at) = CalcIndex(index);
        return ref UnsafeInsertUnchecked(chunk, at);
    }

    public void InsertUnchecked(int chunk, int at, T value) => InsertUnchecked((uint)chunk, (uint)at, value);
    public void InsertUnchecked(uint chunk, uint at, T value) => UnsafeInsertUnchecked(chunk, at) = value;

    public ref T UnsafeInsertUnchecked(int chunk, int at) => ref UnsafeInsertUnchecked((uint)chunk, (uint)at);
    public ref T UnsafeInsertUnchecked(uint chunk, uint at)
    {
        if (TBehavior.Value is Behavior.MoveItems) return ref UnsafeInsertUncheckedMoveItems(chunk, at);
        return ref UnsafeInsertUncheckedSwapToEnd(chunk, at);
    }

    public ref T UnsafeInsertUncheckedMoveItems(uint chunk, uint at)
    {
        var old_size = m_size_in_chunk;
        UnsafeAdd();
        if (chunk == m_cur_chunk)
        {
            var arr = m_chunks.GetUnchecked(chunk);
            Array.Copy(arr, at, arr, at + 1, old_size - at);
        }
        else
        {
            var pre = m_cur_chunk;
            if (m_size_in_chunk > 1)
            {
                var arr = m_chunks.GetUnchecked(pre);
                Array.Copy(arr, 0, arr, 1, m_size_in_chunk - 1);
            }
            var cur = pre - 1;
            for (; cur >= chunk; pre = cur, cur--)
            {
                var arr = m_chunks.GetUnchecked(cur);
                m_chunks.GetUnchecked(pre).GetUnchecked(0) = arr.GetUnchecked(TChunkSize.Value - 1);
                Array.Copy(arr, 0, arr, 1, TChunkSize.Value - 1);
            }
        }
        return ref GetUnchecked(chunk, at);
    }

    public ref T UnsafeInsertUncheckedSwapToEnd(uint chunk, uint at)
    {
        ref var end = ref UnsafeAdd();
        ref var pos = ref GetUnchecked(chunk, at);
        end = pos;
        #pragma warning disable CS8619
        return ref pos;
        #pragma warning restore CS8619
    }

    #endregion

    #region Remove

    public bool Remove(T item)
    {
        var index = IndexOf(item);
        if (index < 0) return false;
        RemoveAt(index);
        return true;
    }

    public void RemoveAt(int index) => RemoveAt((uint)index);
    public void RemoveAt(uint index)
    {
        var (chunk, at) = CalcIndex(index);
        CheckRange(chunk, at);
        UnsafeRemoveAt(chunk, at);
    }

    public void RemoveAt(int chunk, int at) => RemoveAt((uint)chunk, (uint)at);
    public void RemoveAt(uint chunk, uint at)
    {
        CheckRange(chunk, at);
        UnsafeRemoveAt(chunk, at);
    }

    public void UnsafeRemoveAt(int index) => UnsafeRemoveAt((uint)index);
    public void UnsafeRemoveAt(uint index)
    {
        var (chunk, at) = CalcIndex(index);
        UnsafeRemoveAt(chunk, at);
    }

    public void UnsafeRemoveAt(int chunk, int at) => UnsafeRemoveAt((uint)chunk, (uint)at);
    public void UnsafeRemoveAt(uint chunk, uint at)
    {
        if (TBehavior.Value is Behavior.MoveItems) UnsafeRemoveAtMoveItems(chunk, at);
        else UnsafeRemoveAtSwapToEnd(chunk, at);
    }

    private void UnsafeRemoveAtMoveItems(uint chunk, uint at)
    {
        var old_at = --m_size_in_chunk;
        Debug.Assert(old_at >= 0);
        var cur_chunk = chunk;
        var chunk_arr = m_chunks.GetUnchecked(cur_chunk);
        if (at < TChunkSize.Value - 1)
        {
            if (chunk == m_cur_chunk)
            {
                if (old_at > 0)
                {
                    Array.Copy(chunk_arr, at + 1, chunk_arr, at, old_at - at);
                }
            }
            else
            {
                Array.Copy(chunk_arr, at + 1, chunk_arr, at, TChunkSize.Value - 1 - at);
            }
        }
        var pre_chunk = cur_chunk;
        cur_chunk++;
        for (; cur_chunk <= m_cur_chunk; pre_chunk = cur_chunk, cur_chunk++)
        {
            var arr = m_chunks.GetUnchecked(cur_chunk);
            m_chunks.GetUnchecked(pre_chunk).GetUnchecked(TChunkSize.Value - 1) = arr.GetUnchecked(0);
            Array.Copy(arr, 1, arr, 0, TChunkSize.Value - 1);
        }
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            m_chunks.GetUnchecked(pre_chunk).GetUnchecked(old_at) = default!;
        }
        if (old_at == 0 && m_cur_chunk > 0)
        {
            m_size_in_chunk = TChunkSize.Value;
            m_cur_chunk--;
        }
    }

    private void UnsafeRemoveAtSwapToEnd(uint chunk, uint at)
    {
        var old_chunk = m_cur_chunk;
        var old_at = --m_size_in_chunk;
        Debug.Assert(old_at >= 0);
        ref var end = ref GetUnchecked((uint)old_chunk, (uint)old_at);
        if (old_chunk != chunk || old_at != at)
        {
            ref var pos = ref GetUnchecked(chunk, at);
            pos = end;
        }
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            end = default!;
        }
        if (old_at == 0 && m_cur_chunk > 0)
        {
            m_size_in_chunk = TChunkSize.Value;
            m_cur_chunk--;
        }
    }

    #endregion

    #region Find

    public readonly int IndexOf(T item)
    {
        for (var i = 0; i < m_cur_chunk; i++)
        {
            var chunk = m_chunks.GetUnchecked(i);
            var index = Array.IndexOf(chunk, item);
            if (index >= 0) return i * TChunkSize.Value + index;
        }
        {
            var chunk = m_chunks.GetUnchecked(m_cur_chunk);
            var index = Array.IndexOf(chunk, item, 0, m_size_in_chunk);
            if (index >= 0) return m_cur_chunk * TChunkSize.Value + index;
        }
        return -1;
    }

    public readonly bool Contains(T item) => IndexOf(item) >= 0;

    #endregion

    #region Clear

    public void Clear()
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            for (var i = 0; i <= m_cur_chunk; i++)
            {
                if (i == m_cur_chunk)
                {
                    if (m_size_in_chunk > 0) Array.Clear(m_chunks.GetUnchecked(i), 0, m_size_in_chunk);
                }
                else Array.Clear(m_chunks.GetUnchecked(i));
            }
        }
        m_cur_chunk = 0;
        m_size_in_chunk = 0;
    }

    #endregion

    #region CopyTo

    public void CopyTo(T[] array, int startIndex)
    {
        for (var i = 0; i <= m_cur_chunk; i++)
        {
            if (i == m_cur_chunk)
            {
                if (m_size_in_chunk > 0)
                {
                    Array.Copy(m_chunks.GetUnchecked(i), 0, array, startIndex, m_size_in_chunk);
                }
            }
            else
            {
                Array.Copy(m_chunks.GetUnchecked(i), 0, array, startIndex, TChunkSize.Value);
                startIndex += TChunkSize.Value;
            }
        }
    }

    #endregion

    #region Enumerator

    public readonly Enumerator GetEnumerator() => new(in this);
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => new EnumeratorClass(in this);
    IEnumerator IEnumerable.GetEnumerator() => new EnumeratorClass(in this);

    [UnscopedRef]
    public readonly ReverseEnumerable Reverse
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(this);
    }

    [StructLayout(LayoutKind.Auto)]
    public ref struct Enumerator(scoped in SChunkedVector<T, TChunkSize, TBehavior> self)
    {
        private int m_index = -1;
        private readonly int m_last_chunk_size = self.m_size_in_chunk;
        private ref T[] m_cur_chunk = ref self.m_chunks.GetUnchecked(0);
        private readonly ref T[] m_end_chunk = ref self.m_chunks.GetUnchecked(self.m_cur_chunk);
        private readonly T[][] m_chunks = self.m_chunks.UnsafeInternalArray;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            m_cur_chunk = ref m_chunks.GetUnchecked(-1);
            m_index = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            re:
            ref var cur_chunk = ref m_cur_chunk;
            if (Unsafe.AreSame(ref cur_chunk, ref m_end_chunk))
            {
                var index = m_index + 1;
                if (index >= m_last_chunk_size) return false;
                m_index = index;
                return true;
            }
            {
                var index = m_index + 1;
                if (index >= TChunkSize.Value)
                {
                    m_index = -1;
                    m_cur_chunk = ref Unsafe.Add(ref cur_chunk, 1);
                    goto re;
                }
                m_index = index;
                return true;
            }
        }

        public ref T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref m_cur_chunk.GetUnchecked(m_index);
        }
    }

    public class EnumeratorClass(scoped in SChunkedVector<T, TChunkSize, TBehavior> self) : IEnumerator<T>
    {
        private readonly T[][] m_chunks = self.m_chunks.UnsafeInternalArray;
        private int m_cur_chunk;
        private int m_index = -1;
        private readonly int m_end_chunk = self.m_cur_chunk;
        private readonly int m_last_chunk_size = self.m_size_in_chunk;

        public void Reset()
        {
            m_cur_chunk = 0;
            m_index = -1;
        }
        public bool MoveNext()
        {
            re:
            var cur_chunk = m_cur_chunk;
            if (cur_chunk == m_end_chunk)
            {
                var index = m_index + 1;
                if (index >= m_last_chunk_size) return false;
                m_index = index;
                return true;
            }
            {
                var index = m_index + 1;
                if (index >= TChunkSize.Value)
                {
                    m_index = -1;
                    m_cur_chunk = cur_chunk + 1;
                    goto re;
                }
                m_index = index;
                return true;
            }
        }
        public T Current => m_chunks.GetUnchecked(m_cur_chunk).GetUnchecked(m_index);
        object? IEnumerator.Current => Current;
        public void Dispose() { }
    }

    [StructLayout(LayoutKind.Auto)]
    public readonly struct ReverseEnumerable(SChunkedVector<T, TChunkSize, TBehavior> self) : IEnumerable<T>
    {
        public ReverseEnumerator GetEnumerator() => new(in self);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => new ReverseEnumeratorClass(in self);
        IEnumerator IEnumerable.GetEnumerator() => new ReverseEnumeratorClass(in self);
    }

    [StructLayout(LayoutKind.Auto)]
    public ref struct ReverseEnumerator(scoped in SChunkedVector<T, TChunkSize, TBehavior> self)
    {
        private int m_index = self.m_size_in_chunk;
        private ref T[] m_cur_chunk = ref self.m_chunks.GetUnchecked(self.m_cur_chunk);
        private readonly ref T[] m_end_chunk = ref self.m_chunks.GetUnchecked(0);
        private readonly T[][] m_chunks = self.m_chunks.UnsafeInternalArray;
        private readonly int m_last_chunk_size = self.m_size_in_chunk;
        private readonly int m_cur_chunk_index = self.m_cur_chunk;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            m_cur_chunk = ref m_chunks.GetUnchecked(m_cur_chunk_index);
            m_index = m_last_chunk_size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var index = m_index - 1;
            if (index < 0)
            {
                ref var cur_chunk = ref Unsafe.Subtract(ref m_cur_chunk, 1);
                if (Unsafe.IsAddressLessThan(ref cur_chunk, ref m_end_chunk)) return false;
                m_index = TChunkSize.Value - 1;
#pragma warning disable CS8619
                m_cur_chunk = ref cur_chunk;
#pragma warning restore CS8619
                return true;
            }
            m_index = index;
            return true;
        }

        public ref T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref m_cur_chunk.GetUnchecked(m_index);
        }
    }

    public class ReverseEnumeratorClass(scoped in SChunkedVector<T, TChunkSize, TBehavior> self)
        : IEnumerator<T>
    {
        private readonly T[][] m_chunks = self.m_chunks.UnsafeInternalArray;
        private int m_index = self.m_size_in_chunk;
        private int m_cur_chunk = self.m_cur_chunk;
        private const int m_end_chunk = 0;
        private readonly int m_last_chunk_size = self.m_size_in_chunk;
        private readonly int m_cur_chunk_index = self.m_cur_chunk;

        public void Reset()
        {
            m_index = m_last_chunk_size;
            m_cur_chunk = m_cur_chunk_index;
        }
        public bool MoveNext()
        {
            var index = m_index - 1;
            if (index < 0)
            {
                var cur_chunk = m_cur_chunk - 1;
                if (cur_chunk < m_end_chunk) return false;
                m_index = TChunkSize.Value - 1;
                m_cur_chunk = cur_chunk;
                return true;
            }
            m_index = index;
            return true;
        }
        public T Current => m_chunks.GetUnchecked(m_cur_chunk).GetUnchecked(m_index);
        object? IEnumerator.Current => Current;
        public void Dispose() { }
    }

    #endregion
}
