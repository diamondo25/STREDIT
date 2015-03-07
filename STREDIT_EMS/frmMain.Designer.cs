namespace STREDIT
{
    partial class frmMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
            this.dgvStrings = new System.Windows.Forms.DataGridView();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.openToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.saveToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.cSVToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.exportToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.importToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.toEnumerationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exportToCSVToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openUpDlogtxtToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openUpExlogtxtToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openUpLtxtToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.goToCraftNetToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.tsBytesAvailable = new System.Windows.Forms.ToolStripStatusLabel();
            this.versionLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.tsClientVersion = new System.Windows.Forms.ToolStripStatusLabel();
            this.tsLoadProgress = new System.Windows.Forms.ToolStripProgressBar();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.seperator1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel4 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel3 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel7 = new System.Windows.Forms.ToolStripStatusLabel();
            this.tsBlockSize = new System.Windows.Forms.ToolStripStatusLabel();
            this.famSep = new System.Windows.Forms.ToolStripStatusLabel();
            this.famLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel2 = new System.Windows.Forms.ToolStripStatusLabel();
            this.derpLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.btnSearchHelp = new System.Windows.Forms.Button();
            this.ckSearchUp = new System.Windows.Forms.CheckBox();
            this.btnSearchQuery = new System.Windows.Forms.Button();
            this.txtQuery = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.runCMD = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.oneTimeTimer = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.dgvStrings)).BeginInit();
            this.toolStrip1.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgvStrings
            // 
            this.dgvStrings.AllowUserToAddRows = false;
            this.dgvStrings.AllowUserToDeleteRows = false;
            this.dgvStrings.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvStrings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvStrings.Location = new System.Drawing.Point(0, 24);
            this.dgvStrings.Name = "dgvStrings";
            this.dgvStrings.Size = new System.Drawing.Size(632, 460);
            this.dgvStrings.TabIndex = 0;
            this.dgvStrings.CellBeginEdit += new System.Windows.Forms.DataGridViewCellCancelEventHandler(this.dataGridView1_CellBeginEdit);
            this.dgvStrings.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView1_CellEndEdit);
            this.dgvStrings.SelectionChanged += new System.EventHandler(this.dgvStrings_SelectionChanged);
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripButton,
            this.saveToolStripButton,
            this.toolStripSeparator1,
            this.toolStripButton1});
            this.toolStrip1.Location = new System.Drawing.Point(0, 24);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(877, 25);
            this.toolStrip1.TabIndex = 2;
            this.toolStrip1.Text = "toolStrip1";
            this.toolStrip1.Visible = false;
            // 
            // openToolStripButton
            // 
            this.openToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.openToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("openToolStripButton.Image")));
            this.openToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.openToolStripButton.Name = "openToolStripButton";
            this.openToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.openToolStripButton.Text = "&Open";
            this.openToolStripButton.Click += new System.EventHandler(this.openToolStripButton_Click);
            // 
            // saveToolStripButton
            // 
            this.saveToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.saveToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("saveToolStripButton.Image")));
            this.saveToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.saveToolStripButton.Name = "saveToolStripButton";
            this.saveToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.saveToolStripButton.Text = "&Save";
            this.saveToolStripButton.Click += new System.EventHandler(this.saveToolStripButton_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripButton1
            // 
            this.toolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton1.Image")));
            this.toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton1.Name = "toolStripButton1";
            this.toolStripButton1.Size = new System.Drawing.Size(82, 22);
            this.toolStripButton1.Text = "Export to CVS";
            this.toolStripButton1.Click += new System.EventHandler(this.toolStripButton1_Click);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.exportToCSVToolStripMenuItem,
            this.helpToolStripMenuItem,
            this.goToCraftNetToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(877, 24);
            this.menuStrip1.TabIndex = 3;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.saveToolStripMenuItem1,
            this.toolStripSeparator2,
            this.cSVToolStripMenuItem1,
            this.toEnumerationToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(159, 22);
            this.openToolStripMenuItem.Text = "Open..";
            // 
            // saveToolStripMenuItem1
            // 
            this.saveToolStripMenuItem1.Enabled = false;
            this.saveToolStripMenuItem1.Name = "saveToolStripMenuItem1";
            this.saveToolStripMenuItem1.Size = new System.Drawing.Size(159, 22);
            this.saveToolStripMenuItem1.Text = "Save";
            this.saveToolStripMenuItem1.Click += new System.EventHandler(this.saveToolStripButton_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(156, 6);
            // 
            // cSVToolStripMenuItem1
            // 
            this.cSVToolStripMenuItem1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exportToolStripMenuItem1,
            this.importToolStripMenuItem1});
            this.cSVToolStripMenuItem1.Name = "cSVToolStripMenuItem1";
            this.cSVToolStripMenuItem1.Size = new System.Drawing.Size(159, 22);
            this.cSVToolStripMenuItem1.Text = "CSV";
            // 
            // exportToolStripMenuItem1
            // 
            this.exportToolStripMenuItem1.Name = "exportToolStripMenuItem1";
            this.exportToolStripMenuItem1.Size = new System.Drawing.Size(119, 22);
            this.exportToolStripMenuItem1.Text = "Export...";
            this.exportToolStripMenuItem1.Click += new System.EventHandler(this.toolStripButton1_Click);
            // 
            // importToolStripMenuItem1
            // 
            this.importToolStripMenuItem1.Name = "importToolStripMenuItem1";
            this.importToolStripMenuItem1.Size = new System.Drawing.Size(119, 22);
            this.importToolStripMenuItem1.Text = "Import...";
            this.importToolStripMenuItem1.Click += new System.EventHandler(this.importToolStripMenuItem_Click);
            // 
            // toEnumerationToolStripMenuItem
            // 
            this.toEnumerationToolStripMenuItem.Name = "toEnumerationToolStripMenuItem";
            this.toEnumerationToolStripMenuItem.Size = new System.Drawing.Size(159, 22);
            this.toEnumerationToolStripMenuItem.Text = "To Enumeration";
            this.toEnumerationToolStripMenuItem.Click += new System.EventHandler(this.toEnumerationToolStripMenuItem_Click);
            // 
            // exportToCSVToolStripMenuItem
            // 
            this.exportToCSVToolStripMenuItem.Name = "exportToCSVToolStripMenuItem";
            this.exportToCSVToolStripMenuItem.Size = new System.Drawing.Size(90, 20);
            this.exportToCSVToolStripMenuItem.Text = "Export to CSV";
            this.exportToCSVToolStripMenuItem.Visible = false;
            this.exportToCSVToolStripMenuItem.Click += new System.EventHandler(this.toolStripButton1_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openUpDlogtxtToolStripMenuItem,
            this.openUpExlogtxtToolStripMenuItem,
            this.openUpLtxtToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "Help";
            // 
            // openUpDlogtxtToolStripMenuItem
            // 
            this.openUpDlogtxtToolStripMenuItem.Name = "openUpDlogtxtToolStripMenuItem";
            this.openUpDlogtxtToolStripMenuItem.Size = new System.Drawing.Size(218, 22);
            this.openUpDlogtxtToolStripMenuItem.Text = "Open up dlog.txt";
            this.openUpDlogtxtToolStripMenuItem.Click += new System.EventHandler(this.openUpDlogtxtToolStripMenuItem_Click);
            // 
            // openUpExlogtxtToolStripMenuItem
            // 
            this.openUpExlogtxtToolStripMenuItem.Name = "openUpExlogtxtToolStripMenuItem";
            this.openUpExlogtxtToolStripMenuItem.Size = new System.Drawing.Size(218, 22);
            this.openUpExlogtxtToolStripMenuItem.Text = "Open up exlog.txt";
            this.openUpExlogtxtToolStripMenuItem.Click += new System.EventHandler(this.openUpExlogtxtToolStripMenuItem_Click);
            // 
            // openUpLtxtToolStripMenuItem
            // 
            this.openUpLtxtToolStripMenuItem.Name = "openUpLtxtToolStripMenuItem";
            this.openUpLtxtToolStripMenuItem.Size = new System.Drawing.Size(218, 22);
            this.openUpLtxtToolStripMenuItem.Text = "Open up Saving File Log.txt";
            this.openUpLtxtToolStripMenuItem.Click += new System.EventHandler(this.openUpLtxtToolStripMenuItem_Click);
            // 
            // goToCraftNetToolStripMenuItem
            // 
            this.goToCraftNetToolStripMenuItem.Name = "goToCraftNetToolStripMenuItem";
            this.goToCraftNetToolStripMenuItem.Size = new System.Drawing.Size(99, 20);
            this.goToCraftNetToolStripMenuItem.Text = "Go to CraftNet!";
            this.goToCraftNetToolStripMenuItem.Click += new System.EventHandler(this.goToCraftNetToolStripMenuItem_Click);
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(90, 19);
            this.toolStripStatusLabel1.Text = "Space available:";
            // 
            // tsBytesAvailable
            // 
            this.tsBytesAvailable.Name = "tsBytesAvailable";
            this.tsBytesAvailable.Size = new System.Drawing.Size(44, 19);
            this.tsBytesAvailable.Text = "0 Bytes";
            // 
            // versionLabel
            // 
            this.versionLabel.Name = "versionLabel";
            this.versionLabel.Size = new System.Drawing.Size(80, 19);
            this.versionLabel.Text = "ClientVersion:";
            // 
            // tsClientVersion
            // 
            this.tsClientVersion.Name = "tsClientVersion";
            this.tsClientVersion.Size = new System.Drawing.Size(27, 19);
            this.tsClientVersion.Text = "V?.?";
            // 
            // tsLoadProgress
            // 
            this.tsLoadProgress.AutoSize = false;
            this.tsLoadProgress.Name = "tsLoadProgress";
            this.tsLoadProgress.Size = new System.Drawing.Size(100, 18);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.versionLabel,
            this.tsClientVersion,
            this.seperator1,
            this.toolStripStatusLabel1,
            this.tsBytesAvailable,
            this.toolStripStatusLabel4,
            this.tsLoadProgress,
            this.toolStripStatusLabel3,
            this.toolStripStatusLabel7,
            this.tsBlockSize,
            this.famSep,
            this.famLabel,
            this.toolStripStatusLabel2,
            this.derpLabel});
            this.statusStrip1.Location = new System.Drawing.Point(0, 484);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(877, 24);
            this.statusStrip1.Stretch = false;
            this.statusStrip1.TabIndex = 1;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // seperator1
            // 
            this.seperator1.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right)));
            this.seperator1.BorderStyle = System.Windows.Forms.Border3DStyle.Etched;
            this.seperator1.Margin = new System.Windows.Forms.Padding(5, 2, 5, 0);
            this.seperator1.Name = "seperator1";
            this.seperator1.Size = new System.Drawing.Size(4, 22);
            // 
            // toolStripStatusLabel4
            // 
            this.toolStripStatusLabel4.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right)));
            this.toolStripStatusLabel4.BorderStyle = System.Windows.Forms.Border3DStyle.Etched;
            this.toolStripStatusLabel4.Margin = new System.Windows.Forms.Padding(5, 2, 5, 0);
            this.toolStripStatusLabel4.Name = "toolStripStatusLabel4";
            this.toolStripStatusLabel4.Size = new System.Drawing.Size(4, 22);
            // 
            // toolStripStatusLabel3
            // 
            this.toolStripStatusLabel3.BackColor = System.Drawing.Color.Transparent;
            this.toolStripStatusLabel3.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right)));
            this.toolStripStatusLabel3.BorderStyle = System.Windows.Forms.Border3DStyle.Etched;
            this.toolStripStatusLabel3.Margin = new System.Windows.Forms.Padding(5, 2, 5, 0);
            this.toolStripStatusLabel3.Name = "toolStripStatusLabel3";
            this.toolStripStatusLabel3.Size = new System.Drawing.Size(4, 22);
            // 
            // toolStripStatusLabel7
            // 
            this.toolStripStatusLabel7.Name = "toolStripStatusLabel7";
            this.toolStripStatusLabel7.Size = new System.Drawing.Size(33, 19);
            this.toolStripStatusLabel7.Text = "Size: ";
            // 
            // tsBlockSize
            // 
            this.tsBlockSize.Name = "tsBlockSize";
            this.tsBlockSize.Size = new System.Drawing.Size(44, 19);
            this.tsBlockSize.Text = "0 Bytes";
            // 
            // famSep
            // 
            this.famSep.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right)));
            this.famSep.BorderStyle = System.Windows.Forms.Border3DStyle.Etched;
            this.famSep.Margin = new System.Windows.Forms.Padding(5, 2, 5, 0);
            this.famSep.Name = "famSep";
            this.famSep.Size = new System.Drawing.Size(4, 22);
            // 
            // famLabel
            // 
            this.famLabel.Name = "famLabel";
            this.famLabel.Size = new System.Drawing.Size(117, 19);
            this.famLabel.Text = "File already modified";
            // 
            // toolStripStatusLabel2
            // 
            this.toolStripStatusLabel2.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right)));
            this.toolStripStatusLabel2.BorderStyle = System.Windows.Forms.Border3DStyle.Etched;
            this.toolStripStatusLabel2.Margin = new System.Windows.Forms.Padding(5, 2, 5, 0);
            this.toolStripStatusLabel2.Name = "toolStripStatusLabel2";
            this.toolStripStatusLabel2.Size = new System.Drawing.Size(4, 22);
            // 
            // derpLabel
            // 
            this.derpLabel.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
            this.derpLabel.BorderStyle = System.Windows.Forms.Border3DStyle.Bump;
            this.derpLabel.Name = "derpLabel";
            this.derpLabel.Size = new System.Drawing.Size(83, 19);
            this.derpLabel.Text = "Calculate Size";
            this.derpLabel.Click += new System.EventHandler(this.famLabel_Click);
            // 
            // panel1
            // 
            this.panel1.AutoScroll = true;
            this.panel1.BackColor = System.Drawing.Color.White;
            this.panel1.Controls.Add(this.groupBox3);
            this.panel1.Controls.Add(this.groupBox1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Right;
            this.panel1.Location = new System.Drawing.Point(632, 24);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(245, 460);
            this.panel1.TabIndex = 4;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.btnSearchHelp);
            this.groupBox3.Controls.Add(this.ckSearchUp);
            this.groupBox3.Controls.Add(this.btnSearchQuery);
            this.groupBox3.Controls.Add(this.txtQuery);
            this.groupBox3.Controls.Add(this.label3);
            this.groupBox3.Location = new System.Drawing.Point(6, 133);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(233, 88);
            this.groupBox3.TabIndex = 2;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Searching";
            // 
            // btnSearchHelp
            // 
            this.btnSearchHelp.Location = new System.Drawing.Point(120, 58);
            this.btnSearchHelp.Name = "btnSearchHelp";
            this.btnSearchHelp.Size = new System.Drawing.Size(26, 23);
            this.btnSearchHelp.TabIndex = 4;
            this.btnSearchHelp.Text = "?";
            this.btnSearchHelp.UseVisualStyleBackColor = true;
            this.btnSearchHelp.Click += new System.EventHandler(this.btnSearchHelp_Click);
            // 
            // ckSearchUp
            // 
            this.ckSearchUp.AutoSize = true;
            this.ckSearchUp.Location = new System.Drawing.Point(9, 62);
            this.ckSearchUp.Name = "ckSearchUp";
            this.ckSearchUp.Size = new System.Drawing.Size(75, 17);
            this.ckSearchUp.TabIndex = 3;
            this.ckSearchUp.Text = "Search up";
            this.ckSearchUp.UseVisualStyleBackColor = true;
            // 
            // btnSearchQuery
            // 
            this.btnSearchQuery.Location = new System.Drawing.Point(152, 58);
            this.btnSearchQuery.Name = "btnSearchQuery";
            this.btnSearchQuery.Size = new System.Drawing.Size(75, 23);
            this.btnSearchQuery.TabIndex = 2;
            this.btnSearchQuery.Text = "Search";
            this.btnSearchQuery.UseVisualStyleBackColor = true;
            this.btnSearchQuery.Click += new System.EventHandler(this.btnSearchQuery_Click);
            // 
            // txtQuery
            // 
            this.txtQuery.Location = new System.Drawing.Point(6, 32);
            this.txtQuery.Name = "txtQuery";
            this.txtQuery.Size = new System.Drawing.Size(221, 20);
            this.txtQuery.TabIndex = 1;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 16);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(59, 13);
            this.label3.TabIndex = 0;
            this.label3.Text = "Search for:";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.runCMD);
            this.groupBox1.Controls.Add(this.textBox1);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(6, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(233, 124);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Filtering";
            // 
            // runCMD
            // 
            this.runCMD.Location = new System.Drawing.Point(187, 97);
            this.runCMD.Name = "runCMD";
            this.runCMD.Size = new System.Drawing.Size(36, 20);
            this.runCMD.TabIndex = 2;
            this.runCMD.Text = "Run";
            this.runCMD.UseVisualStyleBackColor = true;
            this.runCMD.Click += new System.EventHandler(this.runCMD_Click);
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(6, 97);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(175, 20);
            this.textBox1.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(175, 78);
            this.label1.TabIndex = 0;
            this.label1.Text = "You can input your own query here \r\nto filter rows displayed.\r\n\r\nExamples:\r\nLang1" +
    " LIKE \'%admin%\'\r\nId = 1337";
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "IP";
            this.columnHeader1.Width = 135;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Port";
            this.columnHeader2.Width = 80;
            // 
            // oneTimeTimer
            // 
            this.oneTimeTimer.Enabled = true;
            this.oneTimeTimer.Interval = 2000;
            this.oneTimeTimer.Tick += new System.EventHandler(this.oneTimeTimer_Tick);
            // 
            // frmMain
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(877, 508);
            this.Controls.Add(this.dgvStrings);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.MinimumSize = new System.Drawing.Size(726, 220);
            this.Name = "frmMain";
            this.Text = "STREDIT - EMS only - CraftNet";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmMain_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.frmMain_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.frmMain_DragEnter);
            ((System.ComponentModel.ISupportInitialize)(this.dgvStrings)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dgvStrings;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton openToolStripButton;
        private System.Windows.Forms.ToolStripButton saveToolStripButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton toolStripButton1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem exportToCSVToolStripMenuItem;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.ToolStripStatusLabel tsBytesAvailable;
        private System.Windows.Forms.ToolStripStatusLabel versionLabel;
        private System.Windows.Forms.ToolStripStatusLabel tsClientVersion;
        private System.Windows.Forms.ToolStripProgressBar tsLoadProgress;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel4;
        private System.Windows.Forms.ToolStripStatusLabel seperator1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel3;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel7;
        private System.Windows.Forms.ToolStripStatusLabel tsBlockSize;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button runCMD;
        private System.Windows.Forms.ToolStripStatusLabel famSep;
        private System.Windows.Forms.ToolStripStatusLabel derpLabel;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem cSVToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem exportToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem importToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openUpDlogtxtToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openUpExlogtxtToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openUpLtxtToolStripMenuItem;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Button btnSearchHelp;
        private System.Windows.Forms.CheckBox ckSearchUp;
        private System.Windows.Forms.Button btnSearchQuery;
        private System.Windows.Forms.TextBox txtQuery;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Timer oneTimeTimer;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel2;
        private System.Windows.Forms.ToolStripStatusLabel famLabel;
        private System.Windows.Forms.ToolStripMenuItem goToCraftNetToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toEnumerationToolStripMenuItem;
    }
}

