using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using HLView.Formats.Bsp;
using HLView.Formats.Environment;
using Veldrid;

namespace HLView.Graphics.Renderables
{
    public class BspEntityFaceGroupRenderable : BspFaceGroupRenderable
    {
        public Vector4 Colour { get; set; }

        public BspEntityFaceGroupRenderable(BspFile bsp, Environment environment, int mipTexture, IEnumerable<Face> faces) : base(bsp, environment, mipTexture, faces)
        {
        }

        protected override Vector4 GetColour()
        {
            return Colour;
        }

        public override void Render(SceneContext sc, CommandList cl, IRenderContext rc)
        {
            //
        }

        public override void RenderAlpha(SceneContext sc, CommandList cl, IRenderContext rc,
            Vector3 cameraLocation)
        {
            RenderLists(sc, cl);
        }
    }
}