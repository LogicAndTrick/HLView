using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using HLView.Formats.Bsp;
using HLView.Graphics.Primitives;
using Veldrid;
using Veldrid.Utilities;
using Environment = HLView.Formats.Environment.Environment;
using Texture = HLView.Formats.Wad.Texture;

namespace HLView.Graphics.Renderables
{
    public abstract class BspFaceGroupRenderable : IRenderable
    {
        protected BspFile Bsp { get; }
        protected Environment Environment { get; }

        private BoundingBox _bounding;
        private List<ResourceSet> _textureResources;

        private DeviceBuffer _vertexBuffer;
        private DeviceBuffer _indexBuffer;
        private uint _numIndices;

        public Vector3 Origin { get; set; } = Vector3.Zero;

        private readonly Texture _texture;
        private readonly List<Face> _faces;

        private long _lastFrameMillis;
        private int _currentTextureIndex;

        public int RenderPass => 10;

        protected BspFaceGroupRenderable(BspFile bsp, Environment environment, int mipTexture, IEnumerable<Face> faces)
        {
            Bsp = bsp;
            Environment = environment;
            _texture = bsp.Textures[mipTexture];
            _faces = faces.ToList();
            _textureResources = new List<ResourceSet>();
        }
        
        protected abstract Vector4 GetColour();
        public abstract void Render(SceneContext sc, CommandList cl, IRenderContext rc);
        public abstract void RenderAlpha(SceneContext sc, CommandList cl, IRenderContext rc,
            Vector3 cameraLocation);

        public float DistanceFrom(Vector3 location)
        {
            return (location - _bounding.GetCenter()).Length();
        }

        public void Update(long milliseconds)
        {
            var diff = milliseconds - _lastFrameMillis;
            if (diff < 100 || _textureResources.Count <= 1) return;

            var skip = (int) (diff / 100);
            _currentTextureIndex = (_currentTextureIndex + skip) % _textureResources.Count;
            _lastFrameMillis = milliseconds;
        }

        private void LoadTextures(SceneContext sc, Bitmap lightmap)
        {
            var textures = new List<Veldrid.Texture>();

            var tex = _texture.NumMips > 1 ? _texture : Environment.Wads.Get(_texture.Name);

            _currentTextureIndex = 0;
            if (tex != null)
            {
                // Try and load animated textures
                if (tex.Value.Name.Length > 2 && tex.Value.Name[0] == '+')
                {
                    // Animated textures can be 0-9 or A-J
                    var c = tex.Value.Name[1] >= '0' && tex.Value.Name[1] <= '9' ? '0'
                             : tex.Value.Name[1] >= 'A' && tex.Value.Name[1] <= 'J' ? 'A'
                             : '\0';
                    if (c == '\0')
                    {
                        // + texture doesn't follow proper naming standards
                        textures.Add(sc.ResourceCache.GetTexture2D(tex.Value));
                    }
                    else
                    {
                        // Animated texture, load all the variants
                        var name = tex.Value.Name.Substring(2);
                        for (var i = c; i < c + 10; i++)
                        {
                            var frame = "+" + i + name;
                            if (string.Equals(frame, _texture.Name, StringComparison.InvariantCultureIgnoreCase)) _currentTextureIndex = i - c;
                            tex = Environment.Wads.Get(frame);
                            if (tex.HasValue) textures.Add(sc.ResourceCache.GetTexture2D(tex.Value));
                        }
                    }
                }
                else
                {
                    textures.Add(sc.ResourceCache.GetTexture2D(tex.Value));
                }
            }
            else
            {
                textures.Add(sc.ResourceCache.GetPinkTexture());
            }

            var lightmapTexture = sc.ResourceCache.GetTexture2D(lightmap);
            var lightmapTextureView = sc.ResourceCache.GetTextureView(lightmapTexture);

            foreach (var t in textures)
            {
                var tv = sc.ResourceCache.GetTextureView(t);
                _textureResources.Add(sc.ResourceCache.GetTextureResourceSet(tv, lightmapTextureView));
            }
        }

        private class TempVertex
        {
            public Vector3 Position;
            public Vector3 Normal;
            public Vector4 Colour;
            public Vector2 Texture;
            public Vector2 Lightmap;
        }

        private class LightmapBuilder : IDisposable
        {
            private Bitmap _bitmap;
            private BitmapData _lock;
            private int _currentX;
            private int _currentY;
            private int _currentRowHeight;
            private List<(TempVertex, Rectangle)> _values;

