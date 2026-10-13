using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using ImageStitcher.Stitching;

namespace ImageStitcher
{
    /// <summary>
    /// Application shell. Every control is declared in MainForm.Designer.cs; this file
    /// holds state and event handling only.
    /// </summary>
    public partial class MainForm : Form
    {
        private Bitmap _imageA;
        private Bitmap _imageB;
        private string _pathA;
        private string _pathB;

        private Bitmap _autoResult;
        private Bitmap _matchPreview;

        // Which bitmaps the manual canvas is currently holding, so we can tell when the
        // canvas still refers to a disposed image.
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

            UpdateSourceInfo();
            SetManualMode(false);
        }

        #region image loading

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
                loaded = LoadBitmapUnlocked(path);
            }
            catch (Exception ex)
            {
                ShowWarning("无法加载图像：" + ex.Message);
                return;
            }

            Bitmap previous;
            if (slot == 0)
            {
                previous = _imageA;
                _imageA = loaded;
                _pathA = path;
            }
            else
            {
                previous = _imageB;
                _imageB = loaded;
                _pathB = path;
            }

            RefreshPreviews();
            SyncManualCanvasImages(false);

            // Only safe once the picture boxes and the canvas have let go of it.
            if (previous != null) previous.Dispose();

            UpdateSourceInfo();
            SetStatus(string.Format("已加载{0}：{1}（{2} × {3}）",
                slot == 0 ? "图1" : "图2", Path.GetFileName(path),
                loaded.Width, loaded.Height));
        }

        /// <summary>
        /// Reads the file through memory so the source file is not locked, and
        /// normalises it to 24bpp RGB which is what the rest of the app assumes.
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

        #region automatic stitching

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

            // The result appears in the auto-mode tabs, so leave manual mode first.
            if (_manualMode) SetManualMode(false);

            // GDI+ bitmaps are not thread safe, and the previews - and the manual canvas -
            // paint these very objects. The engine locks its inputs with LockBits and draws
            // them for the match preview, so handing it the live bitmaps lets a repaint on
            // the UI thread collide with the worker and fail with "位图区域已被锁定".
            // The engine gets private snapshots instead; taking them here, while we are
            // still on the UI thread, is what keeps the two threads off each other's bits.
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
        /// Takes a private copy of a bitmap for the worker thread to own outright.
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

        /// <summary>Swaps the picture box image before disposing the old bitmap.</summary>
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

        #region manual stitching

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
                SyncManualControlsFromCanvas();
                SetStatus("手动模式：左键拖动图像对齐，双击图像置顶，中键拖动平移画布，滚轮缩放。");
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
        /// Keeps the canvas pointing at the images that are actually alive, and
        /// optionally restarts the arrangement.
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

        #region saving and housekeeping

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
            // Unbind the viewers first: a picture box holding a disposed bitmap would
            // fault on its next paint.
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
