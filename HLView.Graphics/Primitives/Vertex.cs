using System.Numerics;
using System.Runtime.InteropServices;

namespace HLView.Graphics.Primitives
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector4 Colour;
        public Vector2 Texture;
        public Vector2 Lightmap;

        public const int SizeInBytes = (3 + 3 + 4 + 2 + 2) * 4;
    }
}