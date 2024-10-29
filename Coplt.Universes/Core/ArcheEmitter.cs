using System.Collections.Concurrent;

namespace Coplt.Universes.Core;

public static class ArcheEmitter
{
    private static readonly ConcurrentDictionary<TypeSet, ArcheType> s_cache = new();
    
    public static ArcheType Get(TypeSet set) => s_cache.GetOrAdd(set, Emit);
    
    private static ArcheType Emit(TypeSet set)
    {
        // todo
        return null!;
    }
}
