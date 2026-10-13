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
    /// Feature based automatic panorama stitcher.
    ///
    /// Pipeline: grayscale -> detect/describe (SIFT or ORB) -> brute force knn match
    /// -> Lowe ratio test -> RANSAC homography -> warp onto a common canvas ->
    /// distance-weighted feather blend.
    /// </summary>
    public static class AutoStitcher
    {
        /// <summary>
        /// Stitch <paramref name="movingImage"/> onto <paramref name="referenceImage"/>.
        /// The reference image defines the coordinate frame of the panorama.
        /// </summary>
        /// <remarks>
        /// Both inputs are locked and drawn, so they belong to this call for its duration.
        /// GDI+ bitmaps are not thread safe: passing an image that the UI is painting at
        /// the same time fails intermittently with "位图区域已被锁定". Callers that keep
        /// painting their images must hand over private copies.
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

                        // Descriptors of the moving image are the query set, so the
                        // homography we fit maps moving -> reference directly.
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

                            // Always produced; the caller decides whether to display it.
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
                // Argument order follows OpenCV: edgeThreshold comes before firstLevel.
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
            // ORB produces binary descriptors, so it needs the Hamming metric.
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
                    // Mask shape unexpected: treat the match as an outlier rather than aborting.
                    flag = 0;
                }
                flags[i] = flag != 0;
            }
            result.MovingPoints = movingPreview.ToArray();
            result.ReferencePoints = referencePreview.ToArray();
            result.InlierFlags = flags;
        }

        /// <summary>
        /// Warps the moving image into the reference frame, grows the canvas to hold
        /// both, and blends the overlap with distance weighted feathering.
        /// Returns null (with <see cref="StitchResult.Message"/> set) when the result
        /// would be unreasonable in size.
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

            // Fold the canvas offset into the homography translation column.
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
        /// Distance weighted feather blend.
        ///
        /// Each mask is converted to a distance-to-edge map, clamped to the requested
        /// feather width, and used as that layer's weight. Weight = distance / total
        /// distance, so the seam lands where the two distances are equal and each layer
        /// keeps full weight where it is alone.
        ///
        /// The denominator is seeded with an epsilon before accumulating. OpenCV's
        /// divide returns Infinity for a zero denominator, which would poison the whole
        /// float image, so it must never be zero.
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

                // Clamp so the transition keeps a fixed width and the values stay bounded
                // even when a layer covers the whole canvas (no zero pixel to measure from).
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
