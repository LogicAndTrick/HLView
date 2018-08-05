using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HLView.Formats.Bsp;
using Environment = HLView.Formats.Environment.Environment;

namespace HLView.Graphics.Renderables
{
    public class BspRenderable : IRenderableSource
    {
        private readonly BspFile _bsp;
        private readonly Environment _env;
        private readonly List<IRenderable> _children;

        public int Order => 1;
        public string Pipeline => "Lightmapped";

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
                    if (child >= 0)
                    {
                        nodes.Enqueue(_bsp.Nodes[child]);
                    }
                    else
                    {
                        var leaf = _bsp.Leaves[-1 - child];
                        if (leaf.Contents == Contents.Sky)
                        {
                            continue;
                        }
                        for (var ms = 0; ms < leaf.NumMarkSurfaces; ms++)
                        {
                            var faceidx = _bsp.MarkSurfaces[ms + leaf.FirstMarkSurface];
                            var face = _bsp.Faces[faceidx];
                            if (face.Styles[0] != byte.MaxValue) staticFaces.Add(face);
                        }

                    }
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

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<IRenderable> GetEnumerator()
        {
            return _children.GetEnumerator();
        }
    }
}
