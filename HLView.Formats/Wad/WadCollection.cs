using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HLView.Formats.Wad
{
    public class WadCollection : IEnumerable<WadFile>
    {
        private List<WadFile> _files;

        public WadCollection()
        {
            _files = new List<WadFile>();
        }

        public Texture? Get(string name)
        {
            foreach (var wad in _files)
            {
                if (wad.Textures.ContainsKey(name))
                {
                    return wad.Textures[name];
                }
            }

            return null;
        }

        public void Add(WadFile wad) => _files.Add(wad);
        public void Remove(WadFile wad) => _files.Remove(wad);
        public IEnumerator<WadFile> GetEnumerator() => _files.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
