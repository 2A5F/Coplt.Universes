using Coplt.Universes.Collections;

namespace Tests.Collections;

[Parallelizable]
public class TestVector
{
    [Test]
    public void Test1()
    {
        var vec = new SVector<int>();
        Console.WriteLine(vec.Count);
        Assert.That(vec, Has.Count.EqualTo(0));
        vec.Add(1);
        Console.WriteLine(vec.Count);
        Assert.That(vec, Has.Count.EqualTo(1));
        Console.WriteLine(vec[0]);
        Assert.That(vec, Has.ItemAt(0).EqualTo(1));
    }
    
    [Test]
    public void Test2()
    {
        var vec = new SVector<int>();
        Console.WriteLine(vec.Count);
        Assert.That(vec, Has.Count.EqualTo(0));
        for (var i = 0; i < 32; i++)
        {
            vec.Add(i);
        }
        Console.WriteLine(vec.Count);
        Assert.That(vec, Has.Count.EqualTo(32));
        Console.WriteLine(string.Join(", ", vec));
        for (var i = 0; i < 32; i++)
        {
            Console.WriteLine(vec[i]);
            Assert.That(vec, Has.ItemAt(i).EqualTo(i));
        }
    }
}
