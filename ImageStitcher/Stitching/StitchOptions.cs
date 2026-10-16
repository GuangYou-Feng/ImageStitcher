using System;

namespace ImageStitcher.Stitching
{
    /// <summary><see cref="AutoStitcher"/> 的可调参数。</summary>
    public sealed class StitchOptions
    {
        public StitchOptions()
        {
            Algorithm = StitchAlgorithm.Sift;
            RatioThreshold = 0.75;
            RansacReprojectionThreshold = 3.0;
            SiftFeatureCount = 0;      // 0 = 不限制，即 SIFT 的默认值
            OrbFeatureCount = 4000;
            FeatherWidth = 48.0;
            MaxCanvasSide = 30000;
            MaxCanvasPixels = 120000000L;
        }

        /// <summary>使用哪一种检测器与描述子组合。</summary>
        public StitchAlgorithm Algorithm { get; set; }

        /// <summary>
        /// Lowe 比值测试阈值。只有当最近距离小于次近距离的该比例时才保留该匹配。
        /// 数值越小越严格。
        /// </summary>
        public double RatioThreshold { get; set; }

        /// <summary>单应矩阵拟合时 RANSAC 的内点阈值，单位为像素。</summary>
        public double RansacReprojectionThreshold { get; set; }

        /// <summary>请求的 SIFT 特征点数量；为 0 时保留检测到的全部特征点。</summary>
        public int SiftFeatureCount { get; set; }

        /// <summary>请求的 ORB 特征点数量。</summary>
        public int OrbFeatureCount { get; set; }

        /// <summary>
        /// 接缝羽化宽度，单位为像素。距离权重会被限制在该值以内，使融合区域保持
        /// 固定宽度，而不会随重叠面积增大。
        /// </summary>
        public double FeatherWidth { get; set; }

        /// <summary>当全景图的宽度或高度超过该值时，拒绝生成结果。</summary>
        public int MaxCanvasSide { get; set; }

        /// <summary>当全景图的像素总数超过该值时，拒绝生成结果。</summary>
        public long MaxCanvasPixels { get; set; }
    }
}
