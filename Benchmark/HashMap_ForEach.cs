using System.Runtime.InteropServices;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using Coplt.Universes.Collections;

namespace Benchmark;

[DisassemblyDiagnoser(printSource: true, exportHtml: true, syntax: DisassemblySyntax.Intel)]
[MemoryDiagnoser]
public class HashMap_ForEach
{
    private int[] data;
    private SAnkerlHashMap<int, int, Hasher.Default>[] ankerls;
    private Dictionary<int, int>[] dictionaries;
    private static int[] Sizes = [10, 100, 1000, 10000];

    [Params(10, 100, 1000, 10000)]
    public int Size { get; set; }

    private int SizeIndex(int size) => size switch
    {
        10 => 0,
        100 => 1,
        1000 => 2,
        10000 => 3,
        _ => throw new ArgumentOutOfRangeException()
    };

    [GlobalSetup]
    public void SetUp()
    {
        data = new int[10000];
        RandomNumberGenerator.Fill(MemoryMarshal.AsBytes(data.AsSpan()));

        ankerls = new SAnkerlHashMap<int, int, Hasher.Default>[4];
        for (int i = 0; i < 4; i++)
        {
            ankerls[i] = new();
            foreach (var item in data.AsSpan(0, Sizes[i]))
            {
                ankerls[i].TryAdd(item, item);
            }
        }

        dictionaries = new Dictionary<int, int>[4];
        for (int i = 0; i < 4; i++)
        {
            dictionaries[i] = new();
            foreach (var item in data.AsSpan(0, Sizes[i]))
            {
                dictionaries[i].TryAdd(item, item);
            }
        }
    }

    [Benchmark]
    public int[] Ankerl()
    {
        var results = new int[data.Length];
        var size = SizeIndex(Size);
        ref var map = ref ankerls[size];
        var i = 0;
        foreach (var kv in map)
        {
            results[i++] = kv.Value;
        }
        return results;
    }

    [Benchmark(Baseline = true)]
    public int[] Dictionary()
    {
        var results = new int[data.Length];
        var size = SizeIndex(Size);
        ref var map = ref dictionaries[size];
        var i = 0;
        foreach (var kv in map)
        {
            results[i++] = kv.Value;
        }
        return results;
    }
}
