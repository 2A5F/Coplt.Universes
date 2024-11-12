using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using Coplt.Universes.Collections;

namespace Benchmark;

[MemoryDiagnoser]
[JitStatsDiagnoser]
[DisassemblyDiagnoser(maxDepth: 1024, printSource: true, exportHtml: true, syntax: DisassemblySyntax.Intel)]
public class HashMap_ContainsKey
{
    private int[] data;
    private SDenseHashMap<int, int, DenseHashSearcher.Ankerl, Hasher.Default>[] ankerls;
    private SDenseHashMap<int, int, DenseHashSearcher.SysAlg, Hasher.AsIs>[] systems;
    private Dictionary<int, int>[] dictionaries;
    private static int[] Sizes = [10, 100, 1000, 10000];

    [Params(10, 100, 1000, 10000)]
    public int Size { get; set; }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    private int SizeIndex(int size) => size switch
    {
        10 => 0,
        100 => 1,
        1000 => 2,
        10000 => 3,
        _ => throw new ArgumentOutOfRangeException()
    };

    [GlobalSetup]
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void SetUp()
    {
        data = new int[10000];
        RandomNumberGenerator.Fill(MemoryMarshal.AsBytes(data.AsSpan()));

        ankerls = new SDenseHashMap<int, int, DenseHashSearcher.Ankerl, Hasher.Default>[4];
        for (int i = 0; i < 4; i++)
        {
            ankerls[i] = new();
            foreach (var item in data.AsSpan(0, Sizes[i]))
            {
                ankerls[i].TryAdd(item, item);
            }
        }

        systems = new SDenseHashMap<int, int, DenseHashSearcher.SysAlg, Hasher.AsIs>[4];
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
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public bool[] Ankerl()
    {
        var results = new bool[data.Length];
        var size = SizeIndex(Size);
        ref var map = ref ankerls[size];
        for (int i = 0; i < size; i++)
        {
            results[i] = map.ContainsKey(data[i]);
        }
        return results;
    }

    [Benchmark]
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public bool[] SysAlg()
    {
        var results = new bool[data.Length];
        var size = SizeIndex(Size);
        ref var map = ref systems[size];
        for (int i = 0; i < size; i++)
        {
            results[i] = map.ContainsKey(data[i]);
        }
        return results;
    }

    [Benchmark(Baseline = true)]
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public bool[] Dictionary()
    {
        var results = new bool[data.Length];
        var size = SizeIndex(Size);
        ref var map = ref dictionaries[size];
        for (int i = 0; i < size; i++)
        {
            results[i] = map.ContainsKey(data[i]);
        }
        return results;
    }
}
