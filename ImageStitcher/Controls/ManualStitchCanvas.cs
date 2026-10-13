using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace ImageStitcher.Controls
{
    /// <summary>
    /// Manual stitching surface: shows two images that the user drags into alignment.
    ///
    /// Supported interactions
    ///   left drag          move a layer (grab whichever layer is under the cursor)
    ///   double click       bring the clicked layer to the front
    ///   middle drag        pan the view
    ///   mouse wheel        zoom about the cursor
    ///
    /// Z-order is explicit (<see cref="TopLayer"/>), the front layer can be drawn
    /// semi transparent to line features up (<see cref="TopOpacityPercent"/>), and it
    /// can be rotated for images with a slight angular difference.
    ///
    /// The control does not own the bitmaps handed to <see cref="SetImages"/>;
    /// call <see cref="ClearImages"/> before disposing them.
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
        private float _zoom = 1f;
        private PointF _pan;

        private int _draggingLayer = -1;
        private PointF _dragStartCanvas;
        private PointF _dragStartOffset;

        private bool _panning;

        /// <summary>
        /// Set once the user zooms or pans by hand. Until then the view keeps re-fitting
        /// itself whenever the control is resized, which matters because the canvas is
        /// created hidden and only receives its real size when manual mode is entered.
        /// </summary>
        private bool _userAdjustedView;
        private Point _panStartMouse;
        private PointF _panStartPan;

        private bool _needsFit;

        /// <summary>Raised when the front layer changes, including via double click.</summary>
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

        #region public API

        /// <summary>True when both images are present.</summary>
        public bool HasImages
        {
            get { return _layers[0].Image != null && _layers[1].Image != null; }
        }

        /// <summary>Index of the layer drawn on top: 0 = 图1, 1 = 图2.</summary>
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

        /// <summary>Opacity of the front layer, 0..100. 100 means fully opaque.</summary>
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

        /// <summary>Rotation of the front layer in degrees.</summary>
        public float TopRotation
        {
            get { return _layers[_topLayer].Rotation; }
            set { SetRotation(_topLayer, value); }
        }

        public float GetRotation(int index)
        {
            return _layers[ClampIndex(index)].Rotation;
        }

        /// <summary>Top-left position of a layer in canvas coordinates.</summary>
        public PointF GetOffset(int index)
        {
            return _layers[ClampIndex(index)].Offset;
        }

        /// <summary>Moves a layer. The canvas never clamps this; the user drags freely.</summary>
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

        /// <summary>Rotates the front layer by a relative amount.</summary>
        public void RotateTopBy(float deltaDegrees)
        {
            SetRotation(_topLayer, _layers[_topLayer].Rotation + deltaDegrees);
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
        /// Assigns the two images and arranges them side by side with roughly a 30%
        /// horizontal overlap, which is a comfortable starting point for alignment.
        /// Ownership of the bitmaps stays with the caller.
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

        /// <summary>Drops the image references without disposing them.</summary>
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

        /// <summary>Restores the initial overlapped arrangement and fits the view.</summary>
        public void ResetPositions()
        {
            Bitmap a = _layers[0].Image;
            Bitmap b = _layers[1].Image;
            if (a == null || b == null) return;
            SetImages(a, b);
        }

        /// <summary>Zooms and pans so the whole composition is visible.</summary>
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

        /// <summary>Multiplies the zoom about a point in client coordinates.</summary>
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

        /// <summary>Maps a canvas point to client (screen) coordinates.</summary>
        public PointF CanvasToClient(PointF canvas)
        {
            return new PointF(canvas.X * _zoom + _pan.X, canvas.Y * _zoom + _pan.Y);
        }

        /// <summary>Maps a client (screen) point to canvas coordinates.</summary>
        public PointF ClientToCanvas(Point screen)
        {
            return new PointF((screen.X - _pan.X) / _zoom, (screen.Y - _pan.Y) / _zoom);
        }

        /// <summary>Current view scale factor.</summary>
        public float Zoom
        {
            get { return _zoom; }
        }

        /// <summary>Union of both layers in canvas coordinates.</summary>
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

        /// <summary>Axis aligned bounds of one layer after rotation, in canvas coordinates.</summary>
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

        /// <summary>Index of the layer under the given client point, or -1. Front layer wins.</summary>
        public int HitTest(Point screen)
        {
            if (HitLayer(_topLayer, screen)) return _topLayer;
            int other = 1 - _topLayer;
            if (HitLayer(other, screen)) return other;
            return -1;
        }

        /// <summary>
        /// Renders the composition at native resolution into a new bitmap.
        /// <paramref name="opaqueTopLayer"/> forces the front layer to full opacity,
        /// which is normally what you want for the exported result even if the slider
        /// was pulled down while aligning. Returns null when there is nothing to draw.
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

        #region input

        protected override void OnMouseDown(MouseEventArgs e)
        {
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

        // WinForms delivers wheel messages to the focused control, so take focus when
        // the pointer enters the canvas to make wheel zoom behave as users expect.
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            if (HasImages && !Focused) Focus();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            // Refit after a resize until the user takes over the view. The canvas is
            // created hidden, so its real size only arrives when manual mode is entered.
            if (_needsFit || (!_userAdjustedView && HasImages)) TryFit();
        }

        #endregion

        #region painting

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

            // Cheap interpolation while the user is actively dragging keeps it responsive.
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
            // System.Drawing on .NET Framework has no ImageAttributes overload that takes a
            // RectangleF destination, so both layers go through the same rounded Rectangle
            // path. Using one path for every layer keeps the two images perfectly registered
            // with each other whatever the opacity slider is set to.
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
        /// Dashed outline around the front layer, drawn in client space so the pen stays
        /// one pixel wide regardless of zoom. Makes the z-order visible at a glance.
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

        #region geometry helpers

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

            // Undo the rotation about the layer centre, then test the axis aligned rect.
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
