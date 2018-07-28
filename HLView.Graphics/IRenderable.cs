using System;
using System.Numerics;
using Veldrid;

namespace HLView.Graphics
{
    public interface IRenderable : IUpdateable
    {
        void CreateResources(GraphicsDevice gd, SceneContext sc);
        void Render(GraphicsDevice gd, CommandList cl, SceneContext sc);
        void DisposeResources(GraphicsDevice gd);
    }
}