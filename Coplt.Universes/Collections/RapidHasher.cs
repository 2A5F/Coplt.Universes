using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Security.Cryptography;

namespace Coplt.Universes.Collections;

public struct RapidHasher
{
    #region Fields

    internal ulong seed;
    internal ulong a;
    internal ulong b;
    internal ulong size;

    #endregion

    #region Ctor

    public RapidHasher(ulong seed)
    {
        this.seed = seed;
        a = 0;
        b = 0;
        size = 0;
    }

    #endregion

    #region Static

    public static readonly RapidHasher Init = new(DefaultSeed);
    public const ulong DefaultSeed = 0xbdd89aa982704029;
    public const ulong RAPID_SECRET_0 = 0x2d358dccaa6c78a5;
    public const ulong RAPID_SECRET_1 = 0x8bb84b93962eacc9;
    public const ulong RAPID_SECRET_2 = 0x4b33a62ed433d4a3;

    #endregion

    #region Impl

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong RapidHashSeed(ulong seed, ulong size) =>
        seed ^ RapidMix(seed ^ RAPID_SECRET_0, RAPID_SECRET_1) ^ size;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong RapidMix(ulong a, ulong b)
    {
        (a, b) = RapidMum(a, b);
        return a ^ b;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (ulong a, ulong b) RapidMum(ulong a, ulong b)
    {
        var r = (UInt128)a * b;
        return (get_Lower(r), get_Upper(r));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_Lower")]
    private static extern ulong get_Lower(in UInt128 a);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_Upper")]
    private static extern ulong get_Upper(in UInt128 a);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ReadU32(ReadOnlySpan<byte> slice, int offset)
    {
        var val = Unsafe.As<byte, uint>(ref Unsafe.Add(ref Unsafe.AsRef(in slice.GetPinnableReference()), offset));
        if (!BitConverter.IsLittleEndian) val = BinaryPrimitives.ReverseEndianness(val);
        return val;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ReadU64(ReadOnlySpan<byte> slice, int offset)
    {
        var val = Unsafe.As<byte, ulong>(ref Unsafe.Add(ref Unsafe.AsRef(in slice.GetPinnableReference()), offset));
        if (!BitConverter.IsLittleEndian) val = BinaryPrimitives.ReverseEndianness(val);
        return val;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ReadU32Combine(ReadOnlySpan<byte> slice, int offset_top, int offset_bottom)
    {
        var top = (ulong)ReadU32(slice, offset_top);
        var bot = (ulong)ReadU32(slice, offset_bottom);
        return (top << 32) | bot;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RapidHashCore(ulong a, ulong b, ulong seed, ReadOnlySpan<byte> data)
    {
        if (data.Length <= 16)
        {
            if (data.Length >= 8)
            {
                var plast = data.Length - 4;
                var delta = 4;
                a ^= ReadU32Combine(data, 0, plast);
                b ^= ReadU32Combine(data, delta, plast - delta);
            }
            else if (data.Length >= 4)
            {
                var plast = data.Length - 4;
                var v = ReadU32Combine(data, 0, plast);
                a ^= v;
                b ^= v;
            }
            else if (data.Length > 0)
            {
                var len = data.Length;
                a ^= ((ulong)data[0] << 56) | ((ulong)data[len >> 1] << 32) | data[len - 1];
            }
        }
        else
        {
            var slice = data;

            var see1 = seed;
            var see2 = seed;

            while (slice.Length >= 96)
            {
                seed = RapidMix(ReadU64(slice, 0) ^ RAPID_SECRET_0, ReadU64(slice, 8) ^ seed);
                see1 = RapidMix(ReadU64(slice, 16) ^ RAPID_SECRET_1, ReadU64(slice, 24) ^ see1);
                see2 = RapidMix(ReadU64(slice, 32) ^ RAPID_SECRET_2, ReadU64(slice, 40) ^ see2);
                seed = RapidMix(ReadU64(slice, 48) ^ RAPID_SECRET_0, ReadU64(slice, 56) ^ seed);
                see1 = RapidMix(ReadU64(slice, 64) ^ RAPID_SECRET_1, ReadU64(slice, 72) ^ see1);
                see2 = RapidMix(ReadU64(slice, 80) ^ RAPID_SECRET_2, ReadU64(slice, 88) ^ see2);
                slice = slice.Slice(96);
            }
            if (slice.Length >= 48)
            {
                seed = RapidMix(ReadU64(slice, 0) ^ RAPID_SECRET_0, ReadU64(slice, 8) ^ seed);
                see1 = RapidMix(ReadU64(slice, 16) ^ RAPID_SECRET_1, ReadU64(slice, 24) ^ see1);
                see2 = RapidMix(ReadU64(slice, 32) ^ RAPID_SECRET_2, ReadU64(slice, 40) ^ see2);
                slice = slice.Slice(48);
            }
            seed ^= see1 ^ see2;

            if (slice.Length > 16)
            {
                seed = RapidMix(ReadU64(slice, 0) ^ RAPID_SECRET_2, ReadU64(slice, 8) ^ seed ^ RAPID_SECRET_1);
                if (slice.Length > 32)
                {
                    seed = RapidMix(ReadU64(slice, 16) ^ RAPID_SECRET_2, ReadU64(slice, 24) ^ seed);
                }
            }

            a ^= ReadU64(data, data.Length - 16);
            b ^= ReadU64(data, data.Length - 8);
        }

        a ^= RAPID_SECRET_1;
        b ^= seed;

        (this.a, this.b) = RapidMum(a, b);
        this.seed = seed;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RapidHashCore(ulong a, ulong b, ulong seed, ulong data)
    {
        a ^= data;
        b ^= (uint)data;

        a ^= RAPID_SECRET_1;
        b ^= seed;

        (this.a, this.b) = RapidMum(a, b);
        this.seed = seed;
    }

    #endregion

    #region Write

    public void Write(byte data) => Write((ulong)data);
    public void Write(sbyte data) => Write((ulong)data);
    public void Write(short data) => Write((ulong)data);
    public void Write(ushort data) => Write((ulong)data);
    public void Write(int data) => Write((ulong)data);
    public void Write(uint data) => Write((ulong)data);
    public void Write(long data) => Write((ulong)data);
    public void Write(ulong data)
    {
        size += sizeof(ulong);
        seed = RapidHashSeed(seed, size);
        RapidHashCore(a, b, seed, data);
    }
    public void Write(float data) => Write(Unsafe.BitCast<float, uint>(data));
    public void Write(double data) => Write(Unsafe.BitCast<double, ulong>(data));
    public void Write(char data) => Write((ulong)data);
    public unsafe void Write<T>(T value) where T : unmanaged
    {
        if (sizeof(T) <= sizeof(ulong))
        {
            var data = 0ul;
            Unsafe.As<ulong, T>(ref data) = value;
            Write(data);
        }
        else Write(MemoryMarshal.AsBytes(new ReadOnlySpan<T>(ref value)));
    }
    public void Write(ReadOnlySpan<byte> bytes)
    {
        size += (ulong)bytes.Length;
        seed = RapidHashSeed(seed, size);
        RapidHashCore(a, b, seed, bytes);
    }

    #endregion

    #region Finish

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong Finish() => RapidMix(a ^ RAPID_SECRET_0 ^ size, b ^ RAPID_SECRET_1);

    #endregion

    #region Hash

    public static ulong Hash(int hash)
    {
        var hasher = Init;
        hasher.Write(hash);
        return hasher.Finish();
    }

    #endregion
}
