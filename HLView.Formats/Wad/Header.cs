using System.Linq;
using System.Text;

namespace HLView.Formats.Wad
{
    public struct Header
    {
        public Version Version;
        public int NumLumps;
        public int LumpOffest;
    }
}
