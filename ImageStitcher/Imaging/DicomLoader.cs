using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.Imaging.Codec;
using FellowOakDicom.Imaging.NativeCodec;

namespace ImageStitcher.Imaging
{
    /// <summary>
    /// 读取 DICOM 文件，并把其中的像素数据转换成程序其余部分使用的普通 24bpp RGB
    /// 位图——DICOM 必须先变成“一张普通图像”才能参与拼接，因为特征检测器和手动画布
    /// 都不了解任何模态像素数据的概念。
    ///
    /// 这里手工完成转换，而没有使用 <c>DicomImage.RenderImage</c>，是为了让所有分支
    /// 最终都得到同一种 Format24bppRgb 位图：
    ///
    ///   MONOCHROME1 / MONOCHROME2   按 WindowCenter / WindowWidth 做窗宽窗位映射；
    ///                               文件未携带窗值时，改用实际像素范围
    ///   INVERTED                    存储的像素值不做重标定
    ///   RGB                         从交错排列或分平面的帧数据重新组装
    ///
    /// 像素数据被压缩的文件由 fo-dicom 通过 codecs 包即时转码，这也正是
    /// <see cref="EnsureInitialized"/> 要在其他任何操作之前注册本机转码器的原因。
    /// </summary>
    internal static class DicomLoader
    {
        private static readonly object InitLock = new object();
        private static bool _initialised;

        /// <summary>在检查 DICOM 前导码之前，值得先尝试打开的扩展名。</summary>
        private static readonly string[] KnownExtensions = { ".dcm", ".dicom", ".dic", ".ima", ".img" };

        #region 初始化

        /// <summary>
        /// 每个进程只初始化一次 fo-dicom。可从任意线程安全调用。
        /// </summary>
        public static void EnsureInitialized()
        {
            if (_initialised) return;

            lock (InitLock)
            {
                if (_initialised) return;

                new DicomSetupBuilder()
                    .RegisterServices(services => services
                        .AddFellowOakDicom()
                        .AddTranscoderManager<NativeTranscoderManager>()
                        .AddImageManager<WinFormsImageManager>())
                    .SkipValidation()
                    .Build();

                _initialised = true;
            }
        }

        #endregion

        #region 元数据

        /// <summary>
        /// 从数据集中挑出的、在选择要加载的断层图像时有用的那一部分字段。
        /// </summary>
        internal sealed class DicomFrame
        {
            public string Path;
            public int Width;
            public int Height;
            public int BitDepth;
            public int InstanceNumber = int.MinValue;
            public string SeriesNumber = string.Empty;
            public double? SliceLocation;
            public string Description = string.Empty;

            /// <summary>转换灰度帧时使用的窗宽窗位，供状态栏显示。</summary>
            public double? WindowCentre;
            public double? WindowWidth;
            public bool Inverted;
        }

        /// <summary>小型结构体加上辅助方法，使元数据读取和渲染共用同一个窗值范围。</summary>
        private struct WindowRange
        {
            public double Lower;
            public double Upper;
        }

        #endregion

        #region 发现文件

        private static string Scalar(DicomDataset dataset, DicomTag tag)
        {
            string value;
            return dataset.TryGetString(tag, out value) ? value : string.Empty;
        }

        #endregion

        #region 加载

        /// <summary>
        /// 加载一个 DICOM 文件并把它的第一帧转换成 24bpp RGB 位图。
        /// <paramref name="info"/> 会带回所使用的窗值，以便在界面上显示。
        /// 文件不可用时抛出的异常消息是面向用户的。
        /// </summary>
        public static Bitmap Load(string path, out DicomFrame info)
        {
            EnsureInitialized();

            DicomFile file = DicomFile.Open(path);
            DicomDataset dataset = file.Dataset;

            if (!dataset.Contains(DicomTag.PixelData))
            {
                throw new NotSupportedException("这个 DICOM 文件里没有像素数据，无法生成图像。");
            }

            DicomPixelData pixelData = DicomPixelData.Create(dataset, false);
            int width = pixelData.Width;
            int height = pixelData.Height;

            if (width <= 0 || height <= 0)
            {
                throw new NotSupportedException("这个 DICOM 文件没有有效的图像尺寸。");
            }

            if (pixelData.NumberOfFrames > 1)
            {
                throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture,
                    "这个 DICOM 文件包含 {0} 帧，本工具一次只能拼接两张单帧图像，请先导出单帧文件。",
                    pixelData.NumberOfFrames));
            }

            // 下面所有转换分支都产出这种格式，画布、各预览图和 OpenCV 桥接层都依赖它。
            const PixelFormat format = PixelFormat.Format24bppRgb;

            byte[] data;
            try
            {
                data = FrameBytes(pixelData, 0);
            }
            catch (DicomCodecException error)
            {
                throw new NotSupportedException(
                    "这个 DICOM 文件使用了本机解码器不支持的压缩格式（" + error.Message + "）。", error);
            }

