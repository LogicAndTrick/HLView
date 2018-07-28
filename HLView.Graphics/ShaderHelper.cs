using System;
using System.IO;
using Veldrid;

namespace HLView.Graphics
{
    public static class ShaderHelper
    {
        public static (Shader vs, Shader fs) LoadShaders(GraphicsDevice gd, string name)
        {
            var factory = gd.ResourceFactory;
            byte[] vertexShader;
            byte[] fragmentShader;
            switch (gd.BackendType)
            {
                case GraphicsBackend.Direct3D11:
                    (vertexShader, fragmentShader) = LoadBytes(name, ".vert.hlsl.bytes", ".frag.hlsl.bytes");
                    break;
                case GraphicsBackend.Vulkan:
                    (vertexShader, fragmentShader) = LoadBytes(name, ".vk.vert.spv", ".vk.frag.spv");
                    break;
                case GraphicsBackend.OpenGL:
                case GraphicsBackend.Metal:
                case GraphicsBackend.OpenGLES:
                    throw new NotSupportedException($"{gd.BackendType} backend is not supported.");
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return (
                factory.CreateShader(new ShaderDescription(ShaderStages.Vertex, vertexShader, "main")),
                factory.CreateShader(new ShaderDescription(ShaderStages.Fragment, fragmentShader, "main"))
            );
        }

        private static (byte[], byte[]) LoadBytes(string name, string vertexExtension, string fragmentExtension)
        {
            var vp = GetPath(name + vertexExtension);
            var fp = GetPath(name + fragmentExtension);
            return (
                File.ReadAllBytes(vp),
                File.ReadAllBytes(fp)
            );
        }

        private static string GetPath(string path)
        {
            // TODO
            return Path.Combine(@"D:\Github\HLView\HLView.Graphics\Shaders", path);
        }
    }
}