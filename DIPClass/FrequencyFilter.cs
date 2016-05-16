using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Drawing.Imaging;
using FFTW;

namespace ImageProcess
{
    public static class FrequencyFilter
    {
        //////////////////////////////////////////////////////////////////////////
        // 转换到频域再转回来有误差，连续多次使用IHPF再使用ILPF不能恢复原图，待排查//
        //////////////////////////////////////////////////////////////////////////

        #region LPF
        public static Bitmap IdealLPF(this Bitmap b, int radius)
        {
            int dist2 = b.Width * b.Width + b.Height * b.Height;
            double[] ks = IdealLFunc(dist2, radius, 1, 0);
            return IFFT(MultiplyFk(FFT(b), ks, ks, ks));
        }

        public static Bitmap ButterworthLPF(this Bitmap b, int radius, int n)
        {
            int dist2 = b.Width * b.Width + b.Height * b.Height;
            double[] ks = ButterworthLFunc(dist2, radius, n, 1, 0);
            return IFFT(MultiplyFk(FFT(b), ks, ks, ks));
        }

        public static Bitmap GaussianLPF(this Bitmap b, int radius)
        {
            int dist2 = b.Width * b.Width + b.Height * b.Height;
            double[] ks = GaussianLFunc(dist2, radius, 1, 0);
            return IFFT(MultiplyFk(FFT(b), ks, ks, ks));
        }
        #endregion

        #region HPF

        public static Bitmap IdealHPF(this Bitmap b, int radius)
        {
            int dist2 = b.Width * b.Width + b.Height * b.Height;
            double[] ks = IdealLFunc(dist2, radius, -1, 2);
            return IFFT(MultiplyFk(FFT(b), ks, ks, ks));
        }

        public static Bitmap ButterworthHPF(this Bitmap b, int radius, int n)
        {
            int dist2 = b.Width * b.Width + b.Height * b.Height;
            double[] ks = ButterworthLFunc(dist2, radius, n, -1, 2);
            return IFFT(MultiplyFk(FFT(b), ks, ks, ks));
        }

        public static Bitmap GaussianHPF(this Bitmap b, int radius)
        {
            int dist2 = b.Width * b.Width + b.Height * b.Height;
            double[] ks = GaussianLFunc(dist2, radius, -1, 2);
            return IFFT(MultiplyFk(FFT(b), ks, ks, ks));
        }
        #endregion

        #region common array

        /// <summary>
        /// 产生特定半径的ILPF函数，下标为距离的平方，并将函数乘以multiK后再加上addD。
        /// 比如(multiK,addD)为(1,0)时产生ILPF函数；为(-1,2)则为IHPF函数。
        /// </summary>
        /// <param name="distSquare"></param>
        /// <param name="D0"></param>
        /// <param name="multiK"></param>
        /// <param name="addD"></param>
        /// <returns></returns>
        public static double[] IdealLFunc(int distSquare, int D0, double multiK, double addD)
        {
            double[] ks = new double[distSquare];
            int rad2 = D0 * D0;
            for (int i = 0; i < distSquare; i++)
            {
                ks[i] = (i < rad2) ? (multiK + addD) : addD; //n * multiK + addD
            }
            return ks;
        }

        public static double[] ButterworthLFunc(int distSquare, int D0, int n, double multiK, double addD)
        {
            double[] ks = new double[distSquare];
            int rad2 = D0 * D0;
            for (int i = 0; i < distSquare; i++)
            {
                ks[i] = 1.0 / (1 + Math.Pow(1.0 * i / rad2, n)) * multiK + addD;
            }
            return ks;
        }

        public static double[] GaussianLFunc(int distSquare, int D0, double multiK, double addD)
        {
            double[] ks = new double[distSquare];
            int rad22 = 2 * D0 * D0;
            for (int i = 0; i < distSquare; i++)
            {
                ks[i] = Math.Exp(1.0 * -i / rad22) * multiK + addD;
            }
            return ks;
        }
        #endregion

        #region Helper

        /// <summary>
        /// 将图像b转换到频域
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static unsafe ComplexRGB FFT(Bitmap b)
        {
            int Width = b.Width, Height = b.Height;
            double[,] dR = new double[Height, Width];
            double[,] dG = new double[Height, Width];
            double[,] dB = new double[Height, Width];
            BitmapData data = b.LockBits(new Rectangle(0, 0, Width, Height)
                , ImageLockMode.ReadWrite
                , PixelFormat.Format24bppRgb);
            int Stride = data.Stride;
            int x, y, k;
            byte* pdata0 = (byte*)data.Scan0, src;
            fixed (double* pdR0 = dR, pdG0 = dG, pdB0 = dB)
            {
                //使用中间变量速度会更快（原因不明）
                double* pr = pdR0, pg = pdG0, pb = pdB0;
                //start calculate
                for (y = 0; y < Height; y++)
                {
                    src = pdata0 + y * Stride;
                    k = (y % 2 == 0) ? 1 : -1;
                    for (x = 0; x < Width; x++)
                    {
                        *pr = *(src + 2) * k;
                        *pg = *(src + 1) * k;
                        *pb = *src * k;

                        pr++; pg++; pb++;
                        src += 3;
                        k = -k;
                    }
                }
            }
            b.UnlockBits(data);
            Complex2 fred = FFTClass.FFT2(dR);
            Complex2 fgreen = FFTClass.FFT2(dG);
            Complex2 fblue = FFTClass.FFT2(dB);
            return new ComplexRGB(fred.Real, fred.Imag, fgreen.Real, fgreen.Imag, fblue.Real, fblue.Imag);
        }

