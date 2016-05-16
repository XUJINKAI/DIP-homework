using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;

namespace ImageProcess
{
    public static class SpatialFilter
    {

        #region Correlation

        /// <summary>
        /// use a mask matrix to do a correlation to bitmap b, divisor can't be 0.
        /// </summary>
        /// <param name="b"></param>
        /// <param name="mask"></param>
        /// <param name="divisor"></param>
        public static unsafe Bitmap Correlation(this Bitmap b, int[,] mask, int divisor)
        {
            b = b.Clone() as Bitmap;
            int Height = b.Height, Width = b.Width
                , maskHeight = mask.GetLength(0), maskWidth = mask.GetLength(1);
            int padWidth = maskWidth / 2, padHeight = maskHeight / 2;

            //padding to a byte array
            byte[] dataCopy = BitmapPadding(b, padWidth, padHeight);

            BitmapData data = b.LockBits(new Rectangle(0, 0, b.Width, b.Height)
                , ImageLockMode.ReadWrite
                , PixelFormat.Format24bppRgb);
            int Stride = data.Stride, StrideC = (int)(((Width + padWidth * 2) * 3 + 3) & 0XFFFFFFFC);

            int x, y, i, nx, ny, ni, nMi, tmp;
            byte* p = (byte*)data.Scan0;
            fixed (byte* pC0 = dataCopy)
            {
                fixed (int* pM0 = mask)
                {
                    //使用中间变量速度会更快（原因不明）
                    byte* pC = pC0;
                    int* pM = pM0;
                    //position of pixels
                    byte* dst;
                    //每一行的地址，maskHeight行同步推进
                    byte*[] src = new byte*[maskHeight];
                    //start calculate
                    for (y = 0; y < Height; y++)
                    {
                        dst = p + y * Stride;
                        src[0] = pC + y * StrideC;
                        for (i = 1; i < maskHeight; i++)
                        {
                            src[i] = src[i - 1] + StrideC;
                        }
                        for (x = 0; x < Stride; x++)
                        {
                            //最小值不会超出范围故不做检查
                            tmp = 0;
                            nMi = 0;
                            for (ny = 0; ny < maskHeight; ny++)
                            {
                                ni = 0;
                                for (nx = 0; nx < maskWidth; nx++)
                                {
                                    tmp += *(src[ny] + ni) * *(pM + nMi);
                                    ni += 3;
                                    nMi++;
                                }
                            }
                            tmp /= divisor;
                            tmp = (tmp > 255) ? 255 : tmp;
                            tmp = (tmp < 0) ? 0 : tmp;
                            *dst = (byte)tmp;
                            dst++;
                            for (i = 0; i < maskHeight; i++)
                            {
                                src[i]++;
                            }
                        }//for x
                    }//for y
                }//fixed mask
            }//fixed dataCopy
            b.UnlockBits(data);
            return b;
        }

        /// <summary>
        /// a quick way to achieve 3X3 Mean Filter
        /// </summary>
        /// <param name="b"></param>
        public static unsafe Bitmap Mean3Filter(this Bitmap b)
        {
            b = b.Clone() as Bitmap;
            int padWidth = 1, padHeight = 1;
            int Height = b.Height, Width = b.Width;

            //padding to a byte array
            byte[] dataCopy = BitmapPadding(b, padWidth, padHeight);

            BitmapData data = b.LockBits(new Rectangle(0, 0, b.Width, b.Height)
                , ImageLockMode.ReadWrite
                , PixelFormat.Format24bppRgb);
            int Stride = data.Stride, StrideC = (int)(((Width + padWidth * 2) * 3 + 3) & 0XFFFFFFFC);

            int x, y;
            byte* p = (byte*)data.Scan0;
            fixed (byte* pC0 = dataCopy)
            {
                //使用中间变量速度会更快（原因不明）
                byte* pC = pC0;
                //position of pixels
                byte* dst, src0, src1, src2;
                //start calculate
                for (y = 0; y < Height; y++)
                {
                    dst = p + y * Stride;
                    src0 = pC + y * StrideC;
                    src1 = src0 + StrideC;
                    src2 = src1 + StrideC;
                    for (x = 0; x < Stride; x++)
                    {
                        //平均值不会超出范围故不做检查
                        *dst = (byte)((*src0 + *(src0 + 3) + *(src0 + 6)
                                     + *src1 + *(src1 + 3) + *(src1 + 6)
                                     + *src2 + *(src2 + 3) + *(src2 + 6)) / 9);
                        dst++;
                        src0++; src1++; src2++;
                    }
                }
            }
            b.UnlockBits(data);
            return b;
        }

