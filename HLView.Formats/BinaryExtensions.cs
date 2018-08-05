using System;
using System.IO;
using System.Numerics;

namespace HLView.Formats
{
    public static class BinaryExtensions
    {
        public static Vector3 ReadVector3(this BinaryReader br)
        {
            return new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
        }

        public static string ReadFixedLengthString(this BinaryReader br, int length)
        {
            var name = br.ReadChars(length);
            var len = Array.IndexOf(name, '\0');
            return new string(name, 0, len < 0 ? name.Length : len);
        }

        public static T[] ReadArray<T>(this BinaryReader br, int num, Func<BinaryReader, T> read)
        {
            var t = new T[num];
            for (var i = 0; i < num; i++) t[i] = read(br);
            return t;
        }
    }
}