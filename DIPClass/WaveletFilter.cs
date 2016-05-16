using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;

namespace ImageProcess
{
    public static class WaveletFilter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="b"></param>
        /// <param name="district">"1" means "HH1,HL1,LH1", comma is supported</param>
        /// <param name="threshold"></param>
        /// <param name="IsHardThreshold"></param>
        /// <returns></returns>
        public static unsafe Bitmap Threshold(Bitmap b, string district, int threshold, bool IsHardThreshold)
        {

            string[] strs = district.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            int level = 1;
                int lev;
            foreach (string s in strs)
            {
                string dis = s.Trim();
                if (dis.ToLower() == dis.ToUpper()) //数字
                    lev = Convert.ToInt32(dis);
                else
                    lev = Convert.ToInt32(dis.Substring(2));
                level = (level > lev) ? level : lev;
            }
            b = WaveletTransform(b, level);
            foreach (string s in strs)
            {
                string dis = s.Trim();
                if (dis.ToLower() == dis.ToUpper()) //数字
                {
                    WaveletThreshold(b, "HH" + dis, threshold, IsHardThreshold);
                    WaveletThreshold(b, "LH" + dis, threshold, IsHardThreshold);
                    WaveletThreshold(b, "HL" + dis, threshold, IsHardThreshold);
                }
                else
                {
                    WaveletThreshold(b, dis, threshold, IsHardThreshold);
                }
            }
            return IWaveletTransform(b, level);
        }
        /// <summary>
        /// denoise with wavelet transform
        /// </summary>
        /// <param name="b"></param>
        /// <param name="district">"HH1","LH1","HL1","HH2",...</param>
        /// <param name="threshold"></param>
        /// <param name="IsHardThreshold"></param>
        /// <returns></returns>
        private static unsafe void WaveletThreshold(Bitmap b, string district, int threshold, bool IsHardThreshold)
        {
            string firstChar = district.Substring(0, 1);
            string secondChar = district.Substring(1, 1);
            int level = Convert.ToInt32(district.Substring(2));

            int Width = b.Width, Height = b.Height;
            int levP2 = 1 << level;
            int levWidth = Width / levP2, levHeight = Height / levP2;
            int pointX = (firstChar == "H") ? levWidth : 0;
            int pointY = (secondChar == "H") ? levHeight : 0;

            BitmapData data = b.LockBits(new Rectangle(pointX, pointY, levWidth, levHeight)
                , ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            int dataHeight = data.Height, dataWidth3 = data.Width * 3, Stride = data.Stride;
            byte* p0 = (byte*)data.Scan0;
            byte* p;
            int x, y;
            int min = 127 - threshold, max = 127 + threshold;

            for (y = 0; y < dataHeight; y++)
            {
                p = p0 + y * Stride;
                for (x = 0; x < dataWidth3; x++)
                {
                    *p = (byte)(((*p > min) && (*p < max)) ? 127 : *p);
                    *p = (byte)((!IsHardThreshold && (*p < min)) ? (*p + threshold) : *p);
                    *p = (byte)((!IsHardThreshold && (*p > max)) ? (*p - threshold) : *p);
                    p++;
                }
            }
            b.UnlockBits(data);
        }

        #region Helper
        public static Bitmap WaveletTransform(Bitmap b, int Iterations)
        {
            return ApplyHaarTransform(Expand2Power2(b), true, false, Iterations);
        }

        public static Bitmap IWaveletTransform(Bitmap b, int Iterations)
        {
            return ApplyHaarTransform(Expand2Power2(b), false, false, Iterations);
        }

        public static Bitmap Expand2Power2(Bitmap b)
        {
            return b;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Forward"></param>
        /// <param name="Safe"></param>
        /// <param name="sIterations"></param>
        /// modified from http://www.codeproject.com/Articles/683663/Discrete-Haar-Wavelet-Transformation
        private static Bitmap ApplyHaarTransform(Bitmap bmp, bool Forward, bool Safe, int Iterations)
        {
            bmp = bmp.Clone() as Bitmap;

            int maxScale = (int)(Math.Log(bmp.Width < bmp.Height ? bmp.Width : bmp.Height) / Math.Log(2));
            if (Iterations < 1 || Iterations > maxScale)
            {
                throw new Exception("Iteration must be Integer from 1 to " + maxScale);
            }

            double[,] Red = new double[bmp.Width, bmp.Height];
            double[,] Green = new double[bmp.Width, bmp.Height];
            double[,] Blue = new double[bmp.Width, bmp.Height];

            int PixelSize = 3;
            BitmapData bmData = null;

            if (Safe)
            {
                Color c;

                for (int j = 0; j < bmp.Height; j++)
                {
                    for (int i = 0; i < bmp.Width; i++)
                    {
                        c = bmp.GetPixel(i, j);
                        Red[i, j] = (double)Scale(0, 255, -1, 1, c.R);
                        Green[i, j] = (double)Scale(0, 255, -1, 1, c.G);
                        Blue[i, j] = (double)Scale(0, 255, -1, 1, c.B);
                    }
                }
            }
            else
            {
                bmData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                unsafe
                {
                    for (int j = 0; j < bmData.Height; j++)
                    {
                        byte* row = (byte*)bmData.Scan0 + (j * bmData.Stride);
                        for (int i = 0; i < bmData.Width; i++)
                        {
                            Red[i, j] = (double)Scale(0, 255, -1, 1, row[i * PixelSize + 2]);
                            Green[i, j] = (double)Scale(0, 255, -1, 1, row[i * PixelSize + 1]);
                            Blue[i, j] = (double)Scale(0, 255, -1, 1, row[i * PixelSize]);
                        }
                    }
                }
            }

            if (Forward)
            {
                FWT(Red, Iterations);
                FWT(Green, Iterations);
                FWT(Blue, Iterations);
            }
            else
            {
                IWT(Red, Iterations);
                IWT(Green, Iterations);
                IWT(Blue, Iterations);
            }

            if (Safe)
            {
                for (int j = 0; j < bmp.Height; j++)
                {
                    for (int i = 0; i < bmp.Width; i++)
                    {
                        bmp.SetPixel(i, j, Color.FromArgb((int)Scale(-1, 1, 0, 255, Red[i, j]), (int)Scale(-1, 1, 0, 255, Green[i, j]), (int)Scale(-1, 1, 0, 255, Blue[i, j])));
                    }
                }
            }
            else
            {
                unsafe
                {
                    for (int j = 0; j < bmData.Height; j++)
                    {
                        byte* row = (byte*)bmData.Scan0 + (j * bmData.Stride);
                        for (int i = 0; i < bmData.Width; i++)
                        {
                            row[i * PixelSize + 2] = (byte)Scale(-1, 1, 0, 255, Red[i, j]);
                            row[i * PixelSize + 1] = (byte)Scale(-1, 1, 0, 255, Green[i, j]);
                            row[i * PixelSize] = (byte)Scale(-1, 1, 0, 255, Blue[i, j]);
                        }
                    }
                }

                bmp.UnlockBits(bmData);
            }

            return bmp;
        }

        #region ApplyHaarTransform use

        private const double w0 = 0.5;
        private const double w1 = -0.5;
        private const double s0 = 0.5;
        private const double s1 = 0.5;

        /// <summary>
        ///   Discrete Haar Wavelet Transform
        /// </summary>
        /// 
        private static void FWT(double[] data)
        {
            double[] temp = new double[data.Length];

            int h = data.Length >> 1;
            for (int i = 0; i < h; i++)
            {
                int k = (i << 1);
                temp[i] = data[k] * s0 + data[k + 1] * s1;
                temp[i + h] = data[k] * w0 + data[k + 1] * w1;
            }

            for (int i = 0; i < data.Length; i++)
                data[i] = temp[i];
        }

        /// <summary>
        ///   Discrete Haar Wavelet 2D Transform
        /// </summary>
        /// 
        private static void FWT(double[,] data, int iterations)
        {
            int rows = data.GetLength(0);
            int cols = data.GetLength(1);

            double[] row;
            double[] col;

            for (int k = 0; k < iterations; k++)
            {
                int lev = 1 << k;

                int levCols = cols / lev;
                int levRows = rows / lev;

                row = new double[levCols];
                for (int i = 0; i < levRows; i++)
                {
                    for (int j = 0; j < row.Length; j++)
                        row[j] = data[i, j];

                    FWT(row);

                    for (int j = 0; j < row.Length; j++)
                        data[i, j] = row[j];
                }


                col = new double[levRows];
                for (int j = 0; j < levCols; j++)
                {
                    for (int i = 0; i < col.Length; i++)
                        col[i] = data[i, j];

                    FWT(col);

                    for (int i = 0; i < col.Length; i++)
                        data[i, j] = col[i];
                }
            }
        }

        /// <summary>
        ///   Inverse Haar Wavelet Transform
        /// </summary>
        /// 
        private static void IWT(double[] data)
        {
            double[] temp = new double[data.Length];

            int h = data.Length >> 1;
            for (int i = 0; i < h; i++)
            {
                int k = (i << 1);
                temp[k] = (data[i] * s0 + data[i + h] * w0) / w0;
                temp[k + 1] = (data[i] * s1 + data[i + h] * w1) / s0;
            }

            for (int i = 0; i < data.Length; i++)
                data[i] = temp[i];
        }

        /// <summary>
        ///   Inverse Haar Wavelet 2D Transform
        /// </summary>
        /// 
        private static void IWT(double[,] data, int iterations)
        {
            int rows = data.GetLength(0);
            int cols = data.GetLength(1);

            double[] col;
            double[] row;

            for (int k = iterations - 1; k >= 0; k--)
            {
                int lev = 1 << k;

                int levCols = cols / lev;
                int levRows = rows / lev;

                col = new double[levRows];
                for (int j = 0; j < levCols; j++)
                {
                    for (int i = 0; i < col.Length; i++)
                        col[i] = data[i, j];

                    IWT(col);

                    for (int i = 0; i < col.Length; i++)
                        data[i, j] = col[i];
                }

                row = new double[levCols];
                for (int i = 0; i < levRows; i++)
                {
                    for (int j = 0; j < row.Length; j++)
                        row[j] = data[i, j];

                    IWT(row);

                    for (int j = 0; j < row.Length; j++)
                        data[i, j] = row[j];
                }
            }
        }

        private static double Scale(double fromMin, double fromMax, double toMin, double toMax, double x)
        {
            if (fromMax - fromMin == 0) return 0;
            double value = (toMax - toMin) * (x - fromMin) / (fromMax - fromMin) + toMin;
            if (value > toMax)
            {
                value = toMax;
            }
            if (value < toMin)
            {
                value = toMin;
            }
            return value;
        }

        #endregion

        #endregion
    }
}
