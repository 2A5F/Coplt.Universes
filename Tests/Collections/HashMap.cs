using Coplt.Universes.Collections;

namespace Tests.Collections;

[Parallelizable]
public class TestHashMap
{
    [Test]
    public void Test1()
    {
        var map = new SHashMap<int, string, Hasher.Default>();
        for (int i = 0; i < 100; i++)
        {
            Assert.That(map.TryAdd(i, $"{i}"), Is.True);
        }
        Assert.That(map.TryAdd(1, "123"), Is.False);
        Assert.That(map.Remove(1), Is.True);
        Assert.That(map.ContainsKey(1), Is.False);
    }
}