        /// <summary>
        /// 从频域转换回图像
        /// </summary>
        /// <param name="cRGB"></param>
        /// <returns></returns>
        public static unsafe Bitmap IFFT(ComplexRGB cRGB)
        {
            Complex2 c2red = FFTClass.IFFT2(cRGB.RedRe, cRGB.RedIm);
            Complex2 c2green = FFTClass.IFFT2(cRGB.GreenRe, cRGB.GreenIm);
            Complex2 c2blue = FFTClass.IFFT2(cRGB.BlueRe, cRGB.BlueIm);
            double[,] dR = c2red.Real;
            double[,] dG = c2green.Real;
            double[,] dB = c2blue.Real;
            int Height = dR.GetLength(0), Width = dR.GetLength(1), Size = Width * Height;
            Bitmap b = new Bitmap(Width, Height);
            BitmapData data = b.LockBits(new Rectangle(0, 0, Width, Height)
                , ImageLockMode.ReadWrite
                , PixelFormat.Format24bppRgb);
            int Stride = data.Stride;
            int x, y, k;
            double tmp;
            byte* pdata0 = (byte*)data.Scan0, dst;
            fixed (double* pdR0 = dR, pdG0 = dG, pdB0 = dB)
            {
                //使用中间变量速度会更快（原因不明）
                double* pr = pdR0, pg = pdG0, pb = pdB0;
                //start calculate
                for (y = 0; y < Height; y++)
                {
                    dst = pdata0 + y * Stride;
                    k = (y % 2 == 0) ? 1 : -1;
                    for (x = 0; x < Width; x++)
                    {
                        tmp = *pb * k / Size;
                        if (tmp < 0) tmp = 0;
                        if (tmp > 255) tmp = 255;
                        *dst = (byte)tmp;

                        tmp = *pg * k / Size;
                        if (tmp < 0) tmp = 0;
                        if (tmp > 255) tmp = 255;
                        *(dst + 1) = (byte)tmp;

                        tmp = *pr * k / Size;
                        if (tmp < 0) tmp = 0;
                        if (tmp > 255) tmp = 255;
                        *(dst + 2) = (byte)tmp;

                        pr++; pg++; pb++;
                        dst += 3;
                        k = -k;
                    }
                }
            }
            b.UnlockBits(data);
            return b;
        }

