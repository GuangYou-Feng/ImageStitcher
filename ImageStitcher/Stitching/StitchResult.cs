using System;
using System.Drawing;

namespace ImageStitcher.Stitching
{
    /// <summary>Outcome of one automatic stitching run.</summary>
    public sealed class StitchResult
    {
        public StitchResult()
        {
            Algorithm = StitchAlgorithm.Sift;
            MovingPoints = new PointF[0];
            ReferencePoints = new PointF[0];
            InlierFlags = new bool[0];
        }

        /// <summary>True when <see cref="Image"/> holds a usable panorama.</summary>
        public bool Success { get; set; }

        /// <summary>Human readable explanation, always set (success or failure).</summary>
        public string Message { get; set; }

        /// <summary>The stitched panorama. The caller owns it.</summary>
        public Bitmap Image { get; set; }

        /// <summary>Side-by-side rendering of the keypoint matches. The caller owns it.</summary>
        public Bitmap MatchPreview { get; set; }

        public StitchAlgorithm Algorithm { get; set; }

        public int KeyPointsReference { get; set; }
        public int KeyPointsMoving { get; set; }
        public int GoodMatches { get; set; }
        public int Inliers { get; set; }

        /// <summary>Number of pixels covered by both images after warping.</summary>
        public long OverlapPixels { get; set; }

        /// <summary>Matched points in the moving image, used by the preview renderer.</summary>
        public PointF[] MovingPoints { get; set; }

        /// <summary>Matched points in the reference image, one per entry of <see cref="MovingPoints"/>.</summary>
        public PointF[] ReferencePoints { get; set; }

        /// <summary>True where the corresponding match survived RANSAC.</summary>
        public bool[] InlierFlags { get; set; }

        public TimeSpan Elapsed { get; set; }

        /// <summary>Compact one line summary for the status bar.</summary>
        public string Summary
        {
            get
            {
                return string.Format("{0}：特征点 {1}/{2}，匹配 {3}，内点 {4}，用时 {5:F0} ms",
                    Algorithm == StitchAlgorithm.Sift ? "SIFT" : "ORB",
                    KeyPointsReference, KeyPointsMoving, GoodMatches, Inliers,
                    Elapsed.TotalMilliseconds);
            }
        }
    }
}
