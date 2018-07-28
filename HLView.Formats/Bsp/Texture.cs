namespace HLView.Formats.Bsp
{
    public struct Texture
    {
        public const int NameLength = 16;
        public const int MipLevels = 4;

        public string Name;
        public uint Width;
        public uint Height;
        public uint[] Offsets;
    }
}