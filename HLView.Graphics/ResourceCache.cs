using System.Collections.Generic;
using Veldrid;

namespace HLView.Graphics
{
    public class ResourceCache
    {
        private readonly Dictionary<GraphicsPipelineDescription, Pipeline> _pipelines = new Dictionary<GraphicsPipelineDescription, Pipeline>();
        private readonly Dictionary<ResourceLayoutDescription, ResourceLayout> _layouts = new Dictionary<ResourceLayoutDescription, ResourceLayout>();
        private readonly Dictionary<(string, ShaderStages), Shader> _shaders = new Dictionary<(string, ShaderStages), Shader>();
        private readonly Dictionary<string, (Shader, Shader)> _shaderSets = new Dictionary<string, (Shader, Shader)>();
        private readonly Dictionary<string, Texture> _textures = new Dictionary<string, Texture>();
        private readonly Dictionary<Texture, TextureView> _textureViews = new Dictionary<Texture, TextureView>();
        private readonly Dictionary<ResourceSetDescription, ResourceSet> _resourceSets = new Dictionary<ResourceSetDescription, ResourceSet>();

        private Texture _pinkTex;

        public readonly ResourceLayoutDescription ProjViewLayoutDescription = new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("Projection", ResourceKind.UniformBuffer, ShaderStages.Vertex),
            new ResourceLayoutElementDescription("View", ResourceKind.UniformBuffer, ShaderStages.Vertex)
        );

        public Pipeline GetPipeline(ResourceFactory factory, ref GraphicsPipelineDescription desc)
        {
            if (!_pipelines.TryGetValue(desc, out var p))
            {
                p = factory.CreateGraphicsPipeline(ref desc);
                _pipelines.Add(desc, p);
            }

            return p;
        }

        public ResourceLayout GetResourceLayout(ResourceFactory factory, ResourceLayoutDescription desc)
        {
            if (!_layouts.TryGetValue(desc, out var p))
            {
                p = factory.CreateResourceLayout(ref desc);
                _layouts.Add(desc, p);
            }

            return p;
        }

        public (Shader vs, Shader fs) GetShaders(GraphicsDevice gd, ResourceFactory factory, string name)
        {
            if (!_shaderSets.TryGetValue(name, out var set))
            {
                set = ShaderHelper.LoadShaders(gd, name);
                _shaderSets.Add(name, set);
            }
        
            return set;
        }

        public void DestroyAllDeviceObjects()
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

        // internal Texture GetTexture2D(GraphicsDevice gd, ResourceFactory factory, ImageSharpTexture textureData)
        // {
        //     if (!_textures.TryGetValue(textureData, out Texture tex))
        //     {
        //         tex = textureData.CreateDeviceTexture(gd, factory);
        //         _textures.Add(textureData, tex);
        //     }
        // 
        //     return tex;
        // }

        internal TextureView GetTextureView(ResourceFactory factory, Texture texture)
        {
            if (!_textureViews.TryGetValue(texture, out var view))
            {
                view = factory.CreateTextureView(texture);
                _textureViews.Add(texture, view);
            }

            return view;
        }

        internal Texture GetPinkTexture(GraphicsDevice gd, ResourceFactory factory)
        {
            if (_pinkTex == null)
            {
                var pink = RgbaByte.Pink;
                _pinkTex = factory.CreateTexture(TextureDescription.Texture2D(1, 1, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));
                gd.UpdateTexture(_pinkTex, new byte[] { pink.R, pink.G, pink.B, pink.A }, 0, 0, 0, 1, 1, 1, 0, 0);
            }

            return _pinkTex;
        }

        internal ResourceSet GetResourceSet(ResourceFactory factory, ResourceSetDescription description)
        {
            if (!_resourceSets.TryGetValue(description, out var ret))
            {
                ret = factory.CreateResourceSet(ref description);
                _resourceSets.Add(description, ret);
            }

            return ret;
        }
    }
}