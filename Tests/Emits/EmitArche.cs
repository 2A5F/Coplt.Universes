using System.Runtime.CompilerServices;
using Coplt.Universes.Core;

namespace Tests.Emits;

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
        var r = TypeSet.Of<int, float, object, Tag, Position>().ArcheType();
        dynamic chunk = r.CreateChunk();
        Console.WriteLine(chunk);
        Console.WriteLine(chunk.View4);
    }

    [Test]
    public unsafe void Test2()
    {
        var r = TypeSet.Of<int, float, object, Tag, Position>().ArcheType();
        var chunk = r.CreateChunk();
        ref var a = ref chunk.TryGetAt<int>(1);
        var b = chunk.TryGetImmAt<int>(1);
        var c = chunk.TryGetMutAt<int>(1);
        Console.WriteLine($"{(nuint)Unsafe.AsPointer(ref a):X}");
        Console.WriteLine(a);
        a = 123;
        Console.WriteLine(a);
        Console.WriteLine(b);
        Console.WriteLine(c);
    }
}
