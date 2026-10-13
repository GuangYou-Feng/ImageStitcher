using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ImageStitcher.Stitching
{
    /// <summary>
    /// Draws the two source images side by side with a line per keypoint match.
    /// RANSAC outliers are drawn first in red, inliers on top in green, which makes
    /// it obvious at a glance whether the homography is trustworthy.
    /// </summary>
    internal static class MatchPreviewRenderer
    {
        private const int Gap = 12;
        private const int MaxLines = 300;
        private const int DefaultMaxWidth = 1800;

        /// <summary>
        /// Draws the two source images side by side with the match lines over them.
        /// </summary>
        /// <remarks>
        /// Both images are read with <see cref="Graphics.DrawImage(Image, RectangleF)"/>, so
        /// they must not be painted by another thread at the same time.
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
                        // Pass 1: outliers, so inliers are never hidden behind them.
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
