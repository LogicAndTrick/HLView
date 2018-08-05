using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using HLView.Graphics.Pipelines;
using Veldrid;

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

        public IReadOnlyCollection<IRenderPipeline> Pipelines => _pipelines;

        private readonly Thread _renderThread;
        private readonly CancellationTokenSource _token;

        private readonly List<RenderTarget> _renderTargets;
        private readonly Stopwatch _timer;
        private Scene _scene;
        private readonly object _lock = new object();

        private readonly List<IRenderPipeline> _pipelines;

        public SceneContext(GraphicsDevice graphicsDevice)
        {
            Device = graphicsDevice;
            ResourceCache = new ResourceCache(this);
            _token = new CancellationTokenSource();
            _renderThread = new Thread(Loop);
            _renderTargets = new List<RenderTarget>();
            _timer = new Stopwatch();

            _pipelines = new List<IRenderPipeline>();
            AddPipeline(new SkyboxRenderPipeline());
            AddPipeline(new LightmappedRenderPipeline());
            AddPipeline(new ModelRenderPipeline());
        }

        public void AddPipeline(IRenderPipeline pipeline)
        {
            pipeline.CreateResources(this);
            lock (_lock)
            {
                _pipelines.Add(pipeline);
            }
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
            _pipelines.ForEach(x => x.DisposeResources(this));

            _token.Cancel();
            _renderThread.Join(100);
            _renderThread.Abort();
            ResourceCache.Dispose();
        }
    }
}