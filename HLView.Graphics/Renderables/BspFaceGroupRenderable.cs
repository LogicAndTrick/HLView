using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using HLView.Formats.Bsp;
using HLView.Graphics.Primitives;
using Veldrid;
using Texture = HLView.Formats.Bsp.Texture;

namespace HLView.Graphics.Renderables
{
    public class BspFaceGroupRenderable : IRenderable
    {
        private readonly BspFile _bsp;
        private readonly Texture _texture;
        private readonly List<Face> _faces;

        private DeviceBuffer _vertexBuffer;
        private DeviceBuffer _indexBuffer;
        private uint _numIndices;

        public BspFaceGroupRenderable(BspFile bsp, int mipTexture, IEnumerable<Face> faces)
        {
            _bsp = bsp;
            _texture = bsp.Textures[mipTexture];
            _faces = faces.ToList();
        }

        public void Update(long milliseconds)
        {
            //
        }

        public void CreateResources(GraphicsDevice gd)
        {
            var verts = new List<Vertex>();
            var indices = new List<uint>();

            foreach (var face in _faces)
            {
                var textureInfo = _bsp.TextureInfos[face.TextureInfo];
                var plane = _bsp.Planes[face.Plane];

                var faceVerts = new List<Vector3>();
                for (var i = 0; i < face.NumEdges; i++)
                {
                    var ei = _bsp.SurfaceEdges[face.FirstEdge + i];
                    var edge = _bsp.Edges[Math.Abs(ei)];
                    var vtx = _bsp.Vertices[ei > 0 ? edge.Start : edge.End];
                    faceVerts.Add(vtx);
                }

                var start = (uint) verts.Count;

                // Triangulate the face
                for (uint i = 1; i < faceVerts.Count - 1; i++)
                {
                    indices.Add(start);
                    indices.Add(start + i);
                    indices.Add(start + i + 1);
                }

                foreach (var point in faceVerts)
                {
                    var sn = new Vector3(textureInfo.S.X, textureInfo.S.Y, textureInfo.S.Z);
                    var tn = new Vector3(textureInfo.T.X, textureInfo.T.Y, textureInfo.T.Z);
                    var s = Vector3.Dot(point, sn) + textureInfo.S.W;
                    s /= _texture.Width;

                    var t = Vector3.Dot(point, tn) + textureInfo.T.W;
                    t /= _texture.Height;

                    verts.Add(new Vertex
                    {
                        Position = point,
                        Normal = plane.Normal,
                        //Texture = new Vector2(s, t)
                    });
                }
            }

            _vertexBuffer = gd.ResourceFactory.CreateBuffer(new BufferDescription((uint)verts.Count * 32, BufferUsage.VertexBuffer));
            _indexBuffer = gd.ResourceFactory.CreateBuffer(new BufferDescription((uint)indices.Count * sizeof(uint), BufferUsage.IndexBuffer));

            gd.UpdateBuffer(_vertexBuffer, 0, verts.ToArray());
            gd.UpdateBuffer(_indexBuffer, 0, indices.ToArray());

            _numIndices = (uint) indices.Count;
        }

        public void Render(GraphicsDevice gd, CommandList cl, SceneContext sc)
        {
            cl.SetVertexBuffer(0, _vertexBuffer);
            cl.SetIndexBuffer(_indexBuffer, IndexFormat.UInt32);
            cl.DrawIndexed(_numIndices, 1, 0, 0, 0);
        }

        public void DisposeResources(GraphicsDevice gd)
        {
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
        }
    }
}