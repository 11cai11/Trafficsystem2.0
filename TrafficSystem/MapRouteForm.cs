// 文件：MapRouteForm.cs — 完整版（不改 Designer、不改布局）
// .NET Framework 4.7.2 + C# 7.3
// 方式二：计算后自动生成五图（内存出图），默认显示图1；按钮1~5切换
// 方式二：新增“保存图片到本地”按钮（运行时添加，不改 Designer），一次保存5张PNG
// 结果列表与数据库：不新增五图列，仍按你当前结果表字段（只有路线快照）
//
// ✅ 关键要求：只允许系统 DSN=OKS（Access/ODBC）
// ✅ 修复：不再出现 BtnXXX / btnXXX 重名/重复定义导致的编译报错
// ✅ 修复：方式一保存只保存方式一；方式二保存只保存方式二（可在保存窗体内选择保存哪些行）

using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json.Linq;

namespace TrafficSystem
{
    public partial class MapRouteForm : Form
    {
        // --- 请按实际环境修改这几个常量 ---
        private const string AMapKey = "b78567adc1cc56673e1ee6e9deafc0aa";
        private const string SecurityKey = "";

        // ✅ 只允许系统 DSN=OKS（Access/ODBC）
        private const string DefaultDsnName = "OKS";
        private static readonly string ForcedConnStr = "DSN=" + DefaultDsnName + ";";

        // ✅ 全程强制使用 DSN=OKS;
        private readonly string connStr = ForcedConnStr;

        private readonly HttpClient http = new HttpClient();

        // ========== 地图选点修复关键字段 ==========
        private bool _mapReady = false;
        private readonly Queue<string> _pendingJs = new Queue<string>();
        private bool selectingMode = false;
        // ========================================

        // 点列表
        private readonly List<PointEntry> pointEntries = new List<PointEntry>();

        // 方式二：两两路径结果（用于挑选最优 + 出图）
        private readonly List<RoutePairResult> pairResults = new List<RoutePairResult>();
        private RoutePairResult _bestPair = null;            // 最优那一对（只在成功路径里挑）
        private string _bestPathIdWay2 = "";                 // 对应 dtWay2 的“最优行”的 路径ID

        // 运行时给方式一补“出行方式”下拉（不改 Designer）
        private ComboBox comboTravelByName;
        private Label lblTravelByName;

        // 当前用户
        private string _currentUser = "未登录用户";

        // 车辆ID：方式二自动 1/2；方式一若没给定也自动给 1
        private int _autoVehicleToggle = 1;
        private string _vehicleIdWay1 = "";

        // 结果表
        private DataTable dtWay1; // 绑定 dataGridView1
        private DataTable dtWay2; // 绑定 dgvGraph（底部）

        // 最近一次快照
        private string _lastSnapshotWay1 = "";
        private string _lastSnapshotWay2 = "";

        // ========= 五图（只做可视化，不写入表、不写入数据库） =========
        private readonly Bitmap[] _plots = new Bitmap[5];
        private Button btnSavePlotsRuntime = null;
        // =========================================================

        public MapRouteForm()
        {
            InitializeComponent();

            this.MinimumSize = new Size(1000, 600);

            // comboMode：权值策略（最短时间/最短距离）
            try
            {
                comboMode.Items.Clear();
                comboMode.Items.Add("最短时间");
                comboMode.Items.Add("最短距离");
                comboMode.SelectedIndex = 0;
            }
            catch { }

            try
            {
                comboBox1.Items.Clear();
                comboBox1.Items.Add("最短时间");
                comboBox1.Items.Add("最短距离");
                comboBox1.SelectedIndex = 0;
            }
            catch { }

            // ✅ 删除“地铁”：方式二点的出行方式只保留 步行/驾车
            try
            {
                comboPointMode.Items.Clear();
                comboPointMode.Items.Add("步行");
                comboPointMode.Items.Add("驾车");
                comboPointMode.SelectedIndex = 1;
            }
            catch { }

            // DataGridView 基本设置
            try
            {
                dgvPoints.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dgvPoints.MultiSelect = true;
                dgvPoints.ReadOnly = true;
                dgvPoints.AllowUserToAddRows = false;
            }
            catch { }

            try
            {
                dgvGraph.ReadOnly = true;
                dgvGraph.AllowUserToAddRows = false;
                dgvGraph.AutoGenerateColumns = true;
                dgvGraph.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            }
            catch { }

            try
            {
                dataGridView1.ReadOnly = true;
                dataGridView1.AllowUserToAddRows = false;
                dataGridView1.AutoGenerateColumns = true;
                dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            }
            catch { }

            // ✅ 初始化结果表结构，并绑定到两个结果框
            InitResultTables();
            try { dataGridView1.DataSource = dtWay1; } catch { }
            try { dgvGraph.DataSource = dtWay2; } catch { }

            // 绑定按钮事件（统一用 Designer 同名 btnXXX_Click，避免重名/重复定义报错）
            BindHandlers();

            // 运行时补“方式一：出行方式”下拉（只保留步行/驾车）
            InitTravelComboForWay1();

            // ✅ 运行时补“保存图片到本地”按钮（不改 Designer，不动布局）
            InitSavePlotsButtonRuntime();

            // WebView2 初始化（ready 握手）
            _ = InitializeWebViewAsync();

            RefreshPointsGrid();

            // 初始禁用图按钮（等计算后自动出图再启用）
            EnablePlotButtons(false);

            // ✅【新增：B方案】缩放时不挤压重叠；缩小出现滚动条（不改 Designer）
            LockLayout_ScrollInsteadOfOverlap();
        }

        public MapRouteForm(string username) : this()
        {
            if (!string.IsNullOrWhiteSpace(username))
            {
                _currentUser = username.Trim();
                this.Text += " - 用户：" + _currentUser;
            }
        }

        // 可选：外部传入方式一车辆ID（不传则自动给 1）
        public MapRouteForm(string username, string vehicleIdWay1) : this(username)
        {
            _vehicleIdWay1 = vehicleIdWay1 ?? "";
        }

        // ✅【新增：B方案核心】允许缩放，但不让控件互相挤压重叠；缩小就滚动查看
        private void LockLayout_ScrollInsteadOfOverlap()
        {
            try
            {
                // 1) 小于设计尺寸时显示滚动条
                this.AutoScroll = true;

                // 你的 Designer：ClientSize = 1857 x 900
                this.AutoScrollMinSize = new Size(1857, 900);

                // 2) 关键：右侧大控件固定左上，避免变窄时挤压覆盖
                if (groupByName != null) groupByName.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                if (groupPoint != null) groupPoint.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                if (webView21 != null) webView21.Anchor = AnchorStyles.Top | AnchorStyles.Left;

                // 3) 左侧图片区也固定
                if (pictureBox1 != null) pictureBox1.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            }
            catch { }
        }

        private string SnapshotsDir
        {
            get
            {
                string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "snapshots");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                return dir;
            }
        }

        private void InitResultTables()
        {
            dtWay1 = CreateWay1Schema();
            dtWay2 = CreateWay2Schema();
        }

        // ===== 动态“所用时间/所用距离”列名切换 =====
        private void EnsureCostColumn(DataTable dt, bool isDistance)
        {
            if (dt == null) return;

            string want = isDistance ? "所用距离" : "所用时间";
            string other = isDistance ? "所用时间" : "所用距离";

            if (dt.Columns.Contains(want)) return;

            if (dt.Columns.Contains(other))
            {
                dt.Columns[other].ColumnName = want;
                return;
            }

            dt.Columns.Add(want, typeof(string));
        }

