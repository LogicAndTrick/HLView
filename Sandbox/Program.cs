using System;
using System.IO;
using System.Runtime.InteropServices;
using HLView.Formats.Bsp;
using HLView.Formats.Wad;

namespace Sandbox
{
    class Program
    {
        static void Main(string[] args)
        {
            var file = @"F:\Steam\SteamApps\common\Half-Life\valve\maps\verc_18.bsp";
            file = @"F:\Steam\SteamApps\common\Half-Life\valve\maps\aaa.bsp";
            using (var stream = File.OpenRead(file))
            {
                var bsp = new BspFile(stream);
                var header = bsp.Header;
            }

            file = @"F:\Steam\SteamApps\common\Half-Life\valve\halflife.wad";
            using (var stream = File.OpenRead(file))
            {
                var wad = new WadFile(stream);
                var header = wad.Header;
            }
        }
    }
}
