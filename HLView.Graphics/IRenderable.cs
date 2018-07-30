using System;
using System.Numerics;
using Veldrid;

namespace HLView.Graphics
{
    public interface IRenderable : IUpdateable
    {
        float DistanceFrom(Vector3 location);
        void CreateResources(SceneContext sc);
        void Render(SceneContext sc, CommandList cl);
        void RenderAlpha(SceneContext sc, CommandList cl, Vector3 cameraLocation);
        void DisposeResources(SceneContext sc);
    }
}