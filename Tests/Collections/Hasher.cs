using System.Numerics;
using Coplt.Universes.Collections;

namespace Tests.Collections;

[Parallelizable]
public class TestHasher
{
    [Test, Parallelizable, Repeat(100)]
    public void Test_Aes()
    {
        var size = 10000;
        var sum = 0.0;
        for (int i = 0; i <= size; i++)
        {
            var hasher = AesHasher.Init;
            hasher.Write(Random.Shared.NextInt64());
            var h = hasher.Finish();
            var c = BitOperations.PopCount(h);
            sum += c / 64.0;
        }
        var r = sum / size;
        Console.WriteLine(r);
        Assert.That(r, Is.EqualTo(0.5).Within(0.1));
    }
    
    [Test, Parallelizable, Repeat(100)]
    public void Test_Rapid()
    {
        var size = 10000;
        var sum = 0.0;
        for (int i = 0; i <= size; i++)
        {
            var hasher = RapidHasher.Init;
            hasher.Write(Random.Shared.NextInt64());
            var h = hasher.Finish();
            var c = BitOperations.PopCount(h);
            sum += c / 64.0;
        }
        var r = sum / size;
        Console.WriteLine(r);
        Assert.That(r, Is.EqualTo(0.5).Within(0.1));
    }
}
