using Coplt.Universes;
using Coplt.Universes.Core;

namespace Tests;

[Parallelizable]
public class TestTypeSet
{
    [Test]
    public void Test1()
    {
        var set1 = TypeSet.Of<int, float>();
        Console.WriteLine(set1);
        CollectionAssert.AreEquivalent(set1,
            (TypeMeta[]) [TypeMeta.Of<Entity>(), TypeMeta.Of<int>(), TypeMeta.Of<float>()]);
    }

    [Test]
    public void IsOverlap1()
    {
        var set1 = TypeSet.Of<int, float>();
        var set2 = TypeSet.Of<float, double>();
        Console.WriteLine(set1);
        Console.WriteLine(set2);
        var r = set1.IsOverlap(set2);
        Console.WriteLine(r);
        Assert.That(r, Is.True);
    }

    [Test]
    public void IsOverlap2()
    {
        var set1 = TypeSet.Of<int, long>();
        var set2 = TypeSet.Of<float, double>();
        Console.WriteLine(set1);
        Console.WriteLine(set2);
        var r = set1.IsOverlap(set2);
        Console.WriteLine(r);
        Assert.That(r, Is.False);
    }

    [Test]
    public void IsSubsetOf1()
    {
        var set1 = TypeSet.Of<int, float>();
        var set2 = TypeSet.Of<int, float, double>();
        Console.WriteLine(set1);
        Console.WriteLine(set2);
        var r = set1.IsSubsetOf(set2);
        Console.WriteLine(r);
        Assert.That(r, Is.True);
    }

    [Test]
    public void IsSubsetOf2()
    {
        var set1 = TypeSet.Of<int, float>();
        var set2 = TypeSet.Of<int, float, double>();
        Console.WriteLine(set1);
        Console.WriteLine(set2);
        var r = set2.IsSubsetOf(set1);
        Console.WriteLine(r);
        Assert.That(r, Is.False);
    }

    [Test]
    public void IsSupersetOf1()
    {
        var set1 = TypeSet.Of<int, float>();
        var set2 = TypeSet.Of<int, float, double>();
        Console.WriteLine(set1);
        Console.WriteLine(set2);
        var r = set1.IsSupersetOf(set2);
        Console.WriteLine(r);
        Assert.That(r, Is.False);
    }

    [Test]
    public void IsSupersetOf2()
    {
        var set1 = TypeSet.Of<int, float>();
        var set2 = TypeSet.Of<int, float, double>();
        Console.WriteLine(set1);
        Console.WriteLine(set2);
        var r = set2.IsSupersetOf(set1);
        Console.WriteLine(r);
        Assert.That(r, Is.True);
    }
}
