using Coplt.Universes.Core;

namespace Tests;

[Align(16)]
public struct Position
{
    public float x;
    public float y;
    public float z;
}

[Parallelizable]
public class TestEmitArche
{
    public struct Tag;

    [Test]
    public void Test1()
    {
        var r = ArcheEmitter.Get(TypeSet.Of<int, float, object, Tag, Position>());
        dynamic chunk = r.CreateChunk();
        Console.WriteLine(chunk);
        Console.WriteLine(chunk.View4);
    }
}
