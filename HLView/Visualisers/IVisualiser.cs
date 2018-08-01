using System;
using System.Windows.Forms;
using Environment = HLView.Formats.Environment.Environment;

namespace HLView.Visualisers
{
    public interface IVisualiser
    {
        Control Container { get; }
        bool Supports(string path);

        void Open(Environment environment, string path);
        void Close();
    }
}