        /// <summary>
        /// unsharp filter using mean filter
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static unsafe Bitmap UnsharpFilter(this Bitmap b)
        {
            b = b.Clone() as Bitmap;
            int padWidth = 1, padHeight = 1;
            int Height = b.Height, Width = b.Width;

            //padding to a byte array
            byte[] dataCopy = BitmapPadding(b, padWidth, padHeight);

            BitmapData data = b.LockBits(new Rectangle(0, 0, b.Width, b.Height)
                , ImageLockMode.ReadWrite
                , PixelFormat.Format24bppRgb);
            int Stride = data.Stride, StrideC = (int)(((Width + padWidth * 2) * 3 + 3) & 0XFFFFFFFC);

            int x, y, tmp;
            byte* p = (byte*)data.Scan0;
            fixed (byte* pC0 = dataCopy)
            {
                //使用中间变量速度会更快（原因不明）
                byte* pC = pC0;
                //position of pixels
                byte* dst, src0, src1, src2;
                //start calculate
                for (y = 0; y < Height; y++)
                {
                    dst = p + y * Stride;
                    src0 = pC + y * StrideC;
                    src1 = src0 + StrideC;
                    src2 = src1 + StrideC;
                    for (x = 0; x < Stride; x++)
                    {
                        tmp = (*(src1 + 3) * 2
                              - (*src0 + *(src0 + 3) + *(src0 + 6)
                               + *src1 + *(src1 + 3) + *(src1 + 6)
                               + *src2 + *(src2 + 3) + *(src2 + 6)) / 9);
                        tmp = (tmp > 255) ? 255 : tmp;
                        tmp = (tmp < 0) ? 0 : tmp;
                        *dst = (byte)tmp;
                        dst++;
                        src0++; src1++; src2++;
                    }
                }
            }
            b.UnlockBits(data);
            return b;
        }

        #endregion


        #region gradient

        /// <summary>
        /// 以后写
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static unsafe Bitmap SobelFilter(this Bitmap b)
        {
            throw new Exception();//以后写
            b = b.Clone() as Bitmap;
            int padWidth = 1, padHeight = 1;
            int Height = b.Height, Width = b.Width;

            //padding to a byte array
            byte[] dataCopy = BitmapPadding(b, padWidth, padHeight);

            BitmapData data = b.LockBits(new Rectangle(0, 0, b.Width, b.Height)
                , ImageLockMode.ReadWrite
                , PixelFormat.Format24bppRgb);
            int Stride = data.Stride, StrideC = (int)(((Width + padWidth * 2) * 3 + 3) & 0XFFFFFFFC);

            int x, y;
            byte* p = (byte*)data.Scan0;
            fixed (byte* pC0 = dataCopy)
            {
                //使用中间变量速度会更快（原因不明）
                byte* pC = pC0;
                //position of pixels
                byte* dst, src0, src1, src2;
                //start calculate
                for (y = 0; y < Height; y++)
                {
                    dst = p + y * Stride;
                    src0 = pC + y * StrideC;
                    src1 = src0 + StrideC;
                    src2 = src1 + StrideC;
                    for (x = 0; x < Stride; x++)
                    {
                        //平均值不会超出范围故不做检查
                        *dst = (byte)((*src0 + *(src0 + 3) + *(src0 + 6)
                                     + *src1 + *(src1 + 3) + *(src1 + 6)
                                     + *src2 + *(src2 + 3) + *(src2 + 6)) / 9);
                        dst++;
                        src0++; src1++; src2++;
                    }
                }
            }
            b.UnlockBits(data);
            return b;
        }

