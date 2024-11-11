using Coplt.Universes.Collections;

namespace Tests.Collections;

[Parallelizable]
public class Test_HashSet_System
{
    [Test]
    public void Test1()
    {
        var set = new SHashSet<int, SystemHashSearcher, Hasher.AsIs>();
        for (int i = 0; i < 100; i++)
        {
            Assert.That(set.TryAdd(i), Is.True);
        }
        for (int i = 0; i < 100; i++)
        {
            Assert.That(set.Contains(i), Is.True);
        }
        Assert.That(set.TryAdd(1), Is.False);
        Assert.That(set.Remove(1), Is.True);
        Assert.That(set.Contains(1), Is.False);
        Assert.That(set.Contains(99), Is.True);
    }
}
