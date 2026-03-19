namespace TrafficSystem
{
    partial class MapRouteForm
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows 窗体 设计器生成的代码

        private void InitializeComponent()
        {
            this.labelTitle = new System.Windows.Forms.Label();
            this.groupByName = new System.Windows.Forms.GroupBox();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.btnCalcByName = new System.Windows.Forms.Button();
            this.btnSnapshotByName = new System.Windows.Forms.Button();
            this.btnSaveByName = new System.Windows.Forms.Button();
            this.labelStart = new System.Windows.Forms.Label();
            this.txtStart = new System.Windows.Forms.TextBox();
            this.labelEnd = new System.Windows.Forms.Label();
            this.txtEnd = new System.Windows.Forms.TextBox();
            this.labelMode = new System.Windows.Forms.Label();
            this.comboMode = new System.Windows.Forms.ComboBox();
            this.groupPoint = new System.Windows.Forms.GroupBox();
            this.btnCalcByPoints = new System.Windows.Forms.Button();
            this.btnSnapshotByPoints = new System.Windows.Forms.Button();
            this.btnSaveByPoints = new System.Windows.Forms.Button();
            this.dgvGraph = new System.Windows.Forms.DataGridView();
            this.lblLastSavedByPoints = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.labelPointName = new System.Windows.Forms.Label();
            this.txtPointName = new System.Windows.Forms.TextBox();
            this.labelPointMode = new System.Windows.Forms.Label();
            this.comboPointMode = new System.Windows.Forms.ComboBox();
            this.labelPointTime = new System.Windows.Forms.Label();
            this.nudPointTime = new System.Windows.Forms.NumericUpDown();
            this.labelPointLng = new System.Windows.Forms.Label();
            this.txtPointLng = new System.Windows.Forms.TextBox();
            this.labelPointLat = new System.Windows.Forms.Label();
            this.txtPointLat = new System.Windows.Forms.TextBox();
            this.btnEnableSelect = new System.Windows.Forms.Button();
            this.btnAddPoint = new System.Windows.Forms.Button();
            this.dgvPoints = new System.Windows.Forms.DataGridView();
            this.btnEditPoint = new System.Windows.Forms.Button();
            this.btnDeletePoint = new System.Windows.Forms.Button();
            this.btnClearPoints = new System.Windows.Forms.Button();
            this.webView21 = new Microsoft.Web.WebView2.WinForms.WebView2();
            this.button1 = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.button2 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.button4 = new System.Windows.Forms.Button();
            this.button5 = new System.Windows.Forms.Button();
            this.groupByName.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.groupPoint.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvGraph)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudPointTime)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPoints)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.webView21)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // labelTitle
            // 
            this.labelTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.labelTitle.Font = new System.Drawing.Font("微软雅黑", 14F, System.Drawing.FontStyle.Bold);
            this.labelTitle.Location = new System.Drawing.Point(0, 0);
            this.labelTitle.Name = "labelTitle";
            this.labelTitle.Size = new System.Drawing.Size(1857, 44);
            this.labelTitle.TabIndex = 0;
            this.labelTitle.Text = "从地图选点 / 地名计算最优路径（高德 JS）";
            this.labelTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // groupByName
            // 
            this.groupByName.Controls.Add(this.dataGridView1);
            this.groupByName.Controls.Add(this.btnCalcByName);
            this.groupByName.Controls.Add(this.btnSnapshotByName);
            this.groupByName.Controls.Add(this.btnSaveByName);
            this.groupByName.Controls.Add(this.labelStart);
            this.groupByName.Controls.Add(this.txtStart);
            this.groupByName.Controls.Add(this.labelEnd);
            this.groupByName.Controls.Add(this.txtEnd);
            this.groupByName.Controls.Add(this.labelMode);
            this.groupByName.Controls.Add(this.comboMode);
            this.groupByName.Location = new System.Drawing.Point(12, 47);
            this.groupByName.Name = "groupByName";
            this.groupByName.Size = new System.Drawing.Size(560, 260);
            this.groupByName.TabIndex = 9;
            this.groupByName.TabStop = false;
            this.groupByName.Text = "方式一：按地名计算（起点/终点）";
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.Location = new System.Drawing.Point(280, 24);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.ReadOnly = true;
            this.dataGridView1.Size = new System.Drawing.Size(272, 218);
            this.dataGridView1.TabIndex = 31;
            // 
            // btnCalcByName
            // 
            this.btnCalcByName.Location = new System.Drawing.Point(22, 140);
            this.btnCalcByName.Name = "btnCalcByName";
            this.btnCalcByName.Size = new System.Drawing.Size(140, 28);
            this.btnCalcByName.TabIndex = 28;
            this.btnCalcByName.Text = "计算最优路径（按地名）";
            this.btnCalcByName.UseVisualStyleBackColor = true;
            // 
            // btnSnapshotByName
            // 
            this.btnSnapshotByName.Location = new System.Drawing.Point(22, 180);
            this.btnSnapshotByName.Name = "btnSnapshotByName";
            this.btnSnapshotByName.Size = new System.Drawing.Size(140, 28);
            this.btnSnapshotByName.TabIndex = 29;
            this.btnSnapshotByName.Text = "截图（按地名）";
            this.btnSnapshotByName.UseVisualStyleBackColor = true;
            // 
            // btnSaveByName
            // 
            this.btnSaveByName.Location = new System.Drawing.Point(22, 216);
            this.btnSaveByName.Name = "btnSaveByName";
            this.btnSaveByName.Size = new System.Drawing.Size(140, 28);
            this.btnSaveByName.TabIndex = 30;
            this.btnSaveByName.Text = "保存到数据库（按名）";
            this.btnSaveByName.UseVisualStyleBackColor = true;
            this.btnSaveByName.Click += new System.EventHandler(this.btnSaveByName_Click_1);
            // 
            // labelStart
            // 
            this.labelStart.Location = new System.Drawing.Point(12, 28);
            this.labelStart.Name = "labelStart";
            this.labelStart.Size = new System.Drawing.Size(80, 22);
            this.labelStart.TabIndex = 20;
            this.labelStart.Text = "起点（地名）:";
            this.labelStart.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtStart
            // 
            this.txtStart.Location = new System.Drawing.Point(98, 28);
            this.txtStart.Name = "txtStart";
            this.txtStart.Size = new System.Drawing.Size(170, 21);
            this.txtStart.TabIndex = 21;
            // 
            // labelEnd
            // 
            this.labelEnd.Location = new System.Drawing.Point(12, 58);
            this.labelEnd.Name = "labelEnd";
            this.labelEnd.Size = new System.Drawing.Size(80, 22);
            this.labelEnd.TabIndex = 22;
            this.labelEnd.Text = "终点（地名）:";
            this.labelEnd.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtEnd
            // 
            this.txtEnd.Location = new System.Drawing.Point(98, 58);
            this.txtEnd.Name = "txtEnd";
            this.txtEnd.Size = new System.Drawing.Size(170, 21);
            this.txtEnd.TabIndex = 23;
            // 
            // labelMode
            // 
            this.labelMode.Location = new System.Drawing.Point(12, 92);
            this.labelMode.Name = "labelMode";
            this.labelMode.Size = new System.Drawing.Size(80, 22);
            this.labelMode.TabIndex = 24;
            this.labelMode.Text = "计算方式：";
            this.labelMode.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // comboMode
            // 
            this.comboMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboMode.FormattingEnabled = true;
            this.comboMode.Location = new System.Drawing.Point(98, 92);
            this.comboMode.Name = "comboMode";
            this.comboMode.Size = new System.Drawing.Size(140, 20);
            this.comboMode.TabIndex = 25;
            // 
            // groupPoint
            // 
            this.groupPoint.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.groupPoint.Controls.Add(this.btnCalcByPoints);
            this.groupPoint.Controls.Add(this.btnSnapshotByPoints);
            this.groupPoint.Controls.Add(this.btnSaveByPoints);
            this.groupPoint.Controls.Add(this.dgvGraph);
            this.groupPoint.Controls.Add(this.lblLastSavedByPoints);
            this.groupPoint.Controls.Add(this.label1);
            this.groupPoint.Controls.Add(this.comboBox1);
            this.groupPoint.Controls.Add(this.labelPointName);
            this.groupPoint.Controls.Add(this.txtPointName);
            this.groupPoint.Controls.Add(this.labelPointMode);
            this.groupPoint.Controls.Add(this.comboPointMode);
            this.groupPoint.Controls.Add(this.labelPointTime);
            this.groupPoint.Controls.Add(this.nudPointTime);
            this.groupPoint.Controls.Add(this.labelPointLng);
            this.groupPoint.Controls.Add(this.txtPointLng);
            this.groupPoint.Controls.Add(this.labelPointLat);
            this.groupPoint.Controls.Add(this.txtPointLat);
            this.groupPoint.Controls.Add(this.btnEnableSelect);
            this.groupPoint.Controls.Add(this.btnAddPoint);
            this.groupPoint.Controls.Add(this.dgvPoints);
            this.groupPoint.Controls.Add(this.btnEditPoint);
            this.groupPoint.Controls.Add(this.btnDeletePoint);
            this.groupPoint.Controls.Add(this.btnClearPoints);
            this.groupPoint.Location = new System.Drawing.Point(599, 47);
            this.groupPoint.Name = "groupPoint";
            this.groupPoint.Size = new System.Drawing.Size(625, 812);
            this.groupPoint.TabIndex = 11;
            this.groupPoint.TabStop = false;
            this.groupPoint.Text = "方式二：按坐标点规划（地图选点 / 点管理）";
            // 
            // btnCalcByPoints
            // 
            this.btnCalcByPoints.Location = new System.Drawing.Point(10, 484);
            this.btnCalcByPoints.Name = "btnCalcByPoints";
            this.btnCalcByPoints.Size = new System.Drawing.Size(180, 30);
            this.btnCalcByPoints.TabIndex = 26;
            this.btnCalcByPoints.Text = "计算最优路径（按坐标点）";
            this.btnCalcByPoints.UseVisualStyleBackColor = true;
            // 
            // btnSnapshotByPoints
            // 
            this.btnSnapshotByPoints.Location = new System.Drawing.Point(235, 484);
            this.btnSnapshotByPoints.Name = "btnSnapshotByPoints";
            this.btnSnapshotByPoints.Size = new System.Drawing.Size(120, 30);
            this.btnSnapshotByPoints.TabIndex = 27;
            this.btnSnapshotByPoints.Text = "截图（按坐标点）";
            this.btnSnapshotByPoints.UseVisualStyleBackColor = true;
            // 
            // btnSaveByPoints
            // 
            this.btnSaveByPoints.Location = new System.Drawing.Point(420, 484);
            this.btnSaveByPoints.Name = "btnSaveByPoints";
            this.btnSaveByPoints.Size = new System.Drawing.Size(197, 30);
            this.btnSaveByPoints.TabIndex = 28;
            this.btnSaveByPoints.Text = "保存到数据库（按坐标）";
            this.btnSaveByPoints.UseVisualStyleBackColor = true;
            // 
            // dgvGraph
            // 
            this.dgvGraph.AllowUserToAddRows = false;
            this.dgvGraph.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.dgvGraph.Location = new System.Drawing.Point(10, 534);
            this.dgvGraph.Name = "dgvGraph";
            this.dgvGraph.ReadOnly = true;
            this.dgvGraph.Size = new System.Drawing.Size(607, 272);
            this.dgvGraph.TabIndex = 29;
            // 
            // lblLastSavedByPoints
            // 
            this.lblLastSavedByPoints.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.lblLastSavedByPoints.Location = new System.Drawing.Point(233, 812);
            this.lblLastSavedByPoints.Name = "lblLastSavedByPoints";
            this.lblLastSavedByPoints.Size = new System.Drawing.Size(384, 28);
            this.lblLastSavedByPoints.TabIndex = 25;
            this.lblLastSavedByPoints.Text = "最后保存（按坐标）：无";
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(8, 72);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(80, 22);
            this.label1.TabIndex = 19;
            this.label1.Text = "计算方式：";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // comboBox1
            // 
            this.comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(96, 72);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(200, 20);
            this.comboBox1.TabIndex = 20;
            // 
            // labelPointName
            // 
            this.labelPointName.Location = new System.Drawing.Point(8, 24);
            this.labelPointName.Name = "labelPointName";
            this.labelPointName.Size = new System.Drawing.Size(60, 20);
            this.labelPointName.TabIndex = 0;
            this.labelPointName.Text = "点名：";
            this.labelPointName.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtPointName
            // 
            this.txtPointName.Location = new System.Drawing.Point(96, 22);
            this.txtPointName.Name = "txtPointName";
            this.txtPointName.Size = new System.Drawing.Size(200, 21);
            this.txtPointName.TabIndex = 1;
            // 
            // labelPointMode
            // 
            this.labelPointMode.Location = new System.Drawing.Point(8, 46);
            this.labelPointMode.Name = "labelPointMode";
            this.labelPointMode.Size = new System.Drawing.Size(80, 20);
            this.labelPointMode.TabIndex = 2;
            this.labelPointMode.Text = "出行方式：";
            this.labelPointMode.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // comboPointMode
            // 
            this.comboPointMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboPointMode.FormattingEnabled = true;
            this.comboPointMode.Location = new System.Drawing.Point(96, 46);
            this.comboPointMode.Name = "comboPointMode";
            this.comboPointMode.Size = new System.Drawing.Size(200, 20);
            this.comboPointMode.TabIndex = 3;
            // 
            // labelPointTime
            // 
            this.labelPointTime.Location = new System.Drawing.Point(8, 100);
            this.labelPointTime.Name = "labelPointTime";
            this.labelPointTime.Size = new System.Drawing.Size(80, 20);
            this.labelPointTime.TabIndex = 4;
            this.labelPointTime.Text = "时间(min)：";
            this.labelPointTime.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // nudPointTime
            // 
            this.nudPointTime.Location = new System.Drawing.Point(96, 100);
            this.nudPointTime.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nudPointTime.Name = "nudPointTime";
            this.nudPointTime.Size = new System.Drawing.Size(200, 21);
            this.nudPointTime.TabIndex = 5;
            // 
            // labelPointLng
            // 
            this.labelPointLng.Location = new System.Drawing.Point(8, 130);
            this.labelPointLng.Name = "labelPointLng";
            this.labelPointLng.Size = new System.Drawing.Size(80, 20);
            this.labelPointLng.TabIndex = 6;
            this.labelPointLng.Text = "经度：";
            this.labelPointLng.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtPointLng
            // 
            this.txtPointLng.Location = new System.Drawing.Point(96, 130);
            this.txtPointLng.Name = "txtPointLng";
            this.txtPointLng.Size = new System.Drawing.Size(200, 21);
            this.txtPointLng.TabIndex = 7;
            // 
            // labelPointLat
            // 
            this.labelPointLat.Location = new System.Drawing.Point(8, 160);
            this.labelPointLat.Name = "labelPointLat";
            this.labelPointLat.Size = new System.Drawing.Size(80, 20);
            this.labelPointLat.TabIndex = 8;
            this.labelPointLat.Text = "纬度：";
            this.labelPointLat.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtPointLat
            // 
            this.txtPointLat.Location = new System.Drawing.Point(96, 160);
            this.txtPointLat.Name = "txtPointLat";
            this.txtPointLat.Size = new System.Drawing.Size(200, 21);
            this.txtPointLat.TabIndex = 9;
            // 
            // btnEnableSelect
            // 
            this.btnEnableSelect.Location = new System.Drawing.Point(381, 41);
            this.btnEnableSelect.Name = "btnEnableSelect";
            this.btnEnableSelect.Size = new System.Drawing.Size(108, 84);
            this.btnEnableSelect.TabIndex = 10;
            this.btnEnableSelect.Text = "启用选点";
            this.btnEnableSelect.UseVisualStyleBackColor = true;
            // 
            // btnAddPoint
            // 
            this.btnAddPoint.Location = new System.Drawing.Point(24, 200);
            this.btnAddPoint.Name = "btnAddPoint";
            this.btnAddPoint.Size = new System.Drawing.Size(80, 28);
            this.btnAddPoint.TabIndex = 11;
            this.btnAddPoint.Text = "添加点";
            this.btnAddPoint.UseVisualStyleBackColor = true;
            // 
            // dgvPoints
            // 
            this.dgvPoints.AllowUserToAddRows = false;
            this.dgvPoints.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvPoints.Location = new System.Drawing.Point(10, 247);
            this.dgvPoints.Name = "dgvPoints";
            this.dgvPoints.ReadOnly = true;
            this.dgvPoints.Size = new System.Drawing.Size(609, 217);
            this.dgvPoints.TabIndex = 15;
            // 
            // btnEditPoint
            // 
            this.btnEditPoint.Location = new System.Drawing.Point(190, 200);
            this.btnEditPoint.Name = "btnEditPoint";
            this.btnEditPoint.Size = new System.Drawing.Size(80, 28);
            this.btnEditPoint.TabIndex = 12;
            this.btnEditPoint.Text = "编辑点";
            this.btnEditPoint.UseVisualStyleBackColor = true;
            // 
            // btnDeletePoint
            // 
            this.btnDeletePoint.Location = new System.Drawing.Point(362, 200);
            this.btnDeletePoint.Name = "btnDeletePoint";
            this.btnDeletePoint.Size = new System.Drawing.Size(80, 28);
            this.btnDeletePoint.TabIndex = 13;
            this.btnDeletePoint.Text = "删除点";
            this.btnDeletePoint.UseVisualStyleBackColor = true;
            // 
            // btnClearPoints
            // 
            this.btnClearPoints.Location = new System.Drawing.Point(512, 200);
            this.btnClearPoints.Name = "btnClearPoints";
            this.btnClearPoints.Size = new System.Drawing.Size(80, 28);
            this.btnClearPoints.TabIndex = 14;
            this.btnClearPoints.Text = "清空点";
            this.btnClearPoints.UseVisualStyleBackColor = true;
            // 
            // webView21
            // 
            this.webView21.AllowExternalDrop = true;
            this.webView21.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.webView21.CreationProperties = null;
            this.webView21.DefaultBackgroundColor = System.Drawing.Color.White;
            this.webView21.Location = new System.Drawing.Point(1230, 47);
            this.webView21.Name = "webView21";
            this.webView21.Size = new System.Drawing.Size(615, 812);
            this.webView21.TabIndex = 17;
            this.webView21.ZoomFactor = 1D;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(21, 810);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(92, 50);
            this.button1.TabIndex = 18;
            this.button1.Text = "带权重的路网连通图";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(203, 323);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(137, 12);
            this.label2.TabIndex = 19;
            this.label2.Text = "根据最优路径生成所需图";
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
            this.pictureBox1.Location = new System.Drawing.Point(12, 338);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(560, 462);
            this.pictureBox1.TabIndex = 20;
            this.pictureBox1.TabStop = false;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(119, 811);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(64, 49);
            this.button2.TabIndex = 21;
            this.button2.Text = "最优路径高亮图";
            this.button2.UseVisualStyleBackColor = true;
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(189, 811);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(70, 49);
            this.button3.TabIndex = 22;
            this.button3.Text = "抽象拓扑结构图";
            this.button3.UseVisualStyleBackColor = true;
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(265, 811);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(63, 49);
            this.button4.TabIndex = 23;
            this.button4.Text = "多路径对比图";
            this.button4.UseVisualStyleBackColor = true;
            // 
            // button5
            // 
            this.button5.Location = new System.Drawing.Point(334, 811);
            this.button5.Name = "button5";
            this.button5.Size = new System.Drawing.Size(72, 49);
            this.button5.TabIndex = 24;
            this.button5.Text = "路径热力权重图";
            this.button5.UseVisualStyleBackColor = true;
            // 
            // MapRouteForm
            // 
            this.ClientSize = new System.Drawing.Size(1857, 900);
            this.Controls.Add(this.button5);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.webView21);
            this.Controls.Add(this.labelTitle);
            this.Controls.Add(this.groupByName);
            this.Controls.Add(this.groupPoint);
            this.MinimumSize = new System.Drawing.Size(1000, 600);
            this.Name = "MapRouteForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "从地名/地图选点计算最优路径（高德 JS）";
            this.Load += new System.EventHandler(this.MapRouteForm_Load);
            this.groupByName.ResumeLayout(false);
            this.groupByName.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.groupPoint.ResumeLayout(false);
            this.groupPoint.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvGraph)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudPointTime)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPoints)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.webView21)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelTitle;
        private System.Windows.Forms.GroupBox groupByName;
        private System.Windows.Forms.GroupBox groupPoint;
        private System.Windows.Forms.Label labelPointName;
        private System.Windows.Forms.TextBox txtPointName;
        private System.Windows.Forms.Label labelPointMode;
        private System.Windows.Forms.ComboBox comboPointMode;
        private System.Windows.Forms.Label labelPointTime;
        private System.Windows.Forms.NumericUpDown nudPointTime;
        private System.Windows.Forms.Label labelPointLng;
        private System.Windows.Forms.TextBox txtPointLng;
        private System.Windows.Forms.Label labelPointLat;
        private System.Windows.Forms.TextBox txtPointLat;
        private System.Windows.Forms.Button btnEnableSelect;
        private System.Windows.Forms.Button btnAddPoint;
        private System.Windows.Forms.Button btnEditPoint;
        private System.Windows.Forms.Button btnDeletePoint;
        private System.Windows.Forms.Button btnClearPoints;
        private System.Windows.Forms.DataGridView dgvPoints;
        private System.Windows.Forms.Label lblLastSavedByPoints;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.Label labelStart;
        private System.Windows.Forms.TextBox txtStart;
        private System.Windows.Forms.Label labelEnd;
        private System.Windows.Forms.TextBox txtEnd;
        private System.Windows.Forms.Label labelMode;
        private System.Windows.Forms.ComboBox comboMode;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Button btnCalcByName;
        private System.Windows.Forms.Button btnSnapshotByName;
        private System.Windows.Forms.Button btnSaveByName;
        private Microsoft.Web.WebView2.WinForms.WebView2 webView21;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Button button5;
        private System.Windows.Forms.Button btnCalcByPoints;
        private System.Windows.Forms.Button btnSnapshotByPoints;
        private System.Windows.Forms.Button btnSaveByPoints;
        private System.Windows.Forms.DataGridView dgvGraph;
    }
}
