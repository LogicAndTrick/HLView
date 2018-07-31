using System.Numerics;
using Veldrid;

namespace HLView.Graphics
{
    public interface IRenderContext
    {
        ICamera Camera { get; }
        void SetModelMatrix(Matrix4x4 model);
        void ClearModelMatrix();
    }
}