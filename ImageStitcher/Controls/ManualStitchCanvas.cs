using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;

namespace ImageStitcher.Controls
{
    /// <summary>
    /// 手动拼接画布：显示两张图像，由用户拖动它们对齐。
    ///
    /// 支持的交互方式
    ///   左键拖动          移动图层（抓住光标下方的那一层）
    ///   方向键            以上层图层为单位微调一个像素（按住 Shift 为十个像素）
    ///   双击              把被点击的图层移到最上层
    ///   中键拖动          平移视图
    ///   鼠标滚轮          以光标位置为中心缩放
    ///
    /// 叠放顺序是显式控制的（<see cref="TopLayer"/>），上层图层可以半透明绘制以便
    /// 对齐特征（<see cref="TopOpacityPercent"/>），也可以旋转，用于处理存在轻微
    /// 角度差异的图像。
    ///
    /// 本控件不接管交给 <see cref="SetImages"/> 的位图；在释放这些位图之前请先调用
    /// <see cref="ClearImages"/>。
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public sealed class ManualStitchCanvas : Control
    {
        private const float MinZoom = 0.02f;
        private const float MaxZoom = 32f;
        private const int CompositeMaxSide = 30000;
        private const long CompositeMaxPixels = 200000000L;

        private sealed class Layer
        {
            public Bitmap Image;
            public PointF Offset;
            public float Rotation;

            public RectangleF CanvasBounds
            {
                get
                {
                    if (Image == null) return RectangleF.Empty;
                    return new RectangleF(Offset.X, Offset.Y, Image.Width, Image.Height);
                }
            }
        }

        private readonly Layer[] _layers;

        private int _topLayer;
        private int _topOpacityPercent = 50;
        private int _nudgeAmount = 1;
        private float _zoom = 1f;
        private PointF _pan;

        private int _draggingLayer = -1;
        private PointF _dragStartCanvas;
        private PointF _dragStartOffset;

        private bool _panning;

        /// <summary>
        /// 一旦用户手动缩放或平移过视图就置位。在那之前，控件尺寸变化时视图会持续
        /// 重新自适应——这一点很重要，因为画布创建时是隐藏的，只有进入手动模式时
        /// 才会获得真实尺寸。
        /// </summary>
        private bool _userAdjustedView;
        private Point _panStartMouse;
        private PointF _panStartPan;

        private bool _needsFit;

        /// <summary>上层图层发生变化时触发，通过双击改变时也会触发。</summary>
        public event EventHandler TopLayerChanged;

        public ManualStitchCanvas()
        {
            _layers = new Layer[] { new Layer(), new Layer() };

            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint |
                     ControlStyles.ResizeRedraw, true);
            DoubleBuffered = true;

            BackColor = Color.FromArgb(45, 45, 48);
            ForeColor = Color.Gainsboro;
            TabStop = true;
        }

        #region 公共 API

        /// <summary>两张图像都已加载时为 true。</summary>
        public bool HasImages
        {
            get { return _layers[0].Image != null && _layers[1].Image != null; }
        }

        /// <summary>绘制在最上层的图层索引：0 = 图1，1 = 图2。</summary>
        public int TopLayer
        {
            get { return _topLayer; }
            set
            {
                int normalised = value == 0 ? 0 : 1;
                if (_topLayer == normalised) return;
                _topLayer = normalised;
                Invalidate();
                EventHandler handler = TopLayerChanged;
                if (handler != null) handler(this, EventArgs.Empty);
            }
        }

        /// <summary>上层图层的不透明度，取值 0..100。100 表示完全不透明。</summary>
        public int TopOpacityPercent
        {
            get { return _topOpacityPercent; }
            set
            {
                int clamped = Math.Max(0, Math.Min(100, value));
                if (_topOpacityPercent == clamped) return;
                _topOpacityPercent = clamped;
                Invalidate();
            }
        }

        /// <summary>上层图层的旋转角度，单位为度。</summary>
        public float TopRotation
        {
            get { return _layers[_topLayer].Rotation; }
            set { SetRotation(_topLayer, value); }
        }

        public float GetRotation(int index)
        {
            return _layers[ClampIndex(index)].Rotation;
        }

