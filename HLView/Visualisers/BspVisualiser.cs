using System.IO;
using System.Windows.Forms;
using HLView.Formats.Bsp;
using HLView.Graphics;
using HLView.Graphics.Renderables;
using Veldrid;
using Environment = HLView.Formats.Environment.Environment;

namespace HLView.Visualisers
{
    public class BspVisualiser : IVisualiser
    {
        private readonly Panel _panel;
        private GraphicsDevice _graphicsDevice;
        private VeldridControl _view;
        private SceneContext _sc;
        private Scene _scene;

        public Control Container => _panel;
        
        public BspVisualiser()
        {
            _panel = new Panel();
        }

        public bool Supports(string path)
        {
            return Path.GetExtension(path) == ".bsp";
        }

        public void Open(Environment environment, string path)
        {
            var options = new GraphicsDeviceOptions()
            {
                HasMainSwapchain = false,
                ResourceBindingModel = ResourceBindingModel.Improved,
                SwapchainDepthFormat = PixelFormat.R32_Float,
            };

            //_graphicsDevice = GraphicsDevice.CreateVulkan(options);
            _graphicsDevice = GraphicsDevice.CreateD3D11(options);

            _view = new VeldridControl(_graphicsDevice, options)
            {
                Dock = DockStyle.Fill
            };
            _panel.Controls.Add(_view);

            _sc = new SceneContext(_graphicsDevice);
            _sc.AddRenderTarget(_view);

            _scene = new Scene();

            BspFile bsp;
            using (var stream = File.OpenRead(path))
            {
                bsp = new BspFile(stream);
            }

            _scene.AddRenderable(new BspRenderable(bsp, environment));
            _sc.Scene = _scene;
            _sc.Start();
        }

        public void Close()
        {
            _sc.RemoveRenderTarget(_view);

            _sc.Stop();
            _sc.Dispose();

            _panel.Controls.Clear();
            _view.Dispose();

            _graphicsDevice.Dispose();

            _scene = null;
            _sc = null;
            _view = null;
            _graphicsDevice = null;
        }
    }
}
