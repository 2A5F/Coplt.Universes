using System.Runtime.InteropServices;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using Coplt.Universes.Collections;
using WyHash;

namespace Benchmark;

[DisassemblyDiagnoser(printSource: true, exportHtml: true, syntax: DisassemblySyntax.Intel)]
[MemoryDiagnoser]
public class Hasher_Alg
{
    private int[] data;

    [GlobalSetup]
    public void SetUp()
    {
        data = new int[10000];
        RandomNumberGenerator.Fill(MemoryMarshal.AsBytes(data.AsSpan()));
    }

    [Benchmark(Baseline = true)]
    public int System_HashCode()
    {
        var hasher = new HashCode();
        foreach (var item in data)
        {
            hasher.Add(item);
        }
        return hasher.ToHashCode();
    }

    [Benchmark]
    public int System_HashCode_Batch()
    {
        var hasher = new HashCode();
        hasher.AddBytes(MemoryMarshal.AsBytes(data.AsSpan()));
        return hasher.ToHashCode();
    }

    [Benchmark]
    public ulong AHash_Aes()
    {
        var hasher = AesHasher.Init;
        foreach (var item in data)
        {
            hasher.Write(item);
        }
        return hasher.Finish();
    }

    [Benchmark]
    public ulong Rapid()
    {
        var hasher = RapidHasher.Init;
        foreach (var item in data)
        {
            hasher.Write(item);
        }
        return hasher.Finish();
    }

    [Benchmark]
    public ulong Rapid_Batch()
    {
        var hasher = RapidHasher.Init;
        hasher.Write(MemoryMarshal.AsBytes(data.AsSpan()));
        return hasher.Finish();
    }

    [Benchmark]
    public ulong WyHash_Batch()
    {
        return WyHash64.ComputeHash64(MemoryMarshal.AsBytes(data.AsSpan()));
    }
}
