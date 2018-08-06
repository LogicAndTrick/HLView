using System.Numerics;
using HLView.Graphics.Renderables;

namespace HLView.Graphics.Cameras
{
    public interface ICamera : IUpdateable
    {
        Matrix4x4 View { get; }
        Matrix4x4 Projection { get; }
        Vector3 Location { get; }
        void WindowResized(int width, int height);
    }
}