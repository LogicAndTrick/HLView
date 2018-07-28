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
        private readonly GraphicsDevice _graphicsDevice;
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

        private Pipeline _pipeline;
        private ResourceLayout _projectionLayout;
        private ResourceLayout _textureLayout;

        public SceneContext(GraphicsDevice graphicsDevice)
        {
            _graphicsDevice = graphicsDevice;
            ResourceCache = new ResourceCache();
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
                new VertexElementDescription("vTexture", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2)
            );

            var (vertex, fragment) = ResourceCache.GetShaders(_graphicsDevice, _graphicsDevice.ResourceFactory, "main");
            
            _projectionLayout = _graphicsDevice.ResourceFactory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("Projection", ResourceKind.UniformBuffer, ShaderStages.Vertex)
                )
            );
            _textureLayout = _graphicsDevice.ResourceFactory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("uTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment)
                )
            );

            var pDesc = new GraphicsPipelineDescription
            {
                BlendState = BlendStateDescription.SingleOverrideBlend,
                DepthStencilState = new DepthStencilStateDescription(true, true, ComparisonKind.LessEqual),
                RasterizerState = new RasterizerStateDescription(FaceCullMode.Back, PolygonFillMode.Solid, FrontFace.Clockwise, true, false),
                PrimitiveTopology = PrimitiveTopology.TriangleList,
                ResourceLayouts = new[] { _projectionLayout, _textureLayout },
                ShaderSet = new ShaderSetDescription(new[] { vertexLayout }, new[] { vertex, fragment }),
                Outputs = new OutputDescription
                {
                    ColorAttachments = new[] { new OutputAttachmentDescription(PixelFormat.B8_G8_R8_A8_UNorm) },
                    DepthAttachment = null,
                    SampleCount = TextureSampleCount.Count1
                }
            };

            _pipeline = _graphicsDevice.ResourceFactory.CreateGraphicsPipeline(pDesc);
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

                    // Update scene
                    Scene.Update(frame);
                    lock (_lock)
                    {
                        foreach (var rt in _renderTargets) rt.Update(frame);
                        foreach (var rt in _renderTargets) rt.Render(Scene);
                    }

                    _graphicsDevice.WaitForIdle();

                    // Render scene
                    // ??
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
        }

        private class RenderTarget : IDisposable
        {
            private readonly SceneContext _self;
            private CommandList _commandList;
            private DeviceBuffer _projectionBuffer;
            private ResourceSet _projectionResourceSet;
            private bool _resizeRequired;

            public IRenderTarget Target { get; set; }

            public RenderTarget(IRenderTarget target, SceneContext self)
            {
                _self = self;
                Target = target;

                target.Resize += OnResize;
                _resizeRequired = Target.Width != target.Swapchain.Framebuffer.Width || Target.Height != Target.Swapchain.Framebuffer.Height;

                _commandList = _self._graphicsDevice.ResourceFactory.CreateCommandList();

                
                
                _projectionBuffer = _self._graphicsDevice.ResourceFactory.CreateBuffer(
                    new BufferDescription((uint) Unsafe.SizeOf<UniformProjection>(), BufferUsage.UniformBuffer)
                );

                _projectionResourceSet = _self._graphicsDevice.ResourceFactory.CreateResourceSet(
                    new ResourceSetDescription(_self._projectionLayout, _projectionBuffer)
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
                    Target.Swapchain.Resize((uint) Target.Width, (uint) Target.Height);
                    Target.Camera.WindowResized(Target.Width, Target.Height);
                    _resizeRequired = false;
                }

                _self._graphicsDevice.UpdateBuffer(_projectionBuffer, 0, new UniformProjection
                {
                    Model = Matrix4x4.Identity,
                    View = Target.Camera.View,
                    Projection = Target.Camera.Projection,
                });

                _commandList.Begin();
                _commandList.SetFramebuffer(Target.Swapchain.Framebuffer);
                _commandList.ClearColorTarget(0, RgbaFloat.Black);
                _commandList.SetPipeline(_self._pipeline);
                _commandList.SetGraphicsResourceSet(0, _projectionResourceSet);

                scene?.Render(_self._graphicsDevice, _commandList, _self);

                _commandList.End();

                _self._graphicsDevice.SubmitCommands(_commandList);
                _self._graphicsDevice.SwapBuffers(Target.Swapchain);
            }

            public void Dispose()
            {
                Target.Resize -= OnResize;
                _commandList.Dispose();
            }
        }
    }
}