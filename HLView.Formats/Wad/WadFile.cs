using System;
using System.Collections.Generic;
using System.IO;

namespace HLView.Formats.Wad
{
    public class WadFile
    {
        public Header Header { get; set; }
        public List<Lump> Lumps { get; set; }
        public List<Texture> Textures { get; set; }

        public WadFile(Stream stream)
        {
            Lumps = new List<Lump>();
            Textures = new List<Texture>();
            using (var br = new BinaryReader(stream)) Read(br);
        }

        private void Read(BinaryReader br)
        {
            Header = new Header
            {
                Version = (Version) br.ReadUInt32(),
                NumLumps = br.ReadInt32(),
                LumpOffest = br.ReadInt32()
            };

            if (Header.Version != Version.Wad2 && Header.Version != Version.Wad3)
            {
                throw new NotSupportedException("Only Goldsource (WAD2 & WAD3) WAD files are supported.");
            }

            br.BaseStream.Seek(Header.LumpOffest, SeekOrigin.Begin);
            for (var i = 0; i < Header.NumLumps; i++)
            {
                var lump = new Lump
                {
                    Offset = br.ReadInt32(),
                    CompressedSize = br.ReadInt32(),
                    UncompressedSize = br.ReadInt32(),
                    Type = (LumpType) br.ReadByte(),
                    Compression = br.ReadByte()
                };
                br.ReadBytes(2);
                var name = br.ReadChars(Lump.NameLength);
                var len = Array.IndexOf(name, '\0');
                lump.Name = new string(name, 0, len < 0 ? name.Length : len);
                Lumps.Add(lump);
            }

            foreach (var lump in Lumps)
            {
                br.BaseStream.Seek(lump.Offset, SeekOrigin.Begin);
                Texture texture;
                switch (lump.Type)
                {
                    case LumpType.Image:
                        texture = new Texture
                        {
                            Name = lump.Name,
                            Width = br.ReadUInt32(),
                            Height = br.ReadUInt32(),
                            NumMips = 1
                        };
                        var size = (int)(texture.Width * texture.Height);
                        texture.MipData = new[] {br.ReadBytes(size)};
                        var paletteSize = br.ReadUInt16();
                        texture.Palette = br.ReadBytes(paletteSize * 3);
                        break;
                    case LumpType.Texture:
                        texture = ReadMipTexture(br);
                        break;
                    default:
                        continue;
                }
                Textures.Add(texture);
            }
        }

        public static Texture ReadMipTexture(BinaryReader br)
        {
            var position = br.BaseStream.Position;

            var texture = new Texture();

            var name = br.ReadChars(Lump.NameLength);
            var len = Array.IndexOf(name, '\0');
            texture.Name = new string(name, 0, len < 0 ? name.Length : len);
            
            texture.Width = br.ReadUInt32();
            texture.Height = br.ReadUInt32();
            var offsets = new[] {br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32()};

            if (offsets[0] == 0)
            {
                texture.NumMips = 0;
                texture.MipData = new byte[0][];
                texture.Palette = new byte[0];
                return texture;
            }

            texture.NumMips = 4;
            texture.MipData = new byte[4][];

            int w = (int) texture.Width, h = (int) texture.Height;
            for (var i = 0; i < 4; i++)
            {
                br.BaseStream.Seek(position + offsets[i], SeekOrigin.Begin);
                texture.MipData[i] = br.ReadBytes(w * h);
                w /= 2;
                h /= 2;
            }

            var paletteSize = br.ReadUInt16();
            texture.Palette = br.ReadBytes(paletteSize * 3);

            return texture;
        }
    }
}