            if (pixelData.PhotometricInterpretation.IsPalette)
            {
                throw new NotSupportedException(
                    "暂不支持调色板（PALETTE COLOR）类型的 DICOM 文件，请先用其他工具转成灰度或 RGB。");
            }

            bool inverted = pixelData.PhotometricInterpretation == PhotometricInterpretation.Monochrome1;
            int samplesPerPixel = pixelData.SamplesPerPixel;
            // 这里用 BitsAllocated 而不是 BitDepth 结构体：此处只转换 8 位和 16 位样本，
            // 而该属性报告的正是这个值。
            int bitDepth = pixelData.BitsAllocated;
            WindowRange? window = null;
            Bitmap bitmap;

            if (pixelData.PhotometricInterpretation.IsColor)
            {
                if (pixelData.PhotometricInterpretation.IsYBR)
                {
                    throw new NotSupportedException(
                        "暂不支持 YBR 色彩空间的 DICOM 文件，请先转换成 RGB。");
                }

                if (samplesPerPixel != 3)
                {
                    throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture,
                        "暂不支持每像素 {0} 个通道的彩色 DICOM 文件。", samplesPerPixel));
                }

                if (bitDepth != 8)
                {
                    throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture,
                        "暂不支持 {0} 位彩色 DICOM 文件。", bitDepth));
                }

                bitmap = ConvertRgbFrame(data, width, height,
                    pixelData.PlanarConfiguration == PlanarConfiguration.Planar);
            }
            else
            {
                if (samplesPerPixel != 1)
                {
                    throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture,
                        "暂不支持每像素 {0} 个通道的 DICOM 文件。", samplesPerPixel));
                }

                if (bitDepth == 8)
                {
                    bitmap = ConvertGrayFrame(data, width, height,
                        pixelData.PixelRepresentation == PixelRepresentation.Signed, inverted);
                }
                else if (bitDepth == 16)
                {
                    bool signedData = pixelData.PixelRepresentation == PixelRepresentation.Signed;
                    WindowRange range = ReadWindow(dataset, pixelData, data);
                    window = range;
                    bitmap = ConvertGray16Frame(data, width, height, range, signedData, inverted);
                }
                else
                {
                    throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture,
                        "暂不支持 {0} 位灰度 DICOM 文件。", bitDepth));
                }
            }

            info = new DicomFrame
            {
                Path = path,
                Width = width,
                Height = height,
                BitDepth = bitDepth,
                SeriesNumber = Scalar(dataset, DicomTag.SeriesNumber),
                Description = dataset.GetSingleValueOrDefault(DicomTag.SeriesDescription, string.Empty),
                Inverted = inverted
            };

            if (info.Description.Length == 0)
            {
                info.Description = dataset.GetSingleValueOrDefault(DicomTag.StudyDescription, string.Empty);
            }

            if (window.HasValue)
            {
                info.WindowCentre = (window.Value.Lower + window.Value.Upper) / 2.0;
                info.WindowWidth = window.Value.Upper - window.Value.Lower;
            }

            decimal instanceNumber;
            if (dataset.TryGetSingleValue(DicomTag.InstanceNumber, out instanceNumber))
            {
                info.InstanceNumber = (int)instanceNumber;
            }

            double sliceLocation;
            if (dataset.TryGetSingleValue(DicomTag.SliceLocation, out sliceLocation))
            {
                info.SliceLocation = sliceLocation;
            }

            if (bitmap.PixelFormat != format)
            {
                // 防御性处理：上面的每个转换分支本来就会产出这种格式。
                Bitmap normalised = bitmap.Clone(new Rectangle(0, 0, bitmap.Width, bitmap.Height), format);
                bitmap.Dispose();
                bitmap = normalised;
            }

            return bitmap;
        }

        private static byte[] FrameBytes(DicomPixelData pixelData, int frame)
        {
            FellowOakDicom.IO.Buffer.IByteBuffer buffer = pixelData.GetFrame(frame);
            int size = (int)buffer.Size;

            if (buffer.IsMemory && buffer.Data != null && buffer.Data.Length == size)
            {
                // 未压缩的帧本身就在托管数组中，无需再复制。
                return buffer.Data;
            }

            byte[] copy = new byte[size];
            if (size > 0) buffer.GetByteRange(0, size, copy);
            return copy;
        }

        #endregion

        #region 像素转换

        /// <summary>灰度帧的行跨距辅助方法：8 位灰度占一个字节，16 位灰度占两个字节。</summary>
        private static void CheckFrameLength(byte[] data, int width, int height, int stride)
        {
            int expected = stride * width * height;
            if (data.Length < expected)
            {
                throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture,
                    "DICOM 像素数据长度（{0} 字节）小于图像尺寸要求（{1} 字节），文件可能已损坏。",
                    data.Length, expected));
            }
        }

        /// <summary>
        /// 在文件中寻找可用的窗值。找不到时退回到实际像素范围，这样即使文件里没有
        /// WindowCenter / WindowWidth，也能以完整对比度渲染。
        /// </summary>
        private static WindowRange ReadWindow(DicomDataset dataset, DicomPixelData pixelData, byte[] data)
        {
            bool signed = pixelData.PixelRepresentation == PixelRepresentation.Signed;
            int pixelCount = pixelData.Width * pixelData.Height;
            CheckFrameLength(data, pixelData.Width, pixelData.Height, 2);

            // 在数据流中存储值是无符号字；只有按补码解读之后图像才是有符号的，而文件里
            // 的 WindowCenter 也正是在同一个有符号空间中表达的。因此下面的回退范围与
            // 转换过程都统一走 Sample()。
            double centre;
            double width;
            if (dataset.TryGetSingleValue(DicomTag.WindowCenter, out centre) &&
                dataset.TryGetSingleValue(DicomTag.WindowWidth, out width) &&
                width > 0.0)
            {
                if (width < 1.0) width = 1.0;
                return new WindowRange { Lower = centre - width / 2.0, Upper = centre + width / 2.0 };
            }

            double min = double.MaxValue;
            double max = double.MinValue;

            for (int i = 0; i < pixelCount; i++)
            {
                double value = Sample(data, i, signed);
                if (value < min) min = value;
                if (value > max) max = value;
            }

            if (max <= min) max = min + 1.0;
            return new WindowRange { Lower = min, Upper = max };
        }

        /// <summary>
        /// 读取一个 16 位样本，按像素表示方式决定是否有符号。实际上所有传输语法都采用
        /// 小端序存储；其他情况在 fo-dicom 读取文件时已经完成字节交换。
        /// </summary>
        private static double Sample(byte[] data, int index, bool signed)
        {
            return signed
                ? (double)BitConverter.ToInt16(data, index * 2)
                : (double)BitConverter.ToUInt16(data, index * 2);
        }

        private static Bitmap ConvertGray16Frame(byte[] data, int width, int height, WindowRange window,
            bool signed, bool inverted)
        {
            CheckFrameLength(data, width, height, 2);

            int pixelCount = width * height;
            double scale = 255.0 / (window.Upper - window.Lower);
            byte[] pixels = new byte[pixelCount * 3];

            for (int i = 0; i < pixelCount; i++)
            {
                double value = Sample(data, i, signed);

                int level = (int)Math.Round((value - window.Lower) * scale);
                if (level < 0) level = 0;
                if (level > 255) level = 255;
                if (inverted) level = 255 - level;

                byte gray = (byte)level;
                int target = i * 3;
                pixels[target] = gray;
                pixels[target + 1] = gray;
                pixels[target + 2] = gray;
            }

            return ToBitmap(pixels, width, height);
        }

        private static Bitmap ConvertGrayFrame(byte[] data, int width, int height, bool signed, bool inverted)
        {
            CheckFrameLength(data, width, height, signed ? 2 : 1);

            int pixelCount = width * height;
            byte[] pixels = new byte[pixelCount * 3];

            for (int i = 0; i < pixelCount; i++)
            {
                int level = signed
                    ? (int)Math.Round(BitConverter.ToInt16(data, i * 2) / 256.0) + 128
                    : data[i];

                if (level < 0) level = 0;
                if (level > 255) level = 255;
                if (inverted) level = 255 - level;

                byte gray = (byte)level;
                int target = i * 3;
                pixels[target] = gray;
                pixels[target + 1] = gray;
                pixels[target + 2] = gray;
            }

            return ToBitmap(pixels, width, height);
        }

        private static Bitmap ConvertRgbFrame(byte[] data, int width, int height, bool planar)
        {
            CheckFrameLength(data, width, height, 3);

            int pixelCount = width * height;
            byte[] pixels = new byte[pixelCount * 3];

            if (planar)
            {
                int planeSize = pixelCount;
                for (int i = 0; i < pixelCount; i++)
                {
                    pixels[i * 3] = data[i];
                    pixels[i * 3 + 1] = data[planeSize + i];
                    pixels[i * 3 + 2] = data[planeSize * 2 + i];
                }
            }
            else
            {
                Buffer.BlockCopy(data, 0, pixels, 0, pixels.Length);
            }

            return ToBitmap(pixels, width, height);
        }

        private static Bitmap ToBitmap(byte[] pixels, int width, int height)
        {
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            try
            {
                BitmapData target = bitmap.LockBits(
                    new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                try
                {
                    IntPtr start = target.Scan0;
                    int rowBytes = width * 3;

                    for (int y = 0; y < height; y++)
                    {
                        Marshal.Copy(pixels, y * rowBytes, IntPtr.Add(start, y * target.Stride), rowBytes);
                    }
                }
                finally
                {
                    bitmap.UnlockBits(target);
                }

                return bitmap;
            }
            catch
            {
                bitmap.Dispose();
                throw;
            }
        }

        #endregion
    }
}
