using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using HLView.Formats.Bsp;
using HLView.Formats.Environment;
using Veldrid;

namespace HLView.Graphics.Renderables
{
    public class BspStaticFaceGroupRenderable : BspFaceGroupRenderable
    {

        public BspStaticFaceGroupRenderable(BspFile bsp, Environment environment, int mipTexture, IEnumerable<Face> faces) : base(bsp, environment, mipTexture, faces)
        {
        }

        protected override Vector4 GetColour()
        {
            return Vector4.One;
        }

        public override void Render(SceneContext sc, CommandList cl, IRenderContext rc)
        {
            RenderLists(sc, cl);
        }

        public override void RenderAlpha(SceneContext sc, CommandList cl, IRenderContext rc, Vector3 cameraLocation)
        {
            // 
        }
    }
}