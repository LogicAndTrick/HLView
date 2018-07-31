using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using HLView.Graphics.Primitives;
using Veldrid;
using Environment = HLView.Formats.Environment.Environment;
using ImageFormat = Pfim.ImageFormat;

namespace HLView.Graphics.Renderables
{
    public class SkyboxRenderable : IRenderable
    {
        private readonly Environment _env;
        private readonly string _skyboxName;

        private DeviceBuffer _vertexBuffer;
        private DeviceBuffer _indexBuffer;

        private ResourceSet[] _textureResources;

        public int RenderPass => 0;

        public SkyboxRenderable(Environment env, string skyboxName)
        {
            _env = env;
            _skyboxName = skyboxName;
        }

        public void Update(long milliseconds)
        {
            //
        }

        public float DistanceFrom(Vector3 location)
        {
            return float.MaxValue;
        }

        private Bitmap LoadTga(string path)
        {
            var image = Pfim.Pfim.FromFile(path);
            var fmt = image.Format == ImageFormat.Rgb24 ? System.Drawing.Imaging.PixelFormat.Format24bppRgb : System.Drawing.Imaging.PixelFormat.Format32bppArgb;

            var bmp = new Bitmap(image.Width, image.Height, fmt);
            var lb = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, fmt);
            Marshal.Copy(image.Data, 0, lb.Scan0, image.Stride * image.Height);
            bmp.UnlockBits(lb);
            return bmp;
        }

        private Bitmap LoadImage(string baseFolder, string side)
        {
            var f = Path.Combine(_env.BaseFolder, "gfx", "env", _skyboxName + side + ".bmp");
            if (File.Exists(f)) return new Bitmap(f);

            f = Path.Combine(_env.BaseFolder, "gfx", "env", _skyboxName + side + ".tga");
            if (File.Exists(f)) return LoadTga(f);

            f = Path.Combine(_env.BaseFolder, "gfx", "env", "desert" + side + ".bmp");
            if (File.Exists(f)) return new Bitmap(f);

            return new Bitmap(1, 1);
        }

        public void CreateResources(SceneContext sc)
        {
            var images = new []
            {
                LoadImage(_env.BaseFolder, "rt"),
                LoadImage(_env.BaseFolder, "bk"),
                LoadImage(_env.BaseFolder, "lf"),
                LoadImage(_env.BaseFolder, "ft"),
                LoadImage(_env.BaseFolder, "up"),
                LoadImage(_env.BaseFolder, "dn"),
            };

            var lightmapTexture = sc.ResourceCache.GetWhiteTexture();
            var lightmapTextureView = sc.ResourceCache.GetTextureView(lightmapTexture);

            var textureViews = images.Select(x =>
            {
                var t = sc.ResourceCache.GetTexture2D(x);
                var tv = sc.ResourceCache.GetTextureView(t);
                return tv;
            }).ToArray();

            foreach (var bitmap in images) bitmap.Dispose();

            const float d = 100;
            var points = new Vertex[]
            {
                // Right
                new Vertex {Position = new Vector3( d,  d,  d), Texture = new Vector2(0, 0)},
                new Vertex {Position = new Vector3( d,  d, -d), Texture = new Vector2(0, 1)},
                new Vertex {Position = new Vector3( d, -d, -d), Texture = new Vector2(1, 1)},
                new Vertex {Position = new Vector3( d, -d,  d), Texture = new Vector2(1, 0)},

                // Back
                new Vertex {Position = new Vector3(-d, d,  d), Texture = new Vector2(0, 0)},
                new Vertex {Position = new Vector3(-d, d, -d), Texture = new Vector2(0, 1)},
                new Vertex {Position = new Vector3( d, d, -d), Texture = new Vector2(1, 1)},
                new Vertex {Position = new Vector3( d, d,  d), Texture = new Vector2(1, 0)},
                
                // Left
                new Vertex {Position = new Vector3(-d, -d,  d), Texture = new Vector2(0, 0)},
                new Vertex {Position = new Vector3(-d, -d, -d), Texture = new Vector2(0, 1)},
                new Vertex {Position = new Vector3(-d,  d, -d), Texture = new Vector2(1, 1)},
                new Vertex {Position = new Vector3(-d,  d,  d), Texture = new Vector2(1, 0)},

                // Front
                new Vertex {Position = new Vector3( d, -d,  d), Texture = new Vector2(0, 0)},
                new Vertex {Position = new Vector3( d, -d, -d), Texture = new Vector2(0, 1)},
                new Vertex {Position = new Vector3(-d, -d, -d), Texture = new Vector2(1, 1)},
                new Vertex {Position = new Vector3(-d, -d,  d), Texture = new Vector2(1, 0)},

                // Up
                new Vertex {Position = new Vector3(-d,  d,  d), Texture = new Vector2(0, 0)},
                new Vertex {Position = new Vector3( d,  d,  d), Texture = new Vector2(0, 1)},
                new Vertex {Position = new Vector3( d, -d,  d), Texture = new Vector2(1, 1)},
                new Vertex {Position = new Vector3(-d, -d,  d), Texture = new Vector2(1, 0)},

                // Down
                new Vertex {Position = new Vector3( d,  d, -d), Texture = new Vector2(0, 0)},
                new Vertex {Position = new Vector3(-d,  d, -d), Texture = new Vector2(0, 1)},
                new Vertex {Position = new Vector3(-d, -d, -d), Texture = new Vector2(1, 1)},
                new Vertex {Position = new Vector3( d, -d, -d), Texture = new Vector2(1, 0)},
            };

            for (var i = 0; i < points.Length; i++)
            {
                points[i].Colour = Vector4.One;
            }

            var indices = new uint[]
            {
                0, 2, 1, 0, 3, 2,
            };

            _vertexBuffer = sc.Device.ResourceFactory.CreateBuffer(new BufferDescription((uint)points.Length * Vertex.SizeInBytes, BufferUsage.VertexBuffer));
            _indexBuffer = sc.Device.ResourceFactory.CreateBuffer(new BufferDescription((uint)indices.Length * sizeof(uint), BufferUsage.IndexBuffer));

            sc.Device.UpdateBuffer(_vertexBuffer, 0, points);
            sc.Device.UpdateBuffer(_indexBuffer, 0, indices);

            _textureResources = textureViews
                .Select(x => sc.ResourceCache.GetTextureResourceSet(x, lightmapTextureView))
                .ToArray();
        }

        public void Render(SceneContext sc, CommandList cl, IRenderContext rc)
        {
            // Move the camera to the origin
            rc.SetModelMatrix(Matrix4x4.CreateTranslation(rc.Camera.Location));

            // Render
            cl.SetVertexBuffer(0, _vertexBuffer);
            cl.SetIndexBuffer(_indexBuffer, IndexFormat.UInt32);

            for (int i = 0; i < 6; i++)
            {
                cl.SetGraphicsResourceSet(1, _textureResources[i]);
                cl.DrawIndexed(6, 1, 0, i * 4, 0);
            }

            // Clear the depth buffer - skybox is always as far away as possible
            cl.ClearDepthStencil(1);
            rc.ClearModelMatrix();
        }

        public void RenderAlpha(SceneContext sc, CommandList cl, IRenderContext rc, Vector3 cameraLocation)
        {
            //
        }

        public void DisposeResources(SceneContext sc)
        {
            _vertexBuffer?.Dispose();
            _indexBuffer?.Dispose();
        }
    }
}