        #endregion


        #region Order

        /// <summary>
        /// spatial min filter
        /// </summary>
        /// <param name="b"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public static unsafe Bitmap MinFilter(this Bitmap b, int n = 3)
        {
            if (n % 2 != 1)
            {
                throw new Exception("窗大小应为奇数");
            }
            b = b.Clone() as Bitmap;
            int padWidth = (n - 1) / 2, padHeight = padWidth;
            int Height = b.Height, Width = b.Width;

            //padding to a byte array
            byte[] dataCopy = BitmapPadding(b, padWidth, padHeight);

            BitmapData data = b.LockBits(new Rectangle(0, 0, b.Width, b.Height)
                , ImageLockMode.ReadWrite
                , PixelFormat.Format24bppRgb);
            int Stride = data.Stride, StrideC = (int)(((Width + padWidth * 2) * 3 + 3) & 0XFFFFFFFC);

            int x, y, i, nx, ny, ni;
            byte tmp, tmp0;
            byte* p = (byte*)data.Scan0;
            fixed (byte* pC0 = dataCopy)
            {
                //使用中间变量速度会更快（原因不明）
                byte* pC = pC0;
                //position of pixels
                byte* dst;
                byte*[] src = new byte*[n];
                //start calculate
                for (y = 0; y < Height; y++)
                {
                    dst = p + y * Stride;
                    src[0] = pC + y * StrideC;
                    for (i = 1; i < n; i++)
                    {
                        src[i] = src[i - 1] + StrideC;
                    }
                    for (x = 0; x < Stride; x++)
                    {
                        //最小值不会超出范围故不做检查
                        tmp0 = 255;
                        for (ny = 0; ny < n; ny++)
                        {
                            ni = 0;
                            for (nx = 0; nx < n; nx++)
                            {
                                tmp = *(src[ny] + ni);
                                if (tmp < tmp0) tmp0 = tmp;
                                ni += 3;
                            }
                        }
                        *dst = tmp0;
                        dst++;
                        for (i = 0; i < n; i++)
                        {
                            src[i]++;
                        }
                    }
                }
            }
            b.UnlockBits(data);
            return b;
        }

        /// <summary>
        /// spatial max filter
        /// </summary>
        /// <param name="b"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public static unsafe Bitmap MaxFilter(this Bitmap b, int n = 3)
        {
            if (n % 2 != 1) throw new Exception("mask size n must be odd number.");
            b = b.Clone() as Bitmap;
            int padWidth = (n - 1) / 2, padHeight = padWidth;
            int Height = b.Height, Width = b.Width;

            //padding to a byte array
            byte[] dataCopy = BitmapPadding(b, padWidth, padHeight);

            BitmapData data = b.LockBits(new Rectangle(0, 0, b.Width, b.Height)
                , ImageLockMode.ReadWrite
                , PixelFormat.Format24bppRgb);
            int Stride = data.Stride, StrideC = (int)(((Width + padWidth * 2) * 3 + 3) & 0XFFFFFFFC);

            int x, y, i, nx, ny, ni;
            byte tmp, tmp0;
            byte* p = (byte*)data.Scan0;
            fixed (byte* pC0 = dataCopy)
            {
                //使用中间变量速度会更快（原因不明）
                byte* pC = pC0;
                //position of pixels
                byte* dst;
                byte*[] src = new byte*[n];
                //start calculate
                for (y = 0; y < Height; y++)
                {
                    dst = p + y * Stride;
                    src[0] = pC + y * StrideC;
                    for (i = 1; i < n; i++)
                    {
                        src[i] = src[i - 1] + StrideC;
                    }
                    for (x = 0; x < Stride; x++)
                    {
                        //最大值不会超出范围故不做检查
                        tmp0 = 0;
                        for (ny = 0; ny < n; ny++)
                        {
                            ni = 0;
                            for (nx = 0; nx < n; nx++)
                            {
                                tmp = *(src[ny] + ni);
                                if (tmp > tmp0) tmp0 = tmp;
                                ni += 3;
                            }
                        }
                        *dst = tmp0;
                        dst++;
                        for (i = 0; i < n; i++)
                        {
                            src[i]++;
                        }
                    }
                }
            }
            b.UnlockBits(data);
            return b;
        }

