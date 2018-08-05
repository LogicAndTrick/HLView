using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using HLView.Formats.Bsp;
using HLView.Formats.Mdl;

namespace Sandbox
{
    class Program
    {
        static void Main(string[] args)
        {
            var file = @"F:\Steam\SteamApps\common\Half-Life\valve\models\hgrunt.mdl";
            var mdl = MdlFile.FromFile(file);
        }
    }
}
