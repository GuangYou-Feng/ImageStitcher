using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using ImageStitcher.Imaging;
using OpenCvSharp;
using OpenCvSharp.Features2D;

namespace ImageStitcher.Stitching
{
    /// <summary>
    /// 基于特征点的自动全景拼接器。
    ///
    /// 处理流程：灰度化 -> 检测与描述（SIFT 或 ORB）-> 暴力 knn 匹配
    /// -> Lowe 比值测试 -> RANSAC 单应矩阵 -> 变换到统一画布
    /// -> 距离加权羽化融合。
    /// </summary>
    public static class AutoStitcher
    {
        /// <summary>
        /// 把 <paramref name="movingImage"/> 拼接 <paramref name="referenceImage"/> 上。
        /// 基准图像决定全景图的坐标系。
        /// </summary>
        /// <remarks>
        /// 两张输入图像在此调用期间会被锁定并绘制，因此在这段时间内由本方法独占。
        /// GDI+ 位图不是线程安全的：如果传入的图像同时正被界面绘制，会间歇性地抛出
        /// “位图区域已被锁定”。需要持续绘制这些图像的调用方必须先传入私有副本。
        /// </remarks>
        public static StitchResult Stitch(Bitmap referenceImage, Bitmap movingImage, StitchOptions options)
        {
            if (referenceImage == null) throw new ArgumentNullException("referenceImage");
            if (movingImage == null) throw new ArgumentNullException("movingImage");
            if (options == null) options = new StitchOptions();

            StitchResult result = new StitchResult();
            result.Algorithm = options.Algorithm;
            Stopwatch watch = Stopwatch.StartNew();

            try
            {
                using (Mat referenceMat = MatConverter.ToMat(referenceImage))
                using (Mat movingMat = MatConverter.ToMat(movingImage))
                using (Mat referenceGray = new Mat())
                using (Mat movingGray = new Mat())
                {
                    Cv2.CvtColor(referenceMat, referenceGray, ColorConversionCodes.BGR2GRAY);
                    Cv2.CvtColor(movingMat, movingGray, ColorConversionCodes.BGR2GRAY);

                    KeyPoint[] referenceKeys;
                    KeyPoint[] movingKeys;
                    using (Mat referenceDesc = new Mat())
                    using (Mat movingDesc = new Mat())
                    {
                        using (Feature2D detector = CreateDetector(options))
                        {
                            detector.DetectAndCompute(referenceGray, null, out referenceKeys, referenceDesc);
                            detector.DetectAndCompute(movingGray, null, out movingKeys, movingDesc);
                        }

                        result.KeyPointsReference = referenceKeys == null ? 0 : referenceKeys.Length;
                        result.KeyPointsMoving = movingKeys == null ? 0 : movingKeys.Length;

                        if (referenceKeys == null || movingKeys == null ||
                            referenceDesc.Empty() || movingDesc.Empty() ||
                            referenceKeys.Length < 4 || movingKeys.Length < 4)
                        {
                            result.Message = "特征点不足。两张图像需要有足够的纹理细节（纯色或严重模糊的图片无法拼接）。";
                            return result;
                        }

                        // 待拼接图像的特征描述子作为查询集，这样拟合出的单应矩阵
                        // 直接把“待拼接图像”映射到“基准图像”。
                        List<Point2d> movingPoints = new List<Point2d>();
                        List<Point2d> referencePoints = new List<Point2d>();
                        List<PointF> movingPreview = new List<PointF>();
                        List<PointF> referencePreview = new List<PointF>();

                        CollectRatioTestMatches(options, movingDesc, referenceDesc,
                            movingKeys, referenceKeys,
                            movingPoints, referencePoints, movingPreview, referencePreview);

                        result.GoodMatches = movingPoints.Count;
                        if (result.GoodMatches < 4)
                        {
                            result.Message = string.Format(
                                "有效匹配点不足（{0} 个）。请尝试换成纹理更丰富的图像，或改用 SIFT 算法。",
                                result.GoodMatches);
                            return result;
                        }

                        Mat homography = null;
                        Mat inlierMask = null;
                        try
                        {
                            inlierMask = new Mat();
                            homography = Cv2.FindHomography(movingPoints, referencePoints,
                                HomographyMethods.Ransac, options.RansacReprojectionThreshold, inlierMask);

                            if (homography == null || homography.Empty())
                            {
                                result.Message = "无法计算单应矩阵，两张图像之间可能没有足够的重叠区域。";
                                return result;
                            }

                            result.Inliers = Cv2.CountNonZero(inlierMask);
                            if (result.Inliers < 8)
                            {
                                result.Message = string.Format(
                                    "内点太少（{0} 个），匹配不可靠。两张图像可能重叠区域过小或内容重复。",
                                    result.Inliers);
                                return result;
                            }

                            BuildInlierFlags(result, inlierMask, movingPreview, referencePreview);

                            Bitmap panorama = ComposePanorama(referenceMat, movingMat, homography, options, result);
                            if (panorama == null) return result;

                            result.Image = panorama;
                            result.Success = true;
                            result.Message = "拼接成功。" + result.Summary;

                            // 始终会生成，是否显示由调用方决定。
                            result.MatchPreview = MatchPreviewRenderer.Render(
                                movingImage, referenceImage,
                                result.MovingPoints, result.ReferencePoints, result.InlierFlags);
                        }
                        finally
                        {
                            if (inlierMask != null) inlierMask.Dispose();
                            if (homography != null) homography.Dispose();
                        }
                    }
                }
            }
            catch (OpenCVException ex)
            {
                result.Success = false;
                result.Message = "OpenCV 处理失败：" + ex.Message;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = IsNativeLibraryFailure(ex)
                    ? "OpenCV 本机库 (OpenCvSharpExtern.dll) 无法加载，请确认它与程序位于同一目录，且程序以 x64 运行。"
                    : "拼接失败：" + ex.Message;
            }
            finally
            {
                watch.Stop();
                result.Elapsed = watch.Elapsed;
            }

            return result;
        }

