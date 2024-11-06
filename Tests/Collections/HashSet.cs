using Coplt.Universes.Collections;

namespace Tests.Collections;

[Parallelizable]
public class TestHashSet
{
    [Test]
    public void Test1()
    {
        var set = new SHashSet<int, Hasher.Default>();
        for (int i = 0; i < 100; i++)
        {
            Console.WriteLine(set.TryAdd(i));
        }
        Console.WriteLine(set.TryAdd(1));
        Console.WriteLine(set.Remove(1));
        Console.WriteLine(set.Contains(1));
    }
}
