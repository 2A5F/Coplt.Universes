namespace Coplt.Universes.Core;

public sealed class IsolateArche
{
    public Isolate Isolate { get;  }
    public ArcheType Archetype { get; }
    
    public IsolateArche(Isolate isolate, ArcheType archetype)
    {
        Isolate = isolate;
        Archetype = archetype;
    }
}
