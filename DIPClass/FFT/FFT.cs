using LibFftw3Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace FFTW
{
    /// <summary>
    /// 对FFTW的二次包装，更加简单地调用FFT算法，需要libfftw3-3.dll文件
    /// 如调用此函数，项目需以X86平台运行
    /// </summary>
    public static class FFTClass
    {
        #region 1D
        public static unsafe Complex1 FFT(double[] Re)
        {
            int N = Re.Length;
            double[] Im = new double[N];
            fixed (double* pim = Im)
            {
                for (int i = 0; i < N; i++)
                {
                    pim[i] = 0;
                }
            }
            return FFT(Re, Im);
        }

        /// <summary>
        /// 结果会扩大length倍
        /// </summary>
        /// <param name="Re"></param>
        /// <param name="Im"></param>
        /// <returns></returns>
        public static unsafe Complex1 IFFT(double[] Re, double[] Im)
        {
            return FFT(Re, Im, true);
        }

        public static unsafe Complex1 FFT(double[] Re, double[] Im, bool Inverse = false)
        {
            int N = Re.Length;
            double[] din = new double[N * 2];
            fixed (double* pin = din, pre = Re, pim = Im)
            {
                for (int i = 0; i < N; i++)
                {
                    pin[2 * i] = pre[i];
                    pin[2 * i + 1] = pim[i];
                }
            }
            double[] dout = fft_1d(N, din, (Inverse) ? fftw_direction.Backward : fftw_direction.Forward);
            double[] resultRe = new double[N];
            double[] resultIm = new double[N];
            fixed (double* pout = dout, prre = resultRe, prim = resultIm)
            {
                for (int i = 0; i < N; i++)
                {
                    prre[i] = pout[2 * i];
                    prim[i] = pout[2 * i + 1];
                }
            }
            return new Complex1(resultRe, resultIm);
        }

        private static double[] fft_1d(int N, double[] din, fftw_direction direction)
        {
            double[] dout = new double[N * 2];
            GCHandle hdin, hdout;
            hdin = GCHandle.Alloc(din, GCHandleType.Pinned);
            hdout = GCHandle.Alloc(dout, GCHandleType.Pinned);
            IntPtr plan = fftw.dft_1d(N, hdin.AddrOfPinnedObject(), hdout.AddrOfPinnedObject(),
                direction, fftw_flags.Estimate);
            fftw.execute(plan);
            fftw.destroy_plan(plan);
            hdin.Free();
            hdout.Free();
            return dout;
        }

        #endregion

        #region 2D

        public static unsafe Complex2 FFT2(double[,] Re)
        {
            int row = Re.GetLength(0);
            int column = Re.GetLength(1);
            int size = row * column;
            double[,] Im = new double[row, column];
            fixed (double* pim = Im)
            {
                for (int i = 0; i < size; i++)
                {
                    pim[i] = 0;
                }
            }
            return FFT2(Re, Im);
        }

        /// <summary>
        /// 结果会扩大width*height倍
        /// </summary>
        /// <param name="Re"></param>
        /// <param name="Im"></param>
        /// <returns></returns>
        public static unsafe Complex2 IFFT2(double[,] Re, double[,] Im)
        {
            return FFT2(Re, Im, true);
        }

        public static unsafe Complex2 FFT2(double[,] Re, double[,] Im, bool Inverse = false)
        {
            int row = Re.GetLength(0);
            int column = Re.GetLength(1);
            int size = row * column;
            double[,] din = new double[row, column * 2];
            fixed (double* pin = din, pre = Re, pim = Im)
            {
                for (int i = 0; i < size; i++)
                {
                    pin[2 * i] = pre[i];
                    pin[2 * i + 1] = pim[i];
                }
            }
            double[,] dout = fft_2d(row, column, din
                , (Inverse) ? fftw_direction.Backward : fftw_direction.Forward);
            double[,] resultRe = new double[row, column];
            double[,] resultIm = new double[row, column];
            fixed (double* pout = dout, prre = resultRe, prim = resultIm)
            {
                for (int i = 0; i < size; i++)
                {
                    prre[i] = pout[2 * i];
                    prim[i] = pout[2 * i + 1];
                }
            }
            return new Complex2(resultRe, resultIm);
        }

        private static double[,] fft_2d(int row, int column, double[,] din, fftw_direction dirction)
        {
            GC.Collect();
            double[,] dout = new double[row, column * 2];
            GCHandle hdin, hdout;
            hdin = GCHandle.Alloc(din, GCHandleType.Pinned);
            hdout = GCHandle.Alloc(dout, GCHandleType.Pinned);
            IntPtr plan = fftw.dft_2d(row, column, hdin.AddrOfPinnedObject(), hdout.AddrOfPinnedObject(),
                dirction, fftw_flags.Estimate);
            fftw.execute(plan);
            fftw.destroy_plan(plan);
            hdin.Free();
            hdout.Free();
            return dout;
        }

        #endregion


    }

    public class Complex1 : Tuple<double[], double[]>
    {
        public Complex1(double[] Re, double[] Im)
            : base(Re, Im)
        { }

        public double[] Real { get { return this.Item1; } }
        public double[] Imag { get { return this.Item2; } }
    }

    public class Complex2 : Tuple<double[,], double[,]>
    {
        public Complex2(double[,] Re, double[,] Im)
            : base(Re, Im)
        { }

        public double[,] Real { get { return this.Item1; } }
        public double[,] Imag { get { return this.Item2; } }
    }
}
