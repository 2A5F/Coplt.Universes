using Coplt.Universes.Collections;
using Coplt.Universes.Collections.Magics;

namespace Tests.Collections;

[Parallelizable]
public class TestChunkedVector
{
    [Test]
    public void Test1()
    {
        var vec = new SChunkedVector<string, Constants.Int_4, ChunkedVector.BehaviorSwapToEnd>();
        for (int i = 0; i < 16; i++)
        {
            vec.Add($"{i}");
        }
        var a = vec[1];
        Console.WriteLine(a);
        Assert.That(a, Is.EqualTo("1"));
        vec.RemoveAt(1);
        var b = vec[1];
        Console.WriteLine(b);
        Assert.That(b, Is.EqualTo("15"));
    }

    [Test]
    public void Test2()
    {
        var vec = new SChunkedVector<string, Constants.Int_4, ChunkedVector.BehaviorMoveItems>();
        for (int i = 0; i < 16; i++)
        {
            vec.Add($"{i}");
        }
        var a = vec[1];
        Console.WriteLine(a);
        Assert.That(a, Is.EqualTo("1"));
        vec.RemoveAt(1);
        var b = vec[1];
        Console.WriteLine(b);
        Assert.That(b, Is.EqualTo("2"));
    }

    [Test]
    public void Test3()
    {
        var vec = new SChunkedVector<int, Constants.Int_4, ChunkedVector.BehaviorSwapToEnd>();
        for (var i = 0; i < 64; i++)
        {
            vec.Add(i);
        }
        Assert.That(vec, Has.Count.EqualTo(64));
        var n = 0;
        foreach (var item in vec)
        {
            Console.WriteLine(item);
            Assert.That(item, Is.EqualTo(n++));
        }
        foreach (var item in vec.Reverse)
        {
            Console.WriteLine(item);
            Assert.That(item, Is.EqualTo(--n));
        }
    }

    [Test]
    public void Test4()
    {
        var vec = new SChunkedVector<int, Constants.Int_4, ChunkedVector.BehaviorSwapToEnd>();
        for (var i = 0; i < 64; i++)
        {
            vec.Add(i);
        }
        Assert.That(vec, Has.Count.EqualTo(64));

        Console.WriteLine(string.Join(", ", vec));
        CollectionAssert.AreEquivalent(Enumerable.Range(0, 64), vec);

        Console.WriteLine(string.Join(", ", vec.Reverse));
        CollectionAssert.AreEquivalent(Enumerable.Range(0, 64).Reverse(), vec.Reverse);
    }

    [Test]
    public void Test5()
    {
        var vec = new SChunkedVector<string, Constants.Int_4, ChunkedVector.BehaviorSwapToEnd>();
        for (int i = 0; i < 16; i++)
        {
            vec.Add($"{i}");
        }
        for (int i = 0; i < 16; i++)
        {
            vec.RemoveAt(0);
            if (vec.Count > 0)
            {
                var b = vec[0];
                Console.WriteLine(b);
            }
        }
        Assert.That(vec, Has.Count.EqualTo(0));
    }

    [Test]
    public void Test6()
    {
        var vec = new SChunkedVector<string, Constants.Int_4, ChunkedVector.BehaviorMoveItems>();
        for (int i = 0; i < 16; i++)
        {
            vec.Add($"{i}");
        }
        for (int i = 0; i < 16; i++)
        {
            vec.RemoveAt(0);
            if (vec.Count > 0)
            {
                var b = vec[0];
                Console.WriteLine(b);
            }
        }
        Assert.That(vec, Has.Count.EqualTo(0));
    }

    [Test]
    public void Test7()
    {
        var vec = new SChunkedVector<int, Constants.Int_4, ChunkedVector.BehaviorSwapToEnd>();
        for (int i = 0; i < 16; i++)
        {
            vec.Insert(0, i + 1);
        }
        Console.WriteLine(string.Join(", ", vec));
        CollectionAssert.AreEquivalent(new[] { 16, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 }, vec);
    }

    [Test]
    public void Test8()
    {
        var vec = new SChunkedVector<int, Constants.Int_4, ChunkedVector.BehaviorMoveItems>();
        for (int i = 0; i < 16; i++)
        {
            vec.Insert(0, i + 1);
        }
        Console.WriteLine(string.Join(", ", vec));
        CollectionAssert.AreEquivalent(new[] { 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 }, vec);
    }
}
