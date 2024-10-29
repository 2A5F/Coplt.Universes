using Coplt.Universes.Core;

namespace Tests;

[Parallelizable]
public class TestEmitFixedArray
{
    [Test]
    public void Test1()
    {
        var type = FixedArrayEmitter.Get(16);
        Console.WriteLine(type);
        var value = Activator.CreateInstance(type.MakeGenericType(typeof(float)));
        Console.WriteLine(value);
        var arr = (IFixedArray<float>)value!;
        ref var item = ref arr[1];
        item = 123;
        Console.WriteLine(item);

        Assert.That(arr.Length, Is.EqualTo(16));
        Assert.That(arr.Size, Is.EqualTo(16 * sizeof(float)));
        Assert.That(arr[1], Is.EqualTo(123));
    }
}
