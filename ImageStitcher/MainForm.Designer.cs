namespace ImageStitcher
{
    partial class MainForm
    {
        /// <summary>Required designer variable.</summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>Clean up any resources being used.</summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.openImageDialog = new System.Windows.Forms.OpenFileDialog();
            this.saveImageDialog = new System.Windows.Forms.SaveFileDialog();
            this.tableRoot = new System.Windows.Forms.TableLayoutPanel();
            this.tableHeader = new System.Windows.Forms.TableLayoutPanel();
            this.grpSource = new System.Windows.Forms.GroupBox();
            this.tblSource = new System.Windows.Forms.TableLayoutPanel();
            this.btnLoadA = new System.Windows.Forms.Button();
            this.btnLoadB = new System.Windows.Forms.Button();
            this.btnSwap = new System.Windows.Forms.Button();
            this.btnClear = new System.Windows.Forms.Button();
            this.lblInfoA = new System.Windows.Forms.Label();
            this.lblInfoB = new System.Windows.Forms.Label();
            this.grpAuto = new System.Windows.Forms.GroupBox();
            this.tblAuto = new System.Windows.Forms.TableLayoutPanel();
            this.lblAlgorithm = new System.Windows.Forms.Label();
            this.cboAlgorithm = new System.Windows.Forms.ComboBox();
            this.lblBase = new System.Windows.Forms.Label();
            this.cboBaseImage = new System.Windows.Forms.ComboBox();
            this.chkShowMatches = new System.Windows.Forms.CheckBox();
            this.btnAutoStitch = new System.Windows.Forms.Button();
            this.btnSaveResult = new System.Windows.Forms.Button();
            this.grpManual = new System.Windows.Forms.GroupBox();
            this.tblManual = new System.Windows.Forms.TableLayoutPanel();
            this.lblTop = new System.Windows.Forms.Label();
            this.cboTopLayer = new System.Windows.Forms.ComboBox();
            this.lblRotation = new System.Windows.Forms.Label();
            this.numRotation = new System.Windows.Forms.NumericUpDown();
            this.btnRotateLeft = new System.Windows.Forms.Button();
            this.btnRotateRight = new System.Windows.Forms.Button();
            this.lblOpacity = new System.Windows.Forms.Label();
            this.trkOpacity = new System.Windows.Forms.TrackBar();
            this.lblOpacityValue = new System.Windows.Forms.Label();
            this.btnManualMode = new System.Windows.Forms.Button();
            this.btnResetPositions = new System.Windows.Forms.Button();
            this.btnFitView = new System.Windows.Forms.Button();
            this.chkOpaqueExport = new System.Windows.Forms.CheckBox();
            this.btnExportManual = new System.Windows.Forms.Button();
            this.tableContent = new System.Windows.Forms.TableLayoutPanel();
            this.tablePreviews = new System.Windows.Forms.TableLayoutPanel();
            this.grpPreviewA = new System.Windows.Forms.GroupBox();
            this.picPreviewA = new System.Windows.Forms.PictureBox();
            this.grpPreviewB = new System.Windows.Forms.GroupBox();
            this.picPreviewB = new System.Windows.Forms.PictureBox();
            this.panelView = new System.Windows.Forms.Panel();
            this.manualCanvas = new ImageStitcher.Controls.ManualStitchCanvas();
            this.tabView = new System.Windows.Forms.TabControl();
            this.tabResult = new System.Windows.Forms.TabPage();
            this.picResult = new System.Windows.Forms.PictureBox();
            this.tabMatches = new System.Windows.Forms.TabPage();
            this.picMatches = new System.Windows.Forms.PictureBox();
            this.statusStripMain = new System.Windows.Forms.StatusStrip();
            this.lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.progressMain = new System.Windows.Forms.ToolStripProgressBar();
            this.lblMode = new System.Windows.Forms.ToolStripStatusLabel();
            this.tableRoot.SuspendLayout();
            this.tableHeader.SuspendLayout();
            this.grpSource.SuspendLayout();
            this.tblSource.SuspendLayout();
            this.grpAuto.SuspendLayout();
            this.tblAuto.SuspendLayout();
            this.grpManual.SuspendLayout();
            this.tblManual.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numRotation)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkOpacity)).BeginInit();
            this.tableContent.SuspendLayout();
            this.tablePreviews.SuspendLayout();
            this.grpPreviewA.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picPreviewA)).BeginInit();
            this.grpPreviewB.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picPreviewB)).BeginInit();
            this.panelView.SuspendLayout();
            this.tabView.SuspendLayout();
            this.tabResult.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picResult)).BeginInit();
            this.tabMatches.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picMatches)).BeginInit();
            this.statusStripMain.SuspendLayout();
            this.SuspendLayout();
            // 
            // openImageDialog
            // 
            this.openImageDialog.Filter = "图像文件|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff;*.webp|所有文件|*.*";
            this.openImageDialog.Title = "选择图像";
            // 
            // saveImageDialog
            // 
            this.saveImageDialog.DefaultExt = "png";
            this.saveImageDialog.Filter = "PNG 图像|*.png|JPEG 图像|*.jpg|BMP 图像|*.bmp|TIFF 图像|*.tif";
            this.saveImageDialog.Title = "保存图像";
            // 
            // tableRoot
            // 
            this.tableRoot.ColumnCount = 1;
            this.tableRoot.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableRoot.Controls.Add(this.tableHeader, 0, 0);
            this.tableRoot.Controls.Add(this.tableContent, 0, 1);
            this.tableRoot.Controls.Add(this.statusStripMain, 0, 2);
            this.tableRoot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableRoot.Location = new System.Drawing.Point(0, 0);
            this.tableRoot.Margin = new System.Windows.Forms.Padding(0);
            this.tableRoot.Name = "tableRoot";
            this.tableRoot.RowCount = 3;
            this.tableRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 196F));
            this.tableRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableRoot.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableRoot.Size = new System.Drawing.Size(1340, 880);
            this.tableRoot.TabIndex = 0;
            // 
            // tableHeader
            // 
            this.tableHeader.ColumnCount = 3;
            this.tableHeader.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 246F));
            this.tableHeader.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 36F));
            this.tableHeader.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 64F));
            this.tableHeader.Controls.Add(this.grpSource, 0, 0);
            this.tableHeader.Controls.Add(this.grpAuto, 1, 0);
            this.tableHeader.Controls.Add(this.grpManual, 2, 0);
            this.tableHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableHeader.Location = new System.Drawing.Point(0, 0);
            this.tableHeader.Margin = new System.Windows.Forms.Padding(0);
            this.tableHeader.Name = "tableHeader";
            this.tableHeader.RowCount = 1;
            this.tableHeader.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableHeader.Size = new System.Drawing.Size(1340, 196);
            this.tableHeader.TabIndex = 0;
            // 
            // grpSource
            // 
            this.grpSource.Controls.Add(this.tblSource);
            this.grpSource.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpSource.Location = new System.Drawing.Point(4, 4);
            this.grpSource.Margin = new System.Windows.Forms.Padding(4);
            this.grpSource.Name = "grpSource";
            this.grpSource.Padding = new System.Windows.Forms.Padding(6);
            this.grpSource.Size = new System.Drawing.Size(238, 188);
            this.grpSource.TabIndex = 0;
            this.grpSource.TabStop = false;
            this.grpSource.Text = "1. 图像源";
            // 
            // tblSource
            // 
            this.tblSource.ColumnCount = 4;
            this.tblSource.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tblSource.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tblSource.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tblSource.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tblSource.Controls.Add(this.btnLoadA, 0, 0);
            this.tblSource.Controls.Add(this.btnLoadB, 1, 0);
            this.tblSource.Controls.Add(this.btnSwap, 2, 0);
            this.tblSource.Controls.Add(this.btnClear, 3, 0);
            this.tblSource.Controls.Add(this.lblInfoA, 0, 1);
            this.tblSource.Controls.Add(this.lblInfoB, 0, 2);
            this.tblSource.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblSource.Location = new System.Drawing.Point(6, 29);
            this.tblSource.Margin = new System.Windows.Forms.Padding(0);
            this.tblSource.Name = "tblSource";
            this.tblSource.RowCount = 4;
            this.tblSource.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.tblSource.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.tblSource.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.tblSource.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblSource.Size = new System.Drawing.Size(226, 153);
            this.tblSource.TabIndex = 0;
            // 
            // btnLoadA
            // 
            this.btnLoadA.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnLoadA.Location = new System.Drawing.Point(3, 3);
            this.btnLoadA.Name = "btnLoadA";
            this.btnLoadA.Size = new System.Drawing.Size(50, 30);
            this.btnLoadA.TabIndex = 0;
            this.btnLoadA.Text = "加载图1";
            this.btnLoadA.UseVisualStyleBackColor = true;
            this.btnLoadA.Click += new System.EventHandler(this.btnLoadA_Click);
            // 
            // btnLoadB
            // 
            this.btnLoadB.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnLoadB.Location = new System.Drawing.Point(59, 3);
            this.btnLoadB.Name = "btnLoadB";
            this.btnLoadB.Size = new System.Drawing.Size(50, 30);
            this.btnLoadB.TabIndex = 1;
            this.btnLoadB.Text = "加载图2";
            this.btnLoadB.UseVisualStyleBackColor = true;
            this.btnLoadB.Click += new System.EventHandler(this.btnLoadB_Click);
            // 
            // btnSwap
            // 
            this.btnSwap.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSwap.Location = new System.Drawing.Point(115, 3);
            this.btnSwap.Name = "btnSwap";
            this.btnSwap.Size = new System.Drawing.Size(50, 30);
            this.btnSwap.TabIndex = 2;
            this.btnSwap.Text = "交换";
            this.btnSwap.UseVisualStyleBackColor = true;
            this.btnSwap.Click += new System.EventHandler(this.btnSwap_Click);
            // 
            // btnClear
            // 
            this.btnClear.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnClear.Location = new System.Drawing.Point(171, 3);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(52, 30);
            this.btnClear.TabIndex = 3;
            this.btnClear.Text = "清空";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // lblInfoA
            // 
            this.lblInfoA.AutoEllipsis = true;
            this.tblSource.SetColumnSpan(this.lblInfoA, 4);
            this.lblInfoA.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblInfoA.Location = new System.Drawing.Point(3, 36);
            this.lblInfoA.Name = "lblInfoA";
            this.lblInfoA.Size = new System.Drawing.Size(220, 24);
            this.lblInfoA.TabIndex = 4;
            this.lblInfoA.Text = "图1：未加载";
            this.lblInfoA.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblInfoB
            // 
            this.lblInfoB.AutoEllipsis = true;
            this.tblSource.SetColumnSpan(this.lblInfoB, 4);
            this.lblInfoB.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblInfoB.Location = new System.Drawing.Point(3, 60);
            this.lblInfoB.Name = "lblInfoB";
            this.lblInfoB.Size = new System.Drawing.Size(220, 24);
            this.lblInfoB.TabIndex = 5;
            this.lblInfoB.Text = "图2：未加载";
            this.lblInfoB.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // grpAuto
            // 
            this.grpAuto.Controls.Add(this.tblAuto);
            this.grpAuto.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpAuto.Location = new System.Drawing.Point(250, 4);
            this.grpAuto.Margin = new System.Windows.Forms.Padding(4);
            this.grpAuto.Name = "grpAuto";
            this.grpAuto.Padding = new System.Windows.Forms.Padding(6);
            this.grpAuto.Size = new System.Drawing.Size(385, 188);
            this.grpAuto.TabIndex = 1;
            this.grpAuto.TabStop = false;
            this.grpAuto.Text = "2. 自动拼接（算法）";
            // 
            // tblAuto
            // 
            this.tblAuto.ColumnCount = 3;
            this.tblAuto.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 62F));
            this.tblAuto.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblAuto.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblAuto.Controls.Add(this.lblAlgorithm, 0, 0);
            this.tblAuto.Controls.Add(this.cboAlgorithm, 1, 0);
            this.tblAuto.Controls.Add(this.lblBase, 0, 1);
            this.tblAuto.Controls.Add(this.cboBaseImage, 1, 1);
            this.tblAuto.Controls.Add(this.chkShowMatches, 0, 2);
            this.tblAuto.Controls.Add(this.btnAutoStitch, 0, 3);
            this.tblAuto.Controls.Add(this.btnSaveResult, 2, 3);
            this.tblAuto.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblAuto.Location = new System.Drawing.Point(6, 29);
            this.tblAuto.Margin = new System.Windows.Forms.Padding(0);
            this.tblAuto.Name = "tblAuto";
            this.tblAuto.RowCount = 4;
            this.tblAuto.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tblAuto.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tblAuto.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.tblAuto.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tblAuto.Size = new System.Drawing.Size(373, 153);
            this.tblAuto.TabIndex = 0;
            // 
            // lblAlgorithm
            // 
            this.lblAlgorithm.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAlgorithm.Location = new System.Drawing.Point(3, 0);
            this.lblAlgorithm.Name = "lblAlgorithm";
            this.lblAlgorithm.Size = new System.Drawing.Size(56, 34);
            this.lblAlgorithm.TabIndex = 0;
            this.lblAlgorithm.Text = "算法";
            this.lblAlgorithm.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cboAlgorithm
            // 
            this.tblAuto.SetColumnSpan(this.cboAlgorithm, 2);
            this.cboAlgorithm.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cboAlgorithm.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboAlgorithm.FormattingEnabled = true;
            this.cboAlgorithm.Items.AddRange(new object[] {
            "SIFT（精度优先，适合航拍）",
            "ORB（速度优先，适合实时）"});
            this.cboAlgorithm.Location = new System.Drawing.Point(65, 3);
            this.cboAlgorithm.Name = "cboAlgorithm";
            this.cboAlgorithm.Size = new System.Drawing.Size(305, 32);
            this.cboAlgorithm.TabIndex = 1;
            // 
            // lblBase
            // 
            this.lblBase.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBase.Location = new System.Drawing.Point(3, 34);
            this.lblBase.Name = "lblBase";
            this.lblBase.Size = new System.Drawing.Size(56, 34);
            this.lblBase.TabIndex = 2;
            this.lblBase.Text = "基准图";
            this.lblBase.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cboBaseImage
            // 
            this.tblAuto.SetColumnSpan(this.cboBaseImage, 2);
            this.cboBaseImage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cboBaseImage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboBaseImage.FormattingEnabled = true;
            this.cboBaseImage.Items.AddRange(new object[] {
            "以图1为基准（图2 变换过去）",
            "以图2为基准（图1 变换过去）"});
            this.cboBaseImage.Location = new System.Drawing.Point(65, 37);
            this.cboBaseImage.Name = "cboBaseImage";
            this.cboBaseImage.Size = new System.Drawing.Size(305, 32);
            this.cboBaseImage.TabIndex = 3;
            // 
            // chkShowMatches
            // 
            this.chkShowMatches.AutoSize = true;
            this.chkShowMatches.Checked = true;
            this.chkShowMatches.CheckState = System.Windows.Forms.CheckState.Checked;
            this.tblAuto.SetColumnSpan(this.chkShowMatches, 3);
            this.chkShowMatches.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkShowMatches.Location = new System.Drawing.Point(3, 71);
            this.chkShowMatches.Name = "chkShowMatches";
            this.chkShowMatches.Size = new System.Drawing.Size(367, 26);
            this.chkShowMatches.TabIndex = 4;
            this.chkShowMatches.Text = "显示特征匹配图（绿色=内点，红色=外点）";
            this.chkShowMatches.UseVisualStyleBackColor = true;
            // 
            // btnAutoStitch
            // 
            this.tblAuto.SetColumnSpan(this.btnAutoStitch, 2);
            this.btnAutoStitch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnAutoStitch.Location = new System.Drawing.Point(3, 103);
            this.btnAutoStitch.Name = "btnAutoStitch";
            this.btnAutoStitch.Size = new System.Drawing.Size(211, 47);
            this.btnAutoStitch.TabIndex = 5;
            this.btnAutoStitch.Text = "开始自动拼接";
            this.btnAutoStitch.UseVisualStyleBackColor = true;
            this.btnAutoStitch.Click += new System.EventHandler(this.btnAutoStitch_Click);
            // 
            // btnSaveResult
            // 
            this.btnSaveResult.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSaveResult.Location = new System.Drawing.Point(220, 103);
            this.btnSaveResult.Name = "btnSaveResult";
            this.btnSaveResult.Size = new System.Drawing.Size(150, 47);
            this.btnSaveResult.TabIndex = 6;
            this.btnSaveResult.Text = "保存结果…";
            this.btnSaveResult.UseVisualStyleBackColor = true;
            this.btnSaveResult.Click += new System.EventHandler(this.btnSaveResult_Click);
            // 
            // grpManual
            // 
            this.grpManual.Controls.Add(this.tblManual);
            this.grpManual.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpManual.Location = new System.Drawing.Point(643, 4);
            this.grpManual.Margin = new System.Windows.Forms.Padding(4);
            this.grpManual.Name = "grpManual";
            this.grpManual.Padding = new System.Windows.Forms.Padding(6);
            this.grpManual.Size = new System.Drawing.Size(693, 188);
            this.grpManual.TabIndex = 2;
            this.grpManual.TabStop = false;
            this.grpManual.Text = "3. 手动拼接（拖动对齐）";
            // 
            // tblManual
            // 
            this.tblManual.ColumnCount = 6;
            this.tblManual.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 68F));
            this.tblManual.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 108F));
            this.tblManual.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 68F));
            this.tblManual.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 88F));
            this.tblManual.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblManual.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblManual.Controls.Add(this.lblTop, 0, 0);
            this.tblManual.Controls.Add(this.cboTopLayer, 1, 0);
            this.tblManual.Controls.Add(this.lblRotation, 2, 0);
            this.tblManual.Controls.Add(this.numRotation, 3, 0);
            this.tblManual.Controls.Add(this.btnRotateLeft, 4, 0);
            this.tblManual.Controls.Add(this.btnRotateRight, 5, 0);
            this.tblManual.Controls.Add(this.lblOpacity, 0, 1);
            this.tblManual.Controls.Add(this.trkOpacity, 1, 1);
            this.tblManual.Controls.Add(this.lblOpacityValue, 4, 1);
            this.tblManual.Controls.Add(this.btnManualMode, 0, 2);
            this.tblManual.Controls.Add(this.btnResetPositions, 3, 2);
            this.tblManual.Controls.Add(this.btnFitView, 0, 3);
            this.tblManual.Controls.Add(this.chkOpaqueExport, 2, 3);
            this.tblManual.Controls.Add(this.btnExportManual, 4, 3);
            this.tblManual.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblManual.Location = new System.Drawing.Point(6, 29);
            this.tblManual.Margin = new System.Windows.Forms.Padding(0);
            this.tblManual.Name = "tblManual";
            this.tblManual.RowCount = 4;
            this.tblManual.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tblManual.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.tblManual.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.tblManual.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.tblManual.Size = new System.Drawing.Size(681, 153);
            this.tblManual.TabIndex = 0;
            // 
            // lblTop
            // 
            this.lblTop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTop.Location = new System.Drawing.Point(3, 0);
            this.lblTop.Name = "lblTop";
            this.lblTop.Size = new System.Drawing.Size(62, 34);
            this.lblTop.TabIndex = 0;
            this.lblTop.Text = "上层图像";
            this.lblTop.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cboTopLayer
            // 
            this.cboTopLayer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cboTopLayer.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboTopLayer.FormattingEnabled = true;
            this.cboTopLayer.Items.AddRange(new object[] {
            "图1 在上",
            "图2 在上"});
            this.cboTopLayer.Location = new System.Drawing.Point(71, 3);
            this.cboTopLayer.Name = "cboTopLayer";
            this.cboTopLayer.Size = new System.Drawing.Size(102, 32);
            this.cboTopLayer.TabIndex = 1;
            this.cboTopLayer.SelectedIndexChanged += new System.EventHandler(this.cboTopLayer_SelectedIndexChanged);
            // 
            // lblRotation
            // 
            this.lblRotation.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRotation.Location = new System.Drawing.Point(179, 0);
            this.lblRotation.Name = "lblRotation";
            this.lblRotation.Size = new System.Drawing.Size(62, 34);
            this.lblRotation.TabIndex = 2;
            this.lblRotation.Text = "旋转(°)";
            this.lblRotation.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // numRotation
            // 
            this.numRotation.DecimalPlaces = 1;
            this.numRotation.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numRotation.Increment = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            this.numRotation.Location = new System.Drawing.Point(247, 3);
            this.numRotation.Maximum = new decimal(new int[] {
            180,
            0,
            0,
            0});
            this.numRotation.Minimum = new decimal(new int[] {
            180,
            0,
            0,
            -2147483648});
            this.numRotation.Name = "numRotation";
            this.numRotation.Size = new System.Drawing.Size(82, 30);
            this.numRotation.TabIndex = 3;
            this.numRotation.ValueChanged += new System.EventHandler(this.numRotation_ValueChanged);
            // 
            // btnRotateLeft
            // 
            this.btnRotateLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnRotateLeft.Location = new System.Drawing.Point(335, 3);
            this.btnRotateLeft.Name = "btnRotateLeft";
            this.btnRotateLeft.Size = new System.Drawing.Size(168, 28);
            this.btnRotateLeft.TabIndex = 4;
            this.btnRotateLeft.Text = "◀ 逆时针 0.5°";
            this.btnRotateLeft.UseVisualStyleBackColor = true;
            this.btnRotateLeft.Click += new System.EventHandler(this.btnRotateLeft_Click);
            // 
            // btnRotateRight
            // 
            this.btnRotateRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnRotateRight.Location = new System.Drawing.Point(509, 3);
            this.btnRotateRight.Name = "btnRotateRight";
            this.btnRotateRight.Size = new System.Drawing.Size(169, 28);
            this.btnRotateRight.TabIndex = 5;
            this.btnRotateRight.Text = "顺时针 0.5° ▶";
            this.btnRotateRight.UseVisualStyleBackColor = true;
            this.btnRotateRight.Click += new System.EventHandler(this.btnRotateRight_Click);
            // 
            // lblOpacity
            // 
            this.lblOpacity.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOpacity.Location = new System.Drawing.Point(3, 34);
            this.lblOpacity.Name = "lblOpacity";
            this.lblOpacity.Size = new System.Drawing.Size(62, 46);
            this.lblOpacity.TabIndex = 6;
            this.lblOpacity.Text = "上层透明度";
            this.lblOpacity.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // trkOpacity
            // 
            this.tblManual.SetColumnSpan(this.trkOpacity, 3);
            this.trkOpacity.Dock = System.Windows.Forms.DockStyle.Fill;
            this.trkOpacity.LargeChange = 10;
            this.trkOpacity.Location = new System.Drawing.Point(71, 37);
            this.trkOpacity.Maximum = 100;
            this.trkOpacity.Name = "trkOpacity";
            this.trkOpacity.Size = new System.Drawing.Size(258, 40);
            this.trkOpacity.TabIndex = 7;
            this.trkOpacity.TickFrequency = 10;
            this.trkOpacity.Value = 50;
            this.trkOpacity.Scroll += new System.EventHandler(this.trkOpacity_Scroll);
            this.trkOpacity.ValueChanged += new System.EventHandler(this.trkOpacity_Scroll);
            // 
            // lblOpacityValue
            // 
            this.tblManual.SetColumnSpan(this.lblOpacityValue, 2);
            this.lblOpacityValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOpacityValue.Location = new System.Drawing.Point(335, 34);
            this.lblOpacityValue.Name = "lblOpacityValue";
            this.lblOpacityValue.Size = new System.Drawing.Size(343, 46);
            this.lblOpacityValue.TabIndex = 8;
            this.lblOpacityValue.Text = "50%";
            this.lblOpacityValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnManualMode
            // 
            this.tblManual.SetColumnSpan(this.btnManualMode, 3);
            this.btnManualMode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnManualMode.Location = new System.Drawing.Point(3, 83);
            this.btnManualMode.Name = "btnManualMode";
            this.btnManualMode.Size = new System.Drawing.Size(238, 30);
            this.btnManualMode.TabIndex = 9;
            this.btnManualMode.Text = "进入手动模式";
            this.btnManualMode.UseVisualStyleBackColor = true;
            this.btnManualMode.Click += new System.EventHandler(this.btnManualMode_Click);
            // 
            // btnResetPositions
            // 
            this.tblManual.SetColumnSpan(this.btnResetPositions, 3);
            this.btnResetPositions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnResetPositions.Location = new System.Drawing.Point(247, 83);
            this.btnResetPositions.Name = "btnResetPositions";
            this.btnResetPositions.Size = new System.Drawing.Size(431, 30);
            this.btnResetPositions.TabIndex = 10;
            this.btnResetPositions.Text = "重置图像位置";
            this.btnResetPositions.UseVisualStyleBackColor = true;
            this.btnResetPositions.Click += new System.EventHandler(this.btnResetPositions_Click);
            // 
            // btnFitView
            // 
            this.tblManual.SetColumnSpan(this.btnFitView, 2);
            this.btnFitView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnFitView.Location = new System.Drawing.Point(3, 119);
            this.btnFitView.Name = "btnFitView";
            this.btnFitView.Size = new System.Drawing.Size(170, 31);
            this.btnFitView.TabIndex = 11;
            this.btnFitView.Text = "适应窗口";
            this.btnFitView.UseVisualStyleBackColor = true;
            this.btnFitView.Click += new System.EventHandler(this.btnFitView_Click);
            // 
            // chkOpaqueExport
            // 
            this.chkOpaqueExport.AutoSize = true;
            this.chkOpaqueExport.Checked = true;
            this.chkOpaqueExport.CheckState = System.Windows.Forms.CheckState.Checked;
            this.tblManual.SetColumnSpan(this.chkOpaqueExport, 2);
            this.chkOpaqueExport.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkOpaqueExport.Location = new System.Drawing.Point(179, 119);
            this.chkOpaqueExport.Name = "chkOpaqueExport";
            this.chkOpaqueExport.Size = new System.Drawing.Size(150, 31);
            this.chkOpaqueExport.TabIndex = 12;
            this.chkOpaqueExport.Text = "导出时顶层不透明";
            this.chkOpaqueExport.UseVisualStyleBackColor = true;
            // 
            // btnExportManual
            // 
            this.tblManual.SetColumnSpan(this.btnExportManual, 2);
            this.btnExportManual.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnExportManual.Location = new System.Drawing.Point(335, 119);
            this.btnExportManual.Name = "btnExportManual";
            this.btnExportManual.Size = new System.Drawing.Size(343, 31);
            this.btnExportManual.TabIndex = 13;
            this.btnExportManual.Text = "导出拼接图…";
            this.btnExportManual.UseVisualStyleBackColor = true;
            this.btnExportManual.Click += new System.EventHandler(this.btnExportManual_Click);
            // 
            // tableContent
            // 
            this.tableContent.ColumnCount = 2;
            this.tableContent.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 236F));
            this.tableContent.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableContent.Controls.Add(this.tablePreviews, 0, 0);
            this.tableContent.Controls.Add(this.panelView, 1, 0);
            this.tableContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableContent.Location = new System.Drawing.Point(0, 196);
            this.tableContent.Margin = new System.Windows.Forms.Padding(0);
            this.tableContent.Name = "tableContent";
            this.tableContent.RowCount = 1;
            this.tableContent.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableContent.Size = new System.Drawing.Size(1340, 653);
            this.tableContent.TabIndex = 1;
            // 
            // tablePreviews
            // 
            this.tablePreviews.ColumnCount = 1;
            this.tablePreviews.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tablePreviews.Controls.Add(this.grpPreviewA, 0, 0);
            this.tablePreviews.Controls.Add(this.grpPreviewB, 0, 1);
            this.tablePreviews.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tablePreviews.Location = new System.Drawing.Point(0, 0);
            this.tablePreviews.Margin = new System.Windows.Forms.Padding(0);
            this.tablePreviews.Name = "tablePreviews";
            this.tablePreviews.RowCount = 2;
            this.tablePreviews.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tablePreviews.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tablePreviews.Size = new System.Drawing.Size(236, 653);
            this.tablePreviews.TabIndex = 0;
            // 
            // grpPreviewA
            // 
            this.grpPreviewA.Controls.Add(this.picPreviewA);
            this.grpPreviewA.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpPreviewA.Location = new System.Drawing.Point(4, 4);
            this.grpPreviewA.Margin = new System.Windows.Forms.Padding(4);
            this.grpPreviewA.Name = "grpPreviewA";
            this.grpPreviewA.Padding = new System.Windows.Forms.Padding(4);
            this.grpPreviewA.Size = new System.Drawing.Size(228, 318);
            this.grpPreviewA.TabIndex = 0;
            this.grpPreviewA.TabStop = false;
            this.grpPreviewA.Text = "图1";
            // 
            // picPreviewA
            // 
            this.picPreviewA.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(32)))));
            this.picPreviewA.Dock = System.Windows.Forms.DockStyle.Fill;
            this.picPreviewA.Location = new System.Drawing.Point(4, 27);
            this.picPreviewA.Name = "picPreviewA";
            this.picPreviewA.Size = new System.Drawing.Size(220, 287);
            this.picPreviewA.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picPreviewA.TabIndex = 0;
            this.picPreviewA.TabStop = false;
            // 
            // grpPreviewB
            // 
            this.grpPreviewB.Controls.Add(this.picPreviewB);
            this.grpPreviewB.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpPreviewB.Location = new System.Drawing.Point(4, 330);
            this.grpPreviewB.Margin = new System.Windows.Forms.Padding(4);
            this.grpPreviewB.Name = "grpPreviewB";
            this.grpPreviewB.Padding = new System.Windows.Forms.Padding(4);
            this.grpPreviewB.Size = new System.Drawing.Size(228, 319);
            this.grpPreviewB.TabIndex = 1;
            this.grpPreviewB.TabStop = false;
            this.grpPreviewB.Text = "图2";
            // 
            // picPreviewB
            // 
            this.picPreviewB.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(32)))));
            this.picPreviewB.Dock = System.Windows.Forms.DockStyle.Fill;
            this.picPreviewB.Location = new System.Drawing.Point(4, 27);
            this.picPreviewB.Name = "picPreviewB";
            this.picPreviewB.Size = new System.Drawing.Size(220, 288);
            this.picPreviewB.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picPreviewB.TabIndex = 0;
            this.picPreviewB.TabStop = false;
            // 
            // panelView
            // 
            this.panelView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(32)))));
            this.panelView.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelView.Controls.Add(this.manualCanvas);
            this.panelView.Controls.Add(this.tabView);
            this.panelView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelView.Location = new System.Drawing.Point(240, 4);
            this.panelView.Margin = new System.Windows.Forms.Padding(4);
            this.panelView.Name = "panelView";
            this.panelView.Size = new System.Drawing.Size(1096, 645);
            this.panelView.TabIndex = 1;
            // 
            // manualCanvas
            // 
            this.manualCanvas.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.manualCanvas.Dock = System.Windows.Forms.DockStyle.Fill;
            this.manualCanvas.ForeColor = System.Drawing.Color.Gainsboro;
            this.manualCanvas.Location = new System.Drawing.Point(0, 0);
            this.manualCanvas.Name = "manualCanvas";
            this.manualCanvas.Size = new System.Drawing.Size(1094, 643);
            this.manualCanvas.TabIndex = 1;
            this.manualCanvas.TopLayer = 0;
            this.manualCanvas.TopOpacityPercent = 50;
            this.manualCanvas.TopRotation = 0F;
            this.manualCanvas.Visible = false;
            this.manualCanvas.TopLayerChanged += new System.EventHandler(this.manualCanvas_TopLayerChanged);
            // 
            // tabView
            // 
            this.tabView.Controls.Add(this.tabResult);
            this.tabView.Controls.Add(this.tabMatches);
            this.tabView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabView.Location = new System.Drawing.Point(0, 0);
            this.tabView.Name = "tabView";
            this.tabView.SelectedIndex = 0;
            this.tabView.Size = new System.Drawing.Size(1094, 643);
            this.tabView.TabIndex = 0;
            // 
            // tabResult
            // 
            this.tabResult.Controls.Add(this.picResult);
            this.tabResult.Location = new System.Drawing.Point(4, 33);
            this.tabResult.Name = "tabResult";
            this.tabResult.Padding = new System.Windows.Forms.Padding(3);
            this.tabResult.Size = new System.Drawing.Size(1086, 606);
            this.tabResult.TabIndex = 0;
            this.tabResult.Text = "拼接结果";
            this.tabResult.UseVisualStyleBackColor = true;
            // 
            // picResult
            // 
            this.picResult.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(32)))));
            this.picResult.Dock = System.Windows.Forms.DockStyle.Fill;
            this.picResult.Location = new System.Drawing.Point(3, 3);
            this.picResult.Name = "picResult";
            this.picResult.Size = new System.Drawing.Size(1080, 600);
            this.picResult.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picResult.TabIndex = 0;
            this.picResult.TabStop = false;
            // 
            // tabMatches
            // 
            this.tabMatches.Controls.Add(this.picMatches);
            this.tabMatches.Location = new System.Drawing.Point(4, 33);
            this.tabMatches.Name = "tabMatches";
            this.tabMatches.Padding = new System.Windows.Forms.Padding(3);
            this.tabMatches.Size = new System.Drawing.Size(1086, 606);
            this.tabMatches.TabIndex = 1;
            this.tabMatches.Text = "特征匹配";
            this.tabMatches.UseVisualStyleBackColor = true;
            // 
            // picMatches
            // 
            this.picMatches.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(32)))));
            this.picMatches.Dock = System.Windows.Forms.DockStyle.Fill;
            this.picMatches.Location = new System.Drawing.Point(3, 3);
            this.picMatches.Name = "picMatches";
            this.picMatches.Size = new System.Drawing.Size(1080, 600);
            this.picMatches.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picMatches.TabIndex = 0;
            this.picMatches.TabStop = false;
            // 
            // statusStripMain
            // 
            this.statusStripMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.statusStripMain.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.statusStripMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblStatus,
            this.progressMain,
            this.lblMode});
            this.statusStripMain.Location = new System.Drawing.Point(0, 849);
            this.statusStripMain.Name = "statusStripMain";
            this.statusStripMain.Size = new System.Drawing.Size(1340, 31);
            this.statusStripMain.TabIndex = 2;
            // 
            // lblStatus
            // 
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(1081, 24);
            this.lblStatus.Spring = true;
            this.lblStatus.Text = "就绪。请先加载两张图像。";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // progressMain
            // 
            this.progressMain.Name = "progressMain";
            this.progressMain.Size = new System.Drawing.Size(140, 23);
            this.progressMain.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressMain.Visible = false;
            // 
            // lblMode
            // 
            this.lblMode.Name = "lblMode";
            this.lblMode.Size = new System.Drawing.Size(100, 24);
            this.lblMode.Text = "模式：自动";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(11F, 24F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1340, 880);
            this.Controls.Add(this.tableRoot);
            this.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.MinimumSize = new System.Drawing.Size(1120, 760);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "图像拼接工具 — OpenCvSharp4 / SIFT · ORB";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.tableRoot.ResumeLayout(false);
            this.tableRoot.PerformLayout();
            this.tableHeader.ResumeLayout(false);
            this.grpSource.ResumeLayout(false);
            this.tblSource.ResumeLayout(false);
            this.grpAuto.ResumeLayout(false);
            this.tblAuto.ResumeLayout(false);
            this.tblAuto.PerformLayout();
            this.grpManual.ResumeLayout(false);
            this.tblManual.ResumeLayout(false);
            this.tblManual.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numRotation)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkOpacity)).EndInit();
            this.tableContent.ResumeLayout(false);
            this.tablePreviews.ResumeLayout(false);
            this.grpPreviewA.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picPreviewA)).EndInit();
            this.grpPreviewB.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picPreviewB)).EndInit();
            this.panelView.ResumeLayout(false);
            this.tabView.ResumeLayout(false);
            this.tabResult.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picResult)).EndInit();
            this.tabMatches.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picMatches)).EndInit();
            this.statusStripMain.ResumeLayout(false);
            this.statusStripMain.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableRoot;
        private System.Windows.Forms.OpenFileDialog openImageDialog;
        private System.Windows.Forms.SaveFileDialog saveImageDialog;
        private System.Windows.Forms.TableLayoutPanel tableHeader;
        private System.Windows.Forms.GroupBox grpSource;
        private System.Windows.Forms.TableLayoutPanel tblSource;
        private System.Windows.Forms.Button btnLoadA;
        private System.Windows.Forms.Button btnLoadB;
        private System.Windows.Forms.Button btnSwap;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.Label lblInfoA;
        private System.Windows.Forms.Label lblInfoB;
        private System.Windows.Forms.GroupBox grpAuto;
        private System.Windows.Forms.TableLayoutPanel tblAuto;
        private System.Windows.Forms.Label lblAlgorithm;
        private System.Windows.Forms.ComboBox cboAlgorithm;
        private System.Windows.Forms.Label lblBase;
        private System.Windows.Forms.ComboBox cboBaseImage;
        private System.Windows.Forms.CheckBox chkShowMatches;
        private System.Windows.Forms.Button btnAutoStitch;
        private System.Windows.Forms.Button btnSaveResult;
        private System.Windows.Forms.GroupBox grpManual;
        private System.Windows.Forms.TableLayoutPanel tblManual;
        private System.Windows.Forms.Label lblTop;
        private System.Windows.Forms.ComboBox cboTopLayer;
        private System.Windows.Forms.Label lblRotation;
        private System.Windows.Forms.NumericUpDown numRotation;
        private System.Windows.Forms.Button btnRotateLeft;
        private System.Windows.Forms.Button btnRotateRight;
        private System.Windows.Forms.Label lblOpacity;
        private System.Windows.Forms.TrackBar trkOpacity;
        private System.Windows.Forms.Label lblOpacityValue;
        private System.Windows.Forms.Button btnManualMode;
        private System.Windows.Forms.Button btnResetPositions;
        private System.Windows.Forms.Button btnFitView;
        private System.Windows.Forms.CheckBox chkOpaqueExport;
        private System.Windows.Forms.Button btnExportManual;
        private System.Windows.Forms.TableLayoutPanel tableContent;
        private System.Windows.Forms.TableLayoutPanel tablePreviews;
        private System.Windows.Forms.GroupBox grpPreviewA;
        private System.Windows.Forms.PictureBox picPreviewA;
        private System.Windows.Forms.GroupBox grpPreviewB;
        private System.Windows.Forms.PictureBox picPreviewB;
        private System.Windows.Forms.Panel panelView;
        private ImageStitcher.Controls.ManualStitchCanvas manualCanvas;
        private System.Windows.Forms.TabControl tabView;
        private System.Windows.Forms.TabPage tabResult;
        private System.Windows.Forms.PictureBox picResult;
        private System.Windows.Forms.TabPage tabMatches;
        private System.Windows.Forms.PictureBox picMatches;
        private System.Windows.Forms.StatusStrip statusStripMain;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;
        private System.Windows.Forms.ToolStripProgressBar progressMain;
        private System.Windows.Forms.ToolStripStatusLabel lblMode;
    }
}