        /// <summary>
        /// 显示幅值图
        /// </summary>
        /// <param name="cRGB"></param>
        /// <param name="multiplier">显示系数，默认为1</param>
        /// <returns></returns>
        public static unsafe Bitmap GetMagnitudeBitmap(ComplexRGB cRGB, double multiplier = 1.0)
        {
            int Height = cRGB.RedRe.GetLength(0), Width = cRGB.RedRe.GetLength(1);
            Bitmap bf = new Bitmap(Width, Height);
            BitmapData data = bf.LockBits(new Rectangle(0, 0, Width, Height)
                , ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            int Stride = data.Stride;
            byte* p0 = (byte*)data.Scan0, pr, pg, pb;
            int x, y;
            double t;
            fixed (double* pdRR0 = cRGB.RedRe, pdRI0 = cRGB.RedIm, pdGR0 = cRGB.GreenRe, pdGI0 = cRGB.GreenIm
                , pdBR0 = cRGB.BlueRe, pdBI0 = cRGB.BlueIm)
            {
                double* prr = pdRR0, pri = pdRI0, pgr = pdGR0, pgi = pdGI0, pbr = pdBR0, pbi = pdBI0;
                for (y = 0; y < Height; y++)
                {
                    pb = p0 + y * Stride;
                    pg = pb + 1;
                    pr = pg + 1;
                    for (x = 0; x < Width; x++)
                    {
                        t = Math.Sqrt(*prr * *prr + *pri * *pri) * multiplier;
                        t = (t > 255) ? 255 : t;
                        t = (t < 0) ? 0 : t;
                        *pr = (byte)t;

                        t = Math.Sqrt(*pgr * *pgr + *pgi * *pgi) * multiplier;
                        t = (t > 255) ? 255 : t;
                        t = (t < 0) ? 0 : t;
                        *pg = (byte)t;

                        t = Math.Sqrt(*pbr * *pbr + *pbi * *pbi) * multiplier;
                        t = (t > 255) ? 255 : t;
                        t = (t < 0) ? 0 : t;
                        *pb = (byte)t;

                        pb += 3; pg += 3; pr += 3;
                        prr++; pri++; pgr++; pgi++; pbr++; pbi++;
                    }
                }
            }
            bf.UnlockBits(data);
            return bf;
        }

        /// <summary>
        /// 对RGB频率分量分别乘以滤镜，kR2[dist^2]=k...
        /// </summary>
        /// <param name="cRGB"></param>
        /// <param name="kR2"></param>
        /// <param name="kG2"></param>
        /// <param name="kB2"></param>
        /// <returns></returns>
        public static unsafe ComplexRGB MultiplyFk(ComplexRGB cRGB, double[] kR2, double[] kG2, double[] kB2)
        {
            cRGB = cRGB.Clone() as ComplexRGB;
            int Height = cRGB.RedRe.GetLength(0), Width = cRGB.RedRe.GetLength(1);
            int hfHt = Height / 2, hfWd = Width / 2, centerShift = hfHt * Width + hfWd;
            int x, y, d2, dy2, yWd;
            double kr, kg, kb;
            fixed (double* pdRR0 = cRGB.RedRe, pdRI0 = cRGB.RedIm
                         , pdGR0 = cRGB.GreenRe, pdGI0 = cRGB.GreenIm
                         , pdBR0 = cRGB.BlueRe, pdBI0 = cRGB.BlueIm
                         , pdKR2 = kR2, pdKG2 = kG2, pdKB2 = kB2)
            {
                double* pr1 = pdRR0 + centerShift, pr2 = pdRI0 + centerShift
                      , pg1 = pdGR0 + centerShift, pg2 = pdGI0 + centerShift
                      , pb1 = pdBR0 + centerShift, pb2 = pdBI0 + centerShift
                      , pkr = pdKR2, pkg = pdKG2, pkb = pdKB2;
                for (y = 0; y < hfHt; y++)
                {
                    dy2 = y * y;
                    yWd = y * Width;
                    for (x = 0; x < hfWd; x++)
                    {
                        d2 = dy2 + x * x;
                        kr = *(pkr + d2);
                        kg = *(pkg + d2);
                        kb = *(pkb + d2);
                        //pr1
                        *(pr1 + yWd + x) *= kr;
                        *(pr1 + yWd - x) *= kr;
                        *(pr1 - yWd + x) *= kr;
                        *(pr1 - yWd - x) *= kr;
                        //pr2
                        *(pr2 + yWd + x) *= kr;
                        *(pr2 + yWd - x) *= kr;
                        *(pr2 - yWd + x) *= kr;
                        *(pr2 - yWd - x) *= kr;
                        //pg1
                        *(pg1 + yWd + x) *= kg;
                        *(pg1 + yWd - x) *= kg;
                        *(pg1 - yWd + x) *= kg;
                        *(pg1 - yWd - x) *= kg;
                        //pg2
                        *(pg2 + yWd + x) *= kg;
                        *(pg2 + yWd - x) *= kg;
                        *(pg2 - yWd + x) *= kg;
                        *(pg2 - yWd - x) *= kg;
                        //pb1
                        *(pb1 + yWd + x) *= kb;
                        *(pb1 + yWd - x) *= kb;
                        *(pb1 - yWd + x) *= kb;
                        *(pb1 - yWd - x) *= kb;
                        //pb2
                        *(pb2 + yWd + x) *= kb;
                        *(pb2 + yWd - x) *= kb;
                        *(pb2 - yWd + x) *= kb;
                        *(pb2 - yWd - x) *= kb;
                    }
                }
            }
            return cRGB;
        }

        #endregion
    }

    /// <summary>
    /// 用于储存RGB三个分量傅里叶变换后的六个矩阵
    /// </summary>
    public class ComplexRGB : Tuple<double[,], double[,], double[,], double[,], double[,], double[,]>
    {
        public ComplexRGB(double[,] RedRe, double[,] RedIm
            , double[,] GreenRe, double[,] GreenIm, double[,] BlueRe, double[,] BlueIm)
            : base(RedRe, RedIm, GreenRe, GreenIm, BlueRe, BlueIm)
        { }

        public double[,] RedRe { get { return this.Item1; } }
        public double[,] RedIm { get { return this.Item2; } }
        public double[,] GreenRe { get { return this.Item3; } }
        public double[,] GreenIm { get { return this.Item4; } }
        public double[,] BlueRe { get { return this.Item5; } }
        public double[,] BlueIm { get { return this.Item6; } }

        public object Clone()
        {
            return new ComplexRGB(this.RedRe.Clone() as double[,], this.RedIm.Clone() as double[,]
                                , this.GreenRe.Clone() as double[,], this.GreenIm.Clone() as double[,]
                                , this.BlueRe.Clone() as double[,], this.BlueIm.Clone() as double[,]);
        }
    }
}
