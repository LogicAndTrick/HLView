using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using HLView.Graphics.Renderables;
using Veldrid;

namespace HLView.Graphics
{
    public class Scene
    {
        private readonly List<IRenderable> _renderables;
        private readonly List<IRenderable> _newRenderables;
        private readonly List<IUpdateable> _updateables;
        
        public Scene()
        {
            _updateables = new List<IUpdateable>();
            _renderables = new List<IRenderable>();
            _newRenderables = new List<IRenderable>();
        }

        public void AddRenderable(IRenderable renderable)
        {
            _renderables.Add(renderable);
            _newRenderables.Add(renderable);
        }

        public void AddRenderableSource(IRenderableSource source)
        {
            var rend = source.ToList();
            _renderables.AddRange(rend);
            _newRenderables.AddRange(rend);
        }

        public void AddUpdateable(IUpdateable updateable)
        {
            _updateables.Add(updateable);
        }

        public void Update(long milliseconds)
        {
            foreach (var updateable in _updateables) updateable.Update(milliseconds);
            foreach (var renderable in _renderables) renderable.Update(milliseconds);
        }

        public void Render(CommandList cl, SceneContext sc, IRenderContext rc)
        {
            foreach (var r in _newRenderables)
            {
                r.CreateResources(sc);
            }
            _newRenderables.Clear();

            foreach (var rp in sc.Pipelines.OrderBy(x => x.Order))
            {
                rp.SetPipeline(cl, sc, rc);
                foreach (var renderable in _renderables.Where(x => x.Pipeline == rp.Name).OrderBy(x => x.Order))
                {
                    renderable.Render(sc, cl, rc);
                }
            }
        }

        public void RenderAlpha(CommandList cl, SceneContext sc, IRenderContext rc, Vector3 cameraLocation)
        {
            foreach (var rp in sc.Pipelines.OrderBy(x => x.Order))
            {
                rp.SetPipeline(cl, sc, rc);
                foreach (var renderable in _renderables.Where(x => x.Pipeline == rp.Name).OrderBy(x => x.Order))
                {
                    renderable.RenderAlpha(sc, cl, rc, cameraLocation);
                }
            }
        }

        public void DisposeResources(SceneContext sc)
        {
            foreach (var r in _renderables) r.DisposeResources(sc);
        }
    }
}