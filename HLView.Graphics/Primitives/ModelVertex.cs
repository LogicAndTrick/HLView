using System.Numerics;
using System.Runtime.InteropServices;

namespace HLView.Graphics.Primitives
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ModelVertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 Texture;
        public uint Bone;

        public const int SizeInBytes = (3 + 3 + 2 + 1) * 4;
    }
}