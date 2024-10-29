namespace Coplt.Universes.Core;

public abstract class ArcheType
{
    #region Fields

    public required TypeSet TypeSet { get; init; }
    public required Type ChunkType { get; init; }

    #endregion

    #region Get

    public static ArcheType Get(TypeSet set) => ArcheEmitter.Get(set);

    #endregion
}