        /// <summary>图层在画布坐标系中的左上角位置。</summary>
        public PointF GetOffset(int index)
        {
            return _layers[ClampIndex(index)].Offset;
        }

        /// <summary>移动某个图层。画布不会对此做任何限制，用户可自由拖动。</summary>
        public void SetOffset(int index, PointF offset)
        {
            Layer layer = _layers[ClampIndex(index)];
            if (layer.Offset == offset) return;
            layer.Offset = offset;
            Invalidate();
        }

        public void SetRotation(int index, float degrees)
        {
            Layer layer = _layers[ClampIndex(index)];
            float normalised = NormaliseAngle(degrees);
            if (Math.Abs(layer.Rotation - normalised) < 1e-4f) return;
            layer.Rotation = normalised;
            Invalidate();
        }

        /// <summary>按相对角度旋转上层图层。</summary>
        public void RotateTopBy(float deltaDegrees)
        {
            SetRotation(_topLayer, _layers[_topLayer].Rotation + deltaDegrees);
        }

        /// <summary>
        /// 按一次方向键所移动的距离，单位为画布像素，始终取整数，这样连续按键的结果
        /// 是可预期的，而不会累积舍入误差。
        /// </summary>
        public int NudgeAmount
        {
            get { return _nudgeAmount; }
            set
            {
                int clamped = Math.Max(1, Math.Min(500, value));
                _nudgeAmount = clamped;
            }
        }

        /// <summary>
        /// 按相对距离移动上层图层，单位为画布像素。方向键和微调按钮都走这条路径：
        /// 拖动适合粗略对齐，但一个像素的误差用鼠标几乎不可能命中。
        /// </summary>
        public void NudgeTopBy(float dx, float dy)
        {
            if (!HasImages || (dx == 0f && dy == 0f)) return;

            Layer layer = _layers[_topLayer];
            layer.Offset = new PointF(layer.Offset.X + dx, layer.Offset.Y + dy);
            Invalidate();
        }

        /// <summary>按相对距离移动指定图层，供需要操作特定图层的调用方使用。</summary>
        public void NudgeBy(int index, float dx, float dy)
        {
            Layer layer = _layers[ClampIndex(index)];
            if (layer.Image == null || (dx == 0f && dy == 0f)) return;

            layer.Offset = new PointF(layer.Offset.X + dx, layer.Offset.Y + dy);
            Invalidate();
        }

        public void BringToFront(int index)
        {
            TopLayer = ClampIndex(index);
        }

        public void SwapTopLayer()
        {
            TopLayer = 1 - _topLayer;
        }

        /// <summary>
        /// 设置两张图像，并把它们并排放置，水平方向大致重叠 30%，这是一个比较舒适的
        /// 对齐起始状态。位图的所有权仍归调用方所有。
        /// </summary>
        public void SetImages(Bitmap imageA, Bitmap imageB)
        {
            _draggingLayer = -1;
            _panning = false;

            _layers[0].Image = imageA;
            _layers[0].Rotation = 0f;
            _layers[0].Offset = PointF.Empty;

            _layers[1].Image = imageB;
            _layers[1].Rotation = 0f;

            if (imageA != null && imageB != null)
            {
                float overlap = imageA.Width * 0.3f;
                _layers[1].Offset = new PointF(
                    imageA.Width - overlap,
                    (imageA.Height - imageB.Height) / 2f);
            }
            else
            {
                _layers[1].Offset = PointF.Empty;
            }

            _topLayer = 1;
            _userAdjustedView = false;
            _needsFit = true;
            TryFit();

            EventHandler handler = TopLayerChanged;
            if (handler != null) handler(this, EventArgs.Empty);

            Invalidate();
        }

        /// <summary>放弃对图像的引用，但不释放它们。</summary>
        public void ClearImages()
        {
            _layers[0].Image = null;
            _layers[1].Image = null;
            _layers[0].Rotation = 0f;
            _layers[1].Rotation = 0f;
            _layers[0].Offset = PointF.Empty;
            _layers[1].Offset = PointF.Empty;
            _draggingLayer = -1;
            _panning = false;
            Invalidate();
        }