        private static bool IsNativeLibraryFailure(Exception error)
        {
            for (Exception current = error; current != null; current = current.InnerException)
            {
                if (current is DllNotFoundException ||
                    current is BadImageFormatException ||
                    current is EntryPointNotFoundException)
                {
                    return true;
                }
            }
            return false;
        }

        private static Feature2D CreateDetector(StitchOptions options)
        {
            if (options.Algorithm == StitchAlgorithm.Orb)
            {
                // 参数顺序与 OpenCV 保持一致：edgeThreshold 在 firstLevel 之前。
                return ORB.Create(
                    nFeatures: options.OrbFeatureCount,
                    scaleFactor: 1.2f,
                    nLevels: 8,
                    edgeThreshold: 31,
                    firstLevel: 0,
                    wtaK: 2,
                    scoreType: ORBScoreType.Harris,
                    patchSize: 31,
                    fastThreshold: 20);
            }

            return SIFT.Create(
                nFeatures: options.SiftFeatureCount,
                nOctaveLayers: 3,
                contrastThreshold: 0.04,
                edgeThreshold: 10.0,
                sigma: 1.6);
        }

        private static void CollectRatioTestMatches(
            StitchOptions options,
            Mat movingDesc, Mat referenceDesc,
            KeyPoint[] movingKeys, KeyPoint[] referenceKeys,
            List<Point2d> movingPoints, List<Point2d> referencePoints,
            List<PointF> movingPreview, List<PointF> referencePreview)
        {
            // ORB 生成的是二进制描述子，因此需要使用汉明距离度量。
            NormTypes norm = options.Algorithm == StitchAlgorithm.Orb ? NormTypes.Hamming : NormTypes.L2;

            using (BFMatcher matcher = new BFMatcher(norm, false))
            {
                DMatch[][] knn = matcher.KnnMatch(movingDesc, referenceDesc, 2, null, true);
                for (int i = 0; i < knn.Length; i++)
                {
                    DMatch[] row = knn[i];
                    if (row == null || row.Length < 2) continue;
                    if (!(row[0].Distance < options.RatioThreshold * row[1].Distance)) continue;

                    int queryIndex = row[0].QueryIdx;
                    int trainIndex = row[0].TrainIdx;
                    if (queryIndex < 0 || queryIndex >= movingKeys.Length) continue;
                    if (trainIndex < 0 || trainIndex >= referenceKeys.Length) continue;

                    Point2f from = movingKeys[queryIndex].Pt;
                    Point2f to = referenceKeys[trainIndex].Pt;

                    movingPoints.Add(new Point2d(from.X, from.Y));
                    referencePoints.Add(new Point2d(to.X, to.Y));
                    movingPreview.Add(new PointF(from.X, from.Y));
                    referencePreview.Add(new PointF(to.X, to.Y));
                }
            }
        }

