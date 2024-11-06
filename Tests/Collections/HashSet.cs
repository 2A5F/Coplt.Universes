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
            Assert.That(set.TryAdd(i), Is.True);
        }
        Assert.That(set.TryAdd(1), Is.False);
        Assert.That(set.Remove(1), Is.True);
        Assert.That(set.Contains(1), Is.False);
    }
}