        /// <summary>恢复初始的重叠摆放方式并让视图自适应。</summary>
        public void ResetPositions()
        {
            Bitmap a = _layers[0].Image;
            Bitmap b = _layers[1].Image;
            if (a == null || b == null) return;
            SetImages(a, b);
        }

        /// <summary>缩放并平移视图，使整个画面都可见。</summary>
        public void ResetView()
        {
            _userAdjustedView = false;

            RectangleF content = GetContentBounds();
            if (content.Width <= 0f || content.Height <= 0f)
            {
                _zoom = 1f;
                _pan = PointF.Empty;
                Invalidate();
                return;
            }

            const float margin = 16f;
            float availableWidth = Math.Max(1f, ClientSize.Width - margin * 2f);
            float availableHeight = Math.Max(1f, ClientSize.Height - margin * 2f);

            float fit = Math.Min(availableWidth / content.Width, availableHeight / content.Height);
            _zoom = Clamp(fit, MinZoom, MaxZoom);

            _pan = new PointF(
                (ClientSize.Width - content.Width * _zoom) / 2f - content.X * _zoom,
                (ClientSize.Height - content.Height * _zoom) / 2f - content.Y * _zoom);

            Invalidate();
        }

        /// <summary>以客户区坐标中的某一点为中心按倍数缩放。</summary>
        public void ZoomAt(Point screenAnchor, float factor)
        {
            if (!HasImages) return;

            float target = Clamp(_zoom * factor, MinZoom, MaxZoom);
            if (Math.Abs(target - _zoom) < 1e-6f) return;

            PointF anchorInCanvas = CanvasFromScreen(screenAnchor);
            _zoom = target;
            _pan = new PointF(
                screenAnchor.X - anchorInCanvas.X * _zoom,
                screenAnchor.Y - anchorInCanvas.Y * _zoom);
            _userAdjustedView = true;
            Invalidate();
        }

        /// <summary>把画布坐标点映射为客户端（屏幕）坐标。</summary>
        public PointF CanvasToClient(PointF canvas)
        {
            return new PointF(canvas.X * _zoom + _pan.X, canvas.Y * _zoom + _pan.Y);
        }

        /// <summary>把客户端（屏幕）坐标点映射为画布坐标。</summary>
        public PointF ClientToCanvas(Point screen)
        {
            return new PointF((screen.X - _pan.X) / _zoom, (screen.Y - _pan.Y) / _zoom);
        }

        /// <summary>当前视图的缩放比例。</summary>
        public float Zoom
        {
            get { return _zoom; }
        }

        /// <summary>两个图层在画布坐标系中的并集。</summary>
        public RectangleF GetContentBounds()
        {
            RectangleF bounds = RectangleF.Empty;
            for (int i = 0; i < _layers.Length; i++)
            {
                RectangleF layerBounds = GetLayerBounds(i);
                if (layerBounds.Width <= 0f || layerBounds.Height <= 0f) continue;
                bounds = bounds.Width <= 0f ? layerBounds : Union(bounds, layerBounds);
            }
            return bounds;
        }

        /// <summary>某个图层旋转后按轴对齐的外接矩形，使用画布坐标。</summary>
        public RectangleF GetLayerBounds(int index)
        {
            Layer layer = _layers[ClampIndex(index)];
            if (layer.Image == null) return RectangleF.Empty;

            RectangleF rect = layer.CanvasBounds;
            if (layer.Rotation == 0f) return rect;

            PointF[] corners = CornersOf(rect);
            RotateAbout(corners, CentreOf(rect), layer.Rotation);
            return BoundingBox(corners);
        }

        /// <summary>给定客户端点下方图层的索引，没有则返回 -1。上层图层优先。</summary>
        public int HitTest(Point screen)
        {
            if (HitLayer(_topLayer, screen)) return _topLayer;
            int other = 1 - _topLayer;
            if (HitLayer(other, screen)) return other;
            return -1;
        }

