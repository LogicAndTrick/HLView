using System.Numerics;
using System.Runtime.InteropServices;

namespace HLView.Graphics
{
    public interface ICamera : IUpdateable
    {
        Matrix4x4 View { get; }
        Matrix4x4 Projection { get; }
        void WindowResized(int width, int height);
    }
}