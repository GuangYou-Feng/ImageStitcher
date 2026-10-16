using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using OpenCvSharp;

namespace ImageStitcher.Imaging
{
    /// <summary>
    /// 在 <see cref="Bitmap"/> 与 <see cref="Mat"/> 之间相互转换，同时不依赖
    /// OpenCvSharp4.Extensions —— 后者会把 System.Drawing.Common 引入 .NET Framework
    /// 项目，与 GAC 中的 System.Drawing 发生冲突。
    /// </summary>
    public static class MatConverter
    {
        /// <summary>把 GDI+ 位图复制到一个新的 8UC3 BGR 矩阵中。</summary>
        /// <remarks>
        /// 返回的矩阵拥有自己的像素数据，但源位图本身已经是 24bpp 时，复制过程会通过
        /// <see cref="Bitmap.LockBits"/> 锁定它。因此在此期间不能在其他线程读取该源
        /// 位图，否则并发的绘制会抛出“位图区域已被锁定”。
        /// </remarks>
        public static Mat ToMat(Bitmap bitmap)
        {
            if (bitmap == null) throw new ArgumentNullException("bitmap");

            Bitmap source = bitmap;
            bool ownsCopy = false;
            if (bitmap.PixelFormat != PixelFormat.Format24bppRgb)
            {
                source = bitmap.Clone(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                                      PixelFormat.Format24bppRgb);
                ownsCopy = true;
            }

            try
            {
                BitmapData data = source.LockBits(new Rectangle(0, 0, source.Width, source.Height),
                    ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                try
                {
                    // FromPixelData 只是包装锁定的内存，因此必须在解锁之前完成克隆。
                    using (Mat view = Mat.FromPixelData(data.Height, data.Width,
                               MatType.CV_8UC3, data.Scan0, data.Stride))
                    {
                        return view.Clone();
                    }
                }
                finally
                {
                    source.UnlockBits(data);
                }
            }
            finally
            {
                if (ownsCopy) source.Dispose();
            }
        }

        /// <summary>把矩阵复制到一个新的 24bpp 位图中，并正确处理行跨距。</summary>
        public static Bitmap ToBitmap(Mat mat)
        {
            if (mat == null) throw new ArgumentNullException("mat");
            if (mat.Empty()) throw new ArgumentException("matrix is empty", "mat");

            Mat bgr = null;
            try
            {
                if (mat.Type() == MatType.CV_8UC3)
                {
                    bgr = mat;
                }
                else
                {
                    bgr = new Mat();
                    if (mat.Channels() == 1) Cv2.CvtColor(mat, bgr, ColorConversionCodes.GRAY2BGR);
                    else mat.ConvertTo(bgr, MatType.CV_8UC3);
                }

                Bitmap bitmap = new Bitmap(bgr.Width, bgr.Height, PixelFormat.Format24bppRgb);
                BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bgr.Width, bgr.Height),
                    ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                try
                {
                    long step = bgr.Step();
                    int copy = (int)Math.Min(step, data.Stride);
                    byte[] buffer = new byte[copy];
                    for (int y = 0; y < bgr.Height; y++)
                    {
                        IntPtr src = IntPtr.Add(bgr.Data, (int)(y * step));
                        IntPtr dst = IntPtr.Add(data.Scan0, y * data.Stride);
                        Marshal.Copy(src, buffer, 0, copy);
                        Marshal.Copy(buffer, 0, dst, copy);
                    }
                }
                finally
                {
                    bitmap.UnlockBits(data);
                }
                return bitmap;
            }
            finally
            {
                if (bgr != null && !ReferenceEquals(bgr, mat)) bgr.Dispose();
            }
        }
    }
}
