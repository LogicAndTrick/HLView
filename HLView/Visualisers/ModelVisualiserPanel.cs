using System;
using System.Linq;
using System.Windows.Forms;
using HLView.Formats.Mdl;

namespace HLView.Visualisers
{
    public partial class ModelVisualiserPanel : UserControl
    {
        private MdlFile _model;
        public event EventHandler<(int, int)> BodyPartModelSelected;
        public event EventHandler<int> SequenceSelected;

        public ModelVisualiserPanel()
        {
            InitializeComponent();
        }

        public void SetModel(MdlFile model)
        {
            _model = model;

            PartSelector.Items.Clear();
            ModelSelector.Items.Clear();
            SequenceSelector.Items.Clear();

            PartSelector.Items.AddRange(model.BodyParts.Select(x => x.Name).OfType<object>().ToArray());
            SequenceSelector.Items.AddRange(model.Sequences.Select(x => x.Name).OfType<object>().ToArray());

            PartSelector.SelectedIndex = 0;
            SequenceSelector.SelectedIndex = 0;
        }

        private void PartChanged(object sender, EventArgs e)
        {
            ModelSelector.Items.Clear();

            var part = PartSelector.SelectedIndex;
            if (part < 0 || part >= _model.BodyParts.Count) return;

            var p = _model.BodyParts[part];
            ModelSelector.Items.AddRange(p.Models.Select(x => x.Name).OfType<object>().ToArray());
            ModelSelector.SelectedIndex = 0;
        }

        private void ModelChanged(object sender, EventArgs e)
        {
            BodyPartModelSelected?.Invoke(this, (PartSelector.SelectedIndex, ModelSelector.SelectedIndex));
        }

        private void SequenceChanged(object sender, EventArgs e)
        {
            SequenceSelected?.Invoke(this, SequenceSelector.SelectedIndex);
        }
    }
}
