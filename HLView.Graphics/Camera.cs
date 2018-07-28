using System;
using System.Numerics;
using System.Windows.Forms;

namespace HLView.Graphics
{
    public class Camera : ICamera
    {
        private const float MoveSpeed = 0.01f;

        private float _yaw;
        private float _pitch;

        private Vector2 _previousMousePos;
        private float _windowWidth;
        private float _windowHeight;

        public Matrix4x4 ViewMatrix { get; private set; }
        public Matrix4x4 ProjectionMatrix { get; private set; }

        private Vector3 _position = new Vector3(0, 0, 2);
        public Vector3 Position
        {
            get => _position;
            set
            {
                _position = value;
                UpdateViewMatrix();
            }
        }

        public Vector3 LookDirection { get; private set; } = Vector3.UnitZ;
        public float FarDistance { get; } = 10000f;
        public float FieldOfView { get; } = 1f;
        public float NearDistance { get; } = 1f;
        public float AspectRatio => _windowWidth / _windowHeight;
        public float Yaw { get => _yaw; set { _yaw = value; UpdateViewMatrix(); } }
        public float Pitch { get => _pitch; set { _pitch = value; UpdateViewMatrix(); } }

        public Matrix4x4 View => ViewMatrix;
        public Matrix4x4 Projection => ProjectionMatrix;

        public Camera(float width, float height)
        {
            _windowWidth = width;
            _windowHeight = height;
            UpdatePerspectiveMatrix();
            UpdateViewMatrix();
        }

        public void WindowResized(int width, int height)
        {
            _windowWidth = width;
            _windowHeight = height;
            UpdatePerspectiveMatrix();
        }

        public void Update(long milliseconds)
        {
            var sprintFactor = KeyboardState.IsKeyDown(Keys.LControlKey)
                ? 0.1f
                : KeyboardState.IsKeyDown(Keys.LShiftKey)
                    ? 2.5f
                    : 1f;
            var motionDir = Vector3.Zero;
            if (KeyboardState.IsKeyDown(Keys.A)) motionDir += -Vector3.UnitX;
            if (KeyboardState.IsKeyDown(Keys.D)) motionDir += Vector3.UnitX;
            if (KeyboardState.IsKeyDown(Keys.W)) motionDir += -Vector3.UnitZ;
            if (KeyboardState.IsKeyDown(Keys.S)) motionDir += Vector3.UnitZ;
            if (KeyboardState.IsKeyDown(Keys.Q)) motionDir += -Vector3.UnitY;
            if (KeyboardState.IsKeyDown(Keys.E)) motionDir += Vector3.UnitY;

            if (motionDir != Vector3.Zero)
            {
                var lookRotation = Quaternion.CreateFromYawPitchRoll(Yaw, Pitch, 0f);
                motionDir = Vector3.Transform(motionDir, lookRotation);
                _position += motionDir * MoveSpeed * sprintFactor * (milliseconds / 1000f);
                UpdateViewMatrix();
            }

            var mousePosPoint = Control.MousePosition;
            var mousePos = new Vector2(mousePosPoint.X, mousePosPoint.Y);
            var mouseDelta = mousePos - _previousMousePos;
            _previousMousePos = mousePos;

            var buttons = Control.MouseButtons;

            if (buttons.HasFlag(MouseButtons.Left) || buttons.HasFlag(MouseButtons.Right))
            {
                Yaw += -mouseDelta.X * 0.01f;
                Pitch += -mouseDelta.Y * 0.01f;
                Pitch = Clamp(Pitch, -1.55f, 1.55f);

                UpdateViewMatrix();
            }
        }

        private static float Clamp(float value, float min, float max) => value <= min ? min : (value >= max ? max : value);

        private void UpdatePerspectiveMatrix()
        {
            ProjectionMatrix = CreatePerspective(FieldOfView, _windowWidth / _windowHeight, NearDistance, FarDistance);
        }

        private static Matrix4x4 CreatePerspective(float fov, float aspectRatio, float near, float far)
        {
            if (fov <= 0.0f || fov >= Math.PI) throw new ArgumentOutOfRangeException(nameof(fov));
            if (near <= 0.0f) throw new ArgumentOutOfRangeException(nameof(near));
            if (far <= 0.0f) throw new ArgumentOutOfRangeException(nameof(far));

            var yScale = 1.0f / (float) Math.Tan(fov * 0.5f);
            var xScale = yScale / aspectRatio;

            Matrix4x4 result;

            result.M11 = xScale;
            result.M12 = result.M13 = result.M14 = 0.0f;

            result.M22 = yScale;
            result.M21 = result.M23 = result.M24 = 0.0f;

            result.M31 = result.M32 = 0.0f;
            var negFarRange = float.IsPositiveInfinity(far) ? -1.0f : far / (near - far);
            result.M33 = negFarRange;
            result.M34 = -1.0f;

            result.M41 = result.M42 = result.M44 = 0.0f;
            result.M43 = near * negFarRange;

            return result;
        }

        private void UpdateViewMatrix()
        {
            var lookRotation = Quaternion.CreateFromYawPitchRoll(Yaw, Pitch, 0f);
            var lookDir = Vector3.Transform(-Vector3.UnitZ, lookRotation);
            LookDirection = lookDir;
            ViewMatrix = Matrix4x4.CreateLookAt(_position, _position + LookDirection, Vector3.UnitY);
        }
    }
}
