namespace HLView.Formats.Wad
{
    public struct Texture
    {
        public string Name;
        public uint Width;
        public uint Height;
        public int NumMips;
        public byte[][] MipData;
        public byte[] Palette;
    }
}