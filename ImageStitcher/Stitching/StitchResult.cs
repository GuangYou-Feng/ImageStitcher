using System;
using System.Drawing;

namespace ImageStitcher.Stitching
{
    /// <summary>一次自动拼接的结果。</summary>
    public sealed class StitchResult
    {
        public StitchResult()
        {
            Algorithm = StitchAlgorithm.Sift;
            MovingPoints = new PointF[0];
            ReferencePoints = new PointF[0];
            InlierFlags = new bool[0];
        }

        /// <summary>当 <see cref="Image"/> 中包含可用的全景图时为 true。</summary>
        public bool Success { get; set; }

        /// <summary>面向用户的说明文字，无论成功或失败都会赋值。</summary>
        public string Message { get; set; }

        /// <summary>拼接得到的全景图，所有权归调用方。</summary>
        public Bitmap Image { get; set; }

        /// <summary>特征点匹配的左右对照图，所有权归调用方。</summary>
        public Bitmap MatchPreview { get; set; }

        public StitchAlgorithm Algorithm { get; set; }

        public int KeyPointsReference { get; set; }
        public int KeyPointsMoving { get; set; }
        public int GoodMatches { get; set; }
        public int Inliers { get; set; }

        /// <summary>变换之后两张图像共同覆盖的像素数量。</summary>
        public long OverlapPixels { get; set; }

        /// <summary>待拼接图像中的匹配点，供预览渲染使用。</summary>
        public PointF[] MovingPoints { get; set; }

        /// <summary>基准图像中的匹配点，与 <see cref="MovingPoints"/> 一一对应。</summary>
        public PointF[] ReferencePoints { get; set; }

        /// <summary>对应匹配是否通过 RANSAC 筛选，通过处为 true。</summary>
        public bool[] InlierFlags { get; set; }

        public TimeSpan Elapsed { get; set; }

        /// <summary>用于状态栏的单行简要结果。</summary>
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
