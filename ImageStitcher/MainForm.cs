using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using ImageStitcher.Imaging;
using ImageStitcher.Stitching;

namespace ImageStitcher
{
    /// <summary>
    /// 应用程序主窗口。所有控件都在 MainForm.Designer.cs 中声明；本文件只保存状态
    /// 与事件处理逻辑。
    /// </summary>
    public partial class MainForm : Form
    {
        private Bitmap _imageA;
        private Bitmap _imageB;
        private string _pathA;
        private string _pathB;

        // 每个图像槽位背后的 DICOM 转换说明（窗值 / 是否反相），显示在状态栏中，
        // 这样窗值设置不当的图像能自我解释，而不会看起来像文件损坏。
        private string _dicomInfoA;
        private string _dicomInfoB;

        private Bitmap _autoResult;
        private Bitmap _matchPreview;

        // 手动画布当前持有的位图，用来判断画布是否仍指向已释放的图像。
        private Bitmap _canvasImageA;
        private Bitmap _canvasImageB;

        private bool _manualMode;
        private bool _busy;
        private bool _syncing;

        public MainForm()
        {
            InitializeComponent();

            cboAlgorithm.SelectedIndex = 0;
            cboBaseImage.SelectedIndex = 0;
            cboTopLayer.SelectedIndex = 1;

            trkOpacity.Value = 50;
            lblOpacityValue.Text = "50%";
            manualCanvas.TopOpacityPercent = 50;
            manualCanvas.TopLayer = 1;
            manualCanvas.NudgeAmount = 1;

            UpdateSourceInfo();
            SetManualMode(false);
        }

        #region 图像加载

        private void btnLoadA_Click(object sender, EventArgs e)
        {
            LoadImage(0);
        }

        private void btnLoadB_Click(object sender, EventArgs e)
        {
            LoadImage(1);
        }

        private void LoadImage(int slot)
        {
            if (_busy) return;

            openImageDialog.Title = slot == 0 ? "选择图1" : "选择图2";
            if (openImageDialog.ShowDialog(this) != DialogResult.OK) return;

            string path = openImageDialog.FileName;
            Bitmap loaded;
            try
            {
                loaded = ReadImage(path, out string dicomInfo);
                AdoptImage(slot, path, loaded, dicomInfo);
            }
            catch (Exception ex)
            {
                ShowWarning("无法加载图像：" + ex.Message);
                return;
            }

            SetStatus(string.Format("已加载{0}：{1}（{2} × {3}）{4}",
                slot == 0 ? "图1" : "图2", Path.GetFileName(path),
                loaded.Width, loaded.Height, SlotNote(slot)));
        }

        /// <summary>
        /// 把磁盘上任意受支持的文件转换成程序其余部分可用的位图。
        /// DICOM 需要先经过 <see cref="DicomLoader"/>，因为它的像素数据还不是图像——
        /// 必须先确定位深、光度解释和窗值，它才会看起来像一张图片。
        /// </summary>
        private static Bitmap ReadImage(string path, out string dicomInfo)
        {
            dicomInfo = null;

            if (!IsDicomPath(path)) return LoadBitmapUnlocked(path);

            Bitmap bitmap = DicomLoader.Load(path, out DicomLoader.DicomFrame frame);
            dicomInfo = DicomDescription(frame);
            return bitmap;
        }

        private void btnLoadDicomA_Click(object sender, EventArgs e)
        {
            LoadDicom(0);
        }

        private void btnLoadDicomB_Click(object sender, EventArgs e)
        {
            LoadDicom(1);
        }

