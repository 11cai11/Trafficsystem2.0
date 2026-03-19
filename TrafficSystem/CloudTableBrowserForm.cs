// 文件：CloudTableBrowserForm.cs
// 说明：Access / ODBC / 系统DSN=OKS 版本（替换 MySqlConnector 版）
// 兼容：.NET Framework 4.7.2 + C# 7.3
// 不改 Designer / 不改布局
// ✅ 新增：引用全局等比例缩放算法 UiZoom（只注册，不改其它逻辑）

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TrafficSystem
{
    public partial class CloudTableBrowserForm : Form
    {
        // ✅ 仅允许系统DSN=OKS
        private const string DefaultDsnName = "OKS";
        private static readonly string ForcedConnStr = "DSN=" + DefaultDsnName + ";";

        private readonly string _connStr;

        // ✅ 如果传入这个，按钮就变为“导入当前结果到该表”，并执行写入 Access
        private readonly DataTable _dataToImport;

        private List<string> _allTables = new List<string>();

        private readonly object _previewLock = new object();
        private CancellationTokenSource _previewCts = null;
        private OdbcConnection _activeConn = null;
        private OdbcCommand _activeCmd = null;
        private int _previewSeq = 0;

        private const string Watermark = "搜索表名（可选）";

        public string SelectedTableName { get; private set; } = null;
        public DataTable SelectedDataTable { get; private set; } = null;

        // 防止每次弹窗：只警告一次
        private static bool _warnedAboutConnStr = false;

        public CloudTableBrowserForm(string connStr) : this(connStr, null) { }

        public CloudTableBrowserForm(string connStr, DataTable dataToImport)
        {
            InitializeComponent();

            // ✅ 强制只使用 DSN=OKS;
            string warn;
            _connStr = ForceOkDsn(connStr, out warn);
            if (!_warnedAboutConnStr && !string.IsNullOrEmpty(warn))
            {
                _warnedAboutConnStr = true;
                try
                {
                    MessageBox.Show(
                        warn + "\n\n已强制使用：DSN=OKS;\n请确保已创建 64位系统DSN：OKS，并指向正确的 Access 数据库文件。",
                        "连接方式已限制",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
                catch { }
            }

            _dataToImport = dataToImport;

            // --- 文案与标题 ---
            labelTitle.Text = (_dataToImport == null)
                ? "云端表浏览与预览（Access/ODBC DSN=OKS）"
                : "保存到云端（Access/ODBC DSN=OKS）- 选择表并导入";

            btnImport.Text = (_dataToImport == null) ? "导入" : "导入当前结果到该表";
            btnCancel.Text = "关闭";

            // --- 事件绑定 ---
            btnRefresh.Click += BtnRefresh_Click;
            btnImport.Click += BtnImport_Click;

            btnCancel.Click += (s, e) =>
            {
                CancelCurrentPreview();
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            listTables.SelectedIndexChanged += ListTables_SelectedIndexChanged;

            txtFilter.TextChanged += TxtFilter_TextChanged;
            txtFilter.GotFocus += TxtFilter_GotFocus;
            txtFilter.LostFocus += TxtFilter_LostFocus;

            gridPreview.ReadOnly = true;
            gridPreview.AllowUserToAddRows = false;
            gridPreview.AllowUserToDeleteRows = false;
            gridPreview.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridPreview.MultiSelect = false;
            gridPreview.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            SetWatermarkIfNeeded();

            this.Shown += async (s, e) => { await ReloadTablesAsync(); };

            // ✅ 引用“全局等比例缩放算法”（只注册基准，不改变你其它逻辑）
            // 说明：本窗体是 Designer 布局，不会 Clear/重建控件，所以直接在构造函数末尾注册即可
            UiZoom.Register(this, scaleFormClientSize: true);
        }

        // ✅ 强制 OKS DSN（并检查是否有人试图用 Driver/Dbq/Server 等方式）
        private static string ForceOkDsn(string raw, out string warning)
        {
            warning = null;

            string s = (raw ?? "").Trim();
            if (string.IsNullOrWhiteSpace(s))
                return ForcedConnStr;

            string lower = s.ToLowerInvariant();

            bool hasOther =
                lower.Contains("driver=") ||
                lower.Contains("server=") ||
                lower.Contains("database=") ||
                lower.Contains("dbq=") ||
                lower.Contains("uid=") ||
                lower.Contains("user=") ||
                lower.Contains("pwd=") ||
                lower.Contains("password=") ||
                lower.Contains("provider=");

            bool hasDsn = lower.Contains("dsn=");

            // 若不是DSN方式或混入其它方式 -> 警告
            if (!hasDsn || hasOther)
            {
                warning =
                    "当前程序仅允许使用 Access 的 ODBC 系统DSN方式连接，并且只允许 DSN=OKS。\n" +
                    "检测到传入的连接串不是纯DSN方式（或包含其它字段），将被忽略。";
                return ForcedConnStr;
            }

            // DSN 不是 OKS -> 警告，但仍强制 OKS
            string passedDsn = ExtractDsnName(s);
            if (!string.IsNullOrWhiteSpace(passedDsn) &&
                !string.Equals(passedDsn.Trim(), DefaultDsnName, StringComparison.OrdinalIgnoreCase))
            {
                warning =
                    "检测到传入的 DSN 不是 OKS（例如 DSN=" + passedDsn + "）。\n" +
                    "按要求将强制使用：DSN=OKS。";
            }

            return ForcedConnStr;
        }

        private static string ExtractDsnName(string connStr)
        {
            if (string.IsNullOrWhiteSpace(connStr)) return null;
            string[] parts = connStr.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                int idx = p.IndexOf('=');
                if (idx <= 0) continue;
                string k = p.Substring(0, idx).Trim();
                string v = p.Substring(idx + 1).Trim();
                if (string.Equals(k, "DSN", StringComparison.OrdinalIgnoreCase))
                    return v;
            }
            return null;
        }

        // ✅ Access 标识符引用：[表名] / [列名]
        private static string Q(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "[]";
            return "[" + name.Replace("]", "]]") + "]";
        }

        private async void BtnRefresh_Click(object sender, EventArgs e)
        {
            await ReloadTablesAsync();
        }

        private async Task ReloadTablesAsync()
        {
            try
            {
                lblStatus.Text = "正在读取 Access 表列表...";
                btnRefresh.Enabled = false;
                btnImport.Enabled = false;

                CancelCurrentPreview();
                SelectedDataTable = null;
                SelectedTableName = null;

                var tables = await Task.Run(() => QueryAllTablesFromAccess());
                _allTables = tables;

                ApplyFilterToList();
                lblStatus.Text = $"已加载表：{_allTables.Count} 个";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "读取表列表失败";
                MessageBox.Show("读取 Access 表列表失败：\n" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnRefresh.Enabled = true;
            }
        }

        private List<string> QueryAllTablesFromAccess()
        {
            var list = new List<string>();

            using (var conn = new OdbcConnection(_connStr))
            {
                conn.Open();

                DataTable schema = conn.GetSchema("Tables");
                foreach (DataRow r in schema.Rows)
                {
                    string type = "";
                    string name = "";

                    if (schema.Columns.Contains("TABLE_TYPE") && r["TABLE_TYPE"] != DBNull.Value)
                        type = (r["TABLE_TYPE"] as string) ?? "";

                    if (schema.Columns.Contains("TABLE_NAME") && r["TABLE_NAME"] != DBNull.Value)
                        name = (r["TABLE_NAME"] as string) ?? "";

                    if (string.IsNullOrWhiteSpace(name)) continue;

                    bool okType =
                        string.Equals(type, "TABLE", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(type, "VIEW", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(type, "BASE TABLE", StringComparison.OrdinalIgnoreCase);

                    if (!okType) continue;

                    if (name.StartsWith("MSys", StringComparison.OrdinalIgnoreCase)) continue;

                    if (!list.Contains(name, StringComparer.Ordinal))
                        list.Add(name);
                }
            }

            return list.OrderBy(x => x, StringComparer.Ordinal).ToList();
        }

        private void TxtFilter_TextChanged(object sender, EventArgs e)
        {
            if (IsWatermark()) return;
            ApplyFilterToList();
        }

        private void ApplyFilterToList()
        {
            string key = GetFilterText();
            var show = string.IsNullOrWhiteSpace(key)
                ? _allTables
                : _allTables.Where(t => (t ?? "").IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            listTables.BeginUpdate();
            listTables.Items.Clear();
            foreach (var t in show) listTables.Items.Add(t);
            listTables.EndUpdate();

            gridPreview.DataSource = null;
            btnImport.Enabled = false;
            SelectedDataTable = null;
            SelectedTableName = null;
        }

        private string GetFilterText()
        {
            string txt = txtFilter.Text?.Trim() ?? "";
            if (txt == Watermark) return "";
            return txt;
        }

        private void TxtFilter_GotFocus(object sender, EventArgs e)
        {
            if (IsWatermark())
            {
                txtFilter.Text = "";
                txtFilter.ForeColor = Color.Black;
            }
        }

        private void TxtFilter_LostFocus(object sender, EventArgs e)
        {
            SetWatermarkIfNeeded();
        }

        private void SetWatermarkIfNeeded()
        {
            if (string.IsNullOrWhiteSpace(txtFilter.Text))
            {
                txtFilter.Text = Watermark;
                txtFilter.ForeColor = Color.Gray;
            }
        }

        private bool IsWatermark()
        {
            return string.Equals(txtFilter.Text?.Trim(), Watermark, StringComparison.OrdinalIgnoreCase)
                   && txtFilter.ForeColor == Color.Gray;
        }

        private async void ListTables_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listTables.SelectedItem == null) return;
            string tableName = listTables.SelectedItem.ToString();
            await PreviewTableAsync(tableName);
        }

        private async Task PreviewTableAsync(string tableName)
        {
            CancelCurrentPreview();
            SelectedDataTable = null;
            SelectedTableName = null;

            int mySeq;
            CancellationToken token;

            lock (_previewLock)
            {
                _previewSeq++;
                mySeq = _previewSeq;
                _previewCts = new CancellationTokenSource();
                token = _previewCts.Token;
            }

            lblStatus.Text = $"正在预览：{tableName} ...";
            btnImport.Enabled = false;

            try
            {
                DataTable dt = await Task.Run(() =>
                {
                    token.ThrowIfCancellationRequested();
                    return LoadTopRows(tableName, 200, token);
                }, token);

                if (mySeq != _previewSeq || token.IsCancellationRequested) return;

                gridPreview.DataSource = dt;
                gridPreview.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

                SelectedTableName = tableName;
                SelectedDataTable = dt;

                lblStatus.Text = $"预览完成：{tableName}（显示 {dt.Rows.Count} 行）";
                btnImport.Enabled = true;
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = "已取消上一次预览";
            }
            catch (Exception ex)
            {
                if (token.IsCancellationRequested) return;

                gridPreview.DataSource = null;
                btnImport.Enabled = false;
                lblStatus.Text = $"预览失败：{tableName}";
                MessageBox.Show("预览失败：\n" + ex.Message, "预览失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                lock (_previewLock)
                {
                    _activeCmd = null;
                    _activeConn = null;
                    try { _previewCts?.Dispose(); } catch { }
                    _previewCts = null;
                }
            }
        }

        private DataTable LoadTopRows(string tableName, int topN, CancellationToken token)
        {
            var dt = new DataTable();

            var conn = new OdbcConnection(_connStr);
            lock (_previewLock) { _activeConn = conn; }

            try
            {
                conn.Open();
                token.ThrowIfCancellationRequested();

                using (var cmd = conn.CreateCommand())
                {
                    lock (_previewLock) { _activeCmd = cmd; }

                    cmd.CommandText = $"SELECT TOP {topN} * FROM {Q(tableName)}";
                    cmd.CommandTimeout = 60;

                    using (var da = new OdbcDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }

                return dt;
            }
            finally
            {
                try { conn.Close(); } catch { }
                try { conn.Dispose(); } catch { }

                lock (_previewLock)
                {
                    if (ReferenceEquals(_activeConn, conn)) _activeConn = null;
                    _activeCmd = null;
                }
            }
        }

        private void CancelCurrentPreview()
        {
            lock (_previewLock)
            {
                try { _previewCts?.Cancel(); } catch { }
                try { _activeCmd?.Cancel(); } catch { }
                try { _activeConn?.Close(); } catch { }
            }
        }

        // =========================
        // ✅ 核心：导入 / 或导入当前结果到选中表
        // =========================
        private async void BtnImport_Click(object sender, EventArgs e)
        {
            if (listTables.SelectedItem == null)
            {
                MessageBox.Show("请选择要导入的表。");
                return;
            }

            var table = listTables.SelectedItem.ToString();

            // 纯浏览模式：只是返回选择
            if (_dataToImport == null)
            {
                if (SelectedDataTable == null || SelectedTableName != table)
                {
                    MessageBox.Show("请先点击表名并等待右侧预览完成，再点击导入。");
                    return;
                }

                SelectedTableName = table;
                CancelCurrentPreview();
                this.DialogResult = DialogResult.OK;
                this.Close();
                return;
            }

            // 保存到云端（Access）模式：把 _dataToImport 逐行插入到 Access 指定表
            if (_dataToImport.Rows.Count == 0)
            {
                MessageBox.Show("当前结果为空，无需导入。");
                return;
            }

            btnImport.Enabled = false;
            btnRefresh.Enabled = false;
            txtFilter.Enabled = false;
            listTables.Enabled = false;

            try
            {
                lblStatus.Text = $"准备导入：{_dataToImport.Rows.Count} 行 -> {table}";

                await Task.Run(() =>
                {
                    ImportDataTableAppend(table, _dataToImport, progress =>
                    {
                        try
                        {
                            if (this.IsHandleCreated)
                            {
                                this.BeginInvoke(new Action(() =>
                                {
                                    lblStatus.Text = progress;
                                }));
                            }
                        }
                        catch { }
                    });
                });

                MessageBox.Show($"已成功导入 {_dataToImport.Rows.Count} 行到表：{table}", "导入成功",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                SelectedTableName = table;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("导入到 Access 失败：\n" + ex.Message, "导入失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "导入失败";
                btnImport.Enabled = true;
            }
            finally
            {
                btnRefresh.Enabled = true;
                txtFilter.Enabled = true;
                listTables.Enabled = true;
            }
        }

        private void ImportDataTableAppend(string tableName, DataTable src, Action<string> progress)
        {
            using (var conn = new OdbcConnection(_connStr))
            {
                conn.Open();

                List<string> targetCols = GetTableColumns(conn, tableName);
                if (targetCols.Count == 0)
                    throw new Exception($"目标表 {tableName} 未读取到列信息。");

                var srcCols = src.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();

                var insertCols = targetCols
                    .Where(tc => srcCols.Any(sc => string.Equals(sc, tc, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                if (insertCols.Count == 0)
                    throw new Exception($"列不匹配：目标表 {tableName} 的列与当前结果列没有交集，无法插入。");

                string colList = string.Join(",", insertCols.Select(c => Q(c)));
                string ps = string.Join(",", insertCols.Select(c => "?"));
                string sql = $"INSERT INTO {Q(tableName)} ({colList}) VALUES ({ps})";

                using (var tx = conn.BeginTransaction())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = sql;
                    cmd.CommandTimeout = 120;

                    cmd.Parameters.Clear();
                    for (int i = 0; i < insertCols.Count; i++)
                    {
                        cmd.Parameters.Add(new OdbcParameter() { Value = DBNull.Value });
                    }

                    int total = src.Rows.Count;
                    int ok = 0;

                    for (int r = 0; r < total; r++)
                    {
                        var row = src.Rows[r];
                        if (row.RowState == DataRowState.Deleted) continue;

                        for (int i = 0; i < insertCols.Count; i++)
                        {
                            string targetCol = insertCols[i];
                            string srcCol = srcCols.First(sc => string.Equals(sc, targetCol, StringComparison.OrdinalIgnoreCase));
                            object v = row[srcCol];

                            cmd.Parameters[i].Value = (v == null || v == DBNull.Value) ? DBNull.Value : v;
                        }

                        cmd.ExecuteNonQuery();
                        ok++;

                        if (ok % 200 == 0)
                            progress?.Invoke($"正在导入... {ok}/{total} 行 -> {tableName}");
                    }

                    tx.Commit();
                    progress?.Invoke($"导入完成：{ok}/{total} 行 -> {tableName}");
                }
            }
        }

        private List<string> GetTableColumns(OdbcConnection conn, string tableName)
        {
            var cols = new List<string>();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $"SELECT * FROM {Q(tableName)} WHERE 1=0";
                cmd.CommandTimeout = 60;

                using (var rd = cmd.ExecuteReader())
                {
                    for (int i = 0; i < rd.FieldCount; i++)
                        cols.Add(rd.GetName(i));
                }
            }

            return cols;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            CancelCurrentPreview();
            base.OnFormClosing(e);
        }

        private void CloudTableBrowserForm_Load(object sender, EventArgs e)
        {
            // 不做任何事（保持 Designer 事件可用）
        }
    }
}
