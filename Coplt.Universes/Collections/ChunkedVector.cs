using System.Numerics;
using System.Runtime.CompilerServices;
using Coplt.Universes.Collections.Magics;
using Coplt.Universes.Utilities;

namespace Coplt.Universes.Collections;

public abstract class ChunkedVector
{
    public const int MinChunkSize = 4;

    public enum DeletingBehavior
    {
        MoveItems,
        SwapToEnd,
    }

    public readonly record struct DB_MoveItems : IConst<DeletingBehavior>
    {
        public static DeletingBehavior Value => DeletingBehavior.MoveItems;
    }

    public readonly record struct DB_SwapToEnd : IConst<DeletingBehavior>
    {
        public static DeletingBehavior Value => DeletingBehavior.SwapToEnd;
    }
}

/// <summary>
/// Chunked List, extremely fast expansion speed (no need to copy old items)
/// <br/>
/// <br/>
/// Note: Reference stability when only add new items
/// </summary>
public class ChunkedVector<T, TChunkSize, TDeletingBehavior> : ChunkedVector
    where TChunkSize : struct, IConst<int>
    where TDeletingBehavior : struct, IConst<ChunkedVector.DeletingBehavior>
{
    #region Static

    static ChunkedVector()
    {
        if (!int.IsPow2(TChunkSize.Value))
            throw new ArgumentException("ChunkSize must be pow of 2", nameof(TChunkSize));
        if (TChunkSize.Value < MinChunkSize)
            throw new ArgumentException($"ChunkSize must >= {MinChunkSize}", nameof(TChunkSize));
        if (TDeletingBehavior.Value is < 0 or > DeletingBehavior.SwapToEnd)
            throw new ArgumentOutOfRangeException(nameof(TDeletingBehavior));
    }

    #endregion

    #region Fields

    private readonly Vector<T[]> m_chunks = new();
    private int m_size;
    private int m_size_in_chunk;

    #endregion

    #region Count

    public int Count => m_size;

    public int ChunkCount => m_chunks.Count;

    #endregion

    #region Capacity

    public int Capacity => m_chunks.Count * TChunkSize.Value;

    #endregion

    #region Get

    public ref T this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)m_size, nameof(index));
            return ref GetUnchecked(index);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetUnchecked(int index) => ref GetUnchecked((uint)index);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetUnchecked(uint index) =>
        ref GetUnchecked(
            index >> BitOperations.TrailingZeroCount((uint)TChunkSize.Value),
            index & ((uint)TChunkSize.Value - 1u)
        );

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetUnchecked(uint chunk, uint index) =>
        ref m_chunks
            .GetUnchecked(chunk)
            .GetUnchecked(index);

    #endregion

    #region Add

    public void Add(T item) => UnsafeAdd() = item;

    public ref T UnsafeAdd()
    {
        if (m_size_in_chunk >= TChunkSize.Value)
        {
            // todo grow
            throw new NotImplementedException();
        }
        var chunk = m_chunks.Count - 1;
        var index = m_size_in_chunk;
        m_size_in_chunk++;
        m_size++;
        return ref GetUnchecked((uint)chunk, (uint)index);
    }

    #endregion
}
