using System;
using System.IO;
using System.Numerics;
using System.Windows.Forms;
using HLView.Formats.Bsp;
using HLView.Formats.Mdl;
using HLView.Graphics;
using HLView.Graphics.Cameras;
using HLView.Graphics.Renderables;
using Veldrid;
using Environment = HLView.Formats.Environment.Environment;

namespace HLView.Visualisers
{
    public class MdlVisualiser : IVisualiser
    {
        private readonly Panel _panel;
        private readonly ModelVisualiserPanel _settingsPanel;

        private GraphicsDevice _graphicsDevice;
        private VeldridControl _view;
        private SceneContext _sc;
        private Scene _scene;
        private MdlRenderable _renderable;
        private MdlFile _mdl;
        private RotationCamera _camera;

        public Control Container => _panel;
        
        public MdlVisualiser()
        {
            _panel = new Panel();
            _settingsPanel = new ModelVisualiserPanel
            {
                Dock = DockStyle.Bottom
            };
        }

        public bool Supports(string path)
        {
            return Path.GetExtension(path) == ".mdl";
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
                Dock = DockStyle.Fill,
            };
            _panel.Controls.Add(_view);
            _panel.Controls.Add(_settingsPanel);

            _camera = new RotationCamera(_view.Width, _view.Height);
            _view.Camera = _camera;

            _sc = new SceneContext(_graphicsDevice);
            _sc.AddRenderTarget(_view);

            _scene = new Scene();

            _mdl = MdlFile.FromFile(path);

            var (min, max) = GetBbox(_mdl, 0);
            _camera.SetBoundingBox(min, max);

            _renderable = new MdlRenderable(_mdl, Vector3.Zero);
            _scene.AddRenderable(_renderable);
            _sc.Scene = _scene;
            _sc.Start();

            _settingsPanel.SetModel(_mdl);
            _settingsPanel.BodyPartModelSelected += BodyPartSelected;
            _settingsPanel.SequenceSelected += SequenceSelected;
        }

        private void BodyPartSelected(object sender, (int, int) e)
        {
            _renderable.RenderSettings.SetBodyPartModel(e.Item1, e.Item2);
        }

        private void SequenceSelected(object sender, int e)
        {
            var (min, max) = GetBbox(_mdl, 0);
            _camera.SetBoundingBox(min, max);
            _renderable.RenderSettings.Sequence = e;
        }

        private (Vector3, Vector3) GetBbox(MdlFile mdl, int sequence)
        {
            if (sequence < 0 || sequence >= mdl.Sequences.Count) return (Vector3.One * -64, Vector3.One * 64);
            var seq = mdl.Sequences[sequence];
            return (seq.Min, seq.Max);
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
