using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using HLView.Graphics.Primitives;
using Veldrid;

namespace HLView.Graphics
{
    internal class RenderTarget : IDisposable, IRenderContext
    {
        private readonly SceneContext _self;
        private readonly CommandList _commandList;
        private readonly DeviceBuffer _projectionBuffer;
        private readonly ResourceSet _projectionResourceSet;
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
                
            _projectionBuffer = _self.Device.ResourceFactory.CreateBuffer(
                new BufferDescription((uint) Unsafe.SizeOf<UniformProjection>(), BufferUsage.UniformBuffer)
            );

            _projectionResourceSet = _self.ResourceCache.GetResourceSet(
                new ResourceSetDescription(_self.ResourceCache.ProjectionLayout, _projectionBuffer)
            );
        }

        private void OnResize(object sender, EventArgs e)
        {
            _resizeRequired = true;
        }

        public void Update(long milliseconds)
        {
            Target.Camera.Update(milliseconds);
        }

        public void SetModelMatrix(Matrix4x4 model)
        {
            _commandList.UpdateBuffer(_projectionBuffer, 0, new UniformProjection
            {
                Model = model,
                View = Target.Camera.View,
                Projection = Target.Camera.Projection,
            });
        }

        public void ClearModelMatrix()
        {
            _commandList.UpdateBuffer(_projectionBuffer, 0, new UniformProjection
            {
                Model = Matrix4x4.Identity,
                View = Target.Camera.View,
                Projection = Target.Camera.Projection,
            });
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

            _self.Device.UpdateBuffer(_projectionBuffer, 0, new UniformProjection
            {
                Model = Matrix4x4.Identity,
                View = Target.Camera.View,
                Projection = Target.Camera.Projection,
            });

            _commandList.Begin();
            _commandList.SetFramebuffer(Target.Swapchain.Framebuffer);
            _commandList.ClearDepthStencil(1);
            _commandList.ClearColorTarget(0, RgbaFloat.Black);
            _commandList.SetPipeline(_self.Pipeline);
            
            _commandList.SetGraphicsResourceSet(0, _projectionResourceSet);

            scene?.Render(_commandList, _self, this);
            scene?.RenderAlpha(_commandList, _self, this, Target.Camera.Location);

            _commandList.End();

            _self.Device.SubmitCommands(_commandList);
            _self.Device.SwapBuffers(Target.Swapchain);
        }

        public void Dispose()
        {
            Target.Resize -= OnResize;
            _projectionBuffer.Dispose();
            _commandList.Dispose();
        }
    }
}