using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Windows.Forms;

namespace HLView.Graphics
{
    public class PerspectiveCamera : ICamera
    {
        private Vector3 _direction;
        private Vector3 _lookAt;

        private int _width;
        private int _height;
        private Vector2 _previousMousePos;

        public int FOV { get; set; }
        public int ClipDistance { get; set; }
        public Vector3 Position { get; set; }

        public Vector3 Direction
        {
            get => _direction;
            set
            {
                _direction = value;
                _lookAt = Position + _direction;
            }
        }

        public Vector3 LookAt
        {
            get => _lookAt;
            set
            {
                _lookAt = value;
                _direction = _lookAt - Position;
            }
        }

        public Matrix4x4 View => GetCameraMatrix();
        public Matrix4x4 Projection => GetViewportMatrix(_width, _height);

        public PerspectiveCamera(int width, int height)
        {
            Position = Vector3.Zero;
            Direction = Vector3.One;
            FOV = 90;
            ClipDistance = 10000;

            _width = width;
            _height = height;
        }

        public void Update(long milliseconds)
        {
            var move = 1f;
            if (KeyboardState.IsKeyDown(Keys.LControlKey)) move *= 0.1f;
            else if (KeyboardState.IsKeyDown(Keys.LShiftKey)) move *= 2.5f;

            IfKey(Keys.W, () => Advance(move));
            IfKey(Keys.S, () => Advance(-move));
            IfKey(Keys.A, () => Strafe(-move));
            IfKey(Keys.D, () => Strafe(move));
            IfKey(Keys.Q, () => AscendAbsolute(move));
            IfKey(Keys.E, () => AscendAbsolute(-move));

            var mousePosPoint = Control.MousePosition;
            var mousePos = new Vector2(mousePosPoint.X, mousePosPoint.Y);
            var mouseDelta = _previousMousePos - mousePos;
            _previousMousePos = mousePos;

            var buttons = Control.MouseButtons;
            if (buttons.HasFlag(MouseButtons.Left) || buttons.HasFlag(MouseButtons.Right))
            {
                var dx = mouseDelta.X;
                var dy = mouseDelta.Y;
                var fovdiv = (_width / 60f) / 5f;
                Pan(dx / fovdiv);
                Tilt(-dy / fovdiv);
            }
        }

        private void IfKey(Keys key, Action action)
        {
            if (KeyboardState.IsKeyDown(key)) action();
        }

        public void WindowResized(int width, int height)
        {
            _width = width;
            _height = height;
        }

        private Matrix4x4 GetCameraMatrix()
        {
            return Matrix4x4.CreateLookAt(Position, _lookAt, Vector3.UnitZ);
        }

        private Matrix4x4 GetViewportMatrix(int width, int height)
        {
            const float near = 1.0f;
            var ratio = width / (float)height;
            if (ratio <= 0) ratio = 1;

            return Matrix4x4.CreatePerspectiveFieldOfView(FOV * (float) Math.PI / 180, ratio, near, ClipDistance);
        }

        public float GetRotation()
        {
            var temp = (LookAt - Position);
            if (Math.Abs(temp.Length()) > 0.001)
            {
                temp = Vector3.Normalize(temp);
            }
            var rot = Math.Atan2(temp.Y, temp.X);
            if (rot < 0) rot += 2 * Math.PI;
            if (rot > 2 * Math.PI) rot = rot % (2 * Math.PI);
            return (float)rot;
        }

        public void SetRotation(float rotation)
        {
            var temp = (LookAt - Position);
            if (Math.Abs(temp.Length()) > 0.001)
            {
                temp = Vector3.Normalize(temp);
            }
            var e = GetElevation();
            var x = Math.Cos(rotation) * Math.Sin(e);
            var y = Math.Sin(rotation) * Math.Sin(e);
            LookAt = new Vector3((float)x + Position.X, (float)y + Position.Y, temp.Z + Position.Z);
        }

        public float GetElevation()
        {
            var temp = (LookAt - Position);
            if (Math.Abs(temp.Length()) > 0.001)
            {
                temp = Vector3.Normalize(temp);
            }
            var elev = Math.Acos(temp.Z);
            return (float)elev;
        }

        public void SetElevation(float elevation)
        {
            if (elevation > (Math.PI * 0.99)) elevation = (float)Math.PI * 0.99f;
            if (elevation < (Math.PI * 0.01)) elevation = (float)Math.PI * 0.01f;
            var rotation = GetRotation();
            var x = Math.Cos(rotation) * Math.Sin(elevation);
            var y = Math.Sin(rotation) * Math.Sin(elevation);
            var z = Math.Cos(elevation);
            LookAt = new Vector3((float)x + Position.X, (float)y + Position.Y, (float)z + Position.Z);
        }

        public void Pan(float degrees)
        {
            var rad = degrees * ((float)Math.PI / 180);
            var rot = GetRotation();
            SetRotation(rot + rad);
        }

        public void Tilt(float degrees)
        {
            SetElevation(GetElevation() + (degrees * ((float)Math.PI / 180)));
        }

        public void Advance(float units)
        {
            var temp = LookAt - Position;
            if (Math.Abs(temp.Length()) > 0.001)
            {
                temp = Vector3.Normalize(temp);
            }
            var add = temp * (float)units;
            LookAt += add;
            Position += add;
        }

        public void Strafe(float units)
        {
            var right = GetRight();
            var add = right * (float)units;
            LookAt += add;
            Position += add;
        }

        public void Ascend(float units)
        {
            var up = GetUp();
            var add = up * (float)units;
            LookAt += add;
            Position += add;
        }

        public void AscendAbsolute(float units)
        {
            var up = new Vector3(0, 0, (float)units);
            LookAt += up;
            Position += up;
        }

        public Vector3 GetUp()
        {
            var temp = LookAt - Position;
            if (Math.Abs(temp.Length()) > 0.001)
            {
                temp = Vector3.Normalize(temp);
            }
            var normal = Vector3.Cross(GetRight(), temp);
            return Vector3.Normalize(normal);
        }

        public Vector3 GetRight()
        {
            var temp = LookAt - Position;
            temp.Z = 0;
            if (Math.Abs(temp.Length()) > 0.001)
            {
                temp = Vector3.Normalize(temp);
            }
            var normal = Vector3.Cross(temp, Vector3.UnitZ);
            return Vector3.Normalize(normal);
        }
    }
}