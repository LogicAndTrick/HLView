using System;
using System.Diagnostics;
using System.Windows.Forms;
using HLView.Graphics;
using Veldrid;

namespace HLView
{
    public class VeldridControl : Control, IRenderTarget
    {
        private static readonly IntPtr HInstance = Process.GetCurrentProcess().Handle;
        private Camera _camera;

        public Swapchain Swapchain { get; }

        public ICamera Camera
        {
            get => _camera;
        }

        public VeldridControl(GraphicsDevice graphics, GraphicsDeviceOptions options)
        {
            SetStyle(ControlStyles.Opaque, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            DoubleBuffered = false;

            var hWnd = Handle; // Will call CreateHandle internally
            var hInstance = HInstance;

            var source = SwapchainSource.CreateWin32(hWnd, hInstance);
            var desc = new SwapchainDescription(source, (uint)Width, (uint)Height, options.SwapchainDepthFormat, options.SyncToVerticalBlank);
            Swapchain = graphics.ResourceFactory.CreateSwapchain(desc);

            _camera = new Camera(Width, Height);
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