        private static void BuildInlierFlags(StitchResult result, Mat inlierMask,
            List<PointF> movingPreview, List<PointF> referencePreview)
        {
            int count = movingPreview.Count;
            bool[] flags = new bool[count];
            for (int i = 0; i < count; i++)
            {
                byte flag = 0;
                try
                {
                    flag = inlierMask.Get<byte>(i, 0);
                }
                catch (Exception)
                {
                    // 掩码形状不符合预期：把该匹配视为外点，而不是直接中断整个流程。
                    flag = 0;
                }
                flags[i] = flag != 0;
            }
            result.MovingPoints = movingPreview.ToArray();
            result.ReferencePoints = referencePreview.ToArray();
            result.InlierFlags = flags;
        }

        /// <summary>
        /// 把待拼接图像变换到基准图像的坐标系中，扩展画布以容纳两者，并以距离加权
        /// 羽化的方式融合重叠区域。
        /// 当结果尺寸不合理时返回 null（同时设置 <see cref="StitchResult.Message"/>）。
        /// </summary>
        private static Bitmap ComposePanorama(Mat referenceMat, Mat movingMat, Mat homography,
            StitchOptions options, StitchResult result)
        {
            Point2f[] projected = Cv2.PerspectiveTransform(new[]
            {
                new Point2f(0, 0),
                new Point2f(movingMat.Width, 0),
                new Point2f(movingMat.Width, movingMat.Height),
                new Point2f(0, movingMat.Height)
            }, homography);

            double minX = 0, minY = 0;
            double maxX = referenceMat.Width, maxY = referenceMat.Height;
            for (int i = 0; i < projected.Length; i++)
            {
                float x = projected[i].X, y = projected[i].Y;
                if (float.IsNaN(x) || float.IsInfinity(x) || float.IsNaN(y) || float.IsInfinity(y))
                {
                    result.Message = "单应矩阵出现非法数值，拼接结果不可信。请更换图像或算法重试。";
                    return null;
                }
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }

            int offsetX = (int)Math.Ceiling(-minX);
            int offsetY = (int)Math.Ceiling(-minY);
            int canvasWidth = (int)Math.Ceiling(maxX + offsetX);
            int canvasHeight = (int)Math.Ceiling(maxY + offsetY);

            if (canvasWidth < 1 || canvasHeight < 1)
            {
                result.Message = "计算出的画布尺寸无效，拼接失败。";
                return null;
            }

            if (canvasWidth > options.MaxCanvasSide || canvasHeight > options.MaxCanvasSide ||
                (long)canvasWidth * canvasHeight > options.MaxCanvasPixels)
            {
                result.Message = string.Format(
                    "拼接结果尺寸异常（{0} x {1} 像素），已中止。两张图像的匹配可能不可靠。",
                    canvasWidth, canvasHeight);
                return null;
            }

            // 把画布偏移量合并到单应矩阵的平移列中。
            homography.Set<double>(0, 2, homography.Get<double>(0, 2) + offsetX);
            homography.Set<double>(1, 2, homography.Get<double>(1, 2) + offsetY);

            OpenCvSharp.Size canvasSize = new OpenCvSharp.Size(canvasWidth, canvasHeight);
            Rect referenceRect = new Rect(offsetX, offsetY, referenceMat.Width, referenceMat.Height);

            using (Mat canvas = new Mat(canvasHeight, canvasWidth, MatType.CV_8UC3, new Scalar(0, 0, 0)))
            using (Mat referenceMask = new Mat(canvasHeight, canvasWidth, MatType.CV_8UC1, new Scalar(0)))
            {
                using (Mat roi = new Mat(canvas, referenceRect)) referenceMat.CopyTo(roi);
                using (Mat roiMask = new Mat(referenceMask, referenceRect)) roiMask.SetTo(new Scalar(255));

                using (Mat warped = new Mat())
                using (Mat warpedMask = new Mat(canvasHeight, canvasWidth, MatType.CV_8UC1, new Scalar(0)))
                {
                    Cv2.WarpPerspective(movingMat, warped, homography, canvasSize,
                        InterpolationFlags.Linear, BorderTypes.Constant, new Scalar(0, 0, 0, 0));

                    using (Mat ones = new Mat(movingMat.Height, movingMat.Width, MatType.CV_8UC1, new Scalar(255)))
                    {
                        Cv2.WarpPerspective(ones, warpedMask, homography, canvasSize,
                            InterpolationFlags.Nearest, BorderTypes.Constant, new Scalar(0));
                    }

                    result.OverlapPixels = MeasureOverlap(referenceMask, warpedMask);

                    using (Mat blended = FeatherBlend(canvas, referenceMask, warped, warpedMask, options.FeatherWidth))
                    {
                        return MatConverter.ToBitmap(blended);
                    }
                }
            }
        }