        private string GetCostColumnName(DataTable dt)
        {
            if (dt == null) return "所用时间";
            if (dt.Columns.Contains("所用距离")) return "所用距离";
            return "所用时间";
        }

        private string FormatDistanceKmFromKm(double km)
        {
            if (km <= 0) return "0.000 km";
            return km.ToString("F3", CultureInfo.InvariantCulture) + " km";
        }
        // ========================================

        private DataTable CreateWay1Schema()
        {
            var dt = new DataTable("方式一结果");
            dt.Columns.Add("路径ID", typeof(string));
            dt.Columns.Add("用户ID", typeof(string));
            dt.Columns.Add("车辆ID", typeof(string));
            dt.Columns.Add("起点名称", typeof(string));
            dt.Columns.Add("起点x", typeof(double));
            dt.Columns.Add("起点y", typeof(double));
            dt.Columns.Add("终点名称", typeof(string));
            dt.Columns.Add("终点x", typeof(double));
            dt.Columns.Add("终点y", typeof(double));
            dt.Columns.Add("所用时间", typeof(string)); // 会动态改名为“所用距离”
            dt.Columns.Add("路线快照", typeof(string));
            dt.Columns.Add("计算时间", typeof(DateTime));
            return dt;
        }

        // ✅ 方式二：只保留路线快照（不含五图列）
        private DataTable CreateWay2Schema()
        {
            var dt = new DataTable("方式二结果");
            dt.Columns.Add("路径ID", typeof(string));
            dt.Columns.Add("用户ID", typeof(string));
            dt.Columns.Add("车辆ID", typeof(string));
            dt.Columns.Add("起点名称", typeof(string));
            dt.Columns.Add("起点x", typeof(double));
            dt.Columns.Add("起点y", typeof(double));
            dt.Columns.Add("终点名称", typeof(string));
            dt.Columns.Add("终点x", typeof(double));
            dt.Columns.Add("终点y", typeof(double));
            dt.Columns.Add("所用时间", typeof(string)); // 会动态改名为“所用距离”
            dt.Columns.Add("路线快照", typeof(string));
            dt.Columns.Add("计算时间", typeof(DateTime));
            return dt;
        }

        private void BindHandlers()
        {
            try
            {
                // 方式一
                if (btnCalcByName != null)
                {
                    btnCalcByName.Click -= btnCalcByName_Click;
                    btnCalcByName.Click += btnCalcByName_Click;
                }

                if (btnSnapshotByName != null)
                {
                    btnSnapshotByName.Click -= btnSnapshotByName_Click;
                    btnSnapshotByName.Click += btnSnapshotByName_Click;
                }

                if (btnSaveByName != null)
                {
                    btnSaveByName.Click -= btnSaveByName_Click_1;
                    btnSaveByName.Click += btnSaveByName_Click_1;
                }

                // 方式二点管理
                if (btnEnableSelect != null)
                {
                    btnEnableSelect.Click -= btnEnableSelect_Click;
                    btnEnableSelect.Click += btnEnableSelect_Click;
                }

                if (btnAddPoint != null)
                {
                    btnAddPoint.Click -= btnAddPoint_Click;
                    btnAddPoint.Click += btnAddPoint_Click;
                }

                if (btnEditPoint != null)
                {
                    btnEditPoint.Click -= btnEditPoint_Click;
                    btnEditPoint.Click += btnEditPoint_Click;
                }

                if (btnDeletePoint != null)
                {
                    btnDeletePoint.Click -= btnDeletePoint_Click;
                    btnDeletePoint.Click += btnDeletePoint_Click;
                }

                if (btnClearPoints != null)
                {
                    btnClearPoints.Click -= btnClearPoints_Click;
                    btnClearPoints.Click += btnClearPoints_Click;
                }

                // 方式二计算/截图/保存
                if (btnCalcByPoints != null)
                {
                    btnCalcByPoints.Click -= btnCalcByPoints_Click;
                    btnCalcByPoints.Click += btnCalcByPoints_Click;
                }

                if (btnSnapshotByPoints != null)
                {
                    btnSnapshotByPoints.Click -= btnSnapshotByPoints_Click;
                    btnSnapshotByPoints.Click += btnSnapshotByPoints_Click;
                }

                if (btnSaveByPoints != null)
                {
                    btnSaveByPoints.Click -= btnSaveByPoints_Click;
                    btnSaveByPoints.Click += btnSaveByPoints_Click;
                }

                // 五图按钮：切换显示
                if (button1 != null) { button1.Click -= Button1_Click; button1.Click += Button1_Click; }
                if (button2 != null) { button2.Click -= Button2_Click; button2.Click += Button2_Click; }
                if (button3 != null) { button3.Click -= Button3_Click; button3.Click += Button3_Click; }
                if (button4 != null) { button4.Click -= Button4_Click; button4.Click += Button4_Click; }
                if (button5 != null) { button5.Click -= Button5_Click; button5.Click += Button5_Click; }
            }
            catch { }
        }

        private void InitTravelComboForWay1()
        {
            try
            {
                if (groupByName == null) return;

                lblTravelByName = new Label
                {
                    Name = "lblTravelByName",
                    Text = "出行方式：",
                    TextAlign = ContentAlignment.MiddleRight,
                    AutoSize = false,
                    Size = new Size(80, 22),
                    Location = new Point(12, 116)
                };

                comboTravelByName = new ComboBox
                {
                    Name = "comboTravelByName",
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Location = new Point(98, 116),
                    Size = new Size(140, 20)
                };

                comboTravelByName.Items.Add("步行");
                comboTravelByName.Items.Add("驾车");
                comboTravelByName.SelectedIndex = 1;

                if (groupByName.Controls.Find(lblTravelByName.Name, true).Length == 0)
                    groupByName.Controls.Add(lblTravelByName);

                if (groupByName.Controls.Find(comboTravelByName.Name, true).Length == 0)
                    groupByName.Controls.Add(comboTravelByName);
            }
            catch { }
        }

        // =========================
        // 运行时新增：保存五图按钮（不改 Designer）
        // =========================
        private void InitSavePlotsButtonRuntime()
        {
            try
            {
                if (pictureBox1 == null) return;

                Control parent = pictureBox1.Parent ?? this;
                if (btnSavePlotsRuntime != null) return;

                btnSavePlotsRuntime = new Button();
                btnSavePlotsRuntime.Name = "btnSavePlotsRuntime";
                btnSavePlotsRuntime.Text = "保存图片到本地";
                btnSavePlotsRuntime.Size = new Size(120, 30);

                // 放在 button5 右侧；否则放 pictureBox1 下方
                if (button5 != null && button5.Parent != null)
                {
                    parent = button5.Parent;
                    btnSavePlotsRuntime.Location = new Point(button5.Right + 10, button5.Top);
                }
                else
                {
                    btnSavePlotsRuntime.Location = new Point(pictureBox1.Left, pictureBox1.Bottom + 8);
                }

                btnSavePlotsRuntime.Click += BtnSavePlotsRuntime_Click;
                parent.Controls.Add(btnSavePlotsRuntime);
            }
            catch { }
        }

        private void EnablePlotButtons(bool enabled)
        {
            try
            {
                if (button1 != null) button1.Enabled = enabled;
                if (button2 != null) button2.Enabled = enabled;
                if (button3 != null) button3.Enabled = enabled;
                if (button4 != null) button4.Enabled = enabled;
                if (button5 != null) button5.Enabled = enabled;
                if (btnSavePlotsRuntime != null) btnSavePlotsRuntime.Enabled = enabled;
            }
            catch { }
        }

        // =========================
        // 方式一：按地名
        // =========================
        private async void btnCalcByName_Click(object sender, EventArgs e)
        {
            if (btnCalcByName != null) { btnCalcByName.Enabled = false; btnCalcByName.Text = "计算中..."; }

            try
            {
                string sName = (txtStart.Text ?? "").Trim();
                string eName = (txtEnd.Text ?? "").Trim();
                if (string.IsNullOrWhiteSpace(sName) || string.IsNullOrWhiteSpace(eName))
                {
                    MessageBox.Show("请输入起点/终点地名。");
                    return;
                }

                var s = await GeocodeAsync(sName);
                var t = await GeocodeAsync(eName);
                if (s == null || t == null)
                {
                    MessageBox.Show("地名解析失败。");
                    return;
                }

                string travelMode = comboTravelByName?.SelectedItem?.ToString() ?? "驾车";

                bool isDistance = (comboMode.SelectedItem?.ToString() == "最短距离");
                int strategy = isDistance ? 2 : 0;

                EnsureCostColumn(dtWay1, isDistance);
                string costCol = GetCostColumnName(dtWay1);

                var route = await GetRouteByModeWithRetryAsync(
                    new PointF(s.Value.X, s.Value.Y),
                    new PointF(t.Value.X, t.Value.Y),
                    travelMode,
                    strategy,
                    2);

                if (route == null || route.PathPoints.Count == 0)
                {
                    MessageBox.Show("路径规划失败。");
                    return;
                }

                // 地图绘制
                string pts = string.Join(";", route.PathPoints.Select(p =>
                    $"{p.X.ToString(CultureInfo.InvariantCulture)},{p.Y.ToString(CultureInfo.InvariantCulture)}"));

                await SafeExecJsAsync($"drawRouteFromCs('{pts}');");
                await SafeExecJsAsync($"addMarkerFromCs({s.Value.X.ToString(CultureInfo.InvariantCulture)},{s.Value.Y.ToString(CultureInfo.InvariantCulture)},'{JsEncode(sName)}','起点');");
                await SafeExecJsAsync($"addMarkerFromCs({t.Value.X.ToString(CultureInfo.InvariantCulture)},{t.Value.Y.ToString(CultureInfo.InvariantCulture)},'{JsEncode(eName)}','终点');");

                dtWay1.Rows.Clear();

                string pathId = Guid.NewGuid().ToString("N");
                string vehicleId = "";
                if (travelMode == "驾车")
                {
                    vehicleId = string.IsNullOrWhiteSpace(_vehicleIdWay1) ? "1" : _vehicleIdWay1;
                }

                string costValue = isDistance
                    ? FormatDistanceKmFromKm(route.DistanceMeters / 1000.0)
                    : FormatDuration(route.DurationSeconds);

                var r = dtWay1.NewRow();
                r["路径ID"] = pathId;
                r["用户ID"] = _currentUser ?? "";
                r["车辆ID"] = vehicleId;
                r["起点名称"] = sName;
                r["起点x"] = (double)s.Value.X;
                r["起点y"] = (double)s.Value.Y;
                r["终点名称"] = eName;
                r["终点x"] = (double)t.Value.X;
                r["终点y"] = (double)t.Value.Y;
                r[costCol] = costValue;
                r["路线快照"] = _lastSnapshotWay1 ?? "";
                r["计算时间"] = DateTime.Now;
                dtWay1.Rows.Add(r);

                dataGridView1.Refresh();

                MessageBox.Show("方式一计算完成！");
            }
            catch (Exception ex)
            {
                MessageBox.Show("方式一计算失败：" + ex.Message);
            }
            finally
            {
                if (btnCalcByName != null) { btnCalcByName.Enabled = true; btnCalcByName.Text = "计算最优路径（按地名）"; }
            }
        }

        private async void btnSnapshotByName_Click(object sender, EventArgs e)
        {
            var snap = await CaptureMapSnapshotAsync("way1_route");
            if (!string.IsNullOrEmpty(snap))
            {
                _lastSnapshotWay1 = snap;

                if (dtWay1.Rows.Count > 0)
                {
                    dtWay1.Rows[0]["路线快照"] = snap;
                    dtWay1.Rows[0]["计算时间"] = DateTime.Now;
                }
                dataGridView1.Refresh();

                MessageBox.Show("方式一路线快照已保存：" + snap);
            }
        }

        // ✅ 方式一：只保存方式一结果（dtWay1）
        private void btnSaveByName_Click_1(object sender, EventArgs e)
        {
            try
            {
                var dlg = new SaveResultDialog(dtWay1, null, connStr);
                dlg.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show("打开保存窗体失败：" + ex.Message);
            }
        }

        // =========================
        // 方式二：点管理
        // =========================
        private void btnEnableSelect_Click(object sender, EventArgs e)
        {
            selectingMode = !selectingMode;
            if (btnEnableSelect != null) btnEnableSelect.Text = selectingMode ? "取消选点" : "启用选点";

            if (selectingMode)
                MessageBox.Show("已进入选点模式：请在地图上点击，将自动回填经纬度到右侧输入框。");

            TryExecJsQueued($"enableMapClick({(selectingMode ? "true" : "false")});");
        }

        private void btnAddPoint_Click(object sender, EventArgs e)
        {
            string name = (txtPointName.Text ?? "").Trim();
            if (string.IsNullOrEmpty(name)) { MessageBox.Show("请填写点名"); return; }

            if (!double.TryParse((txtPointLng.Text ?? "").Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double lng) ||
                !double.TryParse((txtPointLat.Text ?? "").Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double lat))
            {
                MessageBox.Show("经纬度格式错误（建议地图点选回填）。");
                return;
            }

            var pe = new PointEntry
            {
                Name = name,
                Mode = comboPointMode.SelectedItem?.ToString() ?? "驾车",
                TimeMinutes = (double)nudPointTime.Value,
                Lng = lng,
                Lat = lat
            };

            pointEntries.Add(pe);
            RefreshPointsGrid();

            TryExecJsQueued($"addMarkerFromCs({lng.ToString(CultureInfo.InvariantCulture)},{lat.ToString(CultureInfo.InvariantCulture)},'{JsEncode(name)}','{pe.Mode}');");

            ClearPointInput();
        }

        private void btnEditPoint_Click(object sender, EventArgs e)
        {
            if (dgvPoints.SelectedRows.Count == 0) { MessageBox.Show("请选择要编辑的点"); return; }
            int idx = dgvPoints.SelectedRows[0].Index;
            if (idx < 0 || idx >= pointEntries.Count) return;

            var pe = pointEntries[idx];
            txtPointName.Text = pe.Name;
            comboPointMode.SelectedItem = pe.Mode;
            nudPointTime.Value = (decimal)pe.TimeMinutes;
            txtPointLng.Text = pe.Lng.ToString(CultureInfo.InvariantCulture);
            txtPointLat.Text = pe.Lat.ToString(CultureInfo.InvariantCulture);

            pointEntries.RemoveAt(idx);
            RefreshPointsGrid();
            SyncAllMarkersToJs();
        }

        private void btnDeletePoint_Click(object sender, EventArgs e)
        {
            if (dgvPoints.SelectedRows.Count == 0) { MessageBox.Show("请选择要删除的点"); return; }
            var idxs = dgvPoints.SelectedRows.Cast<DataGridViewRow>().Select(r => r.Index).OrderByDescending(i => i).ToList();
            foreach (var i in idxs)
            {
                if (i >= 0 && i < pointEntries.Count)
                    pointEntries.RemoveAt(i);
            }
            RefreshPointsGrid();
            SyncAllMarkersToJs();
        }

        private void btnClearPoints_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确认清空所有点？", "确认", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            pointEntries.Clear();
            RefreshPointsGrid();
            TryExecJsQueued("clearMarkers();");
        }

        private void RefreshPointsGrid()
        {
            try
            {
                dgvPoints.Rows.Clear();
                dgvPoints.Columns.Clear();
                dgvPoints.Columns.Add("Name", "点名");
                dgvPoints.Columns.Add("Mode", "方式");
                dgvPoints.Columns.Add("Time", "时间(min)");
                dgvPoints.Columns.Add("Lng", "经度");
                dgvPoints.Columns.Add("Lat", "纬度");

                foreach (var p in pointEntries)
                {
                    dgvPoints.Rows.Add(
                        p.Name,
                        p.Mode,
                        p.TimeMinutes.ToString("F1", CultureInfo.InvariantCulture),
                        p.Lng.ToString(CultureInfo.InvariantCulture),
                        p.Lat.ToString(CultureInfo.InvariantCulture)
                    );
                }
                dgvPoints.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            }
            catch { }
        }

        private void ClearPointInput()
        {
            try
            {
                txtPointName.Clear();
                txtPointLng.Clear();
                txtPointLat.Clear();
                try { nudPointTime.Value = 0; } catch { }
                try { comboPointMode.SelectedIndex = 1; } catch { }
            }
            catch { }
        }

        private void SyncAllMarkersToJs()
        {
            TryExecJsQueued("clearMarkers();");
            foreach (var p in pointEntries)
            {
                TryExecJsQueued($"addMarkerFromCs({p.Lng.ToString(CultureInfo.InvariantCulture)},{p.Lat.ToString(CultureInfo.InvariantCulture)},'{JsEncode(p.Name)}','{p.Mode}');");
            }
        }

        // =========================
        // 方式二：计算最优路径（计算后自动生成 5 图）
        // =========================
        private async void btnCalcByPoints_Click(object sender, EventArgs e)
        {
            if (btnCalcByPoints != null) { btnCalcByPoints.Enabled = false; btnCalcByPoints.Text = "计算中..."; }

            try
            {
                var sels = dgvPoints.SelectedRows.Cast<DataGridViewRow>().Select(r => r.Index).OrderBy(i => i).ToList();
                if (sels.Count < 2)
                {
                    MessageBox.Show("请在点表中至少选中2个点（多选）。");
                    return;
                }

                // 清空旧图与旧结果
                DisposePlots();
                EnablePlotButtons(false);

                dtWay2.Rows.Clear();
                dgvGraph.Refresh();
                pairResults.Clear();
                _bestPair = null;
                _bestPathIdWay2 = "";
                _lastSnapshotWay2 = "";

                bool isDistance = (comboBox1.SelectedItem?.ToString() == "最短距离");
                int strategy = isDistance ? 2 : 0;

                EnsureCostColumn(dtWay2, isDistance);
                string costCol = GetCostColumnName(dtWay2);

                // 固定生成所有组合行：n*(n-1)/2
                var pendingPairs = new List<(PointEntry A, PointEntry B)>();
                for (int a = 0; a < sels.Count; a++)
                {
                    for (int b = a + 1; b < sels.Count; b++)
                    {
                        pendingPairs.Add((pointEntries[sels[a]], pointEntries[sels[b]]));
                    }
                }

                int total = pendingPairs.Count;
                int done = 0;

                double bestWeight = double.MaxValue;

                foreach (var pair in pendingPairs)
                {
                    done++;
                    if (btnCalcByPoints != null) btnCalcByPoints.Text = $"计算中...({done}/{total})";

                    var start = pair.A;
                    var end = pair.B;

                    string travelMode = start.Mode ?? "驾车";

                    RouteResult route = await GetRouteByModeWithRetryAsync(
                        new PointF((float)start.Lng, (float)start.Lat),
                        new PointF((float)end.Lng, (float)end.Lat),
                        travelMode,
                        strategy,
                        2);

                    string pathId = Guid.NewGuid().ToString("N");
                    string vehicleId = (travelMode == "驾车") ? NextVehicleIdAuto() : "";

                    var row = dtWay2.NewRow();
                    row["路径ID"] = pathId;
                    row["用户ID"] = _currentUser ?? "";
                    row["车辆ID"] = vehicleId;
                    row["起点名称"] = start.Name ?? "";
                    row["起点x"] = start.Lng;
                    row["起点y"] = start.Lat;
                    row["终点名称"] = end.Name ?? "";
                    row["终点x"] = end.Lng;
                    row["终点y"] = end.Lat;
                    row["路线快照"] = "";
                    row["计算时间"] = DateTime.Now;

                    if (route == null || route.PathPoints == null || route.PathPoints.Count == 0)
                    {
                        row[costCol] = "计算失败";
                        dtWay2.Rows.Add(row);
                        dgvGraph.Refresh();
                        await Task.Delay(120);
                        continue;
                    }

                    double distanceKm = route.DistanceMeters / 1000.0;
                    double durationMin = route.DurationSeconds / 60.0;

                    row[costCol] = isDistance
                        ? FormatDistanceKmFromKm(distanceKm)
                        : FormatDuration(route.DurationSeconds);

                    dtWay2.Rows.Add(row);
                    dgvGraph.Refresh();

                    double weightVal = isDistance ? distanceKm : durationMin;

                    var pr = new RoutePairResult
                    {
                        StartName = start.Name,
                        EndName = end.Name,
                        StartX = start.Lng,
                        StartY = start.Lat,
                        EndX = end.Lng,
                        EndY = end.Lat,
                        TravelMode = travelMode,
                        DistanceKm = distanceKm,
                        DurationMin = durationMin,
                        WeightValue = weightVal,
                        WeightUnit = isDistance ? "km" : "min",
                        Created = DateTime.Now,
                        PathPoints = route.PathPoints.ToList(),
                        PathIdInTable = pathId
                    };
                    pairResults.Add(pr);

                    if (weightVal < bestWeight)
                    {
                        bestWeight = weightVal;
                        _bestPair = pr;
                        _bestPathIdWay2 = pathId;
                    }

                    await Task.Delay(120);
                }

                // 若有成功的最优路径：画到地图
                if (_bestPair != null && _bestPair.PathPoints != null && _bestPair.PathPoints.Count > 0)
                {
                    string ptsBest = string.Join(";", _bestPair.PathPoints.Select(p =>
                        $"{p.X.ToString(CultureInfo.InvariantCulture)},{p.Y.ToString(CultureInfo.InvariantCulture)}"));

                    await SafeExecJsAsync($"drawRouteFromCs('{ptsBest}');");
                    await SafeExecJsAsync($"addMarkerFromCs({_bestPair.StartX.ToString(CultureInfo.InvariantCulture)},{_bestPair.StartY.ToString(CultureInfo.InvariantCulture)},'{JsEncode(_bestPair.StartName)}','起点');");
                    await SafeExecJsAsync($"addMarkerFromCs({_bestPair.EndX.ToString(CultureInfo.InvariantCulture)},{_bestPair.EndY.ToString(CultureInfo.InvariantCulture)},'{JsEncode(_bestPair.EndName)}','终点');");
                }
                else
                {
                    MessageBox.Show("所有组合均计算失败（高德接口未返回有效路径）。\n但表格行数已按组合数完整生成。");
                    return;
                }

                // ✅ 关键：计算完成后自动生成五图，并默认显示图1
                GenerateAllPlots();
                EnablePlotButtons(true);
                ShowPlot(1);

                MessageBox.Show($"方式二计算完成：已按两两组合生成 {total} 条数据。\n（其中成功 {pairResults.Count} 条，失败 {total - pairResults.Count} 条）\n已自动生成 5 张图，可点击按钮1~5切换查看。");
            }
            catch (Exception ex)
            {
                MessageBox.Show("方式二计算失败：" + ex.Message);
            }
            finally
            {
                if (btnCalcByPoints != null) { btnCalcByPoints.Enabled = true; btnCalcByPoints.Text = "计算最优路径（按坐标点）"; }
            }
        }

        // =========================
        // 方式二：截图（只保存路线快照，并写回“最优路径”那一行）
        // =========================
        private async void btnSnapshotByPoints_Click(object sender, EventArgs e)
        {
            try
            {
                if (dtWay2.Rows.Count == 0)
                {
                    MessageBox.Show("请先完成方式二计算，再截图保存。");
                    return;
                }

                var snap = await CaptureMapSnapshotAsync("way2_route");
                if (string.IsNullOrEmpty(snap))
                {
                    MessageBox.Show("路线快照截图失败。");
                    return;
                }
                _lastSnapshotWay2 = snap;

                DataRow target = null;
                if (!string.IsNullOrWhiteSpace(_bestPathIdWay2))
                {
                    foreach (DataRow rr in dtWay2.Rows)
                    {
                        string id = Convert.ToString(rr["路径ID"] ?? "");
                        if (string.Equals(id, _bestPathIdWay2, StringComparison.OrdinalIgnoreCase))
                        {
                            target = rr;
                            break;
                        }
                    }
                }
                if (target == null) target = dtWay2.Rows[0];

                target["路线快照"] = _lastSnapshotWay2;
                target["计算时间"] = DateTime.Now;

                dgvGraph.Refresh();

                MessageBox.Show("方式二截图完成：路线快照已保存并写入（最优路径那一行）。");
            }
            catch (Exception ex)
            {
                MessageBox.Show("方式二截图失败：" + ex.Message);
            }
        }

        // ✅ 方式二：只保存方式二结果（dtWay2）
        private void btnSaveByPoints_Click(object sender, EventArgs e)
        {
            try
            {
                var dlg = new SaveResultDialog(dtWay2, null, connStr);
                dlg.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show("打开保存窗体失败：" + ex.Message);
            }
        }

        // =========================
        // 五图：按钮切换显示
        // =========================
        private void Button1_Click(object sender, EventArgs e) { ShowPlot(1); }
        private void Button2_Click(object sender, EventArgs e) { ShowPlot(2); }
        private void Button3_Click(object sender, EventArgs e) { ShowPlot(3); }
        private void Button4_Click(object sender, EventArgs e) { ShowPlot(4); }
        private void Button5_Click(object sender, EventArgs e) { ShowPlot(5); }

        private void ShowPlot(int plotIndex1Based)
        {
            try
            {
                if (plotIndex1Based < 1 || plotIndex1Based > 5) return;
                int idx = plotIndex1Based - 1;

                if (_plots[idx] == null)
                {
                    _plots[idx] = GeneratePlotBitmap(plotIndex1Based);
                }

                if (_plots[idx] == null) return;

                if (pictureBox1 != null)
                {
                    pictureBox1.Image = _plots[idx];
                    pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                }
            }
            catch { }
        }

        private void GenerateAllPlots()
        {
            for (int i = 1; i <= 5; i++)
            {
                int idx = i - 1;
                if (_plots[idx] == null)
                    _plots[idx] = GeneratePlotBitmap(i);
            }
        }

        private void DisposePlots()
        {
            try
            {
                try { if (pictureBox1 != null) pictureBox1.Image = null; } catch { }

                for (int i = 0; i < _plots.Length; i++)
                {
                    if (_plots[i] != null)
                    {
                        _plots[i].Dispose();
                        _plots[i] = null;
                    }
                }
            }
            catch { }
        }

        // ✅ 新增：保存五图到本地（运行时按钮）
        private void BtnSavePlotsRuntime_Click(object sender, EventArgs e)
        {
            try
            {
                if (_plots.All(p => p == null))
                {
                    MessageBox.Show("当前没有可保存的图（请先计算方式二，自动生成五图后再保存）。");
                    return;
                }

                string dir = null;
                using (var fbd = new FolderBrowserDialog())
                {
                    fbd.Description = "选择保存五张图的文件夹";
                    fbd.ShowNewFolderButton = true;
                    fbd.SelectedPath = SnapshotsDir;
                    if (fbd.ShowDialog(this) != DialogResult.OK) return;
                    dir = fbd.SelectedPath;
                }

                string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
                string[] names = new string[]
                {
                    "图1_带权重路网连通图",
                    "图2_最优路径高亮图",
                    "图3_抽象拓扑结构图",
                    "图4_多路径对比图",
                    "图5_路径热力权重图"
                };

                for (int i = 0; i < 5; i++)
                {
                    if (_plots[i] == null) continue;
                    string file = Path.Combine(dir, $"{names[i]}_{stamp}.png");
                    _plots[i].Save(file, ImageFormat.Png);
                }

                MessageBox.Show("已保存五张图到本地：" + dir);
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存图片失败：" + ex.Message);
            }
        }

        // =========================
        // 车 ID 自动 1/2
        // =========================
        private string NextVehicleIdAuto()
        {
            int id = _autoVehicleToggle;
            _autoVehicleToggle = (_autoVehicleToggle == 1) ? 2 : 1;
            return id.ToString(CultureInfo.InvariantCulture);
        }

        // =========================
        // 高德：地名解析 + 路径（仅步行/驾车）
        // =========================
        private async Task<PointF?> GeocodeAsync(string name)
        {
            try
            {
                var pars = new SortedDictionary<string, string>(StringComparer.Ordinal)
                {
                    ["address"] = name,
                    ["output"] = "JSON"
                };
                string url = BuildRequestUrl("/v3/geocode/geo", pars);
                var txt = await http.GetStringAsync(url);
                var jo = JObject.Parse(txt);
                if (jo["status"]?.ToString() != "1") return null;

                var geocodes = jo["geocodes"] as JArray;
                if (geocodes == null || geocodes.Count == 0) return null;

                var loc = geocodes[0]["location"]?.ToString();
                if (string.IsNullOrEmpty(loc)) return null;

                var parts = loc.Split(',');
                if (parts.Length == 2 &&
                    double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double lng) &&
                    double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double lat))
                    return new PointF((float)lng, (float)lat);
            }
            catch { }
            return null;
        }

        private async Task<RouteResult> GetRouteByModeWithRetryAsync(PointF start, PointF end, string travelMode, int strategy, int retryCount)
        {
            for (int i = 0; i <= retryCount; i++)
            {
                var r = await GetRouteByModeAsync(start, end, travelMode, strategy);
                if (r != null && r.PathPoints != null && r.PathPoints.Count > 0) return r;
                await Task.Delay(180 + i * 150);
            }
            return null;
        }

        private async Task<RouteResult> GetRouteByModeAsync(PointF start, PointF end, string travelMode, int strategy)
        {
            travelMode = (travelMode ?? "").Trim();
            if (travelMode == "步行") return await GetWalkingRouteAsync(start, end);
            return await GetDrivingRouteAsync(start, end, strategy);
        }

        private async Task<RouteResult> GetDrivingRouteAsync(PointF start, PointF end, int strategy)
        {
            try
            {
                string origin = $"{start.X.ToString(CultureInfo.InvariantCulture)},{start.Y.ToString(CultureInfo.InvariantCulture)}";
                string destination = $"{end.X.ToString(CultureInfo.InvariantCulture)},{end.Y.ToString(CultureInfo.InvariantCulture)}";

                var pars = new SortedDictionary<string, string>(StringComparer.Ordinal)
                {
                    ["origin"] = origin,
                    ["destination"] = destination,
                    ["strategy"] = strategy.ToString(CultureInfo.InvariantCulture),
                    ["extensions"] = "base",
                    ["output"] = "JSON"
                };

                string url = BuildRequestUrl("/v3/direction/driving", pars);
                var txt = await http.GetStringAsync(url);
                var jo = JObject.Parse(txt);

                if (jo["status"]?.ToString() != "1") return null;

                var paths = jo["route"]?["paths"] as JArray;
                if (paths == null || paths.Count == 0) return null;

                var p0 = paths[0];

                double distance = 0, duration = 0;
                double.TryParse(p0["distance"]?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out distance);
                double.TryParse(p0["duration"]?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out duration);

                string polylineStr = p0["polyline"]?.ToString();
                if (string.IsNullOrEmpty(polylineStr))
                {
                    var steps = p0["steps"] as JArray;
                    var list = new List<string>();
                    if (steps != null)
                    {
                        foreach (var st in steps)
                        {
                            var pl = st["polyline"]?.ToString();
                            if (!string.IsNullOrEmpty(pl)) list.Add(pl);
                        }
                    }
                    polylineStr = string.Join(";", list);
                }

                var pts = ParsePolylineToPoints(polylineStr);
                return new RouteResult { DistanceMeters = distance, DurationSeconds = duration, PathPoints = pts };
            }
            catch { return null; }
        }

        private async Task<RouteResult> GetWalkingRouteAsync(PointF start, PointF end)
        {
            try
            {
                string origin = $"{start.X.ToString(CultureInfo.InvariantCulture)},{start.Y.ToString(CultureInfo.InvariantCulture)}";
                string destination = $"{end.X.ToString(CultureInfo.InvariantCulture)},{end.Y.ToString(CultureInfo.InvariantCulture)}";

                var pars = new SortedDictionary<string, string>(StringComparer.Ordinal)
                {
                    ["origin"] = origin,
                    ["destination"] = destination,
                    ["output"] = "JSON"
                };

                string url = BuildRequestUrl("/v3/direction/walking", pars);
                var txt = await http.GetStringAsync(url);
                var jo = JObject.Parse(txt);

                if (jo["status"]?.ToString() != "1") return null;

                var paths = jo["route"]?["paths"] as JArray;
                if (paths == null || paths.Count == 0) return null;

                var p0 = paths[0];

                double distance = 0, duration = 0;
                double.TryParse(p0["distance"]?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out distance);
                double.TryParse(p0["duration"]?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out duration);

                string polylineStr = p0["polyline"]?.ToString();
                if (string.IsNullOrEmpty(polylineStr))
                {
                    var steps = p0["steps"] as JArray;
                    var list = new List<string>();
                    if (steps != null)
                    {
                        foreach (var st in steps)
                        {
                            var pl = st["polyline"]?.ToString();
                            if (!string.IsNullOrEmpty(pl)) list.Add(pl);
                        }
                    }
                    polylineStr = string.Join(";", list);
                }

                var pts = ParsePolylineToPoints(polylineStr);
                return new RouteResult { DistanceMeters = distance, DurationSeconds = duration, PathPoints = pts };
            }
            catch { return null; }
        }

        private List<PointF> ParsePolylineToPoints(string polylineStr)
        {
            var pts = new List<PointF>();
            if (string.IsNullOrEmpty(polylineStr)) return pts;

            var segs = polylineStr.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var seg in segs)
            {
                var xy = seg.Split(',');
                if (xy.Length == 2 &&
                    double.TryParse(xy[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double lng) &&
                    double.TryParse(xy[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double lat))
                {
                    pts.Add(new PointF((float)lng, (float)lat));
                }
            }
            return pts;
        }

        private string BuildRequestUrl(string path, SortedDictionary<string, string> parameters)
        {
            var qs = string.Join("&", parameters.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));

            if (!string.IsNullOrWhiteSpace(SecurityKey))
            {
                string raw = path + "?" + qs + SecurityKey;
                using (var md5 = MD5.Create())
                {
                    var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(raw));
                    var sb = new StringBuilder();
                    foreach (var b in hash) sb.Append(b.ToString("x2"));
                    string sig = sb.ToString();
                    return $"https://restapi.amap.com{path}?{qs}&sig={sig}&key={Uri.EscapeDataString(AMapKey)}";
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(qs)) qs += "&";
                qs += "key=" + Uri.EscapeDataString(AMapKey);
                return $"https://restapi.amap.com{path}?{qs}";
            }
        }

        private string FormatDuration(double seconds)
        {
            if (seconds <= 0) return "0秒";
            int sec = (int)Math.Round(seconds);
            int h = sec / 3600;
            int m = (sec % 3600) / 60;
            int s = sec % 60;
            if (h > 0) return $"{h}小时{m}分钟{s}秒";
            if (m > 0) return $"{m}分钟{s}秒";
            return $"{s}秒";
        }

        // =========================
        // WebView2 初始化 + JS 队列
        // =========================
        private async Task InitializeWebViewAsync()
        {
            try
            {
                if (webView21 == null) return;

                _mapReady = false;

                await webView21.EnsureCoreWebView2Async();

                webView21.CoreWebView2.WebMessageReceived -= CoreWebView2_WebMessageReceived;
                webView21.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

                webView21.NavigateToString(BuildMapHtml(AMapKey, SecurityKey));
            }
            catch (Exception ex)
            {
                MessageBox.Show("WebView2 初始化失败: " + ex.Message);
            }
        }

        private void CoreWebView2_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                string json = e.WebMessageAsJson;
                if (string.IsNullOrEmpty(json)) return;

                JToken token;
                try
                {
                    token = JToken.Parse(json);
                    if (token.Type == JTokenType.String) token = JToken.Parse(token.ToString());
                }
                catch
                {
                    return;
                }

                var jo = token as JObject;
                if (jo == null) return;

                var t = jo["type"]?.ToString();

                if (t == "ready")
                {
                    _mapReady = true;
                    BeginInvoke(new Action(() =>
                    {
                        FlushPendingJs();
                        TryExecJsQueued($"enableMapClick({(selectingMode ? "true" : "false")});");
                    }));
                    return;
                }

                if (t == "mapclick")
                {
                    string lng = jo["lng"]?.ToString();
                    string lat = jo["lat"]?.ToString();
                    BeginInvoke(new Action(() =>
                    {
                        txtPointLng.Text = lng;
                        txtPointLat.Text = lat;

                        if (string.IsNullOrWhiteSpace(txtPointName.Text))
                            txtPointName.Text = "点" + (pointEntries.Count + 1);
                    }));
                }
            }
            catch { }
        }

        private string BuildMapHtml(string key, string securityKey)
        {
            string securityScript = "";
            if (!string.IsNullOrEmpty(securityKey))
            {
                securityScript = $@"<script type=""text/javascript"">
window._AMapSecurityConfig = {{
  securityJsCode: '{securityKey}'
}};
</script>";
            }

            return $@"
<!doctype html>
<html>
<head>
<meta charset='utf-8'/>
<meta http-equiv='X-UA-Compatible' content='IE=edge' />
<meta name='viewport' content='initial-scale=1.0, user-scalable=no, width=device-width'>
<title>AMap</title>
<style>html,body,#container{{width:100%;height:100%;margin:0;padding:0;}}</style>
{securityScript}
<script src='https://webapi.amap.com/maps?v=2.0&key={key}'></script>
</head>
<body>
<div id='container'></div>
<script>
(function(){{
  var map=null, markers=[], routeLayer=null;
  var clickHandler=null;
  var clickEnabled=false;

  function post(obj) {{ try{{ window.chrome.webview.postMessage(obj); }}catch(e){{}} }}

  function init(){{
    map = new AMap.Map('container',{{zoom:12, center:[116.397428,39.90923], viewMode:'2D', resizeEnable:true}});
    post({{type:'ready'}});
  }}

  window.enableMapClick = function(flag){{
    clickEnabled=!!flag;
    if(!map) return;
    if(clickHandler){{ map.off('click', clickHandler); clickHandler=null; }}
    if(clickEnabled){{
      clickHandler=function(e){{
        try{{
          post({{type:'mapclick', lng:e.lnglat.getLng().toString(), lat:e.lnglat.getLat().toString()}});
        }}catch(ex){{}}
      }};
      map.on('click', clickHandler);
    }}
  }}

  window.addMarkerFromCs = function(lng, lat, title, mode){{
    if(!map) return;
    var marker=new AMap.Marker({{
      position:[parseFloat(lng),parseFloat(lat)],
      title:title,
      label:{{content:title, offset:new AMap.Pixel(0,-20)}}
    }});
    marker.setMap(map);
    markers.push(marker);
    map.setFitView();
  }}

  window.clearMarkers = function(){{
    if(!map) return;
    if(markers.length>0){{ map.remove(markers); markers=[]; }}
    if(routeLayer){{ map.remove(routeLayer); routeLayer=null; }}
  }}

  window.drawRouteFromCs = function(polylineStr){{
    if(!map||!polylineStr) return;
    var parts=polylineStr.split(';'), path=[];
    for(var i=0;i<parts.length;i++) {{
      var xy=parts[i].split(',');
      if(xy.length==2) path.push([parseFloat(xy[0]),parseFloat(xy[1])]);
    }}
    if(routeLayer) map.remove(routeLayer);
    routeLayer=new AMap.Polyline({{
      path:path,
      strokeColor:'#3366FF',
      strokeWeight:6,
      strokeOpacity:0.9,
      showDir:true
    }});
    map.add(routeLayer);
    map.setFitView(markers.concat([routeLayer]));
  }}

  init();
}})();
</script>
</body>
</html>";
        }

        private async Task SafeExecJsAsync(string js)
        {
            try
            {
                if (string.IsNullOrEmpty(js)) return;

                if (webView21?.CoreWebView2 == null || !_mapReady)
                {
                    _pendingJs.Enqueue(js);
                    return;
                }

                await webView21.CoreWebView2.ExecuteScriptAsync(js);
            }
            catch { }
        }

        private void TryExecJsQueued(string js)
        {
            try
            {
                if (string.IsNullOrEmpty(js)) return;

                if (webView21?.CoreWebView2 == null || !_mapReady)
                {
                    _pendingJs.Enqueue(js);
                    return;
                }

                webView21.CoreWebView2.ExecuteScriptAsync(js);
            }
            catch { }
        }

        private void FlushPendingJs()
        {
            try
            {
                if (webView21?.CoreWebView2 == null) return;
                while (_pendingJs.Count > 0)
                {
                    var js = _pendingJs.Dequeue();
                    try { webView21.CoreWebView2.ExecuteScriptAsync(js); } catch { }
                }
            }
            catch { }
        }

        private async Task<string> CaptureMapSnapshotAsync(string prefix)
        {
            try
            {
                if (webView21?.CoreWebView2 == null || !_mapReady)
                {
                    MessageBox.Show("地图尚未就绪，无法截图。");
                    return null;
                }

                string file = Path.Combine(SnapshotsDir, $"{prefix}_{DateTime.Now:yyyyMMdd_HHmmss}.png");
                using (var fs = new FileStream(file, FileMode.Create, FileAccess.Write))
                {
                    await webView21.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, fs);
                }
                return file;
            }
            catch { return null; }
        }

        private string JsEncode(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\r", "\\r").Replace("\n", "\\n");
        }

        // =========================
        // 五图绘制（根据 pairResults + bestPair）
        // =========================
        private Bitmap GeneratePlotBitmap(int plotType)
        {
            int w = 900;
            int h = 650;
            try
            {
                if (pictureBox1 != null)
                {
                    if (pictureBox1.Width > 0) w = Math.Max(900, pictureBox1.Width);
                    if (pictureBox1.Height > 0) h = Math.Max(650, pictureBox1.Height);
                }
            }
            catch { }

            var bmp = new Bitmap(w, h);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.White);

                using (var titleFont = new Font("微软雅黑", 14, FontStyle.Bold))
                using (var labelFont = new Font("微软雅黑", 10, FontStyle.Regular))
                {
                    string titleText;
                    switch (plotType)
                    {
                        case 1: titleText = "带权重的路网连通图"; break;
                        case 2: titleText = "最优路径高亮图"; break;
                        case 3: titleText = "抽象拓扑结构图"; break;
                        case 4: titleText = "多路径对比图"; break;
                        case 5: titleText = "路径热力权重图"; break;
                        default: titleText = "绘图"; break;
                    }

                    g.DrawString(titleText, titleFont, Brushes.Black, new PointF(10, 8));

                    if (pairResults == null || pairResults.Count == 0)
                    {
                        g.DrawString("当前没有可用于绘图的路径数据（请先计算方式二，且至少有一条成功路径）。",
                            labelFont, Brushes.DarkRed, 10, 50);
                        return bmp;
                    }

                    var nodes = new List<string>();
                    try
                    {
                        foreach (var p in pointEntries) if (!string.IsNullOrEmpty(p.Name)) nodes.Add(p.Name);
                        nodes = nodes.Distinct().ToList();
                    }
                    catch
                    {
                        nodes = pairResults.SelectMany(p => new[] { p.StartName, p.EndName }).Distinct().ToList();
                    }

                    if (nodes.Count < 2)
                        nodes = pairResults.SelectMany(p => new[] { p.StartName, p.EndName }).Distinct().ToList();

                    int n = nodes.Count;
                    var center = new PointF(w / 2f, h / 2f + 20);
                    float radius = Math.Min(w, h) * 0.33f;

                    var nodePos = new Dictionary<string, PointF>();
                    for (int i = 0; i < n; i++)
                    {
                        double ang = 2 * Math.PI * i / Math.Max(1, n) - Math.PI / 2;
                        float x = center.X + (float)(Math.Cos(ang) * radius);
                        float y = center.Y + (float)(Math.Sin(ang) * radius);
                        nodePos[nodes[i]] = new PointF(x, y);
                    }

                    double maxW = Math.Max(1e-6, pairResults.Max(r => Math.Max(1e-6, r.WeightValue)));

                    if (plotType == 4)
                    {
                        var list = pairResults.OrderBy(p => p.WeightValue).Take(10).ToList();
                        g.DrawString("（取权值最小的前10条路径）", labelFont, Brushes.Gray, 10, 35);

                        float x0 = 80, y0 = 80;
                        float barW = Math.Max(20, (w - 160) / Math.Max(1, list.Count));
                        float maxBarH = h - 160;

                        for (int i = 0; i < list.Count; i++)
                        {
                            var p = list[i];
                            float ratio = (float)(p.WeightValue / maxW);
                            float barH = Math.Max(6f, maxBarH * ratio);
                            float bx = x0 + i * barW;
                            float by = y0 + (maxBarH - barH);

                            g.FillRectangle(Brushes.LightSteelBlue, bx, by, barW * 0.7f, barH);
                            g.DrawRectangle(Pens.SteelBlue, bx, by, barW * 0.7f, barH);

                            string tag = $"{p.StartName}->{p.EndName}";
                            var sz = g.MeasureString(tag, labelFont);
                            g.DrawString(tag, labelFont, Brushes.Black,
                                bx - (sz.Width - barW * 0.7f) / 2, y0 + maxBarH + 8);

                            string v = p.WeightValue.ToString("F2", CultureInfo.InvariantCulture) + " " + p.WeightUnit;
                            g.DrawString(v, labelFont, Brushes.DarkGreen, bx, Math.Max(10, by - 18));
                        }
                        return bmp;
                    }

                    foreach (var p in pairResults)
                    {
                        if (!nodePos.ContainsKey(p.StartName) || !nodePos.ContainsKey(p.EndName)) continue;
                        var a = nodePos[p.StartName];
                        var b = nodePos[p.EndName];

                        float widthPen = (float)Math.Max(1.2, Math.Min(8.0, p.WeightValue / maxW * 7.0));

                        Color edgeColor = Color.FromArgb(90, 60, 120, 200);
                        if (plotType == 5)
                        {
                            int heat = (int)Math.Max(0, Math.Min(255, (p.WeightValue / maxW) * 255));
                            edgeColor = Color.FromArgb(160, 255, 50 + (205 - heat), 50 + (205 - heat));
                        }

                        if (plotType == 3)
                        {
                            widthPen = 2.0f;
                            edgeColor = Color.FromArgb(80, 120, 120, 120);
                        }

                        using (var pen = new Pen(edgeColor, widthPen))
                        {
                            pen.StartCap = LineCap.Round;
                            pen.EndCap = LineCap.Round;
                            pen.CustomEndCap = new AdjustableArrowCap(5, 6, true);
                            g.DrawLine(pen, a, b);
                        }

                        if (plotType == 1 || plotType == 2 || plotType == 5)
                        {
                            var mid = new PointF((a.X + b.X) / 2, (a.Y + b.Y) / 2);
                            string wtxt = p.WeightValue.ToString("F2", CultureInfo.InvariantCulture) + " " + p.WeightUnit;
                            g.DrawString(wtxt, labelFont, Brushes.DarkGreen, mid.X + 4, mid.Y + 4);
                        }
                    }

                    foreach (var kv in nodePos)
                    {
                        var pos = kv.Value;
                        g.FillEllipse(Brushes.LightBlue, pos.X - 18, pos.Y - 18, 36, 36);
                        g.DrawEllipse(Pens.DarkBlue, pos.X - 18, pos.Y - 18, 36, 36);

                        var sz = g.MeasureString(kv.Key, labelFont);
                        g.DrawString(kv.Key, labelFont, Brushes.Black, pos.X - sz.Width / 2, pos.Y - sz.Height / 2);
                    }

                    if (plotType == 2 && _bestPair != null &&
                        nodePos.ContainsKey(_bestPair.StartName) && nodePos.ContainsKey(_bestPair.EndName))
                    {
                        var a = nodePos[_bestPair.StartName];
                        var b = nodePos[_bestPair.EndName];
                        using (var pen = new Pen(Color.Red, 7f))
                        {
                            pen.CustomEndCap = new AdjustableArrowCap(7, 9, true);
                            g.DrawLine(pen, a, b);
                        }

                        string tip = $"最优: {_bestPair.StartName} → {_bestPair.EndName}  权值={_bestPair.WeightValue:F2} {_bestPair.WeightUnit}";
                        g.DrawString(tip, labelFont, Brushes.Red, 10, h - 30);
                    }
                }
            }
            return bmp;
        }

        // 其它
        private void MapRouteForm_Load(object sender, EventArgs e) { }

        // 如果你 Designer 里确实还有这个按钮，这里也按当前有数据优先（不改变布局）
        private void btnSaveToDb_Click(object sender, EventArgs e)
        {
            try
            {
                if (dtWay2 != null && dtWay2.Rows.Count > 0)
                {
                    var dlg2 = new SaveResultDialog(dtWay2, null, connStr);
                    dlg2.ShowDialog(this);
                    return;
                }

                var dlg1 = new SaveResultDialog(dtWay1, null, connStr);
                dlg1.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show("打开保存窗体失败：" + ex.Message);
            }
        }

        // ✅ 关闭窗体释放资源（位图/HttpClient）
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try { DisposePlots(); } catch { }
            try { http.Dispose(); } catch { }

            try
            {
                if (webView21?.CoreWebView2 != null)
                {
                    webView21.CoreWebView2.WebMessageReceived -= CoreWebView2_WebMessageReceived;
                }
            }
            catch { }

            base.OnFormClosing(e);
        }
    }

    internal class PointEntry
    {
        public string Name { get; set; }
        public string Mode { get; set; }   // 步行/驾车
        public double TimeMinutes { get; set; }
        public double Lng { get; set; }
        public double Lat { get; set; }
    }

    internal class RouteResult
    {
        public double DistanceMeters { get; set; }
        public double DurationSeconds { get; set; }
        public List<PointF> PathPoints { get; set; } = new List<PointF>();
    }

    internal class RoutePairResult
    {
        public string StartName { get; set; }
        public string EndName { get; set; }

        public double StartX { get; set; }
        public double StartY { get; set; }
        public double EndX { get; set; }
        public double EndY { get; set; }

        public string TravelMode { get; set; } = ""; // 步行/驾车
        public double DistanceKm { get; set; }
        public double DurationMin { get; set; }

        public double WeightValue { get; set; }
        public string WeightUnit { get; set; } // min or km

        public DateTime Created { get; set; }
        public List<PointF> PathPoints { get; set; } = new List<PointF>();

        public string PathIdInTable { get; set; } = "";
    }
}
