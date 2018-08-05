using Veldrid;

namespace HLView.Graphics.Pipelines
{
    public interface IRenderPipeline
    {
        string Name { get; }
        int Order { get; }
        void CreateResources(SceneContext sc);
        void SetPipeline(CommandList cl, SceneContext sc, IRenderContext context);
        void DisposeResources(SceneContext sc);
    }
}