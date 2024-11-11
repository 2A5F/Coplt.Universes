using System.Runtime.InteropServices;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using Coplt.Universes.Collections;

namespace Benchmark;

[DisassemblyDiagnoser(printSource: true, exportHtml: true, syntax: DisassemblySyntax.Intel)]
[MemoryDiagnoser]
public class HashMap_TryAdd
{
    private int[] data;

    [Params(10, 100, 1000, 10000)]
    public int Size { get; set; }

    [GlobalSetup]
    public void SetUp()
    {
        data = new int[10000];
        RandomNumberGenerator.Fill(MemoryMarshal.AsBytes(data.AsSpan()));
    }

    [Benchmark]
    public SDenseHashMap<int, int, DenseHashSearcher.Ankerl, Hasher.Default> Ankerl()
    {
        var map = new SDenseHashMap<int, int, DenseHashSearcher.Ankerl, Hasher.Default>();
        foreach (var item in data.AsSpan(0, Size))
        {
            map.TryAdd(item, item);
        }
        return map;
    }

    [Benchmark]
    public SDenseHashMap<int, int, DenseHashSearcher.SysAlg, Hasher.Default> SystemAlg()
    {
        var map = new SDenseHashMap<int, int, DenseHashSearcher.SysAlg, Hasher.Default>();
        foreach (var item in data.AsSpan(0, Size))
        {
            map.TryAdd(item, item);
        }
        return map;
    }

    [Benchmark(Baseline = true)]
    public Dictionary<int, int> Dictionary()
    {
        var map = new Dictionary<int, int>();
        foreach (var item in data.AsSpan(0, Size))
        {
            map.TryAdd(item, item);
        }
        return map;
    }
}
