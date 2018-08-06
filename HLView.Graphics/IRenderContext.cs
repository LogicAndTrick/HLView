using System.Numerics;
using HLView.Graphics.Cameras;
using Veldrid;

namespace HLView.Graphics
{
    public interface IRenderContext
    {
        ICamera Camera { get; }
    }
}