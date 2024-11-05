using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Security.Cryptography;
using X86 = System.Runtime.Intrinsics.X86;
using Arm = System.Runtime.Intrinsics.Arm;

namespace Coplt.Universes.Collections;

public struct AesHasher
{
    #region Feilds

    public Vector128<byte> enc;
    public Vector128<byte> sum;
    public Vector128<byte> key;

    #endregion

    #region Ctor

    public AesHasher(Vector128<byte> key1, Vector128<byte> key2)
    {
        var pi0 = Vector128.Create(0x243f_6a88_85a3_08d3, 0x1319_8a2e_0370_7344).AsByte();
        var pi1 = Vector128.Create(0xa409_3822_299f_31d0, 0x082e_fa98_ec4e_6c89).AsByte();
        enc = key1 ^ pi0;
        sum = key2 ^ pi1;
        key = enc ^ sum;
    }

    #endregion

    #region Static

    public static bool IsSupported => X86.Aes.IsSupported || Arm.Aes.IsSupported;

    public static readonly AesHasher Init;

    static AesHasher()
    {
        Span<Vector128<byte>> keys = stackalloc Vector128<byte>[2];
        RandomNumberGenerator.Fill(MemoryMarshal.AsBytes(keys));
        Init = new(keys[0], keys[1]);
    }

    #endregion

    #region Impl

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector128<byte> AesEnc(Vector128<byte> value, Vector128<byte> xor)
    {
        if (X86.Aes.IsSupported)
        {
            return X86.Aes.Encrypt(value, xor);
        }
        if (Arm.Aes.IsSupported)
        {
            var res = Arm.Aes.MixColumns(Arm.Aes.Encrypt(value, default));
            return xor ^ res;
        }
        throw new NotSupportedException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector128<byte> AesDec(Vector128<byte> value, Vector128<byte> xor)
    {
        if (X86.Aes.IsSupported)
        {
            return X86.Aes.Decrypt(value, xor);
        }
        if (Arm.Aes.IsSupported)
        {
            var res = Arm.Aes.InverseMixColumns(Arm.Aes.Decrypt(value, default));
            return xor ^ res;
        }
        throw new NotSupportedException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector128<byte> Shuffle(Vector128<byte> value) => Vector128.Shuffle(
        value,
        Vector128.Create(
            (byte)0x02, 0x0a, 0x07, 0x00, 0x0c, 0x01, 0x03, 0x0e, 0x05, 0x0f, 0x0d, 0x08, 0x06, 0x09, 0x0b, 0x04
        )
    );

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector128<byte> ShuffleAndAdd(Vector128<byte> value, Vector128<byte> add)
    {
        var shuffled = Shuffle(value);
        return (shuffled.AsUInt64() + add.AsUInt64()).AsByte();
    }

    #endregion

    #region Write Vector128<byte>

    public void Write(Vector128<byte> value)
    {
        enc = AesDec(enc, value);
        sum = ShuffleAndAdd(sum, value);
    }

    public void Write(Vector128<byte> v1, Vector128<byte> v2)
    {
        enc = AesDec(enc, v1);
        sum = ShuffleAndAdd(sum, v1);
        enc = AesDec(enc, v2);
        sum = ShuffleAndAdd(sum, v2);
    }

    #endregion

    #region Write

    public void Write(sbyte value) => Write(Vector128.Create(value).AsByte());
    public void Write(byte value) => Write(Vector128.Create(value).AsByte());
    public void Write(short value) => Write(Vector128.Create(value).AsByte());
    public void Write(ushort value) => Write(Vector128.Create(value).AsByte());
    public void Write(int value) => Write(Vector128.Create(value).AsByte());
    public void Write(uint value) => Write(Vector128.Create(value).AsByte());
    public void Write(long value) => Write(Vector128.Create(value).AsByte());
    public void Write(ulong value) => Write(Vector128.Create(value).AsByte());
    public void Write(char value) => Write(Vector128.Create(value).AsByte());
    public void Write(nint value) => Write(Vector128.Create(value).AsByte());
    public void Write(nuint value) => Write(Vector128.Create(value).AsByte());
    public void Write(Int128 value) => Write(Unsafe.BitCast<Int128, Vector128<byte>>(value));
    public void Write(UInt128 value) => Write(Unsafe.BitCast<UInt128, Vector128<byte>>(value));
    public void Write(Half value) => Write(Vector128.Create(Unsafe.BitCast<Half, ushort>(value)).AsByte());
    public void Write(float value) => Write(Vector128.Create(value).AsByte());
    public void Write(double value) => Write(Vector128.Create(value).AsByte());

    #endregion

    #region Finish

    public Vector128<byte> Finish()
    {
        var combined = AesEnc(sum, enc);
        return AesDec(AesDec(combined, key), combined);
    }

    #endregion

    #region Hash

    public static ulong Hash(int hash)
    {
        var hasher = Init;
        hasher.Write(hash);
        return hasher.Finish().AsUInt64()[0];
    }

    #endregion
}
