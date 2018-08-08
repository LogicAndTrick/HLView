using System;

namespace HLView.Graphics.Renderables
{
    [Flags]
    public enum ModelRenderFlags
    {
        Model = 1 << 0,
        Hitbox = 1 << 1,
        Bone = 1 << 2,
        Attachment = 1 << 3,
        EyePosition = 1 << 4,
        Ground = 1 << 5,
        Wireframe = 1 << 6
    }
}