namespace HLView.Formats.Wad
{
    public struct Lump
    {
        public const int NameLength = 16;

        public int Offset;
        public int CompressedSize;
        public int UncompressedSize;
        public LumpType Type;
        public byte Compression;
        public string Name;
    }
}