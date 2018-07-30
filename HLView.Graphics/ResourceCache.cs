using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Veldrid;
using PixelFormat = Veldrid.PixelFormat;

namespace HLView.Graphics
{
    public class ResourceCache : IDisposable
    {
        private readonly SceneContext _context;
        private readonly ResourceFactory _factory;
        private readonly GraphicsDevice _device;

        private readonly Dictionary<GraphicsPipelineDescription, Pipeline> _pipelines = new Dictionary<GraphicsPipelineDescription, Pipeline>();
        private readonly Dictionary<ResourceLayoutDescription, ResourceLayout> _layouts = new Dictionary<ResourceLayoutDescription, ResourceLayout>();
        private readonly Dictionary<(string, ShaderStages), Shader> _shaders = new Dictionary<(string, ShaderStages), Shader>();
        private readonly Dictionary<string, (Shader, Shader)> _shaderSets = new Dictionary<string, (Shader, Shader)>();
        private readonly Dictionary<string, Texture> _textures = new Dictionary<string, Texture>();
        private readonly Dictionary<Texture, TextureView> _textureViews = new Dictionary<Texture, TextureView>();
        private readonly Dictionary<ResourceSetDescription, ResourceSet> _resourceSets = new Dictionary<ResourceSetDescription, ResourceSet>();

        private Texture _pinkTex;

        public ResourceLayout ProjectionLayout { get; }
        public ResourceLayout TextureLayout { get; }
        public Sampler TextureSampler { get; set; }

        public ResourceCache(SceneContext context)
        {
            _context = context;
            _device = context.Device;
            _factory = context.Device.ResourceFactory;


            ProjectionLayout = _device.ResourceFactory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("Projection", ResourceKind.UniformBuffer, ShaderStages.Vertex)
                )
            );
            TextureLayout = _device.ResourceFactory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("uTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("uLightmap", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("uSampler", ResourceKind.Sampler, ShaderStages.Fragment)
                )
            );
            TextureSampler = _device.ResourceFactory.CreateSampler(SamplerDescription.Aniso4x);
        }
        
        public Pipeline GetPipeline(ref GraphicsPipelineDescription desc)
        {
            if (!_pipelines.TryGetValue(desc, out var p))
            {
                p = _factory.CreateGraphicsPipeline(ref desc);
                _pipelines.Add(desc, p);
            }

            return p;
        }

        public ResourceLayout GetResourceLayout(ResourceLayoutDescription desc)
        {
            if (!_layouts.TryGetValue(desc, out var p))
            {
                p = _factory.CreateResourceLayout(ref desc);
                _layouts.Add(desc, p);
            }

            return p;
        }

        public (Shader vs, Shader fs) GetShaders(string name)
        {
            if (!_shaderSets.TryGetValue(name, out var set))
            {
                set = ShaderHelper.LoadShaders(_device, name);
                _shaderSets.Add(name, set);
            }
        
            return set;
        }

        public Texture GetTexture2D(Bitmap bitmap)
        {
            var tt = _device.ResourceFactory.CreateTexture(TextureDescription.Texture2D((uint) bitmap.Width, (uint) bitmap.Height, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));

            var lb = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var bits = new byte[lb.Stride * lb.Height];
            Marshal.Copy(lb.Scan0, bits, 0, bits.Length);
            // Bits are in BGRA order, we want to flip them to RGBA
            for (var i = 0; i < lb.Stride * lb.Height; i += 4)
            {
                var b = bits[i];
                bits[i] = bits[i + 2];
                bits[i + 2] = b;
            }
            _device.UpdateTexture(tt, bits, 0, 0, 0, tt.Width, tt.Height, tt.Depth, 0, 0);
            bitmap.UnlockBits(lb);

            _textures.Add(Guid.NewGuid().ToString("N"), tt);

            return tt;
        }

        public Texture GetTexture2D(Formats.Wad.Texture tex)
        {
            var name = tex.Name;
            if (!_textures.TryGetValue(name, out Texture tt))
            {
                tt = _device.ResourceFactory.CreateTexture(TextureDescription.Texture2D(tex.Width, tex.Height, 4, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));
                uint w = tex.Width, h = tex.Height;
                for (uint i = 0; i < tex.NumMips; i++)
                {
                    _device.UpdateTexture(tt, GetImageData(tex, i), 0, 0, 0, w, h, tt.Depth, i, 0);
                    w /= 2;
                    h /= 2;
                }
                _textures.Add(name, tt);
            }
        
            return tt;
        }

        private byte[] GetImageData(Formats.Wad.Texture tex, uint mip)
        {
            var transparent = tex.Name.StartsWith("{");
            var d = new List<byte>();
            foreach (var idx in tex.MipData[mip])
            {
                var r = tex.Palette[idx * 3 + 0];
                var g = tex.Palette[idx * 3 + 1];
                var b = tex.Palette[idx * 3 + 2];
                var a = byte.MaxValue;
                if (transparent && idx == byte.MaxValue) r = g = b = a = 0;
                d.Add(r);
                d.Add(g);
                d.Add(b);
                d.Add(a);
            }

            return d.ToArray();
        }

        public TextureView GetTextureView(Texture texture)
        {
            if (!_textureViews.TryGetValue(texture, out var view))
            {
                view = _factory.CreateTextureView(texture);
                _textureViews.Add(texture, view);
            }

            return view;
        }

        public Texture GetPinkTexture()
        {
            if (_pinkTex == null)
            {
                var pink = RgbaByte.Pink;
                _pinkTex = _factory.CreateTexture(TextureDescription.Texture2D(1, 1, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));
                _device.UpdateTexture(_pinkTex, new byte[] { pink.R, pink.G, pink.B, pink.A }, 0, 0, 0, 1, 1, 1, 0, 0);
            }

            return _pinkTex;
        }

        public ResourceSet GetResourceSet(ResourceSetDescription description)
        {
            if (!_resourceSets.TryGetValue(description, out var ret))
            {
                ret = _factory.CreateResourceSet(ref description);
                _resourceSets.Add(description, ret);
            }

            return ret;
        }

        public ResourceSet GetTextureResourceSet(TextureView texture, TextureView lightmap)
        {
            var desc = new ResourceSetDescription(TextureLayout, texture, lightmap, TextureSampler);
            return GetResourceSet(desc);
        }

        public void Dispose()
        {
            foreach (var kvp in _pipelines)
            {
                kvp.Value.Dispose();
            }
            _pipelines.Clear();

            foreach (var kvp in _layouts)
            {
                kvp.Value.Dispose();
            }
            _layouts.Clear();

            foreach (var kvp in _shaders)
            {
                kvp.Value.Dispose();
            }
            _shaders.Clear();

            foreach (var kvp in _shaderSets)
            {
                kvp.Value.Item1.Dispose();
                kvp.Value.Item2.Dispose();
            }
            _shaderSets.Clear();

            foreach (var kvp in _textures)
            {
                kvp.Value.Dispose();
            }
            _textures.Clear();

            foreach (var kvp in _textureViews)
            {
                kvp.Value.Dispose();
            }
            _textureViews.Clear();

            _pinkTex?.Dispose();
            _pinkTex = null;

            foreach (var kvp in _resourceSets)
            {
                kvp.Value.Dispose();
            }
            _resourceSets.Clear();
        }
    }
}