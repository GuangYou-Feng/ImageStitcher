using System;

namespace ImageStitcher.Stitching
{
    /// <summary>Tuning knobs for <see cref="AutoStitcher"/>.</summary>
    public sealed class StitchOptions
    {
        public StitchOptions()
        {
            Algorithm = StitchAlgorithm.Sift;
            RatioThreshold = 0.75;
            RansacReprojectionThreshold = 3.0;
            SiftFeatureCount = 0;      // 0 = unrestricted, the SIFT default
            OrbFeatureCount = 4000;
            FeatherWidth = 48.0;
            MaxCanvasSide = 30000;
            MaxCanvasPixels = 120000000L;
        }

        /// <summary>Which detector/descriptor pair to use.</summary>
        public StitchAlgorithm Algorithm { get; set; }

        /// <summary>
        /// Lowe ratio test threshold. A match is kept only when the best distance is
        /// below this fraction of the second best. Lower is stricter.
        /// </summary>
        public double RatioThreshold { get; set; }

        /// <summary>RANSAC inlier threshold in pixels for the homography fit.</summary>
        public double RansacReprojectionThreshold { get; set; }

        /// <summary>Requested SIFT feature count; 0 keeps every feature found.</summary>
        public int SiftFeatureCount { get; set; }

        /// <summary>Requested ORB feature count.</summary>
        public int OrbFeatureCount { get; set; }

        /// <summary>
        /// Seam feather width in pixels. Distance weights are clamped to this so the
        /// blend stays a fixed width instead of growing with the overlap area.
        /// </summary>
        public double FeatherWidth { get; set; }

        /// <summary>Refuse to build a panorama whose width or height exceeds this.</summary>
        public int MaxCanvasSide { get; set; }

        /// <summary>Refuse to build a panorama with more pixels than this.</summary>
        public long MaxCanvasPixels { get; set; }
    }
}