            public LightmapBuilder(int initialWidth = 256, int initialHeight = 32)
            {
                _bitmap = new Bitmap(initialWidth, initialHeight);
                //using (var g = System.Drawing.Graphics.FromImage(_bitmap)) g.FillRectangle(Brushes.White, 0, 0, _bitmap.Width, _bitmap.Height);
                _lock = _bitmap.LockBits(new Rectangle(0, 0, _bitmap.Width, _bitmap.Height), ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                _currentX = 0;
                _currentY = 0;
                _currentRowHeight = 2;
                _values = new List<(TempVertex, Rectangle)>();

                // (0, 0) is fullbright
                Allocate(1, 1, new[] {byte.MaxValue, byte.MaxValue, byte.MaxValue}, 0);
            }

            public void AddValue(TempVertex vertex, Rectangle rect)
            {
                _values.Add((vertex, rect));
            }

            public void ComputeLightmapValues()
            {
                float w = _bitmap.Width;
                float h = _bitmap.Height;
                foreach (var (vertex, rect) in _values)
                {
                    // Lightmap X/Y are currently mapped to local lightmap space, need to reamap to texture space
                    vertex.Lightmap.X = (rect.X + 0.5f) / w + (vertex.Lightmap.X * (rect.Width - 1)) / w;
                    vertex.Lightmap.Y = (rect.Y + 0.5f) / h + (vertex.Lightmap.Y * (rect.Height - 1)) / h;
                }
            }

            public Rectangle Allocate(int width, int height, byte[] data, int index)
            {
                if (_currentX + width > _bitmap.Width) NewRow();
                if (_currentY + height > _bitmap.Height) Expand();

                for (var i = 0; i < height; i++)
                {
                    // data is in RGB format, but the bitmap wants BGR, so we need to reverse the order
                    var bytes = new byte[width * 3];
                    var st = width * i * 3 + index;
                    for (var j = 0; j < width * 3; j += 3)
                    {
                        bytes[j + 0] = data[st + j + 2];
                        bytes[j + 1] = data[st + j + 1];
                        bytes[j + 2] = data[st + j + 0];
                    }
                    var start = new IntPtr(_lock.Scan0.ToInt64() + ((_currentY + i) * _lock.Stride) + _currentX * 3);
                    Marshal.Copy(bytes, 0, start, bytes.Length);
                }

                var x = _currentX;
                var y = _currentY;

                _currentX += width + 2;
                _currentRowHeight = Math.Max(_currentRowHeight, height + 2);

                return new Rectangle(x, y, width, height);
            }

            private void NewRow()
            {
                _currentX = 0;
                _currentY += _currentRowHeight;
                _currentRowHeight = 2;
            }

            [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
            static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

            private void Expand()
            {
                var newBmp = new Bitmap(_bitmap.Width, _bitmap.Height * 2);
                //using (var g = System.Drawing.Graphics.FromImage(newBmp)) g.FillRectangle(Brushes.White, 0, 0, newBmp.Width, newBmp.Height);
                var newLock = newBmp.LockBits(new Rectangle(0, 0, newBmp.Width, newBmp.Height), ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                CopyMemory(newLock.Scan0, _lock.Scan0, (uint) (_lock.Stride * _lock.Height));

                _bitmap.UnlockBits(_lock);
                _bitmap.Dispose();

                _bitmap = newBmp;
                _lock = newLock;
            }

            public Bitmap GetBitmap()
            {
                if (_lock != null) _bitmap?.UnlockBits(_lock);
                _lock = null;
                return _bitmap;
            }

            public void Dispose()
            {
                if (_lock != null) _bitmap?.UnlockBits(_lock);
                _bitmap?.Dispose();
            }
        }

        public void CreateResources(SceneContext sc)
        {
            var lightmap = new LightmapBuilder();
            
            // Lazy coding, use a vertex class so I don't have to worry about pass-by-value
            var verts = new List<TempVertex>();
            var indices = new List<uint>();

            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            foreach (var face in _faces)
            {
                var textureInfo = Bsp.TextureInfos[face.TextureInfo];
                var plane = Bsp.Planes[face.Plane];

                var faceVerts = new List<Vector3>();
                for (var i = 0; i < face.NumEdges; i++)
                {
                    var ei = Bsp.SurfaceEdges[face.FirstEdge + i];
                    var edge = Bsp.Edges[Math.Abs(ei)];
                    var vtx = Bsp.Vertices[ei > 0 ? edge.Start : edge.End];
                    faceVerts.Add(vtx);

                    min = Vector3.Min(min, vtx);
                    max = Vector3.Max(max, vtx);
                }

                var start = (uint) verts.Count;

                // Triangulate the face
                for (uint i = 1; i < faceVerts.Count - 1; i++)
                {
                    indices.Add(start);
                    indices.Add(start + i);
                    indices.Add(start + i + 1);
                }

                var points = new List<TempVertex>();
                foreach (var point in faceVerts)
                {
                    var sn = new Vector3(textureInfo.S.X, textureInfo.S.Y, textureInfo.S.Z);
                    var u = Vector3.Dot(point, sn) + textureInfo.S.W;

                    var tn = new Vector3(textureInfo.T.X, textureInfo.T.Y, textureInfo.T.Z);
                    var v = Vector3.Dot(point, tn) + textureInfo.T.W;
                    
                    // Don't divide by width/height yet, we want u/v value to calculate surface extents
                    points.Add(new TempVertex
                    {
                        Position = point + Origin,
                        Normal = plane.Normal,
                        Colour = GetColour(),
                        Texture = new Vector2(u, v)
                    });
                }

                var minu = points.Min(x => x.Texture.X);
                var maxu = points.Max(x => x.Texture.X);
                var minv = points.Min(x => x.Texture.Y);
                var maxv = points.Max(x => x.Texture.Y);
                var extentH = maxu - minu;
                var extentV = maxv - minv;

                var mapWidth = (int) Math.Ceiling(maxu / 16) - (int) Math.Floor(minu / 16) + 1;
                var mapHeight = (int) Math.Ceiling(maxv / 16) - (int) Math.Floor(minv / 16) + 1;

                if (face.LightmapOffset < 0)
                {
                    // fullbright
                    points.ForEach(x => x.Lightmap = Vector2.Zero);
                }
                else
                {
                    var rect = lightmap.Allocate(mapWidth, mapHeight, Bsp.Lightmap, face.LightmapOffset);
                    for (var i = 0; i < points.Count; i++)
                    {
                        var point = points[i];
                        point.Lightmap.X = (point.Texture.X - minu) / extentH;
                        point.Lightmap.Y = (point.Texture.Y - minv) / extentV;
                        lightmap.AddValue(point, rect);
                    }
                }

                for (var i = 0; i < points.Count; i++)
                {
                    var point = points[i];
                    point.Texture.X /= _texture.Width;
                    point.Texture.Y /= _texture.Height;
                    verts.Add(point);
                }
            }

            lightmap.ComputeLightmapValues();

            var lightmapImage = lightmap.GetBitmap();
            LoadTextures(sc, lightmapImage);
            lightmap.Dispose();

            _bounding.Min = min;
            _bounding.Max = max;

            var newVerts = verts.Select(x => new Vertex
            {
                Position = x.Position,
                Normal = x.Normal,
                Colour = x.Colour,
                Texture = x.Texture,
                Lightmap = x.Lightmap
            }).ToList();

            _vertexBuffer = sc.Device.ResourceFactory.CreateBuffer(new BufferDescription((uint)newVerts.Count * Vertex.SizeInBytes, BufferUsage.VertexBuffer));
            _indexBuffer = sc.Device.ResourceFactory.CreateBuffer(new BufferDescription((uint)indices.Count * sizeof(uint), BufferUsage.IndexBuffer));

            sc.Device.UpdateBuffer(_vertexBuffer, 0, newVerts.ToArray());
            sc.Device.UpdateBuffer(_indexBuffer, 0, indices.ToArray());

            _numIndices = (uint) indices.Count;
        }

        protected void RenderLists(SceneContext sc, CommandList cl)
        {
            if (string.Equals(_texture.Name, "sky", StringComparison.InvariantCultureIgnoreCase)) return; // temp

            cl.SetVertexBuffer(0, _vertexBuffer);
            cl.SetIndexBuffer(_indexBuffer, IndexFormat.UInt32);
            cl.SetGraphicsResourceSet(1, _textureResources[_currentTextureIndex]);
            cl.DrawIndexed(_numIndices, 1, 0, 0, 0);
        }

        public void DisposeResources(SceneContext sc)
        {
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
        }
    }
}