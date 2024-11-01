using System.Collections.Frozen;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Coplt.Universes.Core;

public abstract class ArcheType
{
    #region Fields

    public TypeSet TypeSet { get; internal set; } = null!;
    public Type Type { get; internal set; } = null!;
    public Type ChunkType { get; internal set; } = null!;
    public int ChunkSize { get; internal set; }
    public int Stride { get; internal set; }
    public FieldInfo? UnmanagedArrayField { get; internal set; }
    public FrozenDictionary<int, FieldInfo> ManagedArrayField { get; internal set; } = null!;
    public FrozenDictionary<int, uint> UnmanagedOffsets { get; internal set; } = null!;

    #endregion

    #region From Chunk

    internal static readonly ConditionalWeakTable<Type, ArcheType> s_from_chunk = new();

    private static class FromChunkValue<C>
    {
        public static readonly ArcheType? Value = FromChunk(typeof(C));
    }

    public static ArcheType? FromChunk<C>() where C : Chunk => FromChunkValue<C>.Value;
    public static ArcheType? FromChunk(Type chunk_type) =>
        s_from_chunk.TryGetValue(chunk_type, out var arche_type) ? arche_type : null;

    #endregion

    #region Get

    public static ArcheType Get(TypeSet set) => set.ArcheType();

    #endregion

    #region Chunk

    public abstract Chunk CreateChunk();

    public abstract class Chunk
    {
        public ArcheType ArcheType { get; protected init; } = null!;
        public int Count { get; set; }

        #region Accessor

        /// <summary>Try to get a reference to <see cref="T"/> component, index range not checked</summary>
        /// <typeparam name="T">The component type</typeparam>
        /// <returns><c>null</c> if the chunk does not contain <see cref="T"/> component.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract ref T TryGetAt<T>(int index);

        /// <summary>Try to get a reference to <see cref="T"/> component, index range not checked</summary>
        /// <typeparam name="T">The component type</typeparam>
        /// <returns><c>null</c> if the chunk does not contain <see cref="T"/> component.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract Ref<T> TryGetImmAt<T>(int index);
        
        /// <summary>Try to get a reference to <see cref="T"/> component, index range not checked</summary>
        /// <typeparam name="T">The component type</typeparam>
        /// <returns><c>null</c> if the chunk does not contain <see cref="T"/> component.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract Mut<T> TryGetMutAt<T>(int index);

        #endregion
    }

    #endregion

    #region Accessor

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T TryGetAt<C, T>(C chunk, int index) where C : Chunk
        => ref ChunkGetAtAccessor<C, T>.Instance.TryGetAt(chunk, index);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ref<T> TryGetImmAt<C, T>(C chunk, int index) where C : Chunk
        => ChunkGetAtAccessor<C, T>.Instance.TryGetImmAt(chunk, index);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Mut<T> TryGetMutAt<C, T>(C chunk, int index) where C : Chunk
        => ChunkGetAtAccessor<C, T>.Instance.TryGetMutAt(chunk, index);

    #endregion
}