        /// <summary>
        /// 按原始分辨率把画面渲染到一个新的位图中。
        /// <paramref name="opaqueTopLayer"/> 会强制上层图层完全不透明，对齐过程中即使
        /// 把滑块调低过，导出的结果通常也希望如此。没有可绘制内容时返回 null。
        /// </summary>
        public Bitmap RenderComposite(bool opaqueTopLayer)
        {
            if (!HasImages) return null;

            RectangleF content = GetContentBounds();
            int width = (int)Math.Ceiling(content.Width);
            int height = (int)Math.Ceiling(content.Height);
            if (width < 1 || height < 1) return null;
            if (width > CompositeMaxSide || height > CompositeMaxSide ||
                (long)width * height > CompositeMaxPixels) return null;

            Bitmap composite = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            try
            {
                using (Graphics graphics = Graphics.FromImage(composite))
                {
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    graphics.Clear(Color.Black);

                    GraphicsState state = graphics.Save();
                    graphics.TranslateTransform(-content.X, -content.Y);
                    DrawLayer(graphics, 1 - _topLayer, 255);
                    DrawLayer(graphics, _topLayer, opaqueTopLayer ? 255 : TopAlpha);
                    graphics.Restore(state);
                }
                return composite;
            }
            catch
            {
                composite.Dispose();
                throw;
            }
        }

        #endregion

        #region 输入处理

        protected override void OnMouseDown(MouseEventArgs e)
        {
            // 点击画布同时也意味着要接管键盘焦点，这样用户拖动结束后方向键仍能继续微调。
            if (!Focused) Focus();
            base.OnMouseDown(e);

            if (e.Button == MouseButtons.Middle)
            {
                _panning = true;
                _panStartMouse = e.Location;
                _panStartPan = _pan;
                Cursor = Cursors.SizeAll;
                return;
            }

            if (e.Button == MouseButtons.Left && HasImages)
            {
                int hit = HitTest(e.Location);
                if (hit >= 0)
                {
                    _draggingLayer = hit;
                    _dragStartCanvas = CanvasFromScreen(e.Location);
                    _dragStartOffset = _layers[hit].Offset;
                    Capture = true;
                    Cursor = Cursors.SizeAll;
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_panning)
            {
                _pan = new PointF(
                    _panStartPan.X + (e.X - _panStartMouse.X),
                    _panStartPan.Y + (e.Y - _panStartMouse.Y));
                _userAdjustedView = true;
                Invalidate();
                return;
            }

            if (_draggingLayer >= 0)
            {
                PointF current = CanvasFromScreen(e.Location);
                Layer layer = _layers[_draggingLayer];
                layer.Offset = new PointF(
                    _dragStartOffset.X + (current.X - _dragStartCanvas.X),
                    _dragStartOffset.Y + (current.Y - _dragStartCanvas.Y));
                Invalidate();
                return;
            }

            Cursor = HasImages && HitTest(e.Location) >= 0 ? Cursors.SizeAll : Cursors.Default;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (e.Button == MouseButtons.Middle)
            {
                _panning = false;
                Cursor = Cursors.Default;
            }

            if (e.Button == MouseButtons.Left && _draggingLayer >= 0)
            {
                _draggingLayer = -1;
                Capture = false;
                Cursor = HasImages && HitTest(e.Location) >= 0 ? Cursors.SizeAll : Cursors.Default;
            }
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);

            if (e.Button != MouseButtons.Left || !HasImages) return;

            int hit = HitTest(e.Location);
            if (hit >= 0) BringToFront(hit);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (!HasImages) return;

            ZoomAt(e.Location, e.Delta > 0 ? 1.15f : 1f / 1.15f);
        }

