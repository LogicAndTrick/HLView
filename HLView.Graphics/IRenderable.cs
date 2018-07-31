using System;
using System.Numerics;
using Veldrid;

namespace HLView.Graphics
{
    public interface IRenderable : IUpdateable
    {
        int RenderPass { get; }
        float DistanceFrom(Vector3 location);
        void CreateResources(SceneContext sc);
        void Render(SceneContext sc, CommandList cl, IRenderContext rc);
        void RenderAlpha(SceneContext sc, CommandList cl, IRenderContext rc, Vector3 cameraLocation);
        void DisposeResources(SceneContext sc);
    }
}