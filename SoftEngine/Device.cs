using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using SharpDX;

namespace SoftEngine
{
    public class Device
    {
        private byte[] backBuffer;
        private readonly float[] depthBuffer;
        private object[] lockBuffer;
        private WriteableBitmap bmp;
        private readonly int renderWidth;
        private readonly int renderHeight;
        
        public Device(WriteableBitmap bmp)
        {
            this.bmp = bmp;
            renderWidth = bmp.PixelWidth;
            renderHeight = bmp.PixelHeight;
            
            backBuffer = new byte[bmp.PixelWidth * bmp.PixelHeight * 4];
            depthBuffer = new float[bmp.PixelWidth * bmp.PixelHeight];
            lockBuffer = new object[renderWidth * renderHeight];
            for (var i = 0; i < lockBuffer.Length; i++)
            {
                lockBuffer[i] = new object();
            }
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

            for (var index = 0; index < depthBuffer.Length; index++)
            {
                depthBuffer[index] = float.MaxValue;
            }
        }

        public void Present()
        {
            var rect = new Int32Rect(0, 0, renderWidth, renderHeight);
            bmp.WritePixels(rect, backBuffer, renderWidth * 4, 0, 0);
        }

        public void PutPixel(int x, int y, float z, Color4 color)
        {
            var index = (x + y * renderWidth);
            var index4 = index * 4;

            lock (lockBuffer[index])
            {
                if (depthBuffer[index] < z)
                {
                    return;
                }

                depthBuffer[index] = z;
                
                backBuffer[index4] = (byte) (color.Blue * 255);
                backBuffer[index4 + 1] = (byte) (color.Green * 255);
                backBuffer[index4 + 2] = (byte) (color.Red * 255);
                backBuffer[index4 + 3] = (byte) (color.Alpha * 255);
            }
        }

        public Vertex Project(Vertex vertex, Matrix transMat, Matrix world)
        {
            var point2d = Vector3.TransformCoordinate(vertex.Coordinates, transMat);
            var point3dWorld = Vector3.TransformCoordinate(vertex.Coordinates, world);
            var normal3dWorld = Vector3.TransformCoordinate(vertex.Normal, world);
            
            var x = point2d.X * renderWidth + renderWidth / 2.0f;
            var y = point2d.Y * renderHeight + renderHeight / 2.0f;
            return new Vertex
            {
                Coordinates =  new Vector3(x, y, point2d.Z),
                Normal = normal3dWorld,
                WorldCoordinates = point3dWorld
            };
        }

        public void DrawPoint(Vector3 point, Color4 color)
        {
            if (point.X >= 0 && point.Y >= 0 && point.X < renderWidth && point.Y < renderHeight)
            {
                PutPixel((int) point.X, (int) point.Y, point.Z, color);
            }
        }

        public void DrawTriangle(Vertex v1, Vertex v2, Vertex v3, Color4 color)
        {
            if (v1.Coordinates.Y > v2.Coordinates.Y)
            {
                var temp = v2;
                v2 = v1;
                v1 = temp;
            }

            if (v2.Coordinates.Y > v3.Coordinates.Y)
            {
                var temp = v2;
                v2 = v3;
                v3 = temp;
            }

            if (v1.Coordinates.Y > v2.Coordinates.Y)
            {
                var temp = v2;
                v2 = v1;
                v1 = temp;
            }

            Vector3 p1 = v1.Coordinates;
            Vector3 p2 = v2.Coordinates;
            Vector3 p3 = v3.Coordinates;

            Vector3 vnFace = (v1.Normal + v2.Normal + v3.Normal) / 3;
            Vector3 centerPoint = (v1.WorldCoordinates + v2.WorldCoordinates + v3.WorldCoordinates) / 3;
            Vector3 lightPos = new Vector3(0, -10, 10);
            float ndotl = ComputeNDotL(centerPoint, vnFace, lightPos);
            
            var data = new ScanLineData { ndotla = ndotl };

            float dP1P2, dP1P3;

            if (p2.Y - p1.Y > 0)
                dP1P2 = (p2.X - p1.X) / (p2.Y - p1.Y);
            else
                dP1P2 = 0;

            if (p3.Y - p1.Y > 0)
                dP1P3 = (p3.X - p1.X) / (p3.Y - p1.Y);
            else
                dP1P3 = 0;

            if (dP1P2 > dP1P3)
            {
                for (var y = (int) p1.Y; y <= (int) p3.Y; y++)
                {
                    data.currentY = y;
                    
                    if (y < p2.Y)
                    {
                        ProcessScanLine(data, v1, v3, v1, v2, color);
                    }
                    else
                    {
                        ProcessScanLine(data, v1, v3, v2, v3, color);
                    }
                }
            }   
            else
            {
                for (var y = (int) p1.Y; y <= (int) p3.Y; y++)
                {
                    data.currentY = y;
                    
                    if (y < p2.Y)
                    {
                        ProcessScanLine(data, v1, v2, v1, v3, color);
                    }
                    else
                    {
                        ProcessScanLine(data, v2, v3, v1, v3, color);
                    }
                }
            }
        }

