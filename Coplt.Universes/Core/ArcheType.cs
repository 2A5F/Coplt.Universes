using System.Collections.Frozen;
using System.Reflection;

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

    #endregion

    #region Get

    public static ArcheType Get(TypeSet set) => ArcheEmitter.Get(set);

    #endregion

    #region Chunk

    public abstract class Chunk
    {
        public int Count { get; internal set; }
    }

    #endregion
}
