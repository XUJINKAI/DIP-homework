using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;

namespace ImageProcess
{
    public static class Noise
    {
        private static Random rand = new Random();

        public static double GaussRandom(double mean, double deviation)
        {
            double r1 = rand.NextDouble();
            double r2 = rand.NextDouble();
            double norm = Math.Sqrt((-2) * Math.Log(r2)) * Math.Sin(2 * Math.PI * r1);
            return norm * deviation + mean;
        }

        /// <summary>
        /// add Gaussian noise to bitmap (I channel)
        /// </summary>
        /// <param name="b"></param>
        /// <param name="mean"></param>
        /// <param name="deviation"></param>
        /// <returns></returns>
        public static unsafe Bitmap GaussianNoiseI(this Bitmap b, double mean, double deviation)
        {
            b = b.Clone() as Bitmap;
            BitmapData data = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite
                , PixelFormat.Format24bppRgb);
            int width = b.Width, height = b.Height, stride = data.Stride;
            int x, y;
            double noise, re;
            int sum;
            deviation *= 3;
            byte* p = (byte*)data.Scan0, p0, p1, p2;
            for (y = 0; y < height; y++)
            {
                p0 = p + y * stride;
                p1 = p0 + 1;
                p2 = p1 + 1;
                for (x = 0; x < width; x++)
                {
                    sum = *p0 + *p1 + *p2;
                    noise = GaussRandom(mean, deviation);

                    re = *p0 + noise * *p0 / sum;
                    re = (re > 255) ? 255 : re;
                    re = (re < 0) ? 0 : re;
                    *p0 = (byte)re;

                    re = *p1 + noise * *p1 / sum;
                    re = (re > 255) ? 255 : re;
                    re = (re < 0) ? 0 : re;
                    *p1 = (byte)re;

                    re = *p2 + noise * *p2 / sum;
                    re = (re > 255) ? 255 : re;
                    re = (re < 0) ? 0 : re;
                    *p2 = (byte)re;

                    p0 += 3;
                    p1 = p0 + 1;
                    p2 = p1 + 1;
                }
            }
            b.UnlockBits(data);
            return b;
        }

        /// <summary>
        /// add Gaussian noise to bitmap, RGB respectively
        /// </summary>
        /// <param name="b"></param>
        /// <param name="mean"></param>
        /// <param name="deviation"></param>
        /// <returns></returns>
        public static unsafe Bitmap GaussianNoiseRGB(this Bitmap b, double mean, double deviation)
        {
            b = b.Clone() as Bitmap;
            BitmapData data = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite
                , PixelFormat.Format24bppRgb);
            int width = b.Width, height = b.Height, stride = data.Stride;
            int x, y;
            double noise, re;
            byte* p = (byte*)data.Scan0, p0;
            for (y = 0; y < height; y++)
            {
                p0 = p + y * stride;
                for (x = 0; x < stride; x++)
                {
                    noise = GaussRandom(mean, deviation);

                    re = *p0 + noise;
                    re = (re > 255) ? 255 : re;
                    re = (re < 0) ? 0 : re;
                    *p0 = (byte)re;

                    p0++;
                }
            }
            b.UnlockBits(data);
            return b;
        }

        /// <summary>
        /// add salt noise(value 255) to bitmap
        /// </summary>
        /// <param name="b"></param>
        /// <param name="probability"></param>
        /// <returns></returns>
        public static unsafe Bitmap SaltNoise(this Bitmap b, double probability)
        {
            b = b.Clone() as Bitmap;
            BitmapData data = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite
                , PixelFormat.Format24bppRgb);
            int width = b.Width, height = b.Height, stride = data.Stride;
            int x, y;
            byte* p = (byte*)data.Scan0, p0, p1, p2;
            for (y = 0; y < height; y++)
            {
                p0 = p + y * stride;
                p1 = p0 + 1;
                p2 = p1 + 1;
                for (x = 0; x < width; x++)
                {
                    if (rand.NextDouble() < probability)
                    {
                        *p0 = 255;
                        *p1 = 255;
                        *p2 = 255;
                    }
                    p0 += 3;
                    p1 = p0 + 1;
                    p2 = p1 + 1;
                }
            }
            b.UnlockBits(data);
            return b;
        }

        /// <summary>
        /// add pepper noise(value 0) to bitmap
        /// </summary>
        /// <param name="b"></param>
        /// <param name="probability"></param>
        /// <returns></returns>
        public static unsafe Bitmap PepperNoise(this Bitmap b, double probability)
        {
            b = b.Clone() as Bitmap;
            BitmapData data = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite
                , PixelFormat.Format24bppRgb);
            int width = b.Width, height = b.Height, stride = data.Stride;
            int x, y;
            byte* p = (byte*)data.Scan0, p0, p1, p2;
            for (y = 0; y < height; y++)
            {
                p0 = p + y * stride;
                p1 = p0 + 1;
                p2 = p1 + 1;
                for (x = 0; x < width; x++)
                {
                    if (rand.NextDouble() < probability)
                    {
                        *p0 = 0;
                        *p1 = 0;
                        *p2 = 0;
                    }
                    p0 += 3;
                    p1 = p0 + 1;
                    p2 = p1 + 1;
                }
            }
            b.UnlockBits(data);
            return b;
        }
    }
}
