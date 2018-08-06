using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HLView.Graphics.Cameras
{
    public class RotationCamera : ICamera
    {
        private Vector3 _origin;
        private Vector3 _angles;
        private float _distance;

        private int _width;
        private int _height;
        private Vector2 _previousMousePos;

        private int _fov;
        private int _clipDistance;

        public Matrix4x4 View => GetCameraMatrix();
        public Matrix4x4 Projection => GetViewportMatrix(_width, _height);
        public Vector3 Location => Vector3.Transform(Vector3.Zero, GetCameraMatrix());

        public RotationCamera(int width, int height)
        {
            _origin = Vector3.Zero;

            _fov = 90;
            _clipDistance = 10000;

            _width = width;
            _height = height;
        }

        public void SetBoundingBox(Vector3 min, Vector3 max)
        {
            _origin = (min + max) / 2;

            var size = max - min;
            _distance = Math.Max(size.X, Math.Max(size.Y, size.Z));

            _angles = Vector3.Zero;
        }

        public void Update(long milliseconds)
        {
            var mousePosPoint = Control.MousePosition;
            var mousePos = new Vector2(mousePosPoint.X, mousePosPoint.Y);
            var mouseDelta = _previousMousePos - mousePos;
            _previousMousePos = mousePos;

            var buttons = Control.MouseButtons;
            if (buttons.HasFlag(MouseButtons.Left))
            {
                var dx = mouseDelta.X;
                var dy = mouseDelta.Y;
                var fovdiv = (_width / 60f) * 5f;
                _angles.X -= dy / fovdiv;
                _angles.Z -= dx / fovdiv;
            }
            else if (buttons.HasFlag(MouseButtons.Right))
            {
                var dy = mouseDelta.Y;
                _distance = Math.Max(1, _distance - dy);
            }
        }
        
        public void WindowResized(int width, int height)
        {
            _width = width;
            _height = height;
        }

        private Matrix4x4 GetCameraMatrix()
        {
            var startLocation = _origin + new Vector3(0, -_distance, 0);
            var t = Matrix4x4.CreateLookAt(startLocation, _origin, Vector3.UnitZ);
            var x = Matrix4x4.CreateFromAxisAngle(Vector3.UnitX, _angles.X);
            var z = Matrix4x4.CreateFromAxisAngle(Vector3.UnitZ, _angles.Z);
            return z * x * t;
        }

        private Matrix4x4 GetViewportMatrix(int width, int height)
        {
            const float near = 1.0f;
            var ratio = width / (float)height;
            if (ratio <= 0) ratio = 1;

            return Matrix4x4.CreatePerspectiveFieldOfView(_fov * (float)Math.PI / 180, ratio, near, _clipDistance);
        }
    }
}
