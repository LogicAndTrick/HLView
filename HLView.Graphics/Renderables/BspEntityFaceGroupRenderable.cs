using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using HLView.Formats.Bsp;
using HLView.Formats.Environment;
using Veldrid;
using Texture = HLView.Formats.Bsp.Texture;

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

        public override void Render(SceneContext sc, CommandList cl)
        {
            //
        }

        public override void RenderAlpha(SceneContext sc, CommandList cl, Vector3 cameraLocation)
        {
            RenderLists(sc, cl);
        }
    }
}