// SaveResultDialog.cs（不改 Designer / 不改布局）
// 目标：只允许 DSN=OKS；全程只用 ODBC（不出现任何 MySqlConnector / SHOW TABLES / LIMIT / information_schema）
// 兼容：.NET Framework 4.7.2 + C# 7.3
// 修复：
// 1) 新建表：不再用业务列当主键 -> 自动添加自增 ID 主键（Access=COUNTER / MySQL=AUTO_INCREMENT，仍是 ODBC 执行）
// 2) 写入：重复键(23000 / 3022) 自动跳过继续写，避免整批失败

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace TrafficSystem
{
    public partial class SaveResultDialog : Form
    {
        private readonly DataTable _dtSingle;
        private readonly DataTable _dtMulti;
        private readonly string _rawConnStr;

        private DataTable _currentLocalPreview;
        private DataTable _cloudEditTable;
        private string _cloudCurrentTableName = "";

        // ✅ 表名/列名安全检查：只允许 字母/数字/下划线/中文
        private static readonly Regex SafeNameRegex = new Regex(@"^[A-Za-z0-9_\u4e00-\u9fa5]+$", RegexOptions.Compiled);

        private string _cacheConnStr = null;

        // QuotePrefix/QuoteSuffix（按 ODBC 元数据读取：Access 通常 []，MySQL 通常 `）
        private string _quotePrefix = "`";
        private string _quoteSuffix = "`";
        private bool _quoteLoaded = false;
        private string _quoteLoadedForConnStr = null;

        private enum SqlDialect { Unknown = 0, MySql = 1, Access = 2 }
        private SqlDialect _dialect = SqlDialect.Unknown;
        private bool _dialectLoaded = false;
        private string _dialectLoadedForConnStr = null;

        private readonly Dictionary<string, List<ColumnInfo>> _tableColsCache =
            new Dictionary<string, List<ColumnInfo>>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, HashSet<string>> _autoIncColsCache =
            new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        private class ColumnInfo
        {
            public string Name;
            public bool IsAutoIncrement;
            public int Ordinal;
        }

        public SaveResultDialog(DataTable dtSingle, DataTable dtMulti, string connStr)
        {
            InitializeComponent();

            _dtSingle = dtSingle ?? throw new ArgumentNullException(nameof(dtSingle));
            _dtMulti = dtMulti; // 可为 null
            _rawConnStr = connStr ?? "";

            InitUi();
        }

        private void InitUi()
        {
            // ✅ 强制显示为 DSN=OKS;
            try
            {
                txtConnStr.Text = NormalizeConnStrToOnlyDsnOks(string.IsNullOrWhiteSpace(_rawConnStr) ? "" : _rawConnStr);
            }
            catch
            {
                txtConnStr.Text = "DSN=OKS;";
            }

            // 本地表选择
            cboLocalTable.Items.Clear();
            cboLocalTable.Items.Add("单方式/最佳结果表");
            if (_dtMulti != null && _dtMulti.Rows.Count > 0) cboLocalTable.Items.Add("多方式明细表");

            bool singleHas = _dtSingle != null && _dtSingle.Rows.Count > 0;
            bool multiHas = _dtMulti != null && _dtMulti.Rows.Count > 0;

            int defaultIndex = 0;
            if (!singleHas && multiHas && cboLocalTable.Items.Count > 1) defaultIndex = 1;

            cboLocalTable.SelectedIndex = defaultIndex;
            BindLocalPreview(cboLocalTable.SelectedItem?.ToString() ?? "单方式/最佳结果表");

            cboLocalTable.SelectedIndexChanged -= CboLocalTable_SelectedIndexChanged;
            cboLocalTable.SelectedIndexChanged += CboLocalTable_SelectedIndexChanged;

            dgvLocalPreview.SelectionChanged -= DgvLocalPreview_SelectionChanged;
            dgvLocalPreview.SelectionChanged += DgvLocalPreview_SelectionChanged;

            // 绑定按钮（不依赖 Designer 事件，避免重复）
            if (btnExportCsv != null) { btnExportCsv.Click -= BtnExportCsv_Click; btnExportCsv.Click += BtnExportCsv_Click; }
            if (btnOpenSnapshot != null) { btnOpenSnapshot.Click -= BtnOpenSnapshot_Click; btnOpenSnapshot.Click += BtnOpenSnapshot_Click; }
            if (btnTestConn != null) { btnTestConn.Click -= BtnTestConn_Click; btnTestConn.Click += BtnTestConn_Click; }
            if (btnRefreshTables != null) { btnRefreshTables.Click -= BtnRefreshTables_Click; btnRefreshTables.Click += BtnRefreshTables_Click; }
            if (btnPreviewTable != null) { btnPreviewTable.Click -= BtnPreviewTable_Click; btnPreviewTable.Click += BtnPreviewTable_Click; }
            if (btnCreateTable != null) { btnCreateTable.Click -= BtnCreateTable_Click; btnCreateTable.Click += BtnCreateTable_Click; }
            if (btnInsertToTable != null) { btnInsertToTable.Click -= BtnInsertToTable_Click; btnInsertToTable.Click += BtnInsertToTable_Click; }
            if (btnLoadEditable != null) { btnLoadEditable.Click -= BtnLoadEditable_Click; btnLoadEditable.Click += BtnLoadEditable_Click; }
            if (btnSaveEdits != null) { btnSaveEdits.Click -= BtnSaveEdits_Click; btnSaveEdits.Click += BtnSaveEdits_Click; }

            // Grid 基本设置（不改布局）
            try
            {
                dgvCloudEdit.AllowUserToAddRows = true;
                dgvCloudEdit.ReadOnly = false;
                dgvCloudEdit.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            }
            catch { }

            try
            {
                dgvCloudPreview.ReadOnly = true;
                dgvCloudPreview.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            }
            catch { }

            ShowSnapshotFromCurrentRow();
        }

        private void CboLocalTable_SelectedIndexChanged(object sender, EventArgs e)
        {
            string name = cboLocalTable.SelectedItem?.ToString() ?? "";
            BindLocalPreview(name);
        }

        private void DgvLocalPreview_SelectionChanged(object sender, EventArgs e) => ShowSnapshotFromCurrentRow();

        private void BtnExportCsv_Click(object sender, EventArgs e) => ExportCurrentToCsv();
        private void BtnOpenSnapshot_Click(object sender, EventArgs e) => OpenSnapshotFile();
        private void BtnTestConn_Click(object sender, EventArgs e) => TestOdbcConnection();
        private void BtnRefreshTables_Click(object sender, EventArgs e) => RefreshCloudTables();
        private void BtnPreviewTable_Click(object sender, EventArgs e) => PreviewCloudTable();
        private void BtnCreateTable_Click(object sender, EventArgs e) => CreateTableFromCurrentResult();
        private void BtnInsertToTable_Click(object sender, EventArgs e) => InsertCurrentResultToSelectedTable();
        private void BtnLoadEditable_Click(object sender, EventArgs e) => LoadTableToEditableGrid();
        private void BtnSaveEdits_Click(object sender, EventArgs e) => SaveEditsToCloud();

        // ✅ 每次操作都从 UI 读取连接串，但必须是且仅是 DSN=OKS;
        private string GetConnStr()
        {
            string raw = (txtConnStr.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(raw))
                throw new Exception("连接字符串为空。请填写：DSN=OKS;");

            string cs = NormalizeConnStrToOnlyDsnOks(raw);

            if (!string.Equals(_cacheConnStr, cs, StringComparison.OrdinalIgnoreCase))
            {
                _cacheConnStr = cs;
                _tableColsCache.Clear();
                _autoIncColsCache.Clear();
                _quoteLoaded = false;
                _dialectLoaded = false;
            }

            return cs;
        }

        /// <summary>
        /// ✅ 只允许 DSN=OKS（允许大小写/允许省略末尾分号），且不允许携带任何其它键值对。
        /// </summary>
        private string NormalizeConnStrToOnlyDsnOks(string raw)
        {
            raw = (raw ?? "").Trim();
            if (!raw.EndsWith(";", StringComparison.Ordinal)) raw += ";";

            var parts = raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            string dsnVal = null;
            var extra = new List<string>();

            foreach (var p in parts)
            {
                int eq = p.IndexOf('=');
                if (eq <= 0) { extra.Add(p); continue; }

                string k = p.Substring(0, eq).Trim();
                string v = p.Substring(eq + 1).Trim();

                if (string.Equals(k, "DSN", StringComparison.OrdinalIgnoreCase)) dsnVal = v;
                else extra.Add(k);
            }

            if (dsnVal == null) throw new Exception("连接串必须是：DSN=OKS;");
            if (!string.Equals(dsnVal, "OKS", StringComparison.OrdinalIgnoreCase))
                throw new Exception("只允许 DSN=OKS;（你的 DSN 不是 OKS）。");
            if (extra.Count > 0)
                throw new Exception("只允许 DSN=OKS;，不允许附加其它字段（如 Server/Database/User 等）。");

            return "DSN=OKS;";
        }

        private void BindLocalPreview(string which)
        {
            if (which == "多方式明细表" && _dtMulti != null) _currentLocalPreview = _dtMulti;
            else _currentLocalPreview = _dtSingle;

            dgvLocalPreview.DataSource = _currentLocalPreview;
            dgvLocalPreview.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            ShowSnapshotFromCurrentRow();
        }

        private void ShowSnapshotFromCurrentRow()
        {
            try
            {
                if (picSnapshot.Image != null)
                {
                    var old = picSnapshot.Image;
                    picSnapshot.Image = null;
                    old.Dispose();
                }

                if (_currentLocalPreview == null) return;
                if (dgvLocalPreview.CurrentRow == null) return;

                var rowView = dgvLocalPreview.CurrentRow.DataBoundItem as DataRowView;
                if (rowView == null) return;

                if (!_currentLocalPreview.Columns.Contains("路线快照"))
                {
                    lblSnapshotPath.Text = "快照：无（该表无“路线快照”列）";
                    return;
                }

                string path = Convert.ToString(rowView.Row["路线快照"] ?? "");
                lblSnapshotPath.Text = string.IsNullOrWhiteSpace(path) ? "快照：无" : ("快照：" + path);

                if (File.Exists(path))
                {
                    using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var img = Image.FromStream(fs))
                    {
                        picSnapshot.Image = new Bitmap(img);
                    }
                }
            }
            catch { }
        }

        private void ExportCurrentToCsv()
        {
            if (_currentLocalPreview == null || _currentLocalPreview.Rows.Count == 0)
            {
                MessageBox.Show("当前没有可导出的数据。");
                return;
            }

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "CSV 文件 (*.csv)|*.csv";
                sfd.FileName = $"route_result_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                if (sfd.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    WriteDataTableToCsv(_currentLocalPreview, sfd.FileName);
                    MessageBox.Show("CSV 导出成功：\n" + sfd.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("CSV 导出失败：\n" + ex.Message);
                }
            }
        }

        private void WriteDataTableToCsv(DataTable dt, string filePath)
        {
            using (var sw = new StreamWriter(filePath, false, new UTF8Encoding(true)))
            {
                sw.WriteLine(string.Join(",", dt.Columns.Cast<DataColumn>().Select(c => CsvEscape(c.ColumnName))));

                foreach (DataRow r in dt.Rows)
                {
                    if (r.RowState == DataRowState.Deleted) continue;
                    var vals = dt.Columns.Cast<DataColumn>().Select(c => CsvEscape(Convert.ToString(r[c] ?? "")));
                    sw.WriteLine(string.Join(",", vals));
                }
            }
        }

        private string CsvEscape(string s)
        {
            if (s == null) return "";
            bool needQuote = s.Contains(",") || s.Contains("\"") || s.Contains("\r") || s.Contains("\n");
            s = s.Replace("\"", "\"\"");
            return needQuote ? ("\"" + s + "\"") : s;
        }

        private void OpenSnapshotFile()
        {
            try
            {
                if (dgvLocalPreview.CurrentRow == null) return;
                var rv = dgvLocalPreview.CurrentRow.DataBoundItem as DataRowView;
                if (rv == null) return;

                if (_currentLocalPreview == null || !_currentLocalPreview.Columns.Contains("路线快照"))
                {
                    MessageBox.Show("该表没有“路线快照”列。");
                    return;
                }

                string path = Convert.ToString(rv.Row["路线快照"] ?? "");
                if (File.Exists(path))
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                else
                    MessageBox.Show("快照文件不存在。");
            }
            catch (Exception ex)
            {
                MessageBox.Show("打开失败：" + ex.Message);
            }
        }

        private void TestOdbcConnection()
        {
            try
            {
                string cs = GetConnStr();
                using (var conn = new OdbcConnection(cs))
                {
                    conn.Open();
                    EnsureQuoteInfo(conn);
                    EnsureDialect(conn);

                    string msg = "连接成功。\n" +
                                 "Driver: " + (conn.Driver ?? "") + "\n" +
                                 "ServerVersion: " + (conn.ServerVersion ?? "") + "\n" +
                                 "Dialect: " + _dialect;
                    MessageBox.Show(msg);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("连接失败：\n" + ex.Message);
            }
        }

        // ✅ 刷新表列表：只用 ODBC GetSchema("Tables")
        private void RefreshCloudTables()
        {
            try
            {
                string cs = GetConnStr();
                cboCloudTables.Items.Clear();

                using (var conn = new OdbcConnection(cs))
                {
                    conn.Open();
                    EnsureQuoteInfo(conn);
                    EnsureDialect(conn);

                    var dt = conn.GetSchema("Tables");
                    var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (DataRow r in dt.Rows)
                    {
                        string type = GetStr(r, "TABLE_TYPE");
                        string name = GetStr(r, "TABLE_NAME");
                        if (string.IsNullOrWhiteSpace(name)) continue;
                        if (name.StartsWith("MSys", StringComparison.OrdinalIgnoreCase)) continue;

                        if (!IsTableTypeOk(type)) continue;

                        if (set.Add(name))
                            cboCloudTables.Items.Add(name);
                    }
                }

                if (cboCloudTables.Items.Count > 0) cboCloudTables.SelectedIndex = 0;
                MessageBox.Show("已刷新表列表。");
            }
            catch (Exception ex)
            {
                MessageBox.Show("刷新失败：\n" + ex.Message);
            }
        }

        private bool IsTableTypeOk(string tableType)
        {
            if (string.IsNullOrWhiteSpace(tableType)) return true;
            string t = tableType.Trim().ToUpperInvariant();
            return (t == "TABLE" || t == "BASE TABLE" || t == "VIEW");
        }

        // ✅ 预览：不用 LIMIT，只用 Fill(start,max)
        private void PreviewCloudTable()
        {
            string table = cboCloudTables.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(table))
            {
                MessageBox.Show("请先选择一个云端表。");
                return;
            }

            try
            {
                string cs = GetConnStr();
                string t = SafeTable(table);

                using (var conn = new OdbcConnection(cs))
                {
                    conn.Open();
                    EnsureQuoteInfo(conn);
                    EnsureDialect(conn);

                    var dt = new DataTable();
                    using (var da = new OdbcDataAdapter("SELECT * FROM " + Q(t), conn))
                    {
                        da.Fill(0, 200, dt);
                    }
                    dgvCloudPreview.DataSource = dt;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("预览失败：\n" + ex.Message);
            }
        }

        // ✅ 云端写入取“当前有数据的结果表”
        private DataTable GetCurrentResultForCloud()
        {
            DataTable dt = _currentLocalPreview;
            if (dt == null || dt.Rows.Count == 0)
            {
                bool multiHas = _dtMulti != null && _dtMulti.Rows.Count > 0;
                bool singleHas = _dtSingle != null && _dtSingle.Rows.Count > 0;
                if (multiHas && !singleHas) dt = _dtMulti;
                else dt = _dtSingle;
            }
            return dt;
        }

        // ✅ 新建表：永远使用自增 ID 当主键，避免你现在遇到的 23000 重复键
        private void CreateTableFromCurrentResult()
        {
            string tableName = (txtNewTableName.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(tableName))
            {
                MessageBox.Show("请输入要新建的表名。");
                return;
            }

            var dt = GetCurrentResultForCloud();
            if (dt == null || dt.Columns.Count == 0)
            {
                MessageBox.Show("当前结果表为空。");
                return;
            }

            try
            {
                string cs = GetConnStr();
                using (var conn = new OdbcConnection(cs))
                {
                    conn.Open();
                    EnsureQuoteInfo(conn);
                    EnsureDialect(conn);

                    string safeTable = SafeTable(tableName);

                    if (TableExists(conn, safeTable))
                    {
                        MessageBox.Show("表已存在：" + safeTable);
                        RefreshCloudTables();
                        return;
                    }

                    string sql = BuildCreateTableSql_AutoIdPk(safeTable, dt);

                    using (var cmd = new OdbcCommand(sql, conn))
                    {
                        cmd.CommandTimeout = 120;
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("建表成功：" + tableName);
                RefreshCloudTables();
            }
            catch (Exception ex)
            {
                MessageBox.Show("建表失败：\n" + ex.Message);
            }
        }

        private string BuildCreateTableSql_AutoIdPk(string tableName, DataTable dt)
        {
            tableName = SafeTable(tableName);

            // 避免与原数据列冲突：优先用 ID；若已存在则用 RID
            string idCol = "ID";
            if (dt.Columns.Contains(idCol)) idCol = "RID";
            idCol = SafeCol(idCol);

            string pkConstraintName = "PK_" + Math.Abs((tableName ?? "").GetHashCode()).ToString(CultureInfo.InvariantCulture);

            var lines = new List<string>();

            // 自增主键列
            string idType;
            if (_dialect == SqlDialect.Access)
            {
                // Access/ACE：COUNTER = AutoNumber
                idType = "COUNTER";
            }
            else if (_dialect == SqlDialect.MySql)
            {
                // MySQL：AUTO_INCREMENT（仍是 ODBC 执行）
                idType = "BIGINT AUTO_INCREMENT";
            }
            else
            {
                // 不确定：尽量用 INTEGER（不保证所有驱动都支持自增）
                idType = "INTEGER";
            }

            lines.Add("  " + Q(idCol) + " " + idType + " NOT NULL");

            // 其余列
            foreach (DataColumn c in dt.Columns)
            {
                string col = SafeCol(c.ColumnName);
                string type = MapTypeByDialect(c);
                lines.Add("  " + Q(col) + " " + type + " NULL");
            }

            string pkLine;
            if (_dialect == SqlDialect.Access)
                pkLine = "  CONSTRAINT " + Q(pkConstraintName) + " PRIMARY KEY (" + Q(idCol) + ")";
            else
                pkLine = "  PRIMARY KEY (" + Q(idCol) + ")";

            return
$@"CREATE TABLE {Q(tableName)} (
{string.Join(",\n", lines)},
{pkLine}
)";
        }

        private string MapTypeByDialect(DataColumn c)
        {
            if (_dialect == SqlDialect.Access) return MapToAccessType(c);
            if (_dialect == SqlDialect.MySql) return MapToMySqlLikeType(c);
            // Unknown：走比较保守的（多数驱动能接受）
            return MapToGenericOdbcType(c);
        }

        private string MapToMySqlLikeType(DataColumn c)
        {
            string name = c.ColumnName ?? "";
            Type t = c.DataType;

            if (name.Contains("快照") || name.Contains("图")) return "TEXT";
            if (name.EndsWith("ID") || name.Contains("用户ID") || name.Contains("车辆ID") || name.Contains("路径ID")) return "VARCHAR(64)";
            if (name.Contains("所用")) return "VARCHAR(64)";

            if (t == typeof(DateTime)) return "DATETIME";
            if (t == typeof(double) || t == typeof(float) || t == typeof(decimal)) return "DOUBLE";
            if (t == typeof(int) || t == typeof(long) || t == typeof(short)) return "BIGINT";
            if (t == typeof(bool)) return "TINYINT";

            return "VARCHAR(255)";
        }

        private string MapToAccessType(DataColumn c)
        {
            string name = c.ColumnName ?? "";
            Type t = c.DataType;

            if (name.Contains("快照") || name.Contains("图")) return "MEMO";
            if (name.EndsWith("ID") || name.Contains("用户ID") || name.Contains("车辆ID") || name.Contains("路径ID")) return "TEXT(64)";
            if (name.Contains("所用")) return "TEXT(64)";

            if (t == typeof(DateTime)) return "DATETIME";
            if (t == typeof(double) || t == typeof(float) || t == typeof(decimal)) return "DOUBLE";
            if (t == typeof(int) || t == typeof(long) || t == typeof(short)) return "LONG";
            if (t == typeof(bool)) return "YESNO";

            return "TEXT(255)";
        }

        private string MapToGenericOdbcType(DataColumn c)
        {
            // 非特定数据库：尽量用常见 ODBC 类型
            string name = c.ColumnName ?? "";
            Type t = c.DataType;

            if (name.Contains("快照") || name.Contains("图")) return "LONGVARCHAR";
            if (name.EndsWith("ID") || name.Contains("用户ID") || name.Contains("车辆ID") || name.Contains("路径ID")) return "VARCHAR(64)";

            if (t == typeof(DateTime)) return "TIMESTAMP";
            if (t == typeof(double) || t == typeof(float) || t == typeof(decimal)) return "DOUBLE";
            if (t == typeof(int) || t == typeof(long) || t == typeof(short)) return "INTEGER";
            if (t == typeof(bool)) return "BIT";

            return "VARCHAR(255)";
        }

        // ✅ 写入到所选云端表：只用 ODBC；重复键自动跳过继续写（核心修复）
        private void InsertCurrentResultToSelectedTable()
        {
            string table = cboCloudTables.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(table))
            {
                MessageBox.Show("请先选择要写入的云端表。");
                return;
            }

            var dt = GetCurrentResultForCloud();
            if (dt == null || dt.Rows.Count == 0)
            {
                MessageBox.Show("当前没有可写入的数据。");
                return;
            }

            try
            {
                string cs = GetConnStr();
                string tname = SafeTable(table);

                using (var conn = new OdbcConnection(cs))
                {
                    conn.Open();
                    EnsureQuoteInfo(conn);
                    EnsureDialect(conn);

                    var targetCols = GetTableColumns(conn, tname);
                    if (targetCols.Count == 0) throw new Exception("读取目标表列信息失败。");

                    var srcCols = dt.Columns.Cast<DataColumn>().Select(x => SafeCol(x.ColumnName)).ToList();
                    var srcColsSet = new HashSet<string>(srcCols, StringComparer.OrdinalIgnoreCase);

                    // 只插入“源列与目标列交集”，并跳过自增列（通常是 ID）
                    var insertCols = new List<ColumnInfo>();
                    foreach (var tc in targetCols)
                    {
                        if (!srcColsSet.Contains(tc.Name)) continue;
                        if (tc.IsAutoIncrement) continue;
                        insertCols.Add(tc);
                    }

                    if (insertCols.Count == 0)
                        throw new Exception("列不匹配：目标表与当前结果列没有可插入的交集（或目标表只有自增列）。");

                    string colList = string.Join(",", insertCols.Select(x => Q(x.Name)));
                    string qs = string.Join(",", insertCols.Select(x => "?"));
                    string ins = "INSERT INTO " + Q(tname) + " (" + colList + ") VALUES (" + qs + ")";

                    int ok = 0, dupSkip = 0;

                    using (var tx = conn.BeginTransaction())
                    using (var cmd = new OdbcCommand(ins, conn, tx))
                    {
                        cmd.CommandTimeout = 120;

                        cmd.Parameters.Clear();
                        for (int i = 0; i < insertCols.Count; i++)
                            cmd.Parameters.Add(new OdbcParameter { Value = DBNull.Value });

                        cmd.Prepare();

                        for (int rIdx = 0; rIdx < dt.Rows.Count; rIdx++)
                        {
                            var r = dt.Rows[rIdx];
                            if (r.RowState == DataRowState.Deleted) continue;

                            for (int i = 0; i < insertCols.Count; i++)
                            {
                                string col = insertCols[i].Name;
                                int srcIndex = FindColumnIndexIgnoreCase(dt, col);
                                object v = (srcIndex < 0) ? DBNull.Value : r[srcIndex];
                                cmd.Parameters[i].Value = (v == null || v == DBNull.Value) ? DBNull.Value : v;
                            }

                            try
                            {
                                cmd.ExecuteNonQuery();
                                ok++;
                            }
                            catch (OdbcException ex)
                            {
                                if (IsDuplicateKey(ex))
                                {
                                    dupSkip++;
                                    continue; // ✅ 重复键跳过
                                }
                                throw; // 其它错误直接抛出
                            }

                            if ((ok + dupSkip) % 500 == 0) Application.DoEvents();
                        }

                        tx.Commit();
                    }

                    MessageBox.Show($"写入完成：成功 {ok} 行，重复跳过 {dupSkip} 行（目标表：{tname}）。");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("写入失败：\n" + ex.Message);
            }
        }

        // ====== 编辑区加载/保存（全 ODBC）======

        private void LoadTableToEditableGrid()
        {
            string table = cboCloudTables.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(table))
            {
                MessageBox.Show("请先选择要加载的云端表。");
                return;
            }

            try
            {
                string cs = GetConnStr();
                string t = SafeTable(table);

                using (var conn = new OdbcConnection(cs))
                {
                    conn.Open();
                    EnsureQuoteInfo(conn);
                    EnsureDialect(conn);

                    var dt = new DataTable();
                    using (var da = new OdbcDataAdapter("SELECT * FROM " + Q(t), conn))
                    {
                        da.Fill(0, 2000, dt);
                    }

                    _cloudEditTable = dt;
                    _cloudCurrentTableName = table;
                    dgvCloudEdit.DataSource = _cloudEditTable;
                }

                MessageBox.Show("已加载到可编辑区（最多2000行）。修改后点击“保存现有改动”。");
            }
            catch (Exception ex)
            {
                MessageBox.Show("加载失败：\n" + ex.Message);
            }
        }

        private void SaveEditsToCloud()
        {
            if (_cloudEditTable == null || _cloudEditTable.Columns.Count == 0 || string.IsNullOrWhiteSpace(_cloudCurrentTableName))
            {
                MessageBox.Show("请先加载一个云端表到可编辑区。");
                return;
            }

            try
            {
                string cs = GetConnStr();
                using (var conn = new OdbcConnection(cs))
                {
                    conn.Open();
                    EnsureQuoteInfo(conn);
                    EnsureDialect(conn);

                    var autoCols = GetAutoIncrementColumns(conn, SafeTable(_cloudCurrentTableName));

                    using (var tx = conn.BeginTransaction())
                    {
                        try
                        {
                            int ins = 0, upd = 0, del = 0;

                            foreach (DataRow r in _cloudEditTable.Rows)
                            {
                                if (r.RowState == DataRowState.Unchanged) continue;

                                if (r.RowState == DataRowState.Added)
                                {
                                    ExecuteInsertDynamic(conn, tx, _cloudCurrentTableName, _cloudEditTable, r, autoCols);
                                    ins++;
                                }
                                else if (r.RowState == DataRowState.Modified)
                                {
                                    ExecuteUpdateDynamic(conn, tx, _cloudCurrentTableName, _cloudEditTable, r);
                                    upd++;
                                }
                                else if (r.RowState == DataRowState.Deleted)
                                {
                                    ExecuteDeleteDynamic(conn, tx, _cloudCurrentTableName, _cloudEditTable, r);
                                    del++;
                                }
                            }

                            tx.Commit();
                            _cloudEditTable.AcceptChanges();
                            MessageBox.Show($"保存成功：新增{ins}，修改{upd}，删除{del}");
                        }
                        catch
                        {
                            tx.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存失败：\n" + ex.Message);
            }
        }

        private void ExecuteInsertDynamic(OdbcConnection conn, OdbcTransaction tx, string table, DataTable schema, DataRow r, HashSet<string> autoIncCols)
        {
            string tname = SafeTable(table);

            var cols = new List<string>();
            for (int i = 0; i < schema.Columns.Count; i++)
            {
                string cn = SafeCol(schema.Columns[i].ColumnName);
                if (autoIncCols != null && autoIncCols.Contains(cn)) continue; // 自增列不插
                cols.Add(cn);
            }

            if (cols.Count == 0) throw new Exception("新增行无可插入的列（可能只有自增列）。");

            string colList = string.Join(",", cols.Select(c => Q(c)));
            string qs = string.Join(",", cols.Select(c => "?"));
            string sql = "INSERT INTO " + Q(tname) + " (" + colList + ") VALUES (" + qs + ")";

            using (var cmd = new OdbcCommand(sql, conn, tx))
            {
                cmd.CommandTimeout = 120;
                for (int i = 0; i < cols.Count; i++)
                {
                    object v = r[cols[i]];
                    cmd.Parameters.Add(new OdbcParameter { Value = (v == null || v == DBNull.Value) ? DBNull.Value : v });
                }
                cmd.ExecuteNonQuery();
            }
        }

        private void ExecuteUpdateDynamic(OdbcConnection conn, OdbcTransaction tx, string table, DataTable schema, DataRow r)
        {
            string tname = SafeTable(table);

            // 默认用第一列当主键（因为我们新建表一定是 ID 在第一列）
            string pk = schema.Columns[0].ColumnName;
            pk = SafeCol(pk);

            var setCols = schema.Columns.Cast<DataColumn>()
                .Select(c => SafeCol(c.ColumnName))
                .Where(cn => !string.Equals(cn, pk, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (setCols.Length == 0) throw new Exception("没有可更新的列（只有主键列）。");

            string setPart = string.Join(",", setCols.Select(c => Q(c) + "=?"));
            string sql = "UPDATE " + Q(tname) + " SET " + setPart + " WHERE " + Q(pk) + "=?";

            using (var cmd = new OdbcCommand(sql, conn, tx))
            {
                cmd.CommandTimeout = 120;

                for (int i = 0; i < setCols.Length; i++)
                {
                    object v = r[setCols[i]];
                    cmd.Parameters.Add(new OdbcParameter { Value = (v == null || v == DBNull.Value) ? DBNull.Value : v });
                }

                object id = r[pk, DataRowVersion.Original];
                cmd.Parameters.Add(new OdbcParameter { Value = (id == null || id == DBNull.Value) ? DBNull.Value : id });

                cmd.ExecuteNonQuery();
            }
        }

        private void ExecuteDeleteDynamic(OdbcConnection conn, OdbcTransaction tx, string table, DataTable schema, DataRow r)
        {
            string tname = SafeTable(table);

            string pk = schema.Columns[0].ColumnName;
            pk = SafeCol(pk);

            object id = r[pk, DataRowVersion.Original];
            string sql = "DELETE FROM " + Q(tname) + " WHERE " + Q(pk) + "=?";

            using (var cmd = new OdbcCommand(sql, conn, tx))
            {
                cmd.CommandTimeout = 120;
                cmd.Parameters.Add(new OdbcParameter { Value = (id == null || id == DBNull.Value) ? DBNull.Value : id });
                cmd.ExecuteNonQuery();
            }
        }

        // ====== ODBC 元数据（全通用）======

        private bool TableExists(OdbcConnection conn, string tableName)
        {
            try
            {
                var dt = conn.GetSchema("Tables", new string[] { null, null, tableName, null });
                if (dt == null) return false;
                foreach (DataRow r in dt.Rows)
                {
                    string name = GetStr(r, "TABLE_NAME");
                    if (string.Equals(name, tableName, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                return false;
            }
            catch
            {
                try
                {
                    var dt2 = conn.GetSchema("Tables");
                    foreach (DataRow r in dt2.Rows)
                    {
                        string name = GetStr(r, "TABLE_NAME");
                        if (string.Equals(name, tableName, StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                }
                catch { }
                return false;
            }
        }

        private List<ColumnInfo> GetTableColumns(OdbcConnection conn, string tableName)
        {
            tableName = SafeTable(tableName);

            if (_tableColsCache.TryGetValue(tableName, out var cached) && cached != null && cached.Count > 0)
                return cached;

            var list = new List<ColumnInfo>();

            DataTable dtCols = null;
            try { dtCols = conn.GetSchema("Columns", new string[] { null, null, tableName, null }); }
            catch { dtCols = conn.GetSchema("Columns"); }

            if (dtCols == null) throw new Exception("无法读取 Columns 元数据（GetSchema(\"Columns\") 失败）。");

            bool hasIsAuto = dtCols.Columns.Contains("IS_AUTOINCREMENT");
            bool hasOrdinal = dtCols.Columns.Contains("ORDINAL_POSITION");

            foreach (DataRow r in dtCols.Rows)
            {
                string tname = GetStr(r, "TABLE_NAME");
                if (!string.IsNullOrWhiteSpace(tname) &&
                    !string.Equals(tname, tableName, StringComparison.OrdinalIgnoreCase))
                    continue;

                string name = GetStr(r, "COLUMN_NAME");
                if (string.IsNullOrWhiteSpace(name)) continue;

                bool isAuto = false;
                if (hasIsAuto)
                {
                    string v = Convert.ToString(r["IS_AUTOINCREMENT"] ?? "");
                    isAuto = IsYes(v);
                }

                int ord = 0;
                if (hasOrdinal)
                {
                    int.TryParse(Convert.ToString(r["ORDINAL_POSITION"] ?? "0"), out ord);
                }

                list.Add(new ColumnInfo
                {
                    Name = SafeCol(name),
                    IsAutoIncrement = isAuto,
                    Ordinal = ord
                });
            }

            if (list.Any(x => x.Ordinal > 0))
                list = list.OrderBy(x => x.Ordinal).ToList();
            else
                list = list.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();

            _tableColsCache[tableName] = list;
            _autoIncColsCache[tableName] = new HashSet<string>(
                list.Where(x => x.IsAutoIncrement).Select(x => x.Name),
                StringComparer.OrdinalIgnoreCase);

            return list;
        }

        private HashSet<string> GetAutoIncrementColumns(OdbcConnection conn, string tableName)
        {
            tableName = SafeTable(tableName);

            if (_autoIncColsCache.TryGetValue(tableName, out var hs) && hs != null)
                return hs;

            GetTableColumns(conn, tableName);

            if (_autoIncColsCache.TryGetValue(tableName, out hs) && hs != null)
                return hs;

            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        private bool IsYes(string v)
        {
            v = (v ?? "").Trim();
            if (v.Length == 0) return false;
            return v.Equals("YES", StringComparison.OrdinalIgnoreCase)
                || v.Equals("Y", StringComparison.OrdinalIgnoreCase)
                || v.Equals("TRUE", StringComparison.OrdinalIgnoreCase)
                || v.Equals("1", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetStr(DataRow r, string col)
        {
            try
            {
                if (r.Table == null || !r.Table.Columns.Contains(col)) return "";
                return Convert.ToString(r[col] ?? "");
            }
            catch { return ""; }
        }

        private void EnsureQuoteInfo(OdbcConnection conn)
        {
            if (_quoteLoaded && string.Equals(_quoteLoadedForConnStr, _cacheConnStr, StringComparison.OrdinalIgnoreCase))
                return;

            _quotePrefix = "`";
            _quoteSuffix = "`";

            try
            {
                var dsInfo = conn.GetSchema("DataSourceInformation");
                if (dsInfo != null && dsInfo.Rows.Count > 0)
                {
                    string p = TryGetSchemaValue(dsInfo, "IdentifierQuotePrefix");
                    string s = TryGetSchemaValue(dsInfo, "IdentifierQuoteSuffix");

                    if (string.IsNullOrWhiteSpace(p) && string.IsNullOrWhiteSpace(s))
                    {
                        string q = TryGetSchemaValue(dsInfo, "QuoteChar");
                        if (!string.IsNullOrWhiteSpace(q)) { p = q; s = q; }
                    }

                    if (!string.IsNullOrWhiteSpace(p) && !string.IsNullOrWhiteSpace(s))
                    {
                        _quotePrefix = p;
                        _quoteSuffix = s;
                    }
                }
            }
            catch { }

            _quoteLoaded = true;
            _quoteLoadedForConnStr = _cacheConnStr;
        }

        private void EnsureDialect(OdbcConnection conn)
        {
            if (_dialectLoaded && string.Equals(_dialectLoadedForConnStr, _cacheConnStr, StringComparison.OrdinalIgnoreCase))
                return;

            _dialect = SqlDialect.Unknown;

            try
            {
                string driver = (conn.Driver ?? "").ToLowerInvariant();
                string sv = (conn.ServerVersion ?? "").ToLowerInvariant();

                if (driver.Contains("access") || driver.Contains("microsoft access") || driver.Contains("ace") || driver.Contains("jet"))
                    _dialect = SqlDialect.Access;
                else if (driver.Contains("mysql"))
                    _dialect = SqlDialect.MySql;
                else
                {
                    if (sv.Contains("mysql")) _dialect = SqlDialect.MySql;
                }
            }
            catch { }

            _dialectLoaded = true;
            _dialectLoadedForConnStr = _cacheConnStr;
        }

        private string TryGetSchemaValue(DataTable dt, string colName)
        {
            try
            {
                if (dt != null && dt.Columns.Contains(colName) && dt.Rows.Count > 0)
                    return Convert.ToString(dt.Rows[0][colName] ?? "");
            }
            catch { }
            return "";
        }

        // ✅ 安全引用标识符（表名/列名）
        private string Q(string identifier)
        {
            identifier = SanitizeName(identifier);
            if (string.IsNullOrWhiteSpace(identifier))
                throw new ArgumentException("identifier 不能为空");

            string escaped = identifier;
            if (!string.IsNullOrEmpty(_quoteSuffix))
                escaped = escaped.Replace(_quoteSuffix, _quoteSuffix + _quoteSuffix);

            return _quotePrefix + escaped + _quoteSuffix;
        }

        private static string SanitizeName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            return name.Replace("`", "").Replace("[", "").Replace("]", "").Trim();
        }

        private string SafeTable(string tableName)
        {
            string n = SanitizeName(tableName);
            if (string.IsNullOrWhiteSpace(n)) throw new ArgumentException("表名不能为空");
            if (!SafeNameRegex.IsMatch(n)) throw new ArgumentException("非法表名（仅允许中文/字母/数字/下划线）： " + n);
            return n;
        }

        private string SafeCol(string colName)
        {
            string n = SanitizeName(colName);
            if (string.IsNullOrWhiteSpace(n)) throw new ArgumentException("列名不能为空");
            if (!SafeNameRegex.IsMatch(n)) throw new ArgumentException("非法列名（仅允许中文/字母/数字/下划线）： " + n);
            return n;
        }

        private static int FindColumnIndexIgnoreCase(DataTable dt, string colName)
        {
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                if (string.Equals(dt.Columns[i].ColumnName, colName, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        // ✅ 判断是否重复键（你截图这个就是）
        private bool IsDuplicateKey(OdbcException ex)
        {
            if (ex == null) return false;

            // Access 常见：NativeError 3022；SQLSTATE 23000
            try
            {
                foreach (OdbcError e in ex.Errors)
                {
                    if (string.Equals(e.SQLState, "23000", StringComparison.OrdinalIgnoreCase))
                        return true;
                    if (e.NativeError == 3022)
                        return true;
                }
            }
            catch { }

            string msg = (ex.Message ?? "").ToLowerInvariant();
            if (msg.Contains("duplicate") || msg.Contains("primary key") || msg.Contains("unique"))
                return true;

            return false;
        }

        private void SaveResultDialog_Load(object sender, EventArgs e) { }
    }
}
