using System.Runtime.InteropServices;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using Coplt.Universes.Collections;

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

    [Benchmark]
    public ulong Aes()
    {
        var hasher = AesHasher.Init;
        foreach (var item in data)
        {
            hasher.Write(item);
        }
        return hasher.Finish()[0];
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
}