        /// <summary>
        /// spatial median filter
        /// </summary>
        /// <param name="b"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public static unsafe Bitmap MedianFilter(this Bitmap b, int n = 3)
        {
            if (n % 2 != 1) throw new Exception("mask size n must be odd number.");
            b = b.Clone() as Bitmap;
            int padWidth = (n - 1) / 2, padHeight = padWidth;
            int Height = b.Height, Width = b.Width;

            //padding to a byte array
            byte[] dataCopy = BitmapPadding(b, padWidth, padHeight);

            BitmapData data = b.LockBits(new Rectangle(0, 0, b.Width, b.Height)
                , ImageLockMode.ReadWrite
                , PixelFormat.Format24bppRgb);
            int Stride = data.Stride, StrideC = (int)(((Width + padWidth * 2) * 3 + 3) & 0XFFFFFFFC);

            int x, y, i, nx, ny, ni;
            byte[] tmpArray = new byte[n * n];
            int nMid = (n * n - 1) / 2, arrayCount;
            byte* p = (byte*)data.Scan0;
            fixed (byte* pC0 = dataCopy)
            {
                //使用中间变量速度会更快（原因不明）
                byte* pC = pC0;
                //position of pixels
                byte* dst;
                byte*[] src = new byte*[n];
                //start calculate
                for (y = 0; y < Height; y++)
                {
                    dst = p + y * Stride;
                    src[0] = pC + y * StrideC;
                    for (i = 1; i < n; i++)
                    {
                        src[i] = src[i - 1] + StrideC;
                    }
                    for (x = 0; x < Stride; x++)
                    {
                        //最大值不会超出范围故不做检查
                        arrayCount = 0;
                        for (ny = 0; ny < n; ny++)
                        {
                            ni = 0;
                            for (nx = 0; nx < n; nx++)
                            {
                                tmpArray[arrayCount] = *(src[ny] + ni);
                                ni += 3;
                                arrayCount++;
                            }
                        }
                        Array.Sort(tmpArray);
                        *dst = tmpArray[nMid];
                        dst++;
                        for (i = 0; i < n; i++)
                        {
                            src[i]++;
                        }
                    }
                }
            }
            b.UnlockBits(data);
            return b;
        }

        /// <summary>
        /// adaptive median filter
        /// </summary>
        /// <param name="b"></param>
        /// <param name="MaxWindowSize"></param>
        /// <returns></returns>
        public static unsafe Bitmap AdaptiveMedianFilter(this Bitmap b, int MaxWindowSize = 5)
        {
            if (MaxWindowSize % 2 != 1) throw new Exception("mask size n must be odd number.");
            b = b.Clone() as Bitmap;
            int padWidth = (MaxWindowSize - 1) / 2, padHeight = padWidth;
            int Height = b.Height, Width = b.Width;

            //padding to a byte array
            byte[] dataCopy = BitmapPadding(b, padWidth, padHeight);

            BitmapData data = b.LockBits(new Rectangle(0, 0, b.Width, b.Height)
                , ImageLockMode.ReadWrite
                , PixelFormat.Format24bppRgb);
            int Stride = data.Stride, Width3 = Width * 3
                , StrideC = (int)(((Width + padWidth * 2) * 3 + 3) & 0XFFFFFFFC);

            byte zmin, zmax, zmed;
            byte* p = (byte*)data.Scan0;
            //临时变量
            int x, y, i, nx, ny, np;
            //储存window下的数字
            byte[] tmpArray = new byte[MaxWindowSize * MaxWindowSize];
            int arrayCount, winSize, WinPad, winlast;
            //提前计算，节省时间
            int[] strideCS = new int[MaxWindowSize];
            for (i = 0; i < MaxWindowSize; i++) strideCS[i] = i * StrideC;
            fixed (byte* pC0 = dataCopy)
            {
                //使用中间变量速度会更快（原因不明）
                byte* pC = pC0;
                //position of pixels
                byte* dst;
                byte* src;
                //start calculate
                for (y = 0; y < Height; y++)
                {
                    dst = p + y * Stride;
                    for (x = 0; x < Width3; x++)
                    {
                        winSize = 3;
                    //adaptive median algorithm
                    Label_Compute:
                        WinPad = (MaxWindowSize - winSize) / 2;
                        winlast = winSize * winSize - 1;
                        src = pC + (y + WinPad) * StrideC + x + WinPad * 3;//src指向window下的第一个数字
                        //将window下的数字保存到数组中
                        arrayCount = 0;
                        for (ny = 0; ny < winSize; ny++)
                        {
                            np = 0;
                            for (nx = 0; nx < winSize; nx++)
                            {
                                tmpArray[arrayCount] = *(src + strideCS[ny] + np);
                                np += 3;
                                arrayCount++;
                            }
                        }
                        Array.Sort(tmpArray, 0, winlast + 1);//仅对前winSize^2个数排序
                        zmin = tmpArray[0];
                        zmax = tmpArray[winlast];
                        zmed = tmpArray[winlast / 2];
                        if (zmed > zmin && zmed < zmax)
                        {
                            if (*dst == zmin || *dst == zmax)
                            {
                                *dst = zmed;
                            }
                            goto Label_Out;
                        }
                        else
                        {
                            winSize += 2;
                        }
                        if (winSize <= MaxWindowSize)
                        {
                            goto Label_Compute;
                        }
                    //end the algorithm
                    Label_Out:
                        dst++;
                    }//for x
                }//for y
            }//fixed
            b.UnlockBits(data);
            return b;
        }

