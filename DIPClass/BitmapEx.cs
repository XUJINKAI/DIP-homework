using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Drawing.Imaging;
using System.IO;
using System.ComponentModel;

namespace ImageProcess
{
    public static class BitmapEx
    {
        /// <summary>
        /// read a color raw file as a bitmap
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="Width"></param>
        /// <param name="Height"></param>
        /// <returns></returns>
        public static unsafe Bitmap ColorRawToBitmap(string filename, int Width, int Height)
        {
            byte[] bytes = File.ReadAllBytes(filename);
            Bitmap b = new Bitmap(Width, Height);
            int planeSize = Width * Height, planeSize2 = planeSize * 2;
            BitmapData data = b.LockBits(new Rectangle(0, 0, Width, Height)
                , ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            int Stride = data.Stride;
            byte* p0 = (byte*)data.Scan0;
            int x, y, i;
            fixed (byte* pSRC0 = bytes)
            {
                byte* src = pSRC0, dst;
                i = 0;
                for (y = 0; y < Height; y++)
                {
                    dst = p0 + y * Stride;
                    for (x = 0; x < Width; x++)
                    {
                        dst[2] = src[i];
                        dst[1] = src[planeSize + i];
                        dst[0] = src[planeSize2 + i];
                        dst += 3;
                        i++;
                    }
                }
            }
            b.UnlockBits(data);
            return b;
        }

        /// <summary>
        /// read a gray raw file as a bitmap
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="Width"></param>
        /// <param name="Height"></param>
        /// <returns></returns>
        public static unsafe Bitmap GrayRawToBitmap(string filename, int Width, int Height)
        {
            byte[] bytes = File.ReadAllBytes(filename);
            Bitmap b = new Bitmap(Width, Height);
            BitmapData data = b.LockBits(new Rectangle(0, 0, Width, Height)
                , ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            int Stride = data.Stride;
            byte* p0 = (byte*)data.Scan0;
            int x, y, i;
            fixed (byte* pSRC0 = bytes)
            {
                byte* src = pSRC0, dst;
                i = 0;
                for (y = 0; y < Height; y++)
                {
                    dst = p0 + y * Stride;
                    for (x = 0; x < Width; x++)
                    {
                        dst[2] = dst[1] = dst[0] = src[i];
                        dst += 3;
                        i++;
                    }
                }
            }
            b.UnlockBits(data);
            return b;
        }

    }
}
