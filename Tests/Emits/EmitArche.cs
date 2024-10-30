using Coplt.Universes.Core;

namespace Tests;

[Parallelizable]
public class TestEmitArche
{
    [Test]
    public void Test1()
    {
        var r = ArcheEmitter.Get(TypeSet.Of<int, float, object>());
        var chunk = Activator.CreateInstance(r.ChunkType)!;
        Console.WriteLine(chunk);
    }
}
