using Coplt.Universes.Collections;

namespace Tests.Collections;

[Parallelizable]
public class Test_HashSet_Ankerl
{
    [Test]
    public void Test1()
    {
        var set = new SAnkerlHashSet<int, Hasher.Default>();
        for (int i = 0; i < 100; i++)
        {
            Assert.That(set.TryAdd(i), Is.True);
        }
        Assert.That(set.TryAdd(1), Is.False);
        Assert.That(set.Remove(1), Is.True);
        Assert.That(set.Contains(1), Is.False);
    }
}