        #endregion

        #region Helper
        /// <summary>
        /// padding a Bitmap to a byte array, a prepare for spatial filter
        /// </summary>
        /// <param name="b"></param>
        /// <param name="padPixWd"></param>
        /// <param name="padPixHt"></param>
        /// <returns></returns>
        public static unsafe byte[] BitmapPadding(Bitmap b, int padPixWd, int padPixHt)
        {
            int x, y;
            int Width = b.Width, Height = b.Height, Stride = (int)((b.Width * 3 + 3) & 0XFFFFFFFC);
            int WidthC = Width + padPixWd * 2, HeightC = Height + 2 * padPixHt
                , StrideC = (int)((WidthC * 3 + 3) & 0XFFFFFFFC);

            byte[] bData = new byte[Stride * Height];
            byte[] bDataCopy = new byte[StrideC * HeightC];

            fixed (byte* pScan0 = bData)
            {
                //point bData to "b"
                BitmapData data = new BitmapData();
                data.Scan0 = (IntPtr)(void*)pScan0;
                data.Stride = Stride;
                b.LockBits(new Rectangle(0, 0, Width, Height)
                    , ImageLockMode.ReadWrite | ImageLockMode.UserInputBuffer
                    , PixelFormat.Format24bppRgb, data);

                //padding
                for (y = 0; y < Height; y++)
                {
                    for (x = 0; x < padPixWd; x++)
                    {
                        Buffer.BlockCopy(bData, Stride * y, bDataCopy, StrideC * (y + padPixHt) + x * 3, 3);//left
                        Buffer.BlockCopy(bData, Stride * y + (Width - 1) * 3
                            , bDataCopy, StrideC * (y + padPixHt) + (Width + padPixWd + x) * 3, 3);//right
                    }
                    Buffer.BlockCopy(bData, Stride * y, bDataCopy, StrideC * (y + padPixHt) + padPixWd * 3, Width * 3);//中
                }
                for (y = 0; y < padPixHt; y++)//top&bottom
                {
                    Buffer.BlockCopy(bDataCopy, StrideC * padPixHt, bDataCopy, y * StrideC, StrideC);
                    Buffer.BlockCopy(bDataCopy, (HeightC - 1 - padPixHt) * StrideC, bDataCopy, (HeightC - 1 - y) * StrideC, StrideC);
                }

                //unlock bitmap
                b.UnlockBits(data);
            }
            return bDataCopy;
        }
        #endregion
    }
}
