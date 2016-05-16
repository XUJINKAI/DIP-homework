using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;

namespace ImageProcess
{
    public static class HSISpace
    {
        /// <summary>
        /// Binarization a bitmap on I channel
        /// </summary>
        /// <param name="b"></param>
        /// <param name="Threshold">threshold on (R+G+B), 0 to 755</param>
        /// <returns></returns>
        public static unsafe Bitmap BinarizationI(this Bitmap b, int Threshold)
        {
            double[] ks = new double[766];
            int[] offset = new int[766];
            for (int i = 0; i < 766; i++)
            {
                ks[i] = (i < Threshold) ? 0 : 1;
                offset[i] = (i < Threshold) ? 0 : 255;
            }
            return MultiplyIk(b, ks, offset);
        }

        /// <summary>
        /// Saturation adjust
        /// </summary>
        /// <param name="b"></param>
        /// <param name="delta"></param>
        /// <returns></returns>
        public static unsafe Bitmap Saturation(this Bitmap b, double delta)
        {
            b = b.Clone() as Bitmap;
            int Width = b.Width, Height = b.Height;
            BitmapData data = b.LockBits(new Rectangle(0, 0, Width, Height)
                , ImageLockMode.ReadWrite
                , PixelFormat.Format24bppRgb);
            int Stride = data.Stride;
            int x, y, i;
            double k = 1 + delta;
            byte* p0 = (byte*)data.Scan0;
            byte* p;
            int sum, min, max, minus, minn, maxn, maxmmin;
            for (y = 0; y < Height; y++)
            {
                p = p0 + y * Stride;
                for (x = 0; x < Width; x++)
                {
                    //略慢，效果不太好
                    sum = *p + *(p + 1) + *(p + 2);
                    min = (*p < *(p + 1)) ? *p : *(p + 1);
                    min = (min < *(p + 2)) ? min : *(p + 2);
                    max = (*p > *(p + 1)) ? *p : *(p + 1);
                    max = (min > *(p + 2)) ? max : *(p + 2);
                    if (max == min)
                    {
                        p = p + 3;
                        continue;
                    }
                    minus = (int)(delta * (sum - 3 * min) / 3);
                    minn = min - minus;
                    minn = ((minn < 0) ? 0 : minn);
                    maxmmin = max - min;
                    maxn = (int)(maxmmin * k + minn);
                    maxn = (maxn > 255) ? 255 : maxn;
                    for (i = 0; i < 3; i++)
                    {
                        if (*p == min)
                        {
                            *p = (byte)(minn);
                        }
                        else if (*p == max)
                        {
                            *p = (byte)(maxn);
                        }
                        else
                        {
                            *p = (byte)((*p - min) * (maxn - minn) / maxmmin + minn);
                        }
                        p++;
                    }
                }
            }
            b.UnlockBits(data);
            return b;
        }

        /// <summary>
        /// 对I分量做gamma变换
        /// </summary>
        /// <param name="b"></param>
        /// <param name="gamma"></param>
        /// <returns></returns>
        public static Bitmap GammaI(this Bitmap b, double gamma)
        {
            double[] ks = new double[766];
            int[] offset = new int[766];
            for (int i = 0; i < 766; i++)
            {
                ks[i] = Math.Pow(i / 765.0, 1.0 / gamma) * 765 / i;
                offset[i] = 0;
            }
            return MultiplyIk(b, ks, offset);
        }

