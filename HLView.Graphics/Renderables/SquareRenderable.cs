using System.Numerics;
using HLView.Graphics.Primitives;
using Veldrid;

namespace HLView.Graphics.Renderables
{
    public class SquareRenderable : IRenderable
    {
        private DeviceBuffer _vertexBuffer;
        private DeviceBuffer _indexBuffer;

        public void CreateResources(GraphicsDevice gd)
        {
            var max = 0.75f;
            var min = -max;

            var verts = new[]
            {
                new Vertex { Position = new Vector3(min, max, max), Normal = Vector3.UnitZ },
                new Vertex { Position = new Vector3(max, max, max), Normal = Vector3.UnitZ },
                new Vertex { Position = new Vector3(max, min, max), Normal = Vector3.UnitZ },
                new Vertex { Position = new Vector3(min, min, max), Normal = Vector3.UnitZ },
            };
            ushort[] indices =
            {
                0, 1, 2, 0, 2, 3
            };

            _vertexBuffer = gd.ResourceFactory.CreateBuffer(new BufferDescription((uint)verts.Length * 32, BufferUsage.VertexBuffer));
            _indexBuffer = gd.ResourceFactory.CreateBuffer(new BufferDescription((uint)indices.Length * sizeof(ushort), BufferUsage.IndexBuffer));
            
            gd.UpdateBuffer(_vertexBuffer, 0, verts);
            gd.UpdateBuffer(_indexBuffer, 0, indices);
        }

        public void Update(long milliseconds)
        {
            // No need
        }

        public void Render(GraphicsDevice gd, CommandList cl, SceneContext sc)
        {
            cl.SetVertexBuffer(0, _vertexBuffer);
            cl.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
            cl.DrawIndexed(6, 1, 0, 0, 0);
        }

        public void DisposeResources(GraphicsDevice gd)
        {
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
        }
    }
}
