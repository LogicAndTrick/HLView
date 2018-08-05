using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using HLView.Formats.Bsp;
using HLView.Graphics.Primitives;
using Veldrid;
using Environment = HLView.Formats.Environment.Environment;

namespace HLView.Graphics.Renderables
{
    public class BspEntityRenderable : IRenderable
    {
        private readonly BspFile _bsp;
        private readonly Environment _env;
        private readonly EntityData _entity;
        private readonly Model _model;
        private readonly List<IRenderable> _children;
        private Vector4 _colour;

        public int Order => 10;
        public string Pipeline => "Lightmapped";

        public BspEntityRenderable(BspFile bsp, Environment env, EntityData entity, Model model)
        {
            _bsp = bsp;
            _env = env;
            _entity = entity;
            _model = model;
            _children = new List<IRenderable>();

            _colour = GetColour();

            var origin = _model.Origin + entity.GetVector3("origin", Vector3.Zero);

            var entityFaces = new List<Face>();
            var nodes = new Queue<Node>();
            nodes.Enqueue(bsp.Nodes[_model.HeadNodes[0]]);
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
                            if (face.Styles[0] != byte.MaxValue) entityFaces.Add(face);
                        }

                    }
                }
            }

            foreach (var group in entityFaces.GroupBy(x => _bsp.TextureInfos[x.TextureInfo].MipTexture))
            {
                _children.Add(new BspEntityFaceGroupRenderable(_bsp, _env, group.Key, group)
                {
                    Origin = origin,
                    Colour = _colour
                });
            }

        }

        private Vector4 GetColour()
        {
            var mode = _entity.Get("rendermode", 0);
            var alpha = _entity.Get("renderamt", 0) / 255f;
            switch (mode)
            {
                case 1:
                    return new Vector4(_entity.GetVector3("rendercolor", Vector3.One), alpha);
                case 2:
                case 3:
                    return new Vector4(Vector3.One, alpha);
                case 5:
                    return new Vector4(Vector3.One, alpha); // Yeah, this is wrong. but whatever
            }

            return Vector4.One;
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
            foreach (var child in _children)
            {
                child.Render(sc, cl, rc);
            }
        }

        public void RenderAlpha(SceneContext sc, CommandList cl, IRenderContext rc, Vector3 cameraLocation)
        {
            foreach (var child in _children.OrderByDescending(x => x.DistanceFrom(cameraLocation)))
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