using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using OpenCvSharp;

namespace ImageStitcher.Imaging
{
    /// <summary>
    /// Converts between <see cref="Bitmap"/> and <see cref="Mat"/> without taking a
    /// dependency on OpenCvSharp4.Extensions, which would pull System.Drawing.Common
    /// into a .NET Framework build and fight with the System.Drawing in the GAC.
    /// </summary>
    public static class MatConverter
    {
        /// <summary>Copies a GDI+ bitmap into a new 8UC3 BGR matrix.</summary>
        /// <remarks>
        /// The matrix owns its own pixels, but the copy is taken by locking the source with
        /// <see cref="Bitmap.LockBits"/> whenever it is already 24bpp. The source must
        /// therefore not be read from another thread while this runs - a concurrent paint
        /// raises "位图区域已被锁定".
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
                    // FromPixelData wraps the locked memory, so clone before unlocking.
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

        /// <summary>Copies a matrix into a new 24bpp bitmap, honouring row stride.</summary>
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
