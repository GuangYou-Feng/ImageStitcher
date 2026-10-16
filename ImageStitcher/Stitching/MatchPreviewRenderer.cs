using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ImageStitcher.Stitching
{
    /// <summary>
    /// 把两张源图像并排绘制，并为每一对特征点匹配画一条连线。
    /// RANSAC 外点先用红色绘制，内点再覆盖在上面用绿色绘制，这样一眼就能看出
    /// 单应矩阵是否可信。
    /// </summary>
    internal static class MatchPreviewRenderer
    {
        private const int Gap = 12;
        private const int MaxLines = 300;
        private const int DefaultMaxWidth = 1800;

        /// <summary>
        /// 把两张源图像并排绘制，并在其上叠加匹配连线。
        /// </summary>
        /// <remarks>
        /// 两张图像都通过 <see cref="Graphics.DrawImage(Image, RectangleF)"/> 读取，
        /// 因此绘制期间不能有另一个线程同时绘制它们。
        /// </remarks>
        public static Bitmap Render(
            Bitmap left, Bitmap right,
            PointF[] leftPoints, PointF[] rightPoints, bool[] inliers)
        {
            if (left == null || right == null) return null;
            if (leftPoints == null || rightPoints == null || inliers == null) return null;

            int count = Math.Min(leftPoints.Length, Math.Min(rightPoints.Length, inliers.Length));
            if (count == 0) return null;

            int contentWidth = left.Width + Gap + right.Width;
            int contentHeight = Math.Max(left.Height, right.Height);

            float scale = 1f;
            if (contentWidth > DefaultMaxWidth)
            {
                scale = (float)DefaultMaxWidth / contentWidth;
            }

            int outputWidth = Math.Max(1, (int)Math.Round(contentWidth * scale));
            int outputHeight = Math.Max(1, (int)Math.Round(contentHeight * scale));

            Bitmap preview = new Bitmap(outputWidth, outputHeight, PixelFormat.Format24bppRgb);
            try
            {
                using (Graphics graphics = Graphics.FromImage(preview))
                {
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    graphics.Clear(Color.FromArgb(24, 24, 24));

                    float rightOffset = left.Width + Gap;

                    graphics.DrawImage(left, new RectangleF(0, 0, left.Width * scale, left.Height * scale));
                    graphics.DrawImage(right, new RectangleF(rightOffset * scale, 0,
                        right.Width * scale, right.Height * scale));

                    int step = count > MaxLines ? (int)Math.Ceiling(count / (double)MaxLines) : 1;

                    using (Pen outlierPen = new Pen(Color.FromArgb(150, 235, 64, 52), 1f))
                    using (Pen inlierPen = new Pen(Color.FromArgb(200, 60, 220, 90), 1.2f))
                    {
                        // 第一遍画外点，这样内点就不会被它们盖住。
                        for (int pass = 0; pass < 2; pass++)
                        {
                            bool wantInlier = pass == 1;
                            Pen pen = wantInlier ? inlierPen : outlierPen;

                            for (int i = 0; i < count; i += step)
                            {
                                if (inliers[i] != wantInlier) continue;

                                float x1 = leftPoints[i].X * scale;
                                float y1 = leftPoints[i].Y * scale;
                                float x2 = (rightPoints[i].X + rightOffset) * scale;
                                float y2 = rightPoints[i].Y * scale;
                                graphics.DrawLine(pen, x1, y1, x2, y2);
                            }
                        }
                    }

                    using (Pen border = new Pen(Color.FromArgb(90, 90, 90), 1f))
                    {
                        graphics.DrawRectangle(border, 0, 0, outputWidth - 1, outputHeight - 1);
                    }
                }
                return preview;
            }
            catch
            {
                preview.Dispose();
                throw;
            }
        }
    }
}