        /// <summary>
        /// 对图像直方图做均衡化
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static unsafe Bitmap HistogramEqualizationI(this Bitmap b)
        {
            b = b.Clone() as Bitmap;
            int Width = b.Width, Height = b.Height;
            BitmapData data = b.LockBits(new Rectangle(0, 0, Width, Height)
                , ImageLockMode.ReadWrite
                , PixelFormat.Format24bppRgb);
            int Stride = data.Stride, Size = Width * Height;
            int x, y;
            byte* p0 = (byte*)data.Scan0;
            //hist
            int[] hist = new int[766];
            for (int i = 0; i < 256; i++) hist[i] = 0;
            fixed (int* phist0 = hist)
            {
                int* ph = phist0;
                byte* p;
                for (y = 0; y < Height; y++)
                {
                    p = p0 + y * Stride;
                    for (x = 0; x < Width; x++)
                    {
                        ph[*(p) + *(p + 1) + *(p + 2)]++;
                        p += 3;
                    }
                }
            }
            b.UnlockBits(data);
            //map
            double[] map = new double[766];
            int[] offset = new int[766];
            int count = 0;
            for (int i = 0; i < 766; i++)
            {
                count += hist[i];
                map[i] = 765.0 * count / Size / i;
                offset[i] = 0;
            }
            map[0] = 0;
            return MultiplyIk(b, map, offset);
        }



        #region Helper

        /// <summary>
        /// 按照给定的I分量对图像做映射，I=(R+G+B)。newI = I * ks[i] + offset[i]。
        /// </summary>
        /// <param name="b"></param>
        /// <param name="ks">长度为766，值为I=(R+G+B)对应要乘的系数。</param>
        /// <param name="offset">长度为766，乘以系数以后相加的值。</param>
        /// <returns></returns>
        public unsafe static Bitmap MultiplyIk(Bitmap b, double[] ks, int[] offset)
        {
            b = b.Clone() as Bitmap;
            BitmapData data = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite
                , PixelFormat.Format24bppRgb);
            byte* p0 = (byte*)data.Scan0;
            int Width = data.Width, Height = data.Height, Stride = data.Stride;
            int x, y;
            fixed (double* pm0 = ks)
            fixed (int* pOS0 = offset)
            {
                double* pm = pm0;
                int* pOff = pOS0;
                double k, t;
                int sum, off;
                byte* p;
                for (y = 0; y < Height; y++)
                {
                    p = p0 + y * Stride;
                    for (x = 0; x < Width; x++)
                    {
                        sum = *p + *(p + 1) + *(p + 2);
                        k = *(pm + sum);
                        off = *(pOff + sum);

                        t = *(p) * k + off;
                        t = (t > 255) ? 255 : t;
                        t = (t < 0) ? 0 : t;
                        *(p) = (byte)t;

                        t = *(p + 1) * k + off;
                        t = (t > 255) ? 255 : t;
                        t = (t < 0) ? 0 : t;
                        *(p + 1) = (byte)t;

                        t = *(p + 2) * k + off;
                        t = (t > 255) ? 255 : t;
                        t = (t < 0) ? 0 : t;
                        *(p + 2) = (byte)t;

                        p += 3;
                    }
                }
            }
            b.UnlockBits(data);
            return b;
        }




        //////////////////////////////////////////////////////////////////////////
        //以下两个函数将图像在HSI空间和RGB空间进行转换，但在实作中用处不大
        //因为换算涉及 double型变量和三角函数的计算，误差太大
        //////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// 将图像从rgb空间转到hsi空间。其中I分量为int矩阵，I=(R+G+B)，没有做除以3的处理；H分量为弧度制。
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static unsafe Tuple<double[,], double[,], int[,]> RGB2HSI_3(Bitmap b)
        {
            b = b.Clone() as Bitmap;
            int Width = b.Width, Height = b.Height;
            double[,] H = new double[Height, Width];
            double[,] S = new double[Height, Width];
            int[,] I = new int[Height, Width];
            BitmapData data = b.LockBits(new Rectangle(0, 0, Width, Height)
                , ImageLockMode.ReadWrite
                , PixelFormat.Format24bppRgb);
            int Stride = data.Stride;
            int x, y;
            byte* p = (byte*)data.Scan0;
            byte* pr, pg, pb;
            double* ph, ps;
            int* pi;
            double theta;
            int mRG, mRB, mGB, min;
            const double con_PI = 3.14159265358979323846; //180度
            const double deg360 = con_PI * 2;
            fixed (double* ph0 = H)
            {
                fixed (double* ps0 = S)
                {
                    fixed (int* pi0 = I)
                    {
                        ph = ph0;
                        ps = ps0;
                        pi = pi0;
                        //start loop
                        for (y = 0; y < Height; y++)
                        {
                            pb = p + y * Stride;
                            pg = pb + 1;
                            pr = pg + 1;
                            for (x = 0; x < Width; x++)
                            {
                                mRG = *pr - *pg;
                                mRB = *pr - *pb;
                                mGB = *pg - *pb;
                                theta = Math.Acos((0.5 * (mRG + mRB)) / Math.Sqrt(mRG * mRG + mRB * mGB));
                                min = (*pr < *pg) ? *pr : *pg;
                                min = (min < *pb) ? min : *pb;

                                *ph = (*pb <= *pg) ? theta : (deg360 - theta);
                                *pi = (*pr + *pg + *pb);
                                *ps = 1.0 - 3.0 * min / *pi;

                                pr += 3; pg += 3; pb += 3;
                                ph++; ps++; pi++;
                            }
                        }//for y
                    }//fixed
                }
            }
            b.UnlockBits(data);
            return new Tuple<double[,], double[,], int[,]>(H, S, I);
        }

