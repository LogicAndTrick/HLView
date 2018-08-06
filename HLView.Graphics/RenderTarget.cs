using System;
using HLView.Graphics.Cameras;
using Veldrid;

namespace HLView.Graphics
{
    internal class RenderTarget : IDisposable, IRenderContext
    {
        private readonly SceneContext _self;
        private readonly CommandList _commandList;
        private bool _resizeRequired;

        public IRenderTarget Target { get; set; }
        public ICamera Camera => Target.Camera;

        public RenderTarget(IRenderTarget target, SceneContext self)
        {
            _self = self;
            Target = target;

            target.Resize += OnResize;
            _resizeRequired = Target.Width != target.Swapchain.Framebuffer.Width || Target.Height != Target.Swapchain.Framebuffer.Height;

            _commandList = _self.Device.ResourceFactory.CreateCommandList();
        }

        private void OnResize(object sender, EventArgs e)
        {
            _resizeRequired = true;
        }

        public void Update(long milliseconds)
        {
            Target.Camera.Update(milliseconds);
        }

        public void Render(Scene scene)
        {
            if (_resizeRequired)
            {
                var w = Math.Max(Target.Width, 1);
                var h = Math.Max(Target.Height, 1);
                Target.Swapchain.Resize((uint) w, (uint) h);
                Target.Camera.WindowResized(w, h);
                _resizeRequired = false;
            }

            _commandList.Begin();
            _commandList.SetFramebuffer(Target.Swapchain.Framebuffer);
            _commandList.ClearDepthStencil(1);
            _commandList.ClearColorTarget(0, RgbaFloat.Black);

            scene?.Render(_commandList, _self, this);
            scene?.RenderAlpha(_commandList, _self, this, Target.Camera.Location);

            _commandList.End();

            _self.Device.SubmitCommands(_commandList);
            _self.Device.SwapBuffers(Target.Swapchain);
        }

        public void Dispose()
        {
            Target.Resize -= OnResize;
            _commandList.Dispose();
        }
    }
}