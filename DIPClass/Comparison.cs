using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;

namespace ImageProcess
{
    public static class Comparison
    {

        public static double PSNR(Bitmap b1, Bitmap b2)
        {
            return PSNR(MSE(b1, b2).MSE);
        }

        public static double PSNR(double mse)
        {
            return 20 * Math.Log10(255 / Math.Sqrt(mse));
        }

        /// <summary>
        /// calculate MSE between 2 images
        /// </summary>
        /// <param name="b1"></param>
        /// <param name="b2"></param>
        /// <returns></returns>
        public static unsafe MseRGB MSE(Bitmap b1, Bitmap b2)
        {
            int Width = b1.Width, Height = b1.Height, Size = Width * Height;
            if (b2.Width != Width || b2.Height != Height)
                throw new Exception("size can't match.");
            BitmapData data1 = b1.LockBits(new Rectangle(0, 0, b1.Width, b1.Height)
                , ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            BitmapData data2 = b2.LockBits(new Rectangle(0, 0, b2.Width, b2.Height)
                , ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            double mseR = 0, mseG = 0, mseB = 0;
            int x, y, stride = data1.Stride;
            byte* p1 = (byte*)data1.Scan0, p2 = (byte*)data2.Scan0;
            byte* pr1, pg1, pb1, pr2, pg2, pb2;
            for (y = 0; y < Height; y++)
            {
                pb1 = p1 + y * stride;
                pg1 = pb1 + 1;
                pr1 = pg1 + 1;
                pb2 = p2 + y * stride;
                pg2 = pb2 + 1;
                pr2 = pg2 + 1;
                for (x = 0; x < Width; x++)
                {
                    mseR += (*pr1 - *pr2) * (*pr1 - *pr2);
                    mseG += (*pg1 - *pg2) * (*pg1 - *pg2);
                    mseB += (*pb1 - *pb2) * (*pb1 - *pb2);

                    pb1 += 3; pg1 += 3; pr1 += 3;
                    pb2 += 3; pg2 += 3; pr2 += 3;
                }
            }
            b1.UnlockBits(data1);
            b2.UnlockBits(data2);
            return new MseRGB(mseR / Size, mseG / Size, mseB / Size);
        }
    }

    public class MseRGB : Tuple<double, double, double>
    {
        public MseRGB(double mseR, double mseG, double mseB)
            : base(mseR, mseG, mseB)
        { }

        public double MseR { get { return this.Item1; } }
        public double MseG { get { return this.Item2; } }
        public double MseB { get { return this.Item3; } }
        public double MSE { get { return (MseR + MseG + MseB) / 3; } }

        public double PSNR()
        {
            return Comparison.PSNR(MSE);
        }

        public override string ToString()
        {
            return ((int)MSE).ToString()
                + "(" 
                + ((int)MseR).ToString()
                + "," + ((int)MseG).ToString()
                + "," + ((int)MseB).ToString()
                + ")";
        }
    }
}
