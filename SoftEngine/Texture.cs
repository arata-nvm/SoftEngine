﻿using System;
 using System.Windows.Media;
using System.Windows.Media.Imaging;
using SharpDX;

namespace SoftEngine
{
    public class Texture
    {
        private byte[] internalBuffer;
        private int width;
        private int height;

        public Texture(string filename, int width, int height)
        {
            this.width = width;
            this.height = height;
            this.internalBuffer = new byte[width * height * 4];
            Load(filename);
        }

        void Load(string filename)
        {
            var bmpOriginal = new BitmapImage(new Uri(filename, UriKind.Relative));
            var bmp = new FormatConvertedBitmap(bmpOriginal, PixelFormats.Pbgra32, null, 0);
            bmp.CopyPixels(internalBuffer, width * 4, 0);
        }

        public Color4 Map(float tu, float tv)
        {
            if (internalBuffer == null)
            {
                return Color4.White;
            }

            int u = Math.Abs((int) (tu * width) % width);
            int v = Math.Abs((int) (tv * height) % height);

            int pos = (u + v * width) * 4;

            if (pos + 3 >= internalBuffer.Length)
            {
                return Color4.Black;
            }
            
            byte b = internalBuffer[pos];
            byte g = internalBuffer[pos + 1];
            byte r = internalBuffer[pos + 2];
            byte a = internalBuffer[pos + 3];

            return new Color4(r / 255.0f, g / 255.0f, b / 255.0f, a / 255.0f);
        }
    }
}