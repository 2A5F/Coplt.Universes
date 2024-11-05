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
            Console.WriteLine(set.TryInsert(i));
        }
        Console.WriteLine(set.TryInsert(1));
    }
}
