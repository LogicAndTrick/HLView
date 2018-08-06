using System;
using System.Diagnostics;
using System.Windows.Forms;
using HLView.Graphics;
using HLView.Graphics.Cameras;
using Veldrid;

namespace HLView
{
    public class VeldridControl : Control, IRenderTarget
    {
        private static readonly IntPtr HInstance = Process.GetCurrentProcess().Handle;

        public Swapchain Swapchain { get; }

        public ICamera Camera { get; set; }

        public VeldridControl(GraphicsDevice graphics, GraphicsDeviceOptions options)
        {
            SetStyle(ControlStyles.Opaque, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            DoubleBuffered = false;

            var hWnd = Handle; // Will call CreateHandle internally
            var hInstance = HInstance;

            uint w = (uint) Width, h = (uint) Height;
            if (w <= 0) w = 1;
            if (h <= 0) h = 1;

            var source = SwapchainSource.CreateWin32(hWnd, hInstance);
            var desc = new SwapchainDescription(source, w, h, options.SwapchainDepthFormat, options.SyncToVerticalBlank);
            Swapchain = graphics.ResourceFactory.CreateSwapchain(desc);

            Camera = new PerspectiveCamera(Width, Height);
        }

        protected override bool IsInputKey(Keys keyData)
        {
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Swapchain.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
