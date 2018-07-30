using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;
using HLView.Graphics.Primitives;
using Veldrid;

namespace HLView.Graphics.Primitives
{
}

namespace HLView.Graphics
{
    public class SceneContext : IDisposable
    {
        public GraphicsDevice Device { get; }
        public ResourceCache ResourceCache { get; }

        public Scene Scene
        {
            get => _scene;
            set
            {
                _scene = value;
                // render targets need to be rebuilt
            }
        }

        private readonly Thread _renderThread;
        private readonly CancellationTokenSource _token;

        private readonly List<RenderTarget> _renderTargets;
        private readonly Stopwatch _timer;
        private Scene _scene;
        private readonly object _lock = new object();

        public Pipeline Pipeline { get; private set; }

        public SceneContext(GraphicsDevice graphicsDevice)
        {
            Device = graphicsDevice;
            ResourceCache = new ResourceCache(this);
            _token = new CancellationTokenSource();
            _renderThread = new Thread(Loop);
            _renderTargets = new List<RenderTarget>();
            _timer = new Stopwatch();

            InitialisePipeline();
        }

        private void InitialisePipeline()
        {
            var vertexLayout = new VertexLayoutDescription(
                new VertexElementDescription("vPosition", VertexElementSemantic.Position, VertexElementFormat.Float3),
                new VertexElementDescription("vNormal", VertexElementSemantic.Normal, VertexElementFormat.Float3),
                new VertexElementDescription("vColour", VertexElementSemantic.Color, VertexElementFormat.Float4),
                new VertexElementDescription("vTexture", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                new VertexElementDescription("vLightmap", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2)
            );

            var (vertex, fragment) = ResourceCache.GetShaders("main");

            var pDesc = new GraphicsPipelineDescription
            {
                BlendState = BlendStateDescription.SingleAlphaBlend,
                DepthStencilState = DepthStencilStateDescription.DepthOnlyLessEqual,
                RasterizerState = new RasterizerStateDescription(FaceCullMode.Back, PolygonFillMode.Solid, FrontFace.Clockwise, true, false),
                PrimitiveTopology = PrimitiveTopology.TriangleList,
                ResourceLayouts = new[] { ResourceCache.ProjectionLayout, ResourceCache.TextureLayout },
                ShaderSet = new ShaderSetDescription(new[] { vertexLayout }, new[] { vertex, fragment }),
                Outputs = new OutputDescription
                {
                    ColorAttachments = new[] { new OutputAttachmentDescription(PixelFormat.B8_G8_R8_A8_UNorm) },
                    DepthAttachment = new OutputAttachmentDescription(PixelFormat.R32_Float),
                    SampleCount = TextureSampleCount.Count1
                }
            };

            Pipeline = ResourceCache.GetPipeline(ref pDesc);
        }

        public void AddRenderTarget(IRenderTarget target)
        {
            var rt = new RenderTarget(target, this);
            lock (_lock)
            {
                _renderTargets.Add(rt);
            }
        }

        public void RemoveRenderTarget(IRenderTarget target)
        {
            lock (_lock)
            {
                var rem = _renderTargets.Where(x => x.Target == target).ToList();
                foreach (var t in rem)
                {
                    _renderTargets.Remove(t);
                    t.Dispose();
                }
            }
        }

        public void Start()
        {
            _timer.Start();
            _renderThread.Start(_token.Token);
        }

        public void Stop()
        {
            _token.Cancel();
            _timer.Stop();
        }

        private void Loop(object o)
        {
            var token = (CancellationToken) o;
            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (Scene == null) break;

                    var frame = _timer.ElapsedMilliseconds;
                    
                    Scene.Update(frame);
                    lock (_lock)
                    {
                        foreach (var rt in _renderTargets) rt.Update(frame);
                        foreach (var rt in _renderTargets) rt.Render(Scene);
                    }

                    Device.WaitForIdle();
                }
            }
            catch (ThreadInterruptedException)
            {
                // exit
            }
            catch (ThreadAbortException)
            {
                // exit
            }
        }

        public void Dispose()
        {
            _token.Cancel();
            _renderThread.Join(100);
            _renderThread.Abort();
            ResourceCache.Dispose();
        }

        private class RenderTarget : IDisposable
        {
            private readonly SceneContext _self;
            private readonly CommandList _commandList;
            private readonly DeviceBuffer _projectionBuffer;
            private readonly ResourceSet _projectionResourceSet;
            private bool _resizeRequired;

            public IRenderTarget Target { get; set; }

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

                scene?.Render(_commandList, _self);
                scene?.RenderAlpha(_commandList, _self, Target.Camera.Location);

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
}