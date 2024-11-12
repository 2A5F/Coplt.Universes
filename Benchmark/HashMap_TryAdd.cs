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
public class HashMap_TryAdd
{
    private int[] data;

    [Params(10, 100, 1000, 10000)]
    public int Size { get; set; }

    [GlobalSetup]
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void SetUp()
    {
        data = new int[10000];
        RandomNumberGenerator.Fill(MemoryMarshal.AsBytes(data.AsSpan()));
    }

    [Benchmark]
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
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
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public SDenseHashMap<int, int, DenseHashSearcher.SysAlg, Hasher.AsIs> SysAlg()
    {
        var map = new SDenseHashMap<int, int, DenseHashSearcher.SysAlg, Hasher.AsIs>();
        foreach (var item in data.AsSpan(0, Size))
        {
            map.TryAdd(item, item);
        }
        return map;
    }

    [Benchmark]
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public SDenseHashMap<int, int, DenseHashSearcher.SysAlg, Hasher.Aes> SysAlg_AesHash()
    {
        var map = new SDenseHashMap<int, int, DenseHashSearcher.SysAlg, Hasher.Aes>();
        foreach (var item in data.AsSpan(0, Size))
        {
            map.TryAdd(item, item);
        }
        return map;
    }

    [Benchmark(Baseline = true)]
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
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
