namespace Coplt.Universes.Collections;

public interface IHashWrapper
{
    public static abstract ulong Hash(int hash);
}

public static class Hasher
{
    public struct Default : IHashWrapper
    {
        public static ulong Hash(int hash) => Aes.Hash(hash);
    }

    public struct AsIs : IHashWrapper
    {
        public static ulong Hash(int hash) => (ulong)hash;
    }

    public struct Rapid : IHashWrapper
    {
        public static ulong Hash(int hash) => RapidHasher.Hash(hash);
    }

    public struct Aes : IHashWrapper
    {
        public static ulong Hash(int hash)
        {
            if (AesHasher.IsSupported) return AesHasher.Hash(hash);
            throw new NotSupportedException("Non x86 and arm architecture CPUs are not yet supported");
            // todo write fallback
        }
    }

    public struct Xx : IHashWrapper
    {
        public static ulong Hash(int hash)
        {
            return (ulong)HashCode.Combine(hash); // todo replace to xx 64
        }
    }
}