        private static long MeasureOverlap(Mat referenceMask, Mat warpedMask)
        {
            using (Mat intersection = new Mat())
            {
                Cv2.BitwiseAnd(referenceMask, warpedMask, intersection);
                return Cv2.CountNonZero(intersection);
            }
        }

        /// <summary>
        /// 距离加权羽化融合。
        ///
        /// 每个掩码都会转换成到边缘的距离图，并按请求的羽化宽度截断，作为该层的权重。
        /// 权重 = 距离 / 总距离，因此接缝会落在两层距离相等的位置，而某一层单独覆盖
        /// 的区域仍保持满权重。
        ///
        /// 累加之前分母先用一个极小值初始化。OpenCV 的除法在分母为零时会返回
        /// Infinity，那会污染整幅浮点图像，因此分母绝不能为零。
        /// </summary>
        private static Mat FeatherBlend(Mat baseBgr, Mat baseMask, Mat warpBgr, Mat warpMask, double featherWidth)
        {
            int height = baseBgr.Rows;
            int width = baseBgr.Cols;

            using (Mat baseDistance = new Mat())
            using (Mat warpDistance = new Mat())
            {
                Cv2.DistanceTransform(baseMask, baseDistance, DistanceTypes.L2, DistanceTransformMasks.Mask3);
                Cv2.DistanceTransform(warpMask, warpDistance, DistanceTypes.L2, DistanceTransformMasks.Mask3);

                // 做截断是为了让过渡区保持固定宽度，同时让数值保持有界——即使某一层
                // 覆盖了整块画布（没有可度量的零值像素）也不会出问题。
                double cap = featherWidth > 0 ? featherWidth : 1.0;
                using (Mat ceiling = new Mat(height, width, MatType.CV_32FC1, new Scalar(cap)))
                {
                    Cv2.Min(baseDistance, ceiling, baseDistance);
                    Cv2.Min(warpDistance, ceiling, warpDistance);
                }

                using (Mat total = new Mat(height, width, MatType.CV_32FC1, new Scalar(1e-6)))
                {
                    Cv2.Add(total, baseDistance, total);
                    Cv2.Add(total, warpDistance, total);

                    using (Mat baseWeight = new Mat())
                    using (Mat warpWeight = new Mat())
                    {
                        Cv2.Divide(baseDistance, total, baseWeight);
                        Cv2.Divide(warpDistance, total, warpWeight);

                        using (Mat baseWeight3 = new Mat())
                        using (Mat warpWeight3 = new Mat())
                        {
                            Cv2.CvtColor(baseWeight, baseWeight3, ColorConversionCodes.GRAY2BGR);
                            Cv2.CvtColor(warpWeight, warpWeight3, ColorConversionCodes.GRAY2BGR);

                            using (Mat baseFloat = new Mat())
                            using (Mat warpFloat = new Mat())
                            using (Mat basePart = new Mat())
                            using (Mat warpPart = new Mat())
                            using (Mat sum = new Mat())
                            {
                                baseBgr.ConvertTo(baseFloat, MatType.CV_32FC3);
                                warpBgr.ConvertTo(warpFloat, MatType.CV_32FC3);

                                Cv2.Multiply(baseFloat, baseWeight3, basePart);
                                Cv2.Multiply(warpFloat, warpWeight3, warpPart);
                                Cv2.Add(basePart, warpPart, sum);

                                Mat blended = new Mat();
                                sum.ConvertTo(blended, MatType.CV_8UC3);
                                return blended;
                            }
                        }
                    }
                }
            }
        }
    }
}
