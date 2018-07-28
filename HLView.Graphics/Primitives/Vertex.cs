using System.Numerics;
using System.Runtime.InteropServices;

namespace HLView.Graphics.Primitives
{
    [StructLayout(LayoutKind.Sequential)]
    struct Vertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 Texture;
    }
}