        /// <summary>
        /// DICOM 入口：既接受单个文件，也接受整个序列文件夹。一个序列就是每个断层
        /// 一个文件，而本工具一次只能拼接两张图像，所以选择文件夹最终也必须落到
        /// 断层图像的挑选上。
        /// </summary>
        private void LoadDicom(int slot)
        {
            if (_busy) return;

            openDicomDialog.Title = slot == 0 ? "选择图1 的 DICOM 文件" : "选择图2 的 DICOM 文件";
            if (openDicomDialog.ShowDialog(this) != DialogResult.OK) return;

            string path = openDicomDialog.FileName;

            Bitmap loaded;
            try
            {
                loaded = DicomLoader.Load(path, out DicomLoader.DicomFrame frame);
                AdoptImage(slot, path, loaded, DicomDescription(frame));
            }
            catch (Exception ex)
            {
                ShowWarning("无法读取 DICOM 文件：" + ex.Message);
                return;
            }

            SetStatus(string.Format("已加载 DICOM 到{0}：{1}（{2} × {3}）{4}",
                slot == 0 ? "图1" : "图2", Path.GetFileName(path),
                loaded.Width, loaded.Height, SlotNote(slot)));
        }

        /// <summary>
        /// 把刚解码出来的位图放入某个槽位，同时保持各预览图和手动画布状态一致，
        /// 并释放被替换掉的位图。
        /// </summary>
        private void AdoptImage(int slot, string path, Bitmap loaded, string dicomInfo)
        {
            Bitmap previous;
            if (slot == 0)
            {
                previous = _imageA;
                _imageA = loaded;
                _pathA = path;
                _dicomInfoA = dicomInfo;
            }
            else
            {
                previous = _imageB;
                _imageB = loaded;
                _pathB = path;
                _dicomInfoB = dicomInfo;
            }

            RefreshPreviews();
            SyncManualCanvasImages(false);

            // 只有在图片框和画布都放开它之后才安全。
            if (previous != null) previous.Dispose();

            UpdateSourceInfo();
        }

        /// <summary>DICOM 文件允许使用的扩展名为真。</summary>
        private static bool IsDicomPath(string path)
        {
            string extension = (Path.GetExtension(path) ?? string.Empty).ToLowerInvariant();
            switch (extension)
            {
                case ".dcm":
                case ".dicom":
                case ".dic":
                case ".ima":
                case ".img":
                    return true;
                default:
                    return false;
            }
        }

        private static string DicomDescription(DicomLoader.DicomFrame frame)
        {
            if (frame == null) return null;

            string description = string.Format(CultureInfo.CurrentCulture,
                "DICOM {0} 位", frame.BitDepth);

            if (frame.WindowWidth.HasValue && frame.WindowCentre.HasValue)
            {
                description += string.Format(CultureInfo.CurrentCulture,
                    "，窗宽窗位 {0:0}/{1:0}", frame.WindowWidth.Value, frame.WindowCentre.Value);
            }

            if (frame.Inverted) description += "，MONOCHROME1 已反相";
            if (frame.SliceLocation.HasValue)
            {
                description += string.Format(CultureInfo.CurrentCulture, "，位置 {0:0.###}", frame.SliceLocation.Value);
            }

            return description;
        }

        private string SlotNote(int slot)
        {
            string note = slot == 0 ? _dicomInfoA : _dicomInfoB;
            return string.IsNullOrEmpty(note) ? string.Empty : "　[" + note + "]";
        }

