using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HLView.Formats.Wad;

namespace HLView.Formats.Environment
{
    public class Environment
    {
        public string Name { get; set; }
        public string BaseFolder { get; set; }
        public string ModFolder { get; set; }

        public WadCollection Wads { get; }

        public Environment(string folder)
        {
            Name = "Unknown";
            BaseFolder = ModFolder = folder;
            Wads = new WadCollection();

            var modFolder = new DirectoryInfo(ModFolder);
            if (!modFolder.Exists) return;

            // Find the game folder (hard-coded to "valve")
            var parent = modFolder.Parent;
            var gameFolder = parent?.GetDirectories("valve").FirstOrDefault();
            if (gameFolder != null) BaseFolder = gameFolder.FullName;

            // Find the mod name
            var liblist = modFolder.GetFiles("liblist.gam").FirstOrDefault();
            if (liblist != null) ParseLiblist(liblist);

            // Load wad files
            foreach (var wad in modFolder.GetFiles("*.wad"))
            {
                using (var s = wad.OpenRead()) Wads.Add(new WadFile(s));
            }

            if (gameFolder != null)
            {
                foreach (var wad in gameFolder.GetFiles("*.wad"))
                {
                    using (var s = wad.OpenRead()) Wads.Add(new WadFile(s));
                }
            }
        }

        public static Environment FromFile(string path)
        {
            var f = File.Exists(path) ? new FileInfo(path).Directory : new DirectoryInfo(path);
            while (f != null && f.Exists)
            {
                if (f.GetFiles("liblist.gam").Any())
                {
                    return new Environment(f.FullName);
                }

                f = f.Parent;
            }
            return new Environment("");
        }

        private void ParseLiblist(FileInfo file)
        {
            var dict = new Dictionary<string, string>();
            var lines = File.ReadAllLines(file.FullName);
            foreach (var line in lines)
            {
                var l = line;

                var c = l.IndexOf("//", StringComparison.Ordinal);
                if (c >= 0) l = l.Substring(0, c);
                l = l.Trim();

                if (String.IsNullOrWhiteSpace(l)) continue;

                c = l.IndexOf(' ');
                if (c < 0) continue;

                var key = l.Substring(0, c).ToLower();
                if (String.IsNullOrWhiteSpace(key)) continue;

                var value = l.Substring(c + 1);
                if (value[0] != '"' || value[value.Length - 1] != '"') continue;

                value = value.Substring(1, value.Length - 2).Trim();
                dict[key] = value;
            }

            foreach (var kv in dict)
            {
                switch (kv.Key)
                {
                    case "game":
                        Name = kv.Value;
                        break;
                }
            }
        }
    }
}