        /// <summary>
        /// 将HSI三个分量合成bitmap图像；其中I分量为R+G+B，没有除以3；H分量为弧度制。
        /// </summary>
        /// <param name="H"></param>
        /// <param name="S"></param>
        /// <param name="I"></param>
        /// <returns></returns>
        public static unsafe Bitmap HSI2RGB_3(double[,] H, double[,] S, int[,] I)
        {
            int Height = H.GetLength(0), Width = H.GetLength(1);
            Bitmap b = new Bitmap(Width, Height);
            BitmapData data = b.LockBits(new Rectangle(0, 0, Width, Height)
                , ImageLockMode.ReadWrite
                , PixelFormat.Format24bppRgb);
            int Stride = data.Stride;
            int x, y;
            byte* p = (byte*)data.Scan0;
            byte* pr, pg, pb;
            double* ph, ps;
            int* pi;
            const double con_PI = 3.14159265358979323846; //180度
            const double deg60 = con_PI / 3, deg120 = deg60 * 2, deg240 = deg60 * 4;
            fixed (double* ph0 = H)
            {
                fixed (double* ps0 = S)
                {
                    fixed (int* pi0 = I)
                    {
                        ph = ph0;
                        ps = ps0;
                        pi = pi0;
                        //start loop
                        for (y = 0; y < Height; y++)
                        {
                            pb = p + y * Stride;
                            pg = pb + 1;
                            pr = pg + 1;
                            for (x = 0; x < Width; x++)
                            {
                                //注意：i = I * 3
                                if (*ph < deg120) // R/G sector
                                {
                                    *pb = (byte)(*pi * (1 - *ps) / 3);
                                    *pr = (byte)(*pi * (1 + *ps * Math.Cos(*ph) / Math.Cos(deg60 - *ph)) / 3);
                                    *pg = (byte)(*pi - (*pr + *pb));
                                }
                                else if (*ph >= deg240) // B/R sector
                                {
                                    *ph = *ph - deg240;
                                    *pg = (byte)(*pi * (1 - *ps) / 3);
                                    *pb = (byte)(*pi * (1 + *ps * Math.Cos(*ph) / Math.Cos(deg60 - *ph)) / 3);
                                    *pr = (byte)(*pi - (*pg + *pb));
                                }
                                else if (double.IsNaN(*ph))
                                {
                                    *pr = *pg = *pb = (byte)(*pi / 3);
                                }
                                else // G/B sector
                                {
                                    *ph = *ph - deg120;
                                    *pr = (byte)(*pi * (1 - *ps) / 3);
                                    *pg = (byte)(*pi * (1 + *ps * Math.Cos(*ph) / Math.Cos(deg60 - *ph)) / 3);
                                    *pb = (byte)(*pi - (*pr + *pg));
                                }

                                pr += 3; pg += 3; pb += 3;
                                ph++; ps++; pi++;
                            }
                        }//for y
                    }//fixed
                }
            }
            b.UnlockBits(data);
            return b;
        }

        #endregion
    }
}
