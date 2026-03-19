namespace TrafficSystem
{
    partial class SaveResultDialog
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabLocal;
        private System.Windows.Forms.TabPage tabCloud;

        private System.Windows.Forms.ComboBox cboLocalTable;
        private System.Windows.Forms.Label lblLocalTable;
        private System.Windows.Forms.DataGridView dgvLocalPreview;
        private System.Windows.Forms.PictureBox picSnapshot;
        private System.Windows.Forms.Label lblSnapshotPath;
        private System.Windows.Forms.Button btnExportCsv;
        private System.Windows.Forms.Button btnOpenSnapshot;

        private System.Windows.Forms.TextBox txtConnStr;
        private System.Windows.Forms.Label lblConnStr;
        private System.Windows.Forms.Button btnTestConn;
        private System.Windows.Forms.Button btnRefreshTables;
        private System.Windows.Forms.ComboBox cboCloudTables;
        private System.Windows.Forms.Label lblCloudTables;
        private System.Windows.Forms.Button btnPreviewTable;
        private System.Windows.Forms.DataGridView dgvCloudPreview;

        private System.Windows.Forms.Label lblNewTable;
        private System.Windows.Forms.TextBox txtNewTableName;
        private System.Windows.Forms.Button btnCreateTable;
        private System.Windows.Forms.Button btnInsertToTable;

        private System.Windows.Forms.Button btnLoadEditable;
        private System.Windows.Forms.Button btnSaveEdits;
        private System.Windows.Forms.DataGridView dgvCloudEdit;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabLocal = new System.Windows.Forms.TabPage();
            this.btnOpenSnapshot = new System.Windows.Forms.Button();
            this.btnExportCsv = new System.Windows.Forms.Button();
            this.lblSnapshotPath = new System.Windows.Forms.Label();
            this.picSnapshot = new System.Windows.Forms.PictureBox();
            this.dgvLocalPreview = new System.Windows.Forms.DataGridView();
            this.cboLocalTable = new System.Windows.Forms.ComboBox();
            this.lblLocalTable = new System.Windows.Forms.Label();
            this.tabCloud = new System.Windows.Forms.TabPage();
            this.dgvCloudEdit = new System.Windows.Forms.DataGridView();
            this.btnSaveEdits = new System.Windows.Forms.Button();
            this.btnLoadEditable = new System.Windows.Forms.Button();
            this.btnInsertToTable = new System.Windows.Forms.Button();
            this.btnCreateTable = new System.Windows.Forms.Button();
            this.txtNewTableName = new System.Windows.Forms.TextBox();
            this.lblNewTable = new System.Windows.Forms.Label();
            this.dgvCloudPreview = new System.Windows.Forms.DataGridView();
            this.btnPreviewTable = new System.Windows.Forms.Button();
            this.lblCloudTables = new System.Windows.Forms.Label();
            this.cboCloudTables = new System.Windows.Forms.ComboBox();
            this.btnRefreshTables = new System.Windows.Forms.Button();
            this.btnTestConn = new System.Windows.Forms.Button();
            this.lblConnStr = new System.Windows.Forms.Label();
            this.txtConnStr = new System.Windows.Forms.TextBox();
            this.tabControl1.SuspendLayout();
            this.tabLocal.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picSnapshot)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvLocalPreview)).BeginInit();
            this.tabCloud.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvCloudEdit)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvCloudPreview)).BeginInit();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabLocal);
            this.tabControl1.Controls.Add(this.tabCloud);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1011, 720);
            this.tabControl1.TabIndex = 0;
            // 
            // tabLocal
            // 
            this.tabLocal.Controls.Add(this.btnOpenSnapshot);
            this.tabLocal.Controls.Add(this.btnExportCsv);
            this.tabLocal.Controls.Add(this.lblSnapshotPath);
            this.tabLocal.Controls.Add(this.picSnapshot);
            this.tabLocal.Controls.Add(this.dgvLocalPreview);
            this.tabLocal.Controls.Add(this.cboLocalTable);
            this.tabLocal.Controls.Add(this.lblLocalTable);
            this.tabLocal.Location = new System.Drawing.Point(4, 22);
            this.tabLocal.Name = "tabLocal";
            this.tabLocal.Padding = new System.Windows.Forms.Padding(3);
            this.tabLocal.Size = new System.Drawing.Size(1003, 694);
            this.tabLocal.TabIndex = 0;
            this.tabLocal.Text = "保存到本地（CSV + 快照预览）";
            this.tabLocal.UseVisualStyleBackColor = true;
            // 
            // btnOpenSnapshot
            // 
            this.btnOpenSnapshot.Location = new System.Drawing.Point(351, 7);
            this.btnOpenSnapshot.Name = "btnOpenSnapshot";
            this.btnOpenSnapshot.Size = new System.Drawing.Size(103, 25);
            this.btnOpenSnapshot.TabIndex = 3;
            this.btnOpenSnapshot.Text = "打开快照文件";
            this.btnOpenSnapshot.UseVisualStyleBackColor = true;
            // 
            // btnExportCsv
            // 
            this.btnExportCsv.Location = new System.Drawing.Point(240, 7);
            this.btnExportCsv.Name = "btnExportCsv";
            this.btnExportCsv.Size = new System.Drawing.Size(103, 25);
            this.btnExportCsv.TabIndex = 2;
            this.btnExportCsv.Text = "导出CSV";
            this.btnExportCsv.UseVisualStyleBackColor = true;
            // 
            // lblSnapshotPath
            // 
            this.lblSnapshotPath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblSnapshotPath.Location = new System.Drawing.Point(722, 40);
            this.lblSnapshotPath.Name = "lblSnapshotPath";
            this.lblSnapshotPath.Size = new System.Drawing.Size(274, 25);
            this.lblSnapshotPath.TabIndex = 6;
            this.lblSnapshotPath.Text = "快照：无";
            // 
            // picSnapshot
            // 
            this.picSnapshot.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.picSnapshot.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
            this.picSnapshot.Location = new System.Drawing.Point(722, 70);
            this.picSnapshot.Name = "picSnapshot";
            this.picSnapshot.Size = new System.Drawing.Size(274, 610);
            this.picSnapshot.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picSnapshot.TabIndex = 5;
            this.picSnapshot.TabStop = false;
            // 
            // dgvLocalPreview
            // 
            this.dgvLocalPreview.AllowUserToAddRows = false;
            this.dgvLocalPreview.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvLocalPreview.Location = new System.Drawing.Point(10, 40);
            this.dgvLocalPreview.Name = "dgvLocalPreview";
            this.dgvLocalPreview.ReadOnly = true;
            this.dgvLocalPreview.Size = new System.Drawing.Size(703, 640);
            this.dgvLocalPreview.TabIndex = 4;
            // 
            // cboLocalTable
            // 
            this.cboLocalTable.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboLocalTable.FormattingEnabled = true;
            this.cboLocalTable.Location = new System.Drawing.Point(67, 9);
            this.cboLocalTable.Name = "cboLocalTable";
            this.cboLocalTable.Size = new System.Drawing.Size(155, 20);
            this.cboLocalTable.TabIndex = 1;
            // 
            // lblLocalTable
            // 
            this.lblLocalTable.AutoSize = true;
            this.lblLocalTable.Location = new System.Drawing.Point(9, 12);
            this.lblLocalTable.Name = "lblLocalTable";
            this.lblLocalTable.Size = new System.Drawing.Size(53, 12);
            this.lblLocalTable.TabIndex = 0;
            this.lblLocalTable.Text = "选择表：";
            // 
            // tabCloud
            // 
            this.tabCloud.Controls.Add(this.dgvCloudEdit);
            this.tabCloud.Controls.Add(this.btnSaveEdits);
            this.tabCloud.Controls.Add(this.btnLoadEditable);
            this.tabCloud.Controls.Add(this.btnInsertToTable);
            this.tabCloud.Controls.Add(this.btnCreateTable);
            this.tabCloud.Controls.Add(this.txtNewTableName);
            this.tabCloud.Controls.Add(this.lblNewTable);
            this.tabCloud.Controls.Add(this.dgvCloudPreview);
            this.tabCloud.Controls.Add(this.btnPreviewTable);
            this.tabCloud.Controls.Add(this.lblCloudTables);
            this.tabCloud.Controls.Add(this.cboCloudTables);
            this.tabCloud.Controls.Add(this.btnRefreshTables);
            this.tabCloud.Controls.Add(this.btnTestConn);
            this.tabCloud.Controls.Add(this.lblConnStr);
            this.tabCloud.Controls.Add(this.txtConnStr);
            this.tabCloud.Location = new System.Drawing.Point(4, 22);
            this.tabCloud.Name = "tabCloud";
            this.tabCloud.Padding = new System.Windows.Forms.Padding(3);
            this.tabCloud.Size = new System.Drawing.Size(1003, 694);
            this.tabCloud.TabIndex = 1;
            this.tabCloud.Text = "保存到云端（MySQL）";
            this.tabCloud.UseVisualStyleBackColor = true;
            // 
            // dgvCloudEdit
            // 
            this.dgvCloudEdit.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvCloudEdit.Location = new System.Drawing.Point(10, 360);
            this.dgvCloudEdit.Name = "dgvCloudEdit";
            this.dgvCloudEdit.Size = new System.Drawing.Size(975, 320);
            this.dgvCloudEdit.TabIndex = 14;
            // 
            // btnSaveEdits
            // 
            this.btnSaveEdits.Location = new System.Drawing.Point(207, 325);
            this.btnSaveEdits.Name = "btnSaveEdits";
            this.btnSaveEdits.Size = new System.Drawing.Size(189, 28);
            this.btnSaveEdits.TabIndex = 13;
            this.btnSaveEdits.Text = "保存现有改动（真正写库）";
            this.btnSaveEdits.UseVisualStyleBackColor = true;
            // 
            // btnLoadEditable
            // 
            this.btnLoadEditable.Location = new System.Drawing.Point(10, 325);
            this.btnLoadEditable.Name = "btnLoadEditable";
            this.btnLoadEditable.Size = new System.Drawing.Size(189, 28);
            this.btnLoadEditable.TabIndex = 12;
            this.btnLoadEditable.Text = "加载所选云端表到可编辑区";
            this.btnLoadEditable.UseVisualStyleBackColor = true;
            // 
            // btnInsertToTable
            // 
            this.btnInsertToTable.Location = new System.Drawing.Point(763, 40);
            this.btnInsertToTable.Name = "btnInsertToTable";
            this.btnInsertToTable.Size = new System.Drawing.Size(223, 25);
            this.btnInsertToTable.TabIndex = 10;
            this.btnInsertToTable.Text = "写入到所选云端表（真正写库）";
            this.btnInsertToTable.UseVisualStyleBackColor = true;
            // 
            // btnCreateTable
            // 
            this.btnCreateTable.Location = new System.Drawing.Point(617, 40);
            this.btnCreateTable.Name = "btnCreateTable";
            this.btnCreateTable.Size = new System.Drawing.Size(137, 25);
            this.btnCreateTable.TabIndex = 9;
            this.btnCreateTable.Text = "新建表（按结果结构）";
            this.btnCreateTable.UseVisualStyleBackColor = true;
            // 
            // txtNewTableName
            // 
            this.txtNewTableName.Location = new System.Drawing.Point(454, 42);
            this.txtNewTableName.Name = "txtNewTableName";
            this.txtNewTableName.Size = new System.Drawing.Size(155, 21);
            this.txtNewTableName.TabIndex = 8;
            this.txtNewTableName.Text = "route_results";
            // 
            // lblNewTable
            // 
            this.lblNewTable.AutoSize = true;
            this.lblNewTable.Location = new System.Drawing.Point(386, 45);
            this.lblNewTable.Name = "lblNewTable";
            this.lblNewTable.Size = new System.Drawing.Size(65, 12);
            this.lblNewTable.TabIndex = 7;
            this.lblNewTable.Text = "新建表名：";
            // 
            // dgvCloudPreview
            // 
            this.dgvCloudPreview.AllowUserToAddRows = false;
            this.dgvCloudPreview.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvCloudPreview.Location = new System.Drawing.Point(10, 75);
            this.dgvCloudPreview.Name = "dgvCloudPreview";
            this.dgvCloudPreview.ReadOnly = true;
            this.dgvCloudPreview.Size = new System.Drawing.Size(975, 240);
            this.dgvCloudPreview.TabIndex = 11;
            // 
            // btnPreviewTable
            // 
            this.btnPreviewTable.Location = new System.Drawing.Point(266, 40);
            this.btnPreviewTable.Name = "btnPreviewTable";
            this.btnPreviewTable.Size = new System.Drawing.Size(103, 25);
            this.btnPreviewTable.TabIndex = 6;
            this.btnPreviewTable.Text = "预览所选表";
            this.btnPreviewTable.UseVisualStyleBackColor = true;
            // 
            // lblCloudTables
            // 
            this.lblCloudTables.AutoSize = true;
            this.lblCloudTables.Location = new System.Drawing.Point(9, 45);
            this.lblCloudTables.Name = "lblCloudTables";
            this.lblCloudTables.Size = new System.Drawing.Size(53, 12);
            this.lblCloudTables.TabIndex = 4;
            this.lblCloudTables.Text = "云端表：";
            // 
            // cboCloudTables
            // 
            this.cboCloudTables.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboCloudTables.FormattingEnabled = true;
            this.cboCloudTables.Location = new System.Drawing.Point(69, 42);
            this.cboCloudTables.Name = "cboCloudTables";
            this.cboCloudTables.Size = new System.Drawing.Size(189, 20);
            this.cboCloudTables.TabIndex = 5;
            // 
            // btnRefreshTables
            // 
            this.btnRefreshTables.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRefreshTables.Location = new System.Drawing.Point(874, 8);
            this.btnRefreshTables.Name = "btnRefreshTables";
            this.btnRefreshTables.Size = new System.Drawing.Size(111, 25);
            this.btnRefreshTables.TabIndex = 3;
            this.btnRefreshTables.Text = "刷新表列表";
            this.btnRefreshTables.UseVisualStyleBackColor = true;
            // 
            // btnTestConn
            // 
            this.btnTestConn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnTestConn.Location = new System.Drawing.Point(780, 8);
            this.btnTestConn.Name = "btnTestConn";
            this.btnTestConn.Size = new System.Drawing.Size(86, 25);
            this.btnTestConn.TabIndex = 2;
            this.btnTestConn.Text = "测试连接";
            this.btnTestConn.UseVisualStyleBackColor = true;
            // 
            // lblConnStr
            // 
            this.lblConnStr.AutoSize = true;
            this.lblConnStr.Location = new System.Drawing.Point(9, 13);
            this.lblConnStr.Name = "lblConnStr";
            this.lblConnStr.Size = new System.Drawing.Size(53, 12);
            this.lblConnStr.TabIndex = 1;
            this.lblConnStr.Text = "连接串：";
            // 
            // txtConnStr
            // 
            this.txtConnStr.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtConnStr.Location = new System.Drawing.Point(69, 10);
            this.txtConnStr.Name = "txtConnStr";
            this.txtConnStr.ReadOnly = true;
            this.txtConnStr.Size = new System.Drawing.Size(703, 21);
            this.txtConnStr.TabIndex = 0;
            // 
            // SaveResultDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1011, 720);
            this.Controls.Add(this.tabControl1);
            this.Name = "SaveResultDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "保存结果（本地CSV / 云端MySQL）";
            this.Load += new System.EventHandler(this.SaveResultDialog_Load);
            this.tabControl1.ResumeLayout(false);
            this.tabLocal.ResumeLayout(false);
            this.tabLocal.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picSnapshot)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvLocalPreview)).EndInit();
            this.tabCloud.ResumeLayout(false);
            this.tabCloud.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvCloudEdit)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvCloudPreview)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
    }
}
