using System;
using Veldrid;

namespace HLView.Graphics
{
    public interface IRenderTarget
    {
        event EventHandler Resize;

        int Width { get; }
        int Height { get; }

        Swapchain Swapchain { get; }
        ICamera Camera { get; }
    }
}