using Coplt.Universes.Core;

namespace Tests;

[Parallelizable]
public class TestEmitArche
{
    public struct Tag;

    [Align(16)]
    public struct Position
    {
        public float x;
        public float y;
        public float z;
    }

    [Test]
    public void Test1()
    {
        var r = ArcheEmitter.Get(TypeSet.Of<int, float, object, Tag, Position>());
        var chunk = Activator.CreateInstance(r.ChunkType)!;
        Console.WriteLine(chunk);
    }
}