        /// <summary>
        /// 通过内存读取文件，以免锁定源文件，并把它规范化为程序其余部分所假定的
        /// 24bpp RGB 格式。
        /// </summary>
        private static Bitmap LoadBitmapUnlocked(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            using (MemoryStream stream = new MemoryStream(bytes, false))
            using (Image image = Image.FromStream(stream))
            {
                Bitmap bitmap = new Bitmap(image.Width, image.Height, PixelFormat.Format24bppRgb);
                try
                {
                    using (Graphics graphics = Graphics.FromImage(bitmap))
                    {
                        graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                        graphics.PixelOffsetMode = PixelOffsetMode.Half;
                        graphics.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height));
                    }
                    return bitmap;
                }
                catch
                {
                    bitmap.Dispose();
                    throw;
                }
            }
        }

        private void btnSwap_Click(object sender, EventArgs e)
        {
            if (_imageA == null && _imageB == null)
            {
                ShowWarning("还没有加载图像。");
                return;
            }

            Bitmap imageSwap = _imageA;
            _imageA = _imageB;
            _imageB = imageSwap;

            string pathSwap = _pathA;
            _pathA = _pathB;
            _pathB = pathSwap;

            string noteSwap = _dicomInfoA;
            _dicomInfoA = _dicomInfoB;
            _dicomInfoB = noteSwap;

            RefreshPreviews();
            SyncManualCanvasImages(true);
            UpdateSourceInfo();
            SetStatus("已交换图1与图2。");
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            manualCanvas.ClearImages();
            _canvasImageA = null;
            _canvasImageB = null;
            ReleaseImages();
            SetStatus("已清空。请重新加载两张图像。");
        }

        private void RefreshPreviews()
        {
            picPreviewA.Image = _imageA;
            picPreviewB.Image = _imageB;
        }

        private void UpdateSourceInfo()
        {
            lblInfoA.Text = _imageA == null
                ? "图1：未加载"
                : string.Format("图1：{0}  ({1}×{2})",
                    Path.GetFileName(_pathA), _imageA.Width, _imageA.Height);

            lblInfoB.Text = _imageB == null
                ? "图2：未加载"
                : string.Format("图2：{0}  ({1}×{2})",
                    Path.GetFileName(_pathB), _imageB.Width, _imageB.Height);
        }

        #endregion

        #region 自动拼接

        private void btnAutoStitch_Click(object sender, EventArgs e)
        {
            if (_busy) return;

            if (_imageA == null || _imageB == null)
            {
                ShowWarning("请先加载两张图像。");
                return;
            }

            bool baseIsFirst = cboBaseImage.SelectedIndex != 1;
            Bitmap reference = baseIsFirst ? _imageA : _imageB;
            Bitmap moving = baseIsFirst ? _imageB : _imageA;

            StitchOptions options = new StitchOptions();
            options.Algorithm = cboAlgorithm.SelectedIndex == 1 ? StitchAlgorithm.Orb : StitchAlgorithm.Sift;

            // 结果显示在自动模式的选项卡中，因此先退出手动模式。
            if (_manualMode) SetManualMode(false);

            // GDI+ 位图不是线程安全的，而预览图和手动画布都会绘制这些对象本身。
            // 拼接引擎会用 LockBits 锁定输入并绘制它们以生成匹配预览图，因此如果把
            // 正在使用的位图直接交给它，界面线程的重绘就会与工作线程冲突，抛出
            // “位图区域已被锁定”。所以改为让引擎使用私有快照；在这里（仍处于界面
            // 线程时）拍下快照，正是让两个线程互不触碰对方数据的关键。
            Bitmap referenceSnapshot = null;
            Bitmap movingSnapshot = null;
            try
            {
                referenceSnapshot = Snapshot(reference);
                movingSnapshot = Snapshot(moving);
            }
            catch (Exception ex)
            {
                if (referenceSnapshot != null) referenceSnapshot.Dispose();
                if (movingSnapshot != null) movingSnapshot.Dispose();
                ShowWarning("无法为拼接准备图像副本：" + ex.Message);
                return;
            }

            string algorithmName = options.Algorithm == StitchAlgorithm.Sift ? "SIFT" : "ORB";
            SetBusy(true, string.Format("正在使用 {0} 拼接（基准：{1}），请稍候…",
                algorithmName, baseIsFirst ? "图1" : "图2"));

            Task.Run(() => AutoStitcher.Stitch(referenceSnapshot, movingSnapshot, options))
                .ContinueWith(task =>
                {
                    try
                    {
                        OnStitchCompleted(task);
                    }
                    finally
                    {
                        referenceSnapshot.Dispose();
                        movingSnapshot.Dispose();
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        /// <summary>
        /// 为工作线程生成一份完全归其所有的位图私有副本。
        /// </summary>
        private static Bitmap Snapshot(Bitmap source)
        {
            return source.Clone(new Rectangle(0, 0, source.Width, source.Height),
                PixelFormat.Format24bppRgb);
        }

        private void OnStitchCompleted(Task<StitchResult> task)
        {
            SetBusy(false, null);

            if (task.IsFaulted)
            {
                Exception error = task.Exception == null ? null : task.Exception.GetBaseException();
                ShowWarning("拼接过程中发生异常：" + (error == null ? "未知错误" : error.Message));
                return;
            }

            StitchResult result = task.Result;

            if (result.Image != null) ReplaceImage(ref _autoResult, result.Image, picResult);
            if (result.MatchPreview != null) ReplaceImage(ref _matchPreview, result.MatchPreview, picMatches);

            if (result.Success)
            {
                tabView.SelectedTab = chkShowMatches.Checked && _matchPreview != null
                    ? tabMatches
                    : tabResult;

                SetStatus(string.Format("{0}  重叠区域 {1:N0} 像素",
                    result.Message, result.OverlapPixels));
            }
            else
            {
                SetStatus(result.Message);
                ShowWarning(result.Message);
            }
        }

        /// <summary>先替换图片框的图像，再释放旧位图。</summary>
        private static void ReplaceImage(ref Bitmap field, Bitmap replacement, PictureBox target)
        {
            Bitmap previous = field;
            field = replacement;
            target.Image = replacement;
            if (previous != null) previous.Dispose();
        }

        private void btnSaveResult_Click(object sender, EventArgs e)
        {
            if (_autoResult == null)
            {
                ShowWarning("还没有自动拼接结果，请先执行自动拼接。");
                return;
            }

            SaveBitmap(_autoResult, "stitch_result.png", "保存自动拼接结果");
        }

        #endregion

        #region 手动拼接

        private void btnManualMode_Click(object sender, EventArgs e)
        {
            SetManualMode(!_manualMode);
        }

        private void SetManualMode(bool manual)
        {
            if (manual && (_imageA == null || _imageB == null))
            {
                ShowWarning("请先加载两张图像。");
                return;
            }

            _manualMode = manual;

            tabView.Visible = !manual;
            manualCanvas.Visible = manual;
            btnManualMode.Text = manual ? "返回自动模式" : "进入手动模式";
            lblMode.Text = manual ? "模式：手动" : "模式：自动";

            if (manual)
            {
                SyncManualCanvasImages(false);
                manualCanvas.TopOpacityPercent = trkOpacity.Value;
                manualCanvas.NudgeAmount = (int)numNudgeStep.Value;
                SyncManualControlsFromCanvas();

                // 把焦点交给画布，而不是留在用户刚点击的按钮上，这样方向键无需先点击
                // 就能立即微调。
                manualCanvas.Focus();

                SetStatus("手动模式：左键拖动对齐，方向键微调（Shift+方向键 10 倍），双击置顶，中键平移，滚轮缩放。");
            }
            else
            {
                manualCanvas.ClearImages();
                _canvasImageA = null;
                _canvasImageB = null;
                SetStatus("自动模式：选择算法与基准图后，点击“开始自动拼接”。");
            }
        }

        /// <summary>
        /// 让画布始终指向真正存活的图像，并可选地重新开始摆放。
        /// </summary>
        private void SyncManualCanvasImages(bool forceReset)
        {
            if (_imageA == null || _imageB == null)
            {
                manualCanvas.ClearImages();
                _canvasImageA = null;
                _canvasImageB = null;
                return;
            }

            bool stale =
                !manualCanvas.HasImages ||
                !ReferenceEquals(_canvasImageA, _imageA) ||
                !ReferenceEquals(_canvasImageB, _imageB);

            if (forceReset || stale)
            {
                manualCanvas.SetImages(_imageA, _imageB);
                _canvasImageA = _imageA;
                _canvasImageB = _imageB;
            }
        }

        private bool EnsureManualReady()
        {
            if (_imageA == null || _imageB == null)
            {
                ShowWarning("请先加载两张图像。");
                return false;
            }

            if (!_manualMode) SetManualMode(true);
            return _manualMode;
        }

        private void cboTopLayer_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_syncing) return;

            _syncing = true;
            try
            {
                manualCanvas.TopLayer = cboTopLayer.SelectedIndex;
                numRotation.Value = ClampRotation((decimal)manualCanvas.TopRotation);
            }
            finally
            {
                _syncing = false;
            }
        }

        private void manualCanvas_TopLayerChanged(object sender, EventArgs e)
        {
            if (_syncing) return;
            SyncManualControlsFromCanvas();
        }

        private void SyncManualControlsFromCanvas()
        {
            _syncing = true;
            try
            {
                cboTopLayer.SelectedIndex = manualCanvas.TopLayer;
                numRotation.Value = ClampRotation((decimal)manualCanvas.TopRotation);
                lblOpacityValue.Text = trkOpacity.Value + "%";
            }
            finally
            {
                _syncing = false;
            }
        }

        private decimal ClampRotation(decimal value)
        {
            if (value < numRotation.Minimum) return numRotation.Minimum;
            if (value > numRotation.Maximum) return numRotation.Maximum;
            return value;
        }

        private void trkOpacity_Scroll(object sender, EventArgs e)
        {
            lblOpacityValue.Text = trkOpacity.Value + "%";
            manualCanvas.TopOpacityPercent = trkOpacity.Value;
        }

        private void numRotation_ValueChanged(object sender, EventArgs e)
        {
            if (_syncing) return;
            manualCanvas.TopRotation = (float)numRotation.Value;
        }

        private void btnRotateLeft_Click(object sender, EventArgs e)
        {
            if (!EnsureManualReady()) return;
            manualCanvas.RotateTopBy(-0.5f);
            SyncManualControlsFromCanvas();
        }

        private void btnRotateRight_Click(object sender, EventArgs e)
        {
            if (!EnsureManualReady()) return;
            manualCanvas.RotateTopBy(0.5f);
            SyncManualControlsFromCanvas();
        }

        private void numNudgeStep_ValueChanged(object sender, EventArgs e)
        {
            manualCanvas.NudgeAmount = (int)numNudgeStep.Value;
        }

        private void btnNudgeLeft_Click(object sender, EventArgs e)
        {
            NudgeTop(-1, 0);
        }

        private void btnNudgeRight_Click(object sender, EventArgs e)
        {
            NudgeTop(1, 0);
        }

        private void btnNudgeUp_Click(object sender, EventArgs e)
        {
            NudgeTop(0, -1);
        }

        private void btnNudgeDown_Click(object sender, EventArgs e)
        {
            NudgeTop(0, 1);
        }

        /// <summary>
        /// 一次方向键按键的按钮等价实现，供更愿意点击而不愿按住键的用户使用。
        /// </summary>
        private void NudgeTop(int directionX, int directionY)
        {
            if (!EnsureManualReady()) return;

            float step = manualCanvas.NudgeAmount;
            manualCanvas.NudgeTopBy(directionX * step, directionY * step);
            ReportTopOffset();
        }

        private void ReportTopOffset()
        {
            PointF offset = manualCanvas.GetOffset(manualCanvas.TopLayer);
            SetStatus(string.Format("图{0} 位置：X {1:0.#}，Y {2:0.#}（方向键微调，Shift 加速）",
                manualCanvas.TopLayer + 1, offset.X, offset.Y));
        }

        private void btnResetPositions_Click(object sender, EventArgs e)
        {
            if (!EnsureManualReady()) return;
            manualCanvas.ResetPositions();
            SyncManualControlsFromCanvas();
            SetStatus("已重置图像位置与旋转角度。");
        }

        private void btnFitView_Click(object sender, EventArgs e)
        {
            if (!_manualMode)
            {
                ShowWarning("请先进入手动模式。");
                return;
            }

            manualCanvas.ResetView();
            SetStatus("视图已适应窗口。");
        }

        private void btnExportManual_Click(object sender, EventArgs e)
        {
            if (!EnsureManualReady()) return;

            Bitmap composite = manualCanvas.RenderComposite(chkOpaqueExport.Checked);
            if (composite == null)
            {
                ShowWarning("无法生成拼接结果：图像缺失，或结果尺寸超出限制。");
                return;
            }

            try
            {
                SaveBitmap(composite, "manual_stitch.png", "保存手动拼接结果");
            }
            finally
            {
                composite.Dispose();
            }
        }

        #endregion

        #region 保存与收尾工作

        private void SaveBitmap(Bitmap bitmap, string defaultFileName, string title)
        {
            saveImageDialog.Title = title;
            saveImageDialog.FileName = defaultFileName;

            if (saveImageDialog.ShowDialog(this) != DialogResult.OK) return;

            string path = saveImageDialog.FileName;
            try
            {
                bitmap.Save(path, FormatForPath(path));
                SetStatus("已保存：" + path);
            }
            catch (Exception ex)
            {
                ShowWarning("保存失败：" + ex.Message);
            }
        }

        private static ImageFormat FormatForPath(string path)
        {
            switch ((Path.GetExtension(path) ?? string.Empty).ToLowerInvariant())
            {
                case ".jpg":
                case ".jpeg": return ImageFormat.Jpeg;
                case ".bmp": return ImageFormat.Bmp;
                case ".tif":
                case ".tiff": return ImageFormat.Tiff;
                default: return ImageFormat.Png;
            }
        }

        private void SetBusy(bool busy, string status)
        {
            _busy = busy;

            progressMain.Visible = busy;

            btnAutoStitch.Enabled = !busy;
            btnSaveResult.Enabled = !busy;
            btnLoadA.Enabled = !busy;
            btnLoadB.Enabled = !busy;
            btnLoadDicomA.Enabled = !busy;
            btnLoadDicomB.Enabled = !busy;
            btnSwap.Enabled = !busy;
            btnClear.Enabled = !busy;
            btnManualMode.Enabled = !busy;
            btnExportManual.Enabled = !busy;
            cboAlgorithm.Enabled = !busy;
            cboBaseImage.Enabled = !busy;

            UseWaitCursor = busy;

            if (status != null) SetStatus(status);
        }

        private void SetStatus(string text)
        {
            lblStatus.Text = text;
            statusStripMain.Refresh();
        }

        private void ShowWarning(string text)
        {
            MessageBox.Show(this, text, "图像拼接", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void ReleaseImages()
        {
            // 先解除各视图的绑定：图片框若持有已释放的位图，下次绘制就会出错。
            picPreviewA.Image = null;
            picPreviewB.Image = null;
            picResult.Image = null;
            picMatches.Image = null;

            DisposeBitmap(ref _imageA);
            DisposeBitmap(ref _imageB);
            DisposeBitmap(ref _autoResult);
            DisposeBitmap(ref _matchPreview);

            _pathA = null;
            _pathB = null;

            UpdateSourceInfo();
        }

        private static void DisposeBitmap(ref Bitmap bitmap)
        {
            if (bitmap == null) return;
            bitmap.Dispose();
            bitmap = null;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_busy)
            {
                DialogResult answer = MessageBox.Show(this,
                    "正在拼接中，确定要退出吗？", "图像拼接",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (answer != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }

            manualCanvas.ClearImages();
            ReleaseImages();
        }

        #endregion
    }
}