        // WinForms 会把滚轮消息发送给拥有焦点的控件，因此指针进入画布时即获取焦点，
        // 让滚轮缩放符合用户预期。
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            if (HasImages && !Focused) Focus();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Handled) return;

            e.Handled = ApplyNudge(e.KeyCode, e.Shift);
        }

        /// <summary>
        /// 方向键属于导航键，否则 Windows 会把它们交给父容器而不是画布。在这里接管
        /// 它们，才能让画布停靠在视图面板内时微调仍然可用。
        /// </summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            Keys key = keyData & Keys.KeyCode;
            if ((keyData & (Keys.Control | Keys.Alt)) == Keys.None &&
                (key == Keys.Left || key == Keys.Right || key == Keys.Up || key == Keys.Down) &&
                ApplyNudge(key, (keyData & Keys.Shift) == Keys.Shift))
            {
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        /// <summary>
        /// 按四个方向键之一微调上层图层。其他按键或没有可移动内容时返回 false，
        /// 让该按键继续向下传递。
        /// </summary>
        private bool ApplyNudge(Keys key, bool fast)
        {
            if (!HasImages) return false;

            float step = NudgeAmount * (fast ? 10f : 1f);
            switch (key)
            {
                case Keys.Left:
                    NudgeTopBy(-step, 0f);
                    return true;
                case Keys.Right:
                    NudgeTopBy(step, 0f);
                    return true;
                case Keys.Up:
                    NudgeTopBy(0f, -step);
                    return true;
                case Keys.Down:
                    NudgeTopBy(0f, step);
                    return true;
                default:
                    return false;
            }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            // 在用户接管视图之前，每次改变尺寸后都重新自适应。画布创建时是隐藏的，
            // 只有进入手动模式时才会获得真实尺寸。
            if (_needsFit || (!_userAdjustedView && HasImages)) TryFit();
        }

        #endregion

        #region 绘制

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics graphics = e.Graphics;
            graphics.Clear(BackColor);

            if (!HasImages)
            {
                TextRenderer.DrawText(graphics, "请先加载两张图像，然后点击“进入手动模式”",
                    Font, ClientRectangle, Color.FromArgb(170, 170, 170),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.WordBreak);
                return;
            }

            // 用户正在拖动时使用开销更低的插值方式以保持流畅。
            bool interacting = _draggingLayer >= 0 || _panning;
            graphics.InterpolationMode = interacting
                ? InterpolationMode.Bilinear
                : InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            GraphicsState state = graphics.Save();
            graphics.TranslateTransform(_pan.X, _pan.Y);
            graphics.ScaleTransform(_zoom, _zoom);

            DrawLayer(graphics, 1 - _topLayer, 255);
            DrawLayer(graphics, _topLayer, TopAlpha);

            graphics.Restore(state);

            DrawFrontLayerOutline(graphics);
        }

        private void DrawLayer(Graphics graphics, int index, int alpha)
        {
            Layer layer = _layers[index];
            if (layer.Image == null || alpha <= 0) return;

            RectangleF rect = layer.CanvasBounds;

            if (layer.Rotation == 0f)
            {
                DrawImage(graphics, layer.Image, rect, alpha);
                return;
            }

            GraphicsState state = graphics.Save();
            PointF centre = CentreOf(rect);
            graphics.TranslateTransform(centre.X, centre.Y);
            graphics.RotateTransform(layer.Rotation);
            graphics.TranslateTransform(-centre.X, -centre.Y);
            DrawImage(graphics, layer.Image, rect, alpha);
            graphics.Restore(state);
        }

        private static void DrawImage(Graphics graphics, Bitmap image, RectangleF destination, int alpha)
        {
            // .NET Framework 上的 System.Drawing 没有接受 RectangleF 目标矩形的
            // ImageAttributes 重载，因此两个图层都走同一条取整后的 Rectangle 路径。
            // 所有图层共用一条路径，可以让两张图像无论不透明度滑块调到多少都保持
            // 完全精确的配准关系。
            Rectangle target = Rectangle.Round(destination);

            using (ImageAttributes attributes = new ImageAttributes())
            {
                ColorMatrix matrix = new ColorMatrix();
                matrix.Matrix33 = alpha / 255f;
                attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                graphics.DrawImage(image, target, 0, 0, image.Width, image.Height,
                    GraphicsUnit.Pixel, attributes);
            }
        }

        /// <summary>
        /// 围绕上层图层绘制的虚线轮廓，在客户端空间绘制，因此无论缩放比例如何画笔
        /// 都保持一个像素宽。可以让叠放顺序一目了然。
        /// </summary>
        private void DrawFrontLayerOutline(Graphics graphics)
        {
            if (_layers[_topLayer].Image == null) return;

            RectangleF rect = _layers[_topLayer].CanvasBounds;
            PointF[] corners = CornersOf(rect);
            if (_layers[_topLayer].Rotation != 0f)
            {
                RotateAbout(corners, CentreOf(rect), _layers[_topLayer].Rotation);
            }
            for (int i = 0; i < corners.Length; i++)
            {
                corners[i] = ScreenFromCanvas(corners[i]);
            }

            using (Pen pen = new Pen(Color.FromArgb(150, 66, 150, 250), 1f))
            {
                pen.DashStyle = DashStyle.Dash;
                graphics.DrawPolygon(pen, corners);
            }
        }

        #endregion

        #region 几何辅助方法

        private int TopAlpha
        {
            get { return (int)Math.Round(_topOpacityPercent * 255.0 / 100.0); }
        }

        private bool HitLayer(int index, Point screen)
        {
            Layer layer = _layers[ClampIndex(index)];
            if (layer.Image == null) return false;

            PointF point = CanvasFromScreen(screen);
            RectangleF rect = layer.CanvasBounds;

            if (layer.Rotation == 0f) return rect.Contains(point);

            // 先按图层中心反向抵消旋转，再对轴对齐矩形做命中判断。
            PointF centre = CentreOf(rect);
            double radians = -layer.Rotation * Math.PI / 180.0;
            float dx = point.X - centre.X;
            float dy = point.Y - centre.Y;
            float localX = (float)(dx * Math.Cos(radians) - dy * Math.Sin(radians));
            float localY = (float)(dx * Math.Sin(radians) + dy * Math.Cos(radians));

            return Math.Abs(localX) <= rect.Width / 2f && Math.Abs(localY) <= rect.Height / 2f;
        }

        private PointF CanvasFromScreen(Point screen)
        {
            return ClientToCanvas(screen);
        }

        private PointF ScreenFromCanvas(PointF canvas)
        {
            return CanvasToClient(canvas);
        }

        private void TryFit()
        {
            if (ClientSize.Width < 16 || ClientSize.Height < 16) return;
            _needsFit = false;
            ResetView();
        }

        private static int ClampIndex(int index)
        {
            return index == 0 ? 0 : 1;
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private static float NormaliseAngle(float degrees)
        {
            while (degrees > 180f) degrees -= 360f;
            while (degrees <= -180f) degrees += 360f;
            return degrees;
        }

        private static PointF CentreOf(RectangleF rect)
        {
            return new PointF(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);
        }

        private static PointF[] CornersOf(RectangleF rect)
        {
            return new[]
            {
                new PointF(rect.Left, rect.Top),
                new PointF(rect.Right, rect.Top),
                new PointF(rect.Right, rect.Bottom),
                new PointF(rect.Left, rect.Bottom)
            };
        }

        private static void RotateAbout(PointF[] points, PointF centre, float degrees)
        {
            double radians = degrees * Math.PI / 180.0;
            double cos = Math.Cos(radians);
            double sin = Math.Sin(radians);

            for (int i = 0; i < points.Length; i++)
            {
                float dx = points[i].X - centre.X;
                float dy = points[i].Y - centre.Y;
                points[i] = new PointF(
                    centre.X + (float)(dx * cos - dy * sin),
                    centre.Y + (float)(dx * sin + dy * cos));
            }
        }

        private static RectangleF BoundingBox(PointF[] points)
        {
            float minX = points[0].X, maxX = points[0].X;
            float minY = points[0].Y, maxY = points[0].Y;

            for (int i = 1; i < points.Length; i++)
            {
                if (points[i].X < minX) minX = points[i].X;
                if (points[i].X > maxX) maxX = points[i].X;
                if (points[i].Y < minY) minY = points[i].Y;
                if (points[i].Y > maxY) maxY = points[i].Y;
            }

            return RectangleF.FromLTRB(minX, minY, maxX, maxY);
        }

        private static RectangleF Union(RectangleF a, RectangleF b)
        {
            return RectangleF.FromLTRB(
                Math.Min(a.Left, b.Left),
                Math.Min(a.Top, b.Top),
                Math.Max(a.Right, b.Right),
                Math.Max(a.Bottom, b.Bottom));
        }

        #endregion
    }
}
