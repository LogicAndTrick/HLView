using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HLView.Formats.Bsp;
using Veldrid;

namespace HLView.Graphics.Renderables
{
    public class BspRenderable : IRenderable
    {
        private readonly BspFile _bsp;
        private readonly List<IRenderable> _children;

        public BspRenderable(BspFile bsp)
        {
            _bsp = bsp;
            _children = new List<IRenderable>();

            foreach (var group in _bsp.Faces.GroupBy(x => _bsp.TextureInfos[x.TextureInfo].MipTexture))
            {
                _children.Add(new BspFaceGroupRenderable(_bsp, group.Key, group));
            }
        }

        public void Update(long milliseconds)
        {
            foreach (var child in _children)
            {
                child.Update(milliseconds);
            }
        }

        public void CreateResources(GraphicsDevice gd)
        {
            foreach (var child in _children)
            {
                child.CreateResources(gd);
            }
        }

        public void Render(GraphicsDevice gd, CommandList cl, SceneContext sc)
        {
            foreach (var child in _children)
            {
                child.Render(gd, cl, sc);
            }
        }

        public void DisposeResources(GraphicsDevice gd)
        {
            foreach (var child in _children)
            {
                child.DisposeResources(gd);
            }

            _children.Clear();
        }
    }
}