        public void Render(Camera camera, params Mesh[] meshes)
        {
            var viewMatrix = Matrix.LookAtLH(camera.Position, camera.Target, Vector3.UnitY);
            var projectionMatrix =
                Matrix.PerspectiveFovRH(0.78f, (float) renderWidth / renderHeight, 0.01f, 1.0f);

            foreach (var mesh in meshes)
            {
                var worldMatrix = Matrix.RotationYawPitchRoll(mesh.Rotation.Y, mesh.Rotation.X, mesh.Rotation.Z) *
                                  Matrix.Translation(mesh.Position);
                var transformMatrix = worldMatrix * viewMatrix * projectionMatrix;
                
                Parallel.For(0, mesh.Faces.Length, faceIndex =>
                {
                    var face = mesh.Faces[faceIndex];
                    var vertexA = mesh.Vertices[face.A];
                    var vertexB = mesh.Vertices[face.B];
                    var vertexC = mesh.Vertices[face.C];

                    var pixelA = Project(vertexA, transformMatrix, worldMatrix);
                    var pixelB = Project(vertexB, transformMatrix, worldMatrix);
                    var pixelC = Project(vertexC, transformMatrix, worldMatrix);

                    DrawTriangle(pixelA, pixelB, pixelC, Color4.White);
                });
            }
        }

        public async Task<Mesh[]> LoadJsonFileAsync(string fileName)
        {
            var meshes = new List<Mesh>();
            var data = "";
            using (var reader = new StreamReader(fileName))
            {
                data = await reader.ReadToEndAsync();
            }

            dynamic jsonObject = Newtonsoft.Json.JsonConvert.DeserializeObject(data);
            for (var meshIndex = 0; meshIndex < jsonObject.meshes.Count; meshIndex++)
            {
                var verticesArray = jsonObject.meshes[meshIndex].vertices;
                var indicesArray = jsonObject.meshes[meshIndex].indices;
                
                var uvCount = jsonObject.meshes[meshIndex].uvCount.Value;
                var verticesStep = 1;

                switch ((int) uvCount)
                {
                    case 0:
                        verticesStep = 6;
                        break;
                    case 1:
                        verticesStep = 8;
                        break;
                    case 2:
                        verticesStep = 10;
                        break;
                }

                var verticesCount = verticesArray.Count / verticesStep;
                var facesCount = indicesArray.Count / 3;
                var mesh = new Mesh(jsonObject.meshes[meshIndex].name.Value, verticesCount, facesCount);

                for (var index = 0; index < verticesCount; index++)
                {
                    var x = (float) verticesArray[index * verticesStep].Value;
                    var y = (float) verticesArray[index * verticesStep + 1].Value;
                    var z = (float) verticesArray[index * verticesStep + 2].Value;
                    
                    var nx = (float) verticesArray[index * verticesStep + 3].Value;
                    var ny = (float) verticesArray[index * verticesStep + 4].Value;
                    var nz = (float) verticesArray[index * verticesStep + 5].Value;
                    mesh.Vertices[index] = new Vertex { Coordinates = new Vector3(x, y, z), Normal = new Vector3(nx, ny, nz)};
                }

                for (var index = 0; index < facesCount; index++)
                {
                    var a = (int) indicesArray[index * 3].Value;
                    var b = (int) indicesArray[index * 3 + 1].Value;
                    var c = (int) indicesArray[index * 3 + 2].Value;
                    mesh.Faces[index] = new Face { A = a, B = b, C = c };
                }

                var position = jsonObject.meshes[meshIndex].position;
                mesh.Position = new Vector3((float) position[0].Value, (float) position[1].Value, (float) position[2].Value);
                meshes.Add(mesh);
            }

            return meshes.ToArray();
        }

        float Clamp(float value, float min = 0, float max = 1)
        {
            return Math.Max(min, Math.Min(value, max));
        }

        float Interpolate(float min, float max, float gradient)
        {
            return min + (max - min) * Clamp(gradient);
        }

        void ProcessScanLine(ScanLineData data, Vertex va, Vertex vb, Vertex vc, Vertex vd, Color4 color)
        {
            Vector3 pa = va.Coordinates;
            Vector3 pb = vb.Coordinates;
            Vector3 pc = vc.Coordinates;
            Vector3 pd = vd.Coordinates;
            
            var gradient1 = pa.Y != pb.Y ? (data.currentY - pa.Y) / (pb.Y - pa.Y) : 1;
            var gradient2 = pc.Y != pd.Y ? (data.currentY - pc.Y) / (pd.Y - pc.Y) : 1;

            var sx = (int) Interpolate(pa.X, pb.X, gradient1);
            var ex = (int) Interpolate(pc.X, pd.X, gradient2);

            float z1 = Interpolate(pa.Z, pb.Z, gradient1);
            float z2 = Interpolate(pc.Z, pd.Z, gradient2);

            for (var x = sx; x < ex; x++)
            {
                float gradient = (x - sx) / (float) (ex - sx);

                var z = Interpolate(z1, z2, gradient);
                var ndotl = data.ndotla;
                DrawPoint(new Vector3(x, data.currentY, z), new Color4(color.Red * ndotl, color.Green * ndotl, color.Blue * ndotl, color.Alpha));
            }
        }

        float ComputeNDotL(Vector3 vertex, Vector3 normal, Vector3 lightPosition)
        {
            var lightDirection = lightPosition - vertex;
            
            normal.Normalize();
            lightDirection.Normalize();
            
            return Math.Max(0, -Vector3.Dot(normal, lightDirection));
        }
    }
}