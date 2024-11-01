using System.Runtime.CompilerServices;
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

    [Test]
    public unsafe void Test2()
    {
        var r = ArcheEmitter.Get(TypeSet.Of<int, float, object, Tag, Position>());
        var chunk = r.CreateChunk();
        chunk.Count = r.Stride;
        ref var a = ref chunk.TryGetAtUnchecked<int>(1);
        Console.WriteLine($"{(nuint)Unsafe.AsPointer(ref a):X}");
        Console.WriteLine(a);
        a = 123;
        Console.WriteLine(a);
    }
}
