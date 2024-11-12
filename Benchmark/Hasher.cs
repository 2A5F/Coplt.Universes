using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using Coplt.Universes.Collections;
using WyHash;

namespace Benchmark;

[MemoryDiagnoser]
[JitStatsDiagnoser]
[DisassemblyDiagnoser(maxDepth: 1024, printSource: true, exportHtml: true, syntax: DisassemblySyntax.Intel)]
public class Hasher_Alg
{
    private int[] data;

    [GlobalSetup]
    public void SetUp()
    {
        data = new int[10000];
        RandomNumberGenerator.Fill(MemoryMarshal.AsBytes(data.AsSpan()));
    }

    #region System_HashCode

    [Benchmark(Baseline = true)]
    [Arguments(false)]
    [Arguments(true)]
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public int System_HashCode(bool Batch)
    {
        if (Batch) return System_HashCode_Batch();
        else return System_HashCode_NoBatch();
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
    private int System_HashCode_NoBatch()
    {
        var hasher = new HashCode();
        foreach (var item in data)
        {
            hasher.Add(item);
        }
        return hasher.ToHashCode();
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
    private int System_HashCode_Batch()
    {
        var hasher = new HashCode();
        hasher.AddBytes(MemoryMarshal.AsBytes(data.AsSpan()));
        return hasher.ToHashCode();
    }

    #endregion

    #region AHash_Aes

    [Benchmark]
    [Arguments(false)]
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public ulong AHash_Aes(bool Batch)
    {
        if (Batch) return 0;
        else return AHash_Aes_NoBatch();
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
    private ulong AHash_Aes_NoBatch()
    {
        var hasher = AesHasher.Init;
        foreach (var item in data)
        {
            hasher.Write(item);
        }
        return hasher.Finish();
    }

    #endregion

    #region Rapid

    [Benchmark]
    [Arguments(false)]
    [Arguments(true)]
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public ulong Rapid(bool Batch)
    {
        if (Batch) return Rapid_Batch();
        else return Rapid_NoBatch();
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
    private ulong Rapid_NoBatch()
    {
        var hasher = RapidHasher.Init;
        foreach (var item in data)
        {
            hasher.Write(item);
        }
        return hasher.Finish();
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
    private ulong Rapid_Batch()
    {
        var hasher = RapidHasher.Init;
        hasher.Write(MemoryMarshal.AsBytes(data.AsSpan()));
        return hasher.Finish();
    }

    #endregion

    #region WyHash

    [Benchmark]
    [Arguments(true)]
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public ulong WyHash(bool Batch)
    {
        if (Batch) return WyHash_Batch();
        else return 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
    private ulong WyHash_Batch()
    {
        return WyHash64.ComputeHash64(MemoryMarshal.AsBytes(data.AsSpan()));
    }

    #endregion
}
