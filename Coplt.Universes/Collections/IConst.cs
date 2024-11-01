using System.Runtime.CompilerServices;

namespace Coplt.Universes.Collections.Magics;

public interface IConst<out T>
{
    public static abstract T Value { get; }
}

public static class Constants
{
    public readonly record struct Int_0 : IConst<int>
    {
        public static int Value => 0;
    }

    public readonly record struct Int_1 : IConst<int>
    {
        public static int Value => 1;
    }

    public readonly record struct Int_2 : IConst<int>
    {
        public static int Value => 2;
    }

    public readonly record struct Int_4 : IConst<int>
    {
        public static int Value => 4;
    }

    public readonly record struct Int_8 : IConst<int>
    {
        public static int Value => 8;
    }

    public readonly record struct Int_16 : IConst<int>
    {
        public static int Value => 16;
    }

    public readonly record struct Int_32 : IConst<int>
    {
        public static int Value => 32;
    }

    public readonly record struct Int_64 : IConst<int>
    {
        public static int Value => 64;
    }

    public readonly record struct Int_128 : IConst<int>
    {
        public static int Value => 128;
    }

    public readonly record struct Int_256 : IConst<int>
    {
        public static int Value => 256;
    }

    public readonly record struct Int_512 : IConst<int>
    {
        public static int Value => 512;
    }

    public readonly record struct Int_1024 : IConst<int>
    {
        public static int Value => 1024;
    }

    public readonly record struct Int_2048 : IConst<int>
    {
        public static int Value => 2048;
    }

    public readonly record struct Int_4096 : IConst<int>
    {
        public static int Value => 4096;
    }
}
