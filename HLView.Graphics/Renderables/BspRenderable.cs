using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using HLView.Formats.Bsp;
using Veldrid;
using Environment = HLView.Formats.Environment.Environment;

namespace HLView.Graphics.Renderables
{
    public class BspRenderable : IRenderable
    {
        private readonly BspFile _bsp;
        private readonly Environment _env;
        private readonly List<IRenderable> _children;

        public int RenderPass => 1;

        public BspRenderable(BspFile bsp, Environment env)
        {
            _bsp = bsp;
            _env = env;
            _children = new List<IRenderable>();

            var skybox = "desert";
            var worldspawn = _bsp.Entities.FirstOrDefault(x => x.ClassName == "worldspawn");
            if (worldspawn != null)
            {
                var wads = worldspawn.Get("wad", "");
                _env.LoadWads(wads.Split(';').Where(x => !String.IsNullOrWhiteSpace(x)).Select(Path.GetFileName));
                skybox = worldspawn.Get("skyname", skybox);
            }

            // Load the skybox
            _children.Add(new SkyboxRenderable(_env, skybox));

            // Collect the static faces in the BSP (no need for special entity treatment)
            var staticFaces = new List<Face>();
            var nodes = new Queue<Node>(_bsp.Nodes.Take(1));
            while (nodes.Any())
            {
                var node = nodes.Dequeue();
                foreach (var child in node.Children)
                {
                    if (child > 0) nodes.Enqueue(_bsp.Nodes[child]);
                    for (var i = 0; i < node.NumFaces; i++) staticFaces.Add(_bsp.Faces[node.FirstFace + i]);
                }
            }

            foreach (var group in staticFaces.GroupBy(x => _bsp.TextureInfos[x.TextureInfo].MipTexture))
            {
                _children.Add(new BspStaticFaceGroupRenderable(_bsp, _env, group.Key, group));
            }

            // Collect entity faces - these have special treatment
            foreach (var ent in _bsp.Entities)
            {
                if (ent.Model <= 0) continue;
                var model = _bsp.Models[ent.Model];
                _children.Add(new BspEntityRenderable(_bsp, _env, ent, model));
            }
        }

        public void Update(long milliseconds)
        {
            foreach (var child in _children)
            {
                child.Update(milliseconds);
            }
        }

        public float DistanceFrom(Vector3 location)
        {
            if (!_children.Any()) return 0;
            return _children.Select(x => x.DistanceFrom(location)).Max();
        }

        public void CreateResources(SceneContext sc)
        {
            foreach (var child in _children)
            {
                child.CreateResources(sc);
            }
        }

        public void Render(SceneContext sc, CommandList cl, IRenderContext rc)
        {
            foreach (var child in _children.OrderBy(x => x.RenderPass))
            {
                child.Render(sc, cl, rc);
            }
        }

        public void RenderAlpha(SceneContext sc, CommandList cl, IRenderContext rc, Vector3 cameraLocation)
        {
            foreach (var child in _children.OrderBy(x => x.RenderPass).ThenByDescending(x => x.DistanceFrom(cameraLocation)))
            {
                child.RenderAlpha(sc, cl, rc, cameraLocation);
            }
        }

        public void DisposeResources(SceneContext sc)
        {
            foreach (var child in _children)
            {
                child.DisposeResources(sc);
            }

            _children.Clear();
        }
    }
}
