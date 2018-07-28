using System.Numerics;

namespace HLView.Graphics.Primitives
{
    struct UniformProjection
    {
        public Matrix4x4 Model { get; set; }
        public Matrix4x4 View { get; set; }
        public Matrix4x4 Projection { get; set; }
    }
}