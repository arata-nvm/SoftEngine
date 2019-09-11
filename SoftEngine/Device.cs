using System.Windows;
using System.Windows.Media.Imaging;
using SharpDX;

namespace SoftEngine
{
    public class Device
    {
        private byte[] backBuffer;
        private WriteableBitmap bmp;

        public Device(WriteableBitmap bmp)
        {
            this.bmp = bmp;
            backBuffer = new byte[bmp.PixelWidth * bmp.PixelHeight * 4];
        }

        public void Clear(byte r, byte g, byte b, byte a)
        {
            for (var index = 0; index < backBuffer.Length; index += 4)
            {
                backBuffer[index] = b;
                backBuffer[index + 1] = g;
                backBuffer[index + 2] = r;
                backBuffer[index + 3] = a;
            }
        }

        public void Present()
        {
            var rect = new Int32Rect(0, 0, bmp.PixelWidth, bmp.PixelHeight);
            bmp.WritePixels(rect, backBuffer, bmp.PixelWidth * 4, 0, 0);
        }

        public void PutPixel(int x, int y, Color4 color)
        {
            var index = (x + y * bmp.PixelWidth) * 4;
            backBuffer[index] = (byte) (color.Blue * 255);
            backBuffer[index + 1] = (byte) (color.Green * 255);
            backBuffer[index + 2] = (byte) (color.Red * 255);
            backBuffer[index + 3] = (byte) (color.Alpha * 255);
        }

        public Vector2 Project(Vector3 coord, Matrix transMat)
        {
            var point = Vector3.TransformCoordinate(coord, transMat);
            var x = point.X * bmp.PixelWidth + bmp.PixelWidth / 2.0f;
            var y = -point.Y * bmp.PixelHeight + bmp.PixelHeight / 2.0f;
            return new Vector2(x, y);
        }

        public void DrawPoint(Vector2 point)
        {
            if (point.X >= 0 && point.Y >= 0 && point.X < bmp.PixelWidth && point.Y < bmp.PixelHeight)
            {
                PutPixel((int) point.X, (int) point.Y, new Color4(1.0f, 1.0f, 1.0f, 1.0f));
            }
        }

        public void Render(Camera camera, params Mesh[] meshes)
        {
            var viewMatrix = Matrix.LookAtLH(camera.Position, camera.Target, Vector3.UnitY);
            var projectionMatrix =
                Matrix.PerspectiveFovRH(0.78f, (float) bmp.PixelWidth / bmp.PixelHeight, 0.01f, 1.0f);

            foreach (var mesh in meshes)
            {
                var worldMatrix = Matrix.RotationYawPitchRoll(mesh.Rotation.Y, mesh.Rotation.X, mesh.Rotation.Z) *
                                  Matrix.Translation(mesh.Position);
                var transformMatrix = worldMatrix * viewMatrix * projectionMatrix;

                foreach (var vertex in mesh.Vertices)
                {
                    var point = Project(vertex, transformMatrix);
                    DrawPoint(point);
                }
            }
        }
    }
}