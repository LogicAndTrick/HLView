using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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
                var lastFrame = _timer.ElapsedMilliseconds;
                while (!token.IsCancellationRequested)
                {
                    if (Scene == null) break;

                    var frame = _timer.ElapsedMilliseconds;
                    var diff = (frame - lastFrame);
                    if (diff < 16)
                    {
                        Thread.Sleep(1);
                        continue;
                    }
                    
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
            _scene.DisposeResources(this);
            _token.Cancel();
            _renderThread.Join(100);
            _renderThread.Abort();
            ResourceCache.Dispose();
        }
    }
}