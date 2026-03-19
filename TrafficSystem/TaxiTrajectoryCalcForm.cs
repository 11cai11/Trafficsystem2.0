// TaxiTrajectoryCalcForm.cs
// 说明：出租车轨迹计算窗体（速度/方位角 + 距离）
// 功能点：
// 1) 导入（主界面已加载 / 本地TXT）-> 列映射 -> 开始计算
// 2) Tab1/Tab2 分开展示结果
// 3) 本地保存：当前Tab是哪个表就只保存哪个表
// 4) 云端保存：✅直接打开 SaveResultDialog（Odbc DSN=OKS）
//
// ✅关键要求：只允许 DSN=OKS;
// .NET Framework 4.7.2 + C# 7.3

using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TrafficSystem
{
    public partial class TaxiTrajectoryCalcForm : Form
    {
        // ✅ 全项目统一：只允许 DSN=OKS;
        private const string OnlyConnStr = "DSN=OKS;";

        private readonly string _currentUser;
        private readonly string _connStr;
        private readonly MainForm _owner;
        private readonly DataTable _providedTable;

        private DataTable _rawTableLoaded;
        private bool _importedOk = false;
        private string _importFilePath = null;
        private Encoding _importEncoding = null;

        private RadioButton rbFromOwner;
        private RadioButton rbFromFile;

        private ComboBox cbIdCol, cbTimeCol, cbXCol, cbYCol;

        private Button btnImportData;
        private Button btnStartCalc;
        private Button btnSaveLocal;
        private Button btnSaveCloud;

        private TabControl tabResults;
        private DataGridView dgvSpeedAzimuth, dgvDistanceResults;
        private Label lblStatus;

        private DataTable tableSpeedAzimuth;
        private DataTable tableDistanceResults;

        public TaxiTrajectoryCalcForm()
        {
            InitializeComponent();

            _currentUser = "未登录用户";
            _connStr = OnlyConnStr; // ✅强制 DSN=OKS;
            _owner = null;
            _providedTable = null;

            InitStyle();
            BuildControls();
        }

        public TaxiTrajectoryCalcForm(string currentUser, string connStr, MainForm owner, DataTable providedTable = null) : this()
        {
            _currentUser = string.IsNullOrWhiteSpace(currentUser) ? "未登录用户" : currentUser.Trim();

            // ✅无论外部传什么，都强制 DSN=OKS;
            _connStr = OnlyConnStr;

            _owner = owner;
            _providedTable = providedTable;

            BuildControls();
        }

        private void TaxiTrajectoryCalcForm_Load(object sender, EventArgs e) { }

        private void InitStyle()
        {
            this.Text = "出租车轨迹计算（速度/方位角 + 距离）";
            this.Size = new Size(920, 620);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
        }

        private bool IsSpeedTab => tabResults != null && tabResults.SelectedIndex == 0;

        private void BuildControls()
        {
            this.Controls.Clear();

            Label lblTip = new Label { Text = "数据来源：", Location = new Point(12, 12), Size = new Size(80, 22) };
            this.Controls.Add(lblTip);

            rbFromOwner = new RadioButton { Text = "主界面已加载", Location = new Point(96, 12), Checked = (_providedTable != null) };
            rbFromFile = new RadioButton { Text = "从本地 TXT 导", Location = new Point(240, 12), Checked = (_providedTable == null) };

            rbFromOwner.CheckedChanged += DataSource_CheckedChanged;
            rbFromFile.CheckedChanged += DataSource_CheckedChanged;

            this.Controls.Add(rbFromOwner);
            this.Controls.Add(rbFromFile);

            Label lblCols = new Label { Text = "列映射（识别列）:", Location = new Point(12, 42), Size = new Size(200, 22) };
            this.Controls.Add(lblCols);

            cbIdCol = new ComboBox { Location = new Point(12, 70), Size = new Size(200, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            cbTimeCol = new ComboBox { Location = new Point(230, 70), Size = new Size(220, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            cbXCol = new ComboBox { Location = new Point(470, 70), Size = new Size(200, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            cbYCol = new ComboBox { Location = new Point(690, 70), Size = new Size(200, 26), DropDownStyle = ComboBoxStyle.DropDownList };

            this.Controls.Add(new Label { Text = "车辆ID/车牌ID", Location = new Point(12, 98), Size = new Size(200, 18) });
            this.Controls.Add(new Label { Text = "时间列", Location = new Point(230, 98), Size = new Size(220, 18) });
            this.Controls.Add(new Label { Text = "X列", Location = new Point(470, 98), Size = new Size(200, 18) });
            this.Controls.Add(new Label { Text = "Y列", Location = new Point(690, 98), Size = new Size(200, 18) });

            this.Controls.Add(cbIdCol);
            this.Controls.Add(cbTimeCol);
            this.Controls.Add(cbXCol);
            this.Controls.Add(cbYCol);

            btnImportData = new Button
            {
                Text = "导入数据（主界面 / 本地TXT）",
                Location = new Point(12, 130),
                Size = new Size(340, 32),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnImportData.FlatAppearance.BorderSize = 0;
            btnImportData.Click += (s, e) =>
            {
                try { BtnImportData_Click(); }
                catch (Exception ex)
                {
                    MessageBox.Show("导入失败：\r\n" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            this.Controls.Add(btnImportData);

            btnStartCalc = new Button
            {
                Text = "开始计算",
                Location = new Point(370, 130),
                Size = new Size(140, 32),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            btnStartCalc.FlatAppearance.BorderSize = 0;
            btnStartCalc.Click += (s, e) =>
            {
                try { BtnStartCalc_Click(); }
                catch (Exception ex)
                {
                    MessageBox.Show("计算失败：\r\n" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            this.Controls.Add(btnStartCalc);

            lblStatus = new Label
            {
                Text = $"状态：当前用户={_currentUser}，等待导入（云端连接=DSN=OKS）",
                Location = new Point(530, 136),
                Size = new Size(360, 22),
                ForeColor = Color.DarkBlue
            };
            this.Controls.Add(lblStatus);

            tabResults = new TabControl { Location = new Point(12, 170), Size = new Size(880, 360) };
            var tab1 = new TabPage("速度方位角计算");
            var tab2 = new TabPage("距离计算结果");

            dgvSpeedAzimuth = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
            dgvDistanceResults = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };

            tab1.Controls.Add(dgvSpeedAzimuth);
            tab2.Controls.Add(dgvDistanceResults);

            tabResults.TabPages.Add(tab1);
            tabResults.TabPages.Add(tab2);

            tabResults.SelectedIndexChanged += (s, e) =>
            {
                lblStatus.Text = IsSpeedTab
                    ? "状态：当前表 = 速度方位角（保存/云端只作用于该表）"
                    : "状态：当前表 = 距离结果（保存/云端只作用于该表）";
            };

            this.Controls.Add(tabResults);

            btnSaveLocal = new Button
            {
                Text = "保存当前表到本地 TXT",
                Location = new Point(12, 540),
                Size = new Size(220, 30),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            btnSaveLocal.FlatAppearance.BorderSize = 0;
            btnSaveLocal.Click += (s, e) =>
            {
                try { BtnSaveLocal_Click(); }
                catch (Exception ex)
                {
                    MessageBox.Show("本地保存失败：\r\n" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            this.Controls.Add(btnSaveLocal);

            btnSaveCloud = new Button
            {
                Text = "保存数据（打开 SaveResultDialog）",
                Location = new Point(250, 540),
                Size = new Size(260, 30),
                BackColor = Color.FromArgb(155, 89, 182),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            btnSaveCloud.FlatAppearance.BorderSize = 0;
            btnSaveCloud.Click += (s, e) =>
            {
                try { BtnSaveCloud_Click(); }
                catch (Exception ex)
                {
                    MessageBox.Show("保存失败：\r\n" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            this.Controls.Add(btnSaveCloud);

            if (_providedTable == null)
            {
                rbFromOwner.Enabled = false;
                rbFromFile.Checked = true;
                lblStatus.Text = "状态：未检测到主界面表，请选择本地 TXT 导入。";
                DisableMappingCombos();
            }
            else
            {
                rbFromOwner.Enabled = true;
                lblStatus.Text = "状态：检测到主界面表。请点击“导入数据”后再开始计算。";
                DisableMappingCombos();
            }
        }

        private void DataSource_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                var rb = sender as RadioButton;
                if (rb == null || !rb.Checked) return;

                ClearResults();
                ClearImportedState();
                ClearMappingCombos();

                lblStatus.Text = rbFromFile.Checked
                    ? "状态：已切换到本地TXT导入（已清理旧结果）。请先点击“导入数据”。"
                    : "状态：已切换到主界面已加载（已清理旧结果）。请点击“导入数据”。";
            }
            catch { }
        }

        private void DisableMappingCombos()
        {
            cbIdCol.Enabled = false;
            cbTimeCol.Enabled = false;
            cbXCol.Enabled = false;
            cbYCol.Enabled = false;
        }

        private void EnableMappingCombos()
        {
            cbIdCol.Enabled = true;
            cbTimeCol.Enabled = true;
            cbXCol.Enabled = true;
            cbYCol.Enabled = true;
        }

        private void ClearMappingCombos()
        {
            cbIdCol.Items.Clear(); cbIdCol.SelectedIndex = -1;
            cbTimeCol.Items.Clear(); cbTimeCol.SelectedIndex = -1;
            cbXCol.Items.Clear(); cbXCol.SelectedIndex = -1;
            cbYCol.Items.Clear(); cbYCol.SelectedIndex = -1;
            DisableMappingCombos();
        }

        private void ClearImportedState()
        {
            _rawTableLoaded = null;
            _importedOk = false;
            _importFilePath = null;
            _importEncoding = null;
            btnStartCalc.Enabled = false;
        }

        private void ClearResults()
        {
            tableSpeedAzimuth = null;
            tableDistanceResults = null;
            dgvSpeedAzimuth.DataSource = null;
            dgvDistanceResults.DataSource = null;
            btnSaveLocal.Enabled = false;
            btnSaveCloud.Enabled = false;
        }

        private void PopulateColumnCombos(DataTable dt)
        {
            ClearMappingCombos();
            if (dt == null || dt.Columns.Count == 0) return;

            var candidates = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
            foreach (var c in candidates)
            {
                cbIdCol.Items.Add(c);
                cbTimeCol.Items.Add(c);
                cbXCol.Items.Add(c);
                cbYCol.Items.Add(c);
            }

            EnableMappingCombos();

            string Find(params string[] names)
            {
                return candidates.FirstOrDefault(col => names.Any(n => string.Equals(col, n, StringComparison.OrdinalIgnoreCase)));
            }

            string id = Find("车辆ID", "车牌ID", "车牌", "车辆标识", "vehicleid", "veh_id", "id", "vehicle");
            string t = Find("时间", "时间列", "timestamp", "time", "datetime", "date");
            string x = Find("X坐标", "x", "lon", "lng", "longitude", "X");
            string y = Find("Y坐标", "y", "lat", "latitude", "Y");

            if (id != null) cbIdCol.SelectedItem = id;
            if (t != null) cbTimeCol.SelectedItem = t;
            if (x != null) cbXCol.SelectedItem = x;
            if (y != null) cbYCol.SelectedItem = y;
        }

        private void BtnImportData_Click()
        {
            ClearResults();

            if (rbFromOwner.Checked)
            {
                if (_providedTable == null)
                {
                    MessageBox.Show("主界面未提供表，请先在主界面加载数据或切换到本地导入。", "导入失败",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _rawTableLoaded = _providedTable.Copy();
                _importedOk = true;
                _importFilePath = null;
                _importEncoding = null;

                PopulateColumnCombos(_rawTableLoaded);
                btnStartCalc.Enabled = true;
                lblStatus.Text = $"状态：导入成功（主界面表），行数={_rawTableLoaded.Rows.Count}，列数={_rawTableLoaded.Columns.Count}";

                MessageBox.Show("导入成功（来自主界面已加载数据）。\r\n请检查上方列映射，确认后点击“开始计算”。",
                    "导入成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var ofd = new OpenFileDialog()
            {
                Filter = "文本文件 (*.txt;*.csv)|*.txt;*.csv|所有文件|*.*",
                Multiselect = false
            })
            {
                if (ofd.ShowDialog(this) != DialogResult.OK) return;

                _importFilePath = ofd.FileName;

                ClearMappingCombos();
                ClearResults();

                var dt = LoadTextFileToDataTable(_importFilePath, out Encoding usedEnc);
                _importEncoding = usedEnc;

                if (dt == null || dt.Rows.Count == 0 || dt.Columns.Count < 4)
                {
                    ShowImportFormatError("文件为空或列数不足。必须至少包含4列：车辆ID/车牌ID、时间、X坐标、Y坐标。");
                    return;
                }

                _rawTableLoaded = dt;
                _importedOk = true;

                PopulateColumnCombos(_rawTableLoaded);
                btnStartCalc.Enabled = true;

                lblStatus.Text = $"状态：导入成功（本地文件），行数={_rawTableLoaded.Rows.Count}，列数={_rawTableLoaded.Columns.Count}";

                MessageBox.Show("导入成功（本地TXT）。\r\n请检查上方列映射，确认后点击“开始计算”。",
                    "导入成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ShowImportFormatError(string extra)
        {
            string msg =
                "导入不成功。\r\n\r\n" +
                extra + "\r\n\r\n" +
                "请保证本地TXT/CSV至少包含以下4个元素（顺序不限，但你要在上方做列映射）：\r\n" +
                "1) 车辆ID/车牌ID\r\n" +
                "2) 时间（支持 yyyy-MM-dd HH:mm:ss 或 14位数字 yyyyMMddHHmmss）\r\n" +
                "3) X坐标（经度或平面X）\r\n" +
                "4) Y坐标（纬度或平面Y）\r\n\r\n" +
                "分隔符支持：Tab、逗号(,)、分号(;)\r\n" +
                "第一行可以是表头，也可以没有表头。";

            MessageBox.Show(msg, "导入失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            lblStatus.Text = "状态：导入失败（请按要求检查格式）";
            ClearImportedState();
            ClearMappingCombos();
        }

        private void BtnStartCalc_Click()
        {
            if (!_importedOk || _rawTableLoaded == null)
            {
                MessageBox.Show("请先点击“导入数据”，导入成功后再计算。", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (cbIdCol.SelectedItem == null || cbTimeCol.SelectedItem == null ||
                cbXCol.SelectedItem == null || cbYCol.SelectedItem == null)
            {
                MessageBox.Show("请在上方选择 车辆ID/车牌ID、时间、X、Y 列。", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string idCol = cbIdCol.SelectedItem.ToString();
            string timeCol = cbTimeCol.SelectedItem.ToString();
            string xCol = cbXCol.SelectedItem.ToString();
            string yCol = cbYCol.SelectedItem.ToString();

            var records = new List<TaxiRecord>();

            foreach (DataRow r in _rawTableLoaded.Rows)
            {
                try
                {
                    string vid = (r[idCol] == DBNull.Value) ? "" : r[idCol].ToString().Trim();
                    string t = (r[timeCol] == DBNull.Value) ? "" : r[timeCol].ToString().Trim();
                    string xs = (r[xCol] == DBNull.Value) ? "" : r[xCol].ToString().Trim();
                    string ys = (r[yCol] == DBNull.Value) ? "" : r[yCol].ToString().Trim();

                    if (string.IsNullOrEmpty(vid)) continue;
                    if (!TryParseDouble(xs, out double xv)) continue;
                    if (!TryParseDouble(ys, out double yv)) continue;
                    if (!TryParseTime(t, out DateTime dt)) continue;

                    records.Add(new TaxiRecord { VehicleId = vid, Time = dt, X = xv, Y = yv });
                }
                catch { }
            }

            if (records.Count == 0)
            {
                ShowImportFormatError("未解析到有效轨迹记录（请确认列映射是否正确）。");
                return;
            }

            var byVeh = records
                .GroupBy(r => r.VehicleId)
                .ToDictionary(g => g.Key, g => g.OrderBy(r => r.Time).ToList());

            tableSpeedAzimuth = new DataTable();
            tableSpeedAzimuth.Columns.Add("车辆ID", typeof(string));
            tableSpeedAzimuth.Columns.Add("时段序号", typeof(int));
            tableSpeedAzimuth.Columns.Add("时段开始时间", typeof(DateTime));
            tableSpeedAzimuth.Columns.Add("时段结束时间", typeof(DateTime));
            tableSpeedAzimuth.Columns.Add("速度_m_s", typeof(double));
            tableSpeedAzimuth.Columns.Add("方位角_deg", typeof(double));
            tableSpeedAzimuth.Columns.Add("计算时间", typeof(DateTime));

            tableDistanceResults = new DataTable();
            tableDistanceResults.Columns.Add("车辆ID", typeof(string));
            tableDistanceResults.Columns.Add("累积距离_m", typeof(double));
            tableDistanceResults.Columns.Add("首尾直线距离_m", typeof(double));
            tableDistanceResults.Columns.Add("起始时间", typeof(DateTime));
            tableDistanceResults.Columns.Add("结束时间", typeof(DateTime));
            tableDistanceResults.Columns.Add("计算时间", typeof(DateTime));

            DateTime now = DateTime.Now;

            foreach (var kv in byVeh)
            {
                string vid = kv.Key;
                var list = kv.Value;
                if (list.Count < 2) continue;

                double cumulativeMeters = 0.0;
                var first = list.First();

                for (int i = 1; i < list.Count; i++)
                {
                    var prev = list[i - 1];
                    var cur = list[i];

                    double distanceMeters;
                    if (IsLongitudeLatitude(prev.X, prev.Y) && IsLongitudeLatitude(cur.X, cur.Y))
                        distanceMeters = HaversineKm(prev.Y, prev.X, cur.Y, cur.X) * 1000.0;
                    else
                    {
                        double dx = cur.X - prev.X;
                        double dy = cur.Y - prev.Y;
                        distanceMeters = Math.Sqrt(dx * dx + dy * dy);
                    }

                    cumulativeMeters += distanceMeters;

                    double straightMeters;
                    if (IsLongitudeLatitude(first.X, first.Y) && IsLongitudeLatitude(cur.X, cur.Y))
                        straightMeters = HaversineKm(first.Y, first.X, cur.Y, cur.X) * 1000.0;
                    else
                    {
                        double dx0 = cur.X - first.X;
                        double dy0 = cur.Y - first.Y;
                        straightMeters = Math.Sqrt(dx0 * dx0 + dy0 * dy0);
                    }

                    double azimuth = ComputeAzimuthDegrees(prev.X, prev.Y, cur.X, cur.Y);
                    double seconds = (cur.Time - prev.Time).TotalSeconds;
                    double speed_m_s = (seconds > 1e-6) ? (distanceMeters / seconds) : 0.0;

                    var rowSpeed = tableSpeedAzimuth.NewRow();
                    rowSpeed["车辆ID"] = vid;
                    rowSpeed["时段序号"] = i - 1;
                    rowSpeed["时段开始时间"] = prev.Time;
                    rowSpeed["时段结束时间"] = cur.Time;
                    rowSpeed["速度_m_s"] = Math.Round(speed_m_s, 3);
                    rowSpeed["方位角_deg"] = Math.Round(azimuth, 3);
                    rowSpeed["计算时间"] = now;
                    tableSpeedAzimuth.Rows.Add(rowSpeed);

                    var rowDist = tableDistanceResults.NewRow();
                    rowDist["车辆ID"] = vid;
                    rowDist["累积距离_m"] = Math.Round(cumulativeMeters, 3);
                    rowDist["首尾直线距离_m"] = Math.Round(straightMeters, 3);
                    rowDist["起始时间"] = prev.Time;
                    rowDist["结束时间"] = cur.Time;
                    rowDist["计算时间"] = now;
                    tableDistanceResults.Rows.Add(rowDist);
                }
            }

            dgvSpeedAzimuth.DataSource = tableSpeedAzimuth;
            dgvDistanceResults.DataSource = tableDistanceResults;

            bool hasAny = (tableSpeedAzimuth.Rows.Count > 0) || (tableDistanceResults.Rows.Count > 0);
            btnSaveLocal.Enabled = hasAny;
            btnSaveCloud.Enabled = hasAny;

            MessageBox.Show(
                $"计算完成：\r\n速度方位角 = {tableSpeedAzimuth.Rows.Count} 条\r\n距离结果 = {tableDistanceResults.Rows.Count} 条",
                "计算完成", MessageBoxButtons.OK, MessageBoxIcon.Information);

            lblStatus.Text = IsSpeedTab ? "状态：计算完成（当前表=速度方位角，可保存）"
                                        : "状态：计算完成（当前表=距离结果，可保存）";
        }

        private void BtnSaveLocal_Click()
        {
            var enc = _importEncoding ?? new UTF8Encoding(true);

            if (IsSpeedTab)
            {
                if (tableSpeedAzimuth == null || tableSpeedAzimuth.Rows.Count == 0)
                {
                    MessageBox.Show("当前“速度方位角计算”没有数据可保存。", "提示",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using (var sfd = new SaveFileDialog()
                {
                    Filter = "文本文件 (*.txt)|*.txt",
                    FileName = $"速度方位角计算_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
                })
                {
                    if (sfd.ShowDialog(this) != DialogResult.OK) return;
                    SaveSpeedTableAsDbFormatTxt(sfd.FileName, enc);
                    MessageBox.Show("已保存当前表（速度方位角）到本地。", "成功",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                if (tableDistanceResults == null || tableDistanceResults.Rows.Count == 0)
                {
                    MessageBox.Show("当前“距离计算结果”没有数据可保存。", "提示",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using (var sfd = new SaveFileDialog()
                {
                    Filter = "文本文件 (*.txt)|*.txt",
                    FileName = $"距离计算结果_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
                })
                {
                    if (sfd.ShowDialog(this) != DialogResult.OK) return;
                    SaveDistanceTableAsDbFormatTxt(sfd.FileName, enc);
                    MessageBox.Show("已保存当前表（距离计算结果）到本地。", "成功",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void SaveSpeedTableAsDbFormatTxt(string filePath, Encoding enc)
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join("\t", new[]
            {
                "速度方位ID","车辆ID","时段序号","时段开始时间","时段结束时间","速度_m_s","方位角_deg","计算时间"
            }));

            int id = 1;
            foreach (DataRow r in tableSpeedAzimuth.Rows)
            {
                sb.Append(id).Append('\t');
                sb.Append(S(GetVehicleCell(r))).Append('\t');
                sb.Append(S(r["时段序号"])).Append('\t');
                sb.Append(FormatDt(r["时段开始时间"])).Append('\t');
                sb.Append(FormatDt(r["时段结束时间"])).Append('\t');
                sb.Append(FormatNum(r["速度_m_s"])).Append('\t');
                sb.Append(FormatNum(r["方位角_deg"])).Append('\t');
                sb.Append(FormatDt(r["计算时间"]));
                sb.AppendLine();
                id++;
            }

            File.WriteAllText(filePath, sb.ToString(), enc);
        }

        private void SaveDistanceTableAsDbFormatTxt(string filePath, Encoding enc)
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join("\t", new[]
            {
                "距离结果ID","车辆ID","累积距离_m","首尾直线距离_m","起始时间","结束时间","计算时间"
            }));

            int id = 1;
            foreach (DataRow r in tableDistanceResults.Rows)
            {
                sb.Append(id).Append('\t');
                sb.Append(S(GetVehicleCell(r))).Append('\t');
                sb.Append(FormatNum(r["累积距离_m"])).Append('\t');
                sb.Append(FormatNum(r["首尾直线距离_m"])).Append('\t');
                sb.Append(FormatDt(r["起始时间"])).Append('\t');
                sb.Append(FormatDt(r["结束时间"])).Append('\t');
                sb.Append(FormatDt(r["计算时间"]));
                sb.AppendLine();
                id++;
            }

            File.WriteAllText(filePath, sb.ToString(), enc);
        }

        private object GetVehicleCell(DataRow r)
        {
            if (r.Table.Columns.Contains("车辆ID")) return r["车辆ID"];
            if (r.Table.Columns.Contains("车牌ID")) return r["车牌ID"];
            return "";
        }

        // ✅ 云端保存：打开 SaveResultDialog（传入 DSN=OKS;）
        private void BtnSaveCloud_Click()
        {
            bool hasSpeed = tableSpeedAzimuth != null && tableSpeedAzimuth.Rows.Count > 0;
            bool hasDist = tableDistanceResults != null && tableDistanceResults.Rows.Count > 0;

            if (!hasSpeed && !hasDist)
            {
                MessageBox.Show("当前没有可保存的计算结果，请先完成计算。", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DataTable dtSingle = hasSpeed ? tableSpeedAzimuth : tableDistanceResults;
            DataTable dtMulti = (hasSpeed && hasDist) ? tableDistanceResults : null;

            using (var dlg = new SaveResultDialog(dtSingle, dtMulti, _connStr))
            {
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.ShowDialog(this);
            }
        }

        private DataTable LoadTextFileToDataTable(string filePath, out Encoding usedEncoding)
        {
            byte[] bytes = File.ReadAllBytes(filePath);
            usedEncoding = DetectTextEncoding(bytes) ?? Encoding.GetEncoding("GB18030");
            string text = usedEncoding.GetString(bytes);

            var lines = text
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                .Select(l => l.TrimEnd('\r'))
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToArray();

            if (lines.Length == 0) return new DataTable();

            char[] seps = new[] { '\t', ',', ';' };

            string[] firstParts = lines[0].Split(seps, StringSplitOptions.None).Select(p => p.Trim()).ToArray();

            bool firstIsHeader = firstParts.Any(p =>
                p.Contains("车") || p.Contains("时间") ||
                p.Equals("X", StringComparison.OrdinalIgnoreCase) || p.Equals("Y", StringComparison.OrdinalIgnoreCase) ||
                p.Contains("经度") || p.Contains("纬度"));

            var dt = new DataTable();

            if (firstIsHeader)
            {
                foreach (var h in firstParts)
                {
                    string col = string.IsNullOrWhiteSpace(h) ? "列" + (dt.Columns.Count + 1) : h;
                    if (dt.Columns.Contains(col)) col = col + "_" + (dt.Columns.Count + 1);
                    dt.Columns.Add(col);
                }

                for (int i = 1; i < lines.Length; i++)
                {
                    var parts = lines[i].Split(seps, StringSplitOptions.None);
                    var row = dt.NewRow();
                    for (int c = 0; c < dt.Columns.Count && c < parts.Length; c++)
                        row[c] = parts[c].Trim();
                    dt.Rows.Add(row);
                }
            }
            else
            {
                var parsed = lines.Select(l => l.Split(seps, StringSplitOptions.None)).ToArray();
                int maxc = parsed.Max(a => a.Length);

                for (int c = 0; c < maxc; c++) dt.Columns.Add("C" + (c + 1));

                foreach (var p in parsed)
                {
                    var r = dt.NewRow();
                    for (int c = 0; c < p.Length; c++) r[c] = p[c].Trim();
                    dt.Rows.Add(r);
                }
            }

            return dt;
        }

        private Encoding DetectTextEncoding(byte[] bytes)
        {
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF) return new UTF8Encoding(true);
            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE) return Encoding.Unicode;
            if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF) return Encoding.BigEndianUnicode;
            if (LooksLikeUtf8(bytes)) return new UTF8Encoding(false);
            return null;
        }

        private bool LooksLikeUtf8(byte[] bytes)
        {
            int i = 0;
            while (i < bytes.Length)
            {
                if (bytes[i] <= 0x7F) { i++; continue; }
                if (bytes[i] >= 0xC2 && bytes[i] <= 0xDF)
                {
                    if (i + 1 >= bytes.Length) return false;
                    if (bytes[i + 1] < 0x80 || bytes[i + 1] > 0xBF) return false;
                    i += 2; continue;
                }
                if (bytes[i] >= 0xE0 && bytes[i] <= 0xEF)
                {
                    if (i + 2 >= bytes.Length) return false;
                    if (bytes[i + 1] < 0x80 || bytes[i + 1] > 0xBF) return false;
                    if (bytes[i + 2] < 0x80 || bytes[i + 2] > 0xBF) return false;
                    i += 3; continue;
                }
                if (bytes[i] >= 0xF0 && bytes[i] <= 0xF4)
                {
                    if (i + 3 >= bytes.Length) return false;
                    if (bytes[i + 1] < 0x80 || bytes[i + 1] > 0xBF) return false;
                    if (bytes[i + 2] < 0x80 || bytes[i + 2] > 0xBF) return false;
                    if (bytes[i + 3] < 0x80 || bytes[i + 3] > 0xBF) return false;
                    i += 4; continue;
                }
                return false;
            }
            return true;
        }

        private class TaxiRecord
        {
            public string VehicleId;
            public DateTime Time;
            public double X;
            public double Y;
        }

        private bool TryParseDouble(string s, out double v)
        {
            s = (s ?? "").Trim();
            if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out v)) return true;
            if (double.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out v)) return true;
            return false;
        }

        private bool TryParseTime(string raw, out DateTime dt)
        {
            raw = (raw ?? "").Trim();
            if (DateTime.TryParse(raw, out dt)) return true;

            if (raw.Length >= 14 && long.TryParse(raw.Substring(0, 14), out _))
            {
                int year = int.Parse(raw.Substring(0, 4));
                int month = int.Parse(raw.Substring(4, 2));
                int day = int.Parse(raw.Substring(6, 2));
                int hour = int.Parse(raw.Substring(8, 2));
                int minute = int.Parse(raw.Substring(10, 2));
                int second = int.Parse(raw.Substring(12, 2));
                dt = new DateTime(year, month, day, hour, minute, second);
                return true;
            }

            dt = default;
            return false;
        }

        private bool IsLongitudeLatitude(double x, double y) => x >= -180 && x <= 180 && y >= -90 && y <= 90;

        private double HaversineKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371.0;
            double dLat = ToRad(lat2 - lat1);
            double dLon = ToRad(lon2 - lon1);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRad(double deg) => deg * Math.PI / 180.0;
        private double ToDeg(double rad) => rad * 180.0 / Math.PI;

        private double ComputeAzimuthDegrees(double lon1, double lat1, double lon2, double lat2)
        {
            if (IsLongitudeLatitude(lon1, lat1) && IsLongitudeLatitude(lon2, lat2))
            {
                double y = Math.Sin(ToRad(lon2 - lon1)) * Math.Cos(ToRad(lat2));
                double x = Math.Cos(ToRad(lat1)) * Math.Sin(ToRad(lat2)) -
                           Math.Sin(ToRad(lat1)) * Math.Cos(ToRad(lat2)) * Math.Cos(ToRad(lon2 - lon1));
                double brng = Math.Atan2(y, x);
                brng = (ToDeg(brng) + 360.0) % 360.0;
                return brng;
            }
            else
            {
                double dx = lon2 - lon1;
                double dy = lat2 - lat1;
                double ang = Math.Atan2(dx, dy);
                double deg = (ToDeg(ang) + 360.0) % 360.0;
                return deg;
            }
        }

        private string S(object o) => (o == null || o == DBNull.Value) ? "" : o.ToString();

        private string FormatDt(object o)
        {
            if (o == null || o == DBNull.Value) return "";
            if (o is DateTime dt) return dt.ToString("yyyy-MM-dd HH:mm:ss");
            if (DateTime.TryParse(o.ToString(), out var d2)) return d2.ToString("yyyy-MM-dd HH:mm:ss");
            return o.ToString();
        }

        private string FormatNum(object o)
        {
            if (o == null || o == DBNull.Value) return "";
            if (o is double dd) return dd.ToString("F3", CultureInfo.InvariantCulture);
            if (double.TryParse(o.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double v)) return v.ToString("F3", CultureInfo.InvariantCulture);
            if (double.TryParse(o.ToString(), NumberStyles.Any, CultureInfo.CurrentCulture, out v)) return v.ToString("F3", CultureInfo.InvariantCulture);
            return o.ToString();
        }
    }
}
