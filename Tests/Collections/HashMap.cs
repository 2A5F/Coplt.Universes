using Coplt.Universes.Collections;

namespace Tests.Collections;

[Parallelizable]
public class TestHashMap
{
    [Test]
    public void Test1()
    {
        var set = new SHashMap<int, string, Hasher.Default>();
        for (int i = 0; i < 100; i++)
        {
            Console.WriteLine(set.TryInsert(i, $"{i}"));
        }
        Console.WriteLine(set.TryInsert(1, "123"));
    }
}
