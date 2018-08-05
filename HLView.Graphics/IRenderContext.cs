using System.Numerics;
using Veldrid;

namespace HLView.Graphics
{
    public interface IRenderContext
    {
        ICamera Camera { get; }
    }
}