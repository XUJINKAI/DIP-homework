using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;

namespace ImageProcess
{
    public static class RGBSpace
    {
        /// <summary>
        /// Binarization a bitmap RGB separately
        /// </summary>
        /// <param name="b"></param>
        /// <param name="ThresholdR"></param>
        /// <param name="ThresholdG"></param>
        /// <param name="ThresholdB"></param>
        /// <returns></returns>
        public static unsafe Bitmap BinarizationRGB(this Bitmap b, byte ThresholdR, byte ThresholdG, byte ThresholdB)
        {
            byte[] mapR = new byte[256];
            byte[] mapG = new byte[256];
            byte[] mapB = new byte[256];
            for (int i = 0; i < 256; i++)
            {
                mapR[i] = (byte)((i < ThresholdR) ? 0 : 255);
                mapG[i] = (byte)((i < ThresholdG) ? 0 : 255);
                mapB[i] = (byte)((i < ThresholdB) ? 0 : 255);
            }
            return MappingRGB(b, mapR, mapG, mapB);
        }

        /// <summary>
        /// return a gray bitmap version of b
        /// </summary>
        /// <param name="b"></param>
        public unsafe static Bitmap GrayScale(Bitmap b)
        {
            b = b.Clone() as Bitmap;
            BitmapData data = b.LockBits(new Rectangle(0, 0, b.Width, b.Height)
                , ImageLockMode.ReadWrite
                , PixelFormat.Format24bppRgb);
            byte* p = (byte*)data.Scan0;
            for (int i = 0; i < data.Width * data.Height; i++)//因为是逐像素处理，所以可不考虑 stride offset
            {
                //*p = *(p + 1) = *(p + 2) = (byte)((*p + *(p + 1) + *(p + 2)) / 3);
                p[0] = p[1] = p[2] = (byte)(.299 * p[2] + .587 * p[1] + .114 * p[0]);
                p += 3;
            }
            b.UnlockBits(data);
            return b;
        }

        /// <summary>
        /// add a number to each color
        /// </summary>
        /// <param name="b"></param>
        /// <param name="red"></param>
        /// <param name="green"></param>
        /// <param name="blue"></param>
        public static unsafe Bitmap ColorShift(this Bitmap b, int red, int green, int blue)
        {
            byte[] hashR = new byte[256];
            byte[] hashG = new byte[256];
            byte[] hashB = new byte[256];
            for (int i = 0; i < 256; i++)
            {
                hashR[i] = (byte)Math.Max(Math.Min(i + red, 255), 0);
                hashG[i] = (byte)Math.Max(Math.Min(i + green, 255), 0);
                hashB[i] = (byte)Math.Max(Math.Min(i + blue, 255), 0);
            }
            return MappingRGB(b, hashR, hashG, hashB);
        }

        /// <summary>
        /// Contrast adjust
        /// </summary>
        /// <param name="b"></param>
        /// <param name="nContrast">-1 to 1</param>
        /// <returns></returns>
        public static Bitmap Contrast(this Bitmap b, double Contrast, int Threshold
            , bool doRed = true, bool doGreen = true, bool doBlue = true)
        {
            byte[] hash = new byte[256];
            byte[] hashN = new byte[256];
            Contrast *= 100;
            double pix, nContrast = (Contrast > 0) ? (255 * 255.0 / (255 - Contrast) - 255) : Contrast;
            for (int i = 0; i < 256; i++)
            {
                pix = (i + (i - Threshold) * nContrast / 255);
                if (pix < 0) pix = 0; if (pix > 255) pix = 255;
                hash[i] = (byte)pix;
                hashN[i] = (byte)i;
            }
            return MappingRGB(b, doRed ? hash : hashN, doGreen ? hash : hashN, doBlue ? hash : hashN);
        }

        /// <summary>
        /// Gamma correction
        /// </summary>
        /// <param name="b"></param>
        /// <param name="gammaR"></param>
        /// <param name="gammaG"></param>
        /// <param name="gammaB"></param>
        /// <returns></returns>
        public static Bitmap GammaRGB(this Bitmap b, double gammaR, double gammaG, double gammaB)
        {
            byte[] hashR = new byte[256];
            byte[] hashG = new byte[256];
            byte[] hashB = new byte[256];
            for (int i = 0; i < 256; i++)
            {
                hashR[i] = (byte)(255.0 * Math.Pow(i / 255.0, 1.0 / gammaR));
                hashG[i] = (byte)(255.0 * Math.Pow(i / 255.0, 1.0 / gammaG));
                hashB[i] = (byte)(255.0 * Math.Pow(i / 255.0, 1.0 / gammaB));
            }
            return MappingRGB(b, hashR, hashG, hashB);
        }

        /// <summary>
        /// Histogram Equalization to RGB separately
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Bitmap HistogramEqualizationRGB(this Bitmap b
            , bool doRed = true, bool doGreen = true, bool doBlue = true)
        {
            int[] map = new int[256];
            for (int i = 0; i < 256; i++)
            {
                map[i] = 1;
            }
            return HistogramMatchingRGB(b
                , doRed ? map : null, doGreen ? map : null, doBlue ? map : null);
        }

        /// <summary>
        /// Histogram Matching bitmap b to bmp
        /// </summary>
        /// <param name="b"></param>
        /// <param name="bmp"></param>
        /// <returns></returns>
        public static Bitmap HistogramMatchingRGB(this Bitmap b, Bitmap bmp)
        {
            HistRGB hist = HistCount(bmp);
            return HistogramMatchingRGB(b, hist.HistR, hist.HistG, hist.HistB);
        }

        /// <summary>
        /// Histogram Matching bitmap b
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Bitmap HistogramMatchingRGB(this Bitmap b
            , int[] matchHistR, int[] matchHistG, int[] matchHistB)
        {
            HistRGB histRGB = HistCount(b);
            int histSum = histRGB.Sum;
            byte[] mapN = new byte[256];
            for (int i = 0; i < 256; i++)
            {
                mapN[i] = (byte)i;
            }
            byte[] mapR = (matchHistR == null) ? mapN : MapFunc256(histRGB.HistR, histSum, CIN256(matchHistR));
            byte[] mapG = (matchHistG == null) ? mapN : MapFunc256(histRGB.HistG, histSum, CIN256(matchHistG));
            byte[] mapB = (matchHistB == null) ? mapN : MapFunc256(histRGB.HistB, histSum, CIN256(matchHistB));
            return MappingRGB(b, mapR, mapG, mapB);
        }

        /// <summary>
        /// 将分布图按照给定的函数映射到256*256的尺寸
        /// </summary>
        /// <param name="hist"></param>
        /// <param name="histSum"></param>
        /// <param name="gInv"></param>
        /// <returns></returns>
        private static byte[] MapFunc256(int[] hist, int histSum, byte[] gInv)
        {
            byte[] map = new byte[256];
            int count = 0;
            for (int i = 0; i < 256; i++)
            {
                count += hist[i];
                map[i] = gInv[(int)(255.0 * count / histSum)];
            }
            return map;
        }

        /// <summary>
        /// 将分布图累积(cumulative)、翻转(inverse)、规整(norm)到256*256的尺寸
        /// </summary>
        /// <param name="hist"></param>
        /// <returns></returns>
        private static byte[] CIN256(int[] hist)
        {
            int i, x;
            double iy, xScale, yScale;
            byte[] gInvR = new byte[256];
            //cumulative
            int size = hist.Length;
            int count = 0;
            int[] histCumu=new int[size];
            for (i = 0; i < size; i++)
            {
                count += hist[i];
                histCumu[i] = count;
            }
            //inverse to 256
            xScale = size / 256.0;
            yScale = count / 256.0;
            x = 0;
            for (i = 0; i < 256; i++)
            {
                iy = i * yScale;
                while (x < 256 && histCumu[x] <= iy)
                {
                    x++;
                }
                gInvR[i] = (byte)(x / xScale);
            }
            return gInvR;
        }

        /// <summary>
        /// 返回图像三个颜色的直方图
        /// </summary>
        /// <param name="b"></param>
        /// <param name="scale">直方图高度的比例，scale越大可显示的动态范围越大，scale为0时自动显示，一般定义为4到10左右。</param>
        /// <returns></returns>
        public static unsafe Bitmap HistBitmap(this Bitmap b, double scale = 10)
        {
            const int Width = 256, Height = 256;
            HistRGB hist = HistCount(b);
            int[] histR = hist.HistR;
            int[] histG = hist.HistG;
            int[] histB = hist.HistB;
            int Size = hist.Sum;
            Bitmap bhist = new Bitmap(Width, Height);
            BitmapData data = bhist.LockBits(new Rectangle(0, 0, Width, Height)
                , ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            int Stride = data.Stride;
            byte* p0 = (byte*)data.Scan0;
            int x, y;
            int NR, NG, NB;
            byte* pr, pg, pb;
            double kppp; //k pixels per pixel stand for
            if (scale == 0)
            {
                int Max = 0;
                for (int i = 0; i < 256; i++)
                {
                    Max = (Max > histR[i]) ? Max : histR[i];
                    Max = (Max > histG[i]) ? Max : histG[i];
                    Max = (Max > histB[i]) ? Max : histB[i];
                }
                kppp = Max / 256.0;
            }
            else
            {
                kppp = scale * Size / (Width * Height);
            }
            for (y = 0; y < Height; y++)
            {
                pb = p0 + y * Stride;
                pg = pb + 1;
                pr = pg + 1;
                NR = (int)(histR[y] / kppp);
                NG = (int)(histG[y] / kppp);
                NB = (int)(histB[y] / kppp);
                for (x = 0; x < Width; x++)
                {
                    *pr = (byte)((x < NR) ? 255 : 0);
                    *pg = (byte)((x < NG) ? 255 : 0);
                    *pb = (byte)((x < NB) ? 255 : 0);
                    pb += 3; pg += 3; pr += 3;
                }
            }
            bhist.UnlockBits(data);
            bhist.RotateFlip(RotateFlipType.Rotate270FlipNone);
            return bhist;
        }

        /// <summary>
        /// 计算图像直方图信息，返回三个长度为256的数组记录像素信息
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static unsafe HistRGB HistCount(Bitmap b)
        {
            int Width = b.Width, Height = b.Height;
            BitmapData data = b.LockBits(new Rectangle(0, 0, Width, Height)
                , ImageLockMode.ReadWrite
                , PixelFormat.Format24bppRgb);
            int Stride = data.Stride, Size = Width * Height;
            int x, y;
            byte* p0 = (byte*)data.Scan0;
            int[] histR = new int[256];
            int[] histG = new int[256];
            int[] histB = new int[256];
            for (int i = 0; i < 256; i++) histR[i] = histG[i] = histB[i] = 0;
            fixed (int* phR0 = histR, phG0 = histG, phB0 = histB)
            {
                int* phR = phR0, phG = phG0, phB = phB0;
                byte* p;
                for (y = 0; y < Height; y++)
                {
                    p = p0 + y * Stride;
                    for (x = 0; x < Width; x++)
                    {
                        phR[*(p + 2)]++;
                        phG[*(p + 1)]++;
                        phB[*(p)]++;
                        p += 3;
                    }
                }
            }
            b.UnlockBits(data);
            return new HistRGB(histR, histG, histB, Size);
        }

        /// <summary>
        /// 按照给定的RGB分量数组对图像像素做映射。三个数组大小均为256。
        /// </summary>
        /// <param name="b"></param>
        /// <param name="mapR"></param>
        /// <param name="mapG"></param>
        /// <param name="mapB"></param>
        /// <returns></returns>
        public unsafe static Bitmap MappingRGB(Bitmap b, byte[] mapR, byte[] mapG, byte[] mapB)
        {
            b = b.Clone() as Bitmap;
            BitmapData data = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite
                , PixelFormat.Format24bppRgb);
            byte* p0 = (byte*)data.Scan0;
            int Width = data.Width, Height = data.Height, Stride = data.Stride;
            int x, y;
            fixed (byte* tpr = mapR, tpg = mapG, tpb = mapB)
            {
                byte* pR = tpr, pG = tpg, pB = tpb, p;
                for (y = 0; y < Height; y++)
                {
                    p = p0 + y * Stride;
                    for (x = 0; x < Width; x++)
                    {
                        *(p) = pB[*(p)];
                        *(p + 1) = pG[*(p + 1)];
                        *(p + 2) = pR[*(p + 2)];
                        p += 3;
                    }
                }
            }
            b.UnlockBits(data);
            return b;
        }

    }
    
    /// <summary>
    /// 三个int数组
    /// </summary>
    public class HistRGB : Tuple<int[], int[], int[], int>
    {
        public HistRGB(int[] histR, int[] histG, int[] histB, int sum)
            : base(histR, histG, histB, sum)
        { }

        public int[] HistR { get { return this.Item1; } }
        public int[] HistG { get { return this.Item2; } }
        public int[] HistB { get { return this.Item3; } }
        public int Sum { get { return this.Item4; } }
    }

}
