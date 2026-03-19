// 文件：MainForm.cs  —— Access(Odbc/系统DSN=OKS) 版本
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.Data.Odbc;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace TrafficSystem
{
    public partial class MainForm : Form
    {
        // ✅ 仅允许系统 DSN 连接 Access（OKS）
        private const string DefaultDsnName = "OKS";
        private readonly string _connStr;

        // ✅ 对外统一用这个（只会是 DSN=OKS; 这种形式）
        private string connStr => _connStr;

        private string currentUser = "";
        private string avatarPath = "";

        private DataTable currentDataTable = null;
        private string currentTableName = "";
        private bool isAccessTable = false; // 这里逻辑上表示“数据库表”（变量名沿用不影响功能）
        private OdbcDataAdapter dataAdapter = null;
        private bool dataChanged = false;

        private Encoding lastInputFileEncoding = null;

        internal List<Tuple<string, string, double>> lastGraphEdges = null;
        private Bitmap lastGraphBitmap = null;

        private bool loginShownByAction = false;

        private string userTableName = null;
        private string colUsername = "用户名";
        private string colAvatar = "头像";
        private string colPassword = "用户密码";
        private string colPhone = "手机号";
        private string colWeChat = "WeChatOpenId";
        private string colQQ = "QQOpenId";

        private const int MAX_CLOUD_ROWS_TO_LOAD = 50000;

        public MainForm()
        {
            InitializeComponent();

            // ✅ 固化为“只允许 DSN=OKS;”
            try
            {
                _connStr = BuildDsnOnlyConnStr();
            }
            catch (Exception ex)
            {
                // 不允许其它连接方式：直接回退 OKS
                _connStr = "DSN=" + DefaultDsnName + ";";
                try
                {
                    MessageBox.Show(
                        ex.Message + "\n\n已自动回退为：DSN=OKS;\n请确保同学电脑已创建 64位系统DSN：OKS，并指向正确的 .mdb/.accdb。",
                        "DbConn 配置错误",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
                catch { }
            }

            InitUIStyle();

            // 事件（避免重复绑定）
            btnInput.Click -= btnInput_Click; btnInput.Click += btnInput_Click;
            btnShowInfo.Click -= btnShowInfo_Click; btnShowInfo.Click += btnShowInfo_Click;
            btnLogout.Click -= btnLogout_Click; btnLogout.Click += btnLogout_Click;

            btnAdd.Click -= BtnAdd_Click; btnAdd.Click += BtnAdd_Click;
            btnDelete.Click -= BtnDelete_Click; btnDelete.Click += BtnDelete_Click;
            btnUpdate.Click -= BtnUpdate_Click; btnUpdate.Click += BtnUpdate_Click;
            btnSave.Click -= BtnSave_Click; btnSave.Click += BtnSave_Click;

            btnQuery.Click -= BtnQuery_Click; btnQuery.Click += BtnQuery_Click;
            btnCalc.Click -= BtnCalc_Click; btnCalc.Click += BtnCalc_Click;
            btnAbout.Click -= BtnAbout_Click; btnAbout.Click += BtnAbout_Click;

            dataGridViewMain.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridViewMain.MultiSelect = false;

            pbGraph.SizeMode = PictureBoxSizeMode.Zoom;
            pbGraph.BackColor = Color.White;
            pbGraph.DoubleClick -= PbGraph_DoubleClick;
            pbGraph.DoubleClick += PbGraph_DoubleClick;

            ApplyListGraphLayout(showGraph: false);

            // ✅ ✅ ✅ 接入总缩放算法（不改 Designer、不改布局）
            UiZoom.Register(this, scaleFormClientSize: true);
            UiZoom.EnableCtrlWheelZoom(this);

            this.KeyPreview = true;
            this.KeyDown -= MainForm_KeyDown;
            this.KeyDown += MainForm_KeyDown;
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if ((Control.ModifierKeys & Keys.Control) != Keys.Control) return;

            if (e.KeyCode == Keys.Oemplus || e.KeyCode == Keys.Add)
            {
                UiZoom.ZoomIn(this);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.OemMinus || e.KeyCode == Keys.Subtract)
            {
                UiZoom.ZoomOut(this);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.D0 || e.KeyCode == Keys.NumPad0)
            {
                UiZoom.Reset(this);
                e.Handled = true;
            }
        }

        public MainForm(string username) : this()
        {
            currentUser = username ?? "";
            lblWelcome.Text = "欢迎：" + currentUser;
            LoadUserInfo();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(currentUser))
                {
                    lblWelcome.Text = "欢迎：" + currentUser;
                    LoadUserInfo();
                }
                else
                {
                    lblWelcome.Text = "欢迎：";
                    picAvatar.Image?.Dispose();
                    picAvatar.Image = GeneratePlaceholderAvatar("?", picAvatar.Width, picAvatar.Height);
                }
            }
            catch { }
        }

        // ✅ 只允许 DSN=OKS;，不允许 Driver/Server/Dbq 等其它连接方式
        private static string BuildDsnOnlyConnStr()
        {
            string raw = "";
            try { raw = (ConfigurationManager.AppSettings["DbConn"] ?? "").Trim(); } catch { raw = ""; }

            if (string.IsNullOrWhiteSpace(raw))
                raw = "DSN=" + DefaultDsnName + ";";

            // 只写 DSN 名（例如 OKS）也允许，自动补全
            if (!raw.Contains("="))
                raw = "DSN=" + raw.Trim() + ";";

            if (!raw.EndsWith(";", StringComparison.Ordinal))
                raw += ";";

            string lower = raw.ToLowerInvariant();

            bool hasDsn = lower.Contains("dsn=");
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

            if (!hasDsn || hasOther)
            {
                throw new ConfigurationErrorsException(
                    "当前程序只允许使用 Access 的 ODBC 系统 DSN 方式连接（OKS）。\n\n" +
                    "请在 App.config 的 appSettings 中配置：\n" +
                    "  <add key=\"DbConn\" value=\"DSN=OKS;\" />\n" +
                    "或直接不配置（默认 DSN=OKS;）。\n\n" +
                    "禁止使用 Driver/Server/Database/Dbq/Uid/Pwd/Password 等任何其它连接方式。");
            }

            // 若用户写的是 DSN=TrafficDSN; 也允许，但你要求要 OKS，这里强制改为 OKS
            // 目的：确保“只连接 OKS”
            try
            {
                // 粗暴统一：只保留 DSN=xxx; 中的 xxx，强制写 OKS
                // 这样无论配置写啥，都只会用 OKS
                return "DSN=" + DefaultDsnName + ";";
            }
            catch
            {
                return "DSN=" + DefaultDsnName + ";";
            }
        }

        private void EnsureConnStrReady()
        {
            if (string.IsNullOrWhiteSpace(connStr))
                throw new Exception("数据库连接串为空。请创建 64位系统DSN：OKS（指向正确的 .mdb/.accdb），并在 App.config 配置 DbConn=DSN=OKS;（或不配默认也会用 OKS）。");
        }

        private void InitUIStyle()
        {
            this.BackColor = Color.FromArgb(245, 247, 250);
            panelLeft.BackColor = Color.FromArgb(230, 236, 245);
            panelMain.BackColor = Color.White;
            panelTop.BackColor = Color.White;
            panelUser.BackColor = Color.FromArgb(245, 245, 246);

            labelTitle.Font = new Font("微软雅黑", 14F, FontStyle.Bold);

            picAvatar.SizeMode = PictureBoxSizeMode.Zoom;

            foreach (Control c in panelLeft.Controls)
            {
                if (c is Button btn)
                {
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderSize = 0;
                    btn.BackColor = Color.FromArgb(52, 152, 219);
                    btn.ForeColor = Color.White;
                    btn.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
                }
            }

            foreach (Control c in panelUser.Controls)
            {
                if (c is Button btn)
                {
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderSize = 0;
                    btn.BackColor = Color.White;
                    btn.ForeColor = Color.FromArgb(30, 30, 30);
                    btn.Font = new Font("微软雅黑", 9F, FontStyle.Regular);
                }
            }
        }

        private void PbGraph_DoubleClick(object sender, EventArgs e)
        {
            try
            {
                if (pbGraph.Visible) ApplyListGraphLayout(showGraph: false);
                else ApplyListGraphLayout(showGraph: lastGraphEdges != null && lastGraphEdges.Count > 0);
            }
            catch { }
        }

        private void ApplyListGraphLayout(bool showGraph)
        {
            try
            {
                if (showGraph)
                {
                    pbGraph.Visible = true;
                    int dgvHeight = Math.Max(220, panelMain.ClientSize.Height / 3);
                    dataGridViewMain.Dock = DockStyle.Top;
                    dataGridViewMain.Height = dgvHeight;
                    pbGraph.Dock = DockStyle.Fill;
                    CreateGraphBitmapToFitPictureBox();
                    UpdateGraphPictureBox();
                }
                else
                {
                    pbGraph.Visible = false;
                    pbGraph.Image?.Dispose();
                    pbGraph.Image = null;
                    dataGridViewMain.Dock = DockStyle.Fill;
                }
            }
            catch { }
        }

        private void RestoreFullListView()
        {
            try
            {
                pbGraph.Visible = false;
                pbGraph.Image?.Dispose();
                pbGraph.Image = null;
                dataGridViewMain.Dock = DockStyle.Fill;
                dataGridViewMain.BringToFront();
            }
            catch (Exception ex)
            {
                MessageBox.Show("恢复列表视图失败：" + ex.Message);
            }
        }

        private void LoadUserInfo()
        {
            lblWelcome.Text = "欢迎：" + currentUser;
            avatarPath = "";

            try
            {
                string p = GetAvatarPathFromDb(currentUser);
                if (!string.IsNullOrEmpty(p) && File.Exists(p))
                {
                    avatarPath = p;
                    try
                    {
                        var img = Image.FromFile(avatarPath);
                        picAvatar.Image?.Dispose();
                        picAvatar.Image = new Bitmap(img);
                        img.Dispose();
                    }
                    catch
                    {
                        picAvatar.Image?.Dispose();
                        picAvatar.Image = GeneratePlaceholderAvatar(currentUser, picAvatar.Width, picAvatar.Height);
                    }
                }
                else
                {
                    picAvatar.Image?.Dispose();
                    picAvatar.Image = GeneratePlaceholderAvatar(currentUser, picAvatar.Width, picAvatar.Height);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("读取用户信息失败：" + ex.Message);
                picAvatar.Image?.Dispose();
                picAvatar.Image = GeneratePlaceholderAvatar(currentUser, picAvatar.Width, picAvatar.Height);
            }
        }

        // ✅ Access 标识符引用：[表名] / [列名]
        private static string Q(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "[]";
            return "[" + name.Replace("]", "]]") + "]";
        }

        private string GetAvatarPathFromDb(string username)
        {
            try
            {
                EnsureConnStrReady();
                using (var conn = new OdbcConnection(connStr))
                {
                    SafeOpenWithRetry(conn, 3);

                    string[] tableCandidates = new[] { "用户表", "用户登录表", "用户" };
                    string[] avatarColCandidates = new[] { "用户头像", "头像", "avatar", "useravatar", "头像路径", "用户头像路径" };
                    string[] userColCandidates = new[] { "用户名", "user", "username", "UserName", "账号", "账户" };

                    foreach (var tbl in tableCandidates)
                    {
                        if (!TableExists(conn, tbl)) continue;

                        List<string> cols = GetTableColumns(conn, tbl);

                        string foundUserCol =
                            cols.FirstOrDefault(c => userColCandidates.Any(p => string.Equals(c?.Trim(), p, StringComparison.OrdinalIgnoreCase)))
                            ?? cols.FirstOrDefault();

                        string foundAvatarCol =
                            cols.FirstOrDefault(c => avatarColCandidates.Any(p => string.Equals(c?.Trim(), p, StringComparison.OrdinalIgnoreCase)));

                        if (string.IsNullOrEmpty(foundAvatarCol) || string.IsNullOrEmpty(foundUserCol)) continue;

                        using (var cmd2 = conn.CreateCommand())
                        {
                            cmd2.CommandText = $"SELECT TOP 1 {Q(foundAvatarCol)} FROM {Q(tbl)} WHERE {Q(foundUserCol)} = ?";
                            cmd2.Parameters.Clear();
                            cmd2.Parameters.Add("p1", OdbcType.VarChar).Value = username ?? "";

                            var val = cmd2.ExecuteScalar();
                            if (val != null && val != DBNull.Value)
                            {
                                string pth = (val.ToString() ?? "").Trim();
                                if (!string.IsNullOrEmpty(pth))
                                {
                                    if (!Path.IsPathRooted(pth))
                                    {
                                        var alt = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, pth);
                                        if (File.Exists(alt)) return alt;
                                    }
                                    if (File.Exists(pth)) return pth;
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        private bool UpdateAvatarInDb(string username, string destFullPath)
        {
            try
            {
                EnsureConnStrReady();
                using (var conn = new OdbcConnection(connStr))
                {
                    SafeOpenWithRetry(conn, 3);

                    string[] tableCandidates = new[] { "用户表", "用户登录表", "用户" };
                    string[] avatarColCandidates = new[] { "用户头像", "头像", "avatar", "useravatar", "头像路径", "用户头像路径" };
                    string[] userColCandidates = new[] { "用户名", "user", "username", "UserName", "账号", "账户" };

                    foreach (var tbl in tableCandidates)
                    {
                        if (!TableExists(conn, tbl)) continue;

                        List<string> cols = GetTableColumns(conn, tbl);

                        string foundUserCol =
                            cols.FirstOrDefault(c => userColCandidates.Any(p => string.Equals(c?.Trim(), p, StringComparison.OrdinalIgnoreCase)))
                            ?? cols.FirstOrDefault();

                        string foundAvatarCol =
                            cols.FirstOrDefault(c => avatarColCandidates.Any(p => string.Equals(c?.Trim(), p, StringComparison.OrdinalIgnoreCase)));

                        if (string.IsNullOrEmpty(foundAvatarCol) || string.IsNullOrEmpty(foundUserCol)) continue;

                        using (var upd = conn.CreateCommand())
                        {
                            upd.CommandText = $"UPDATE {Q(tbl)} SET {Q(foundAvatarCol)}=? WHERE {Q(foundUserCol)}=?";
                            upd.Parameters.Clear();
                            upd.Parameters.Add("p1", OdbcType.VarChar).Value = destFullPath ?? "";
                            upd.Parameters.Add("p2", OdbcType.VarChar).Value = username ?? "";
                            int n = upd.ExecuteNonQuery();
                            if (n > 0) return true;
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        private void btnShowInfo_Click(object sender, EventArgs e)
        {
            try
            {
                using (var f = new Form())
                {
                    f.Text = "个人信息";
                    f.FormBorderStyle = FormBorderStyle.FixedDialog;
                    f.StartPosition = FormStartPosition.CenterParent;
                    f.ClientSize = new Size(360, 240);
                    f.MaximizeBox = false;
                    f.MinimizeBox = false;

                    // ✅ 动态窗体也注册缩放（不改布局/Designer）
                    UiZoom.Register(f, scaleFormClientSize: true);
                    UiZoom.EnableCtrlWheelZoom(f);

                    var pic = new PictureBox()
                    {
                        Size = new Size(120, 120),
                        Location = new Point(20, 20),
                        SizeMode = PictureBoxSizeMode.Zoom,
                        BorderStyle = BorderStyle.FixedSingle
                    };

                    if (!string.IsNullOrEmpty(avatarPath) && File.Exists(avatarPath))
                    {
                        try
                        {
                            var img = Image.FromFile(avatarPath);
                            pic.Image = new Bitmap(img);
                            img.Dispose();
                        }
                        catch
                        {
                            pic.Image = GeneratePlaceholderAvatar(currentUser, pic.Width, pic.Height);
                        }
                    }
                    else
                    {
                        var p = GetAvatarPathFromDb(currentUser);
                        if (!string.IsNullOrEmpty(p) && File.Exists(p))
                        {
                            try
                            {
                                var img = Image.FromFile(p);
                                pic.Image = new Bitmap(img);
                                img.Dispose();
                            }
                            catch
                            {
                                pic.Image = GeneratePlaceholderAvatar(currentUser, pic.Width, pic.Height);
                            }
                        }
                        else
                        {
                            pic.Image = GeneratePlaceholderAvatar(currentUser, pic.Width, pic.Height);
                        }
                    }

                    var lblName = new Label()
                    {
                        Text = currentUser,
                        Location = new Point(160, 40),
                        Size = new Size(180, 30),
                        Font = new Font("微软雅黑", 12, FontStyle.Bold)
                    };
                    var lblAccount = new Label()
                    {
                        Text = $"账号：{currentUser}",
                        Location = new Point(160, 80),
                        Size = new Size(180, 22),
                        Font = new Font("微软雅黑", 9)
                    };

                    var btnChange = new Button() { Text = "修改头像", Location = new Point(160, 120), Size = new Size(160, 36) };
                    btnChange.Click += (s, ev) =>
                    {
                        using (var ofd = new OpenFileDialog() { Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp" })
                        {
                            if (ofd.ShowDialog(f) == DialogResult.OK)
                            {
                                string src = ofd.FileName;
                                try
                                {
                                    string avatarsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "avatars");
                                    if (!Directory.Exists(avatarsDir)) Directory.CreateDirectory(avatarsDir);
                                    string dest = Path.Combine(avatarsDir, $"{currentUser}_{Path.GetFileName(src)}");
                                    File.Copy(src, dest, true);

                                    bool ok = UpdateAvatarInDb(currentUser, dest);
                                    if (!ok)
                                    {
                                        MessageBox.Show("已在本地更新头像文件，但未能写回数据库（可能表/列名不一致）。请检查数据库表/列名。", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    }

                                    pic.Image?.Dispose();
                                    var tmp = Image.FromFile(dest);
                                    pic.Image = new Bitmap(tmp);
                                    tmp.Dispose();

                                    picAvatar.Image?.Dispose();
                                    var tmp2 = Image.FromFile(dest);
                                    picAvatar.Image = new Bitmap(tmp2);
                                    tmp2.Dispose();

                                    avatarPath = dest;
                                    MessageBox.Show("头像已更新。");
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show("更新头像失败：" + ex.Message);
                                }
                            }
                        }
                    };

                    f.Controls.Add(pic);
                    f.Controls.Add(lblName);
                    f.Controls.Add(lblAccount);
                    f.Controls.Add(btnChange);
                    f.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("显示个人信息失败：" + ex.Message);
            }
        }

        private Image GeneratePlaceholderAvatar(string username, int w, int h)
        {
            var size = Math.Max(Math.Min(w, h), 80);
            var bmp = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var bg = Color.FromArgb(120, 140, 180);
                g.Clear(Color.Transparent);
                using (var brush = new SolidBrush(bg))
                    g.FillEllipse(brush, 0, 0, size - 1, size - 1);
                string initials = string.IsNullOrEmpty(username) ? "?" : username.Substring(0, 1).ToUpper();
                var f = new Font("微软雅黑", Math.Max(18, size / 2), FontStyle.Bold, GraphicsUnit.Pixel);
                var textSize = g.MeasureString(initials, f);
                using (var sf = new SolidBrush(Color.White))
                    g.DrawString(initials, f, sf, (size - textSize.Width) / 2f, (size - textSize.Height) / 2f);
            }
            return bmp;
        }

        private void btnInput_Click(object sender, EventArgs e)
        {
            using (var f = new Form())
            {
                f.Text = "选择数据来源";
                f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.StartPosition = FormStartPosition.CenterParent;
                f.ClientSize = new Size(420, 150);
                f.MaximizeBox = false;
                f.MinimizeBox = false;

                // ✅ 动态窗体也注册缩放
                UiZoom.Register(f, scaleFormClientSize: true);
                UiZoom.EnableCtrlWheelZoom(f);

                var btnCloud = new Button() { Text = "从数据库导入（Access/ODBC DSN=OKS）", Size = new Size(360, 30), Location = new Point(30, 15) };
                var btnLocal = new Button() { Text = "从本地导入（TXT 文件）", Size = new Size(360, 30), Location = new Point(30, 55) };
                var btnMap = new Button() { Text = "打开地图进行选点/路径", Size = new Size(360, 30), Location = new Point(30, 95) };

                btnCloud.Click += (s, ev) => { f.Close(); DoCloudImport(); };
                btnLocal.Click += (s, ev) => { f.Close(); DoLocalImport(); };
                btnMap.Click += (s, ev) => { f.Close(); OpenMapForSelection(); };

                f.Controls.Add(btnCloud);
                f.Controls.Add(btnLocal);
                f.Controls.Add(btnMap);
                f.ShowDialog(this);
            }
        }

        private void DoCloudImport()
        {
            try
            {
                EnsureConnStrReady();

                // ✅ CloudTableBrowserForm 需要一起改成 ODBC/Access（你后续把它发我）
                using (var dlg = new CloudTableBrowserForm(connStr))
                {
                    dlg.StartPosition = FormStartPosition.CenterParent;
                    var dr = dlg.ShowDialog(this);
                    if (dr != DialogResult.OK) return;

                    var table = dlg.SelectedTableName;
                    if (string.IsNullOrEmpty(table))
                    {
                        MessageBox.Show("未选择任何表。");
                        return;
                    }

                    if (dlg.SelectedDataTable != null)
                    {
                        var dt = dlg.SelectedDataTable.Copy();
                        dt.TableName = table;

                        dt = FilterUsefulRows(dt);

                        if (dt.Rows.Count == 0)
                        {
                            MessageBox.Show($"表 [{table}] 无可用数据（全为空/无有效内容）。", "无数据", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }

                        ShowCloudDataTableToMain(dt, table);
                        return;
                    }

                    LoadAccessTableToGrid(table, MAX_CLOUD_ROWS_TO_LOAD);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("从数据库导入失败：\r\n" + GetDbErrorDetail(ex));
            }
        }

        private void ShowCloudDataTableToMain(DataTable dt, string tableName)
        {
            currentDataTable = dt;
            currentTableName = tableName;
            isAccessTable = true;
            dataAdapter = null;
            dataChanged = false;

            dataGridViewMain.DataSource = null;
            dataGridViewMain.DataSource = currentDataTable;
            dataGridViewMain.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            RestoreFullListView();
        }

        private DataTable FilterUsefulRows(DataTable dt)
        {
            if (dt == null) return dt;
            if (dt.Rows.Count == 0) return dt;

            var outDt = dt.Clone();
            foreach (DataRow r in dt.Rows)
            {
                bool useful = false;
                foreach (DataColumn c in dt.Columns)
                {
                    var v = r[c];
                    if (v == null || v == DBNull.Value) continue;
                    string s = v.ToString();
                    if (!string.IsNullOrWhiteSpace(s))
                    {
                        useful = true;
                        break;
                    }
                }
                if (useful) outDt.ImportRow(r);
            }
            return outDt;
        }

        private static string RemoveControlChars(string s)
        {
            if (s == null) return "";
            var arr = s.Where(c => !char.IsControl(c)).ToArray();
            return new string(arr);
        }

        private void LoadAccessTableToGrid(string tableName, int maxRows)
        {
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                EnsureConnStrReady();

                var candidates = new List<string>();
                candidates.Add(tableName);
                candidates.Add(RemoveControlChars(tableName));
                candidates.Add(RemoveControlChars(tableName).Trim());
                candidates.Add((tableName ?? "").Trim());
                candidates = candidates.Where(x => !string.IsNullOrEmpty(x)).Distinct(StringComparer.Ordinal).ToList();

                var triesTop = new List<int>();
                if (maxRows > 0) triesTop.Add(maxRows);
                triesTop.Add(20000);
                triesTop.Add(5000);
                triesTop.Add(2000);
                triesTop.Add(200);
                triesTop = triesTop.Where(x => x > 0).Distinct().ToList();

                Exception lastEx = null;
                DataTable dtOK = null;
                string nameOK = null;

                foreach (var cand in candidates)
                {
                    foreach (var topN in triesTop)
                    {
                        try
                        {
                            DataTable dt = new DataTable();
                            string sql = $"SELECT TOP {topN} * FROM {Q(cand)}";

                            using (var conn = new OdbcConnection(connStr))
                            {
                                SafeOpenWithRetry(conn, 3);
                                using (var cmd = conn.CreateCommand())
                                {
                                    cmd.CommandText = sql;
                                    cmd.CommandTimeout = 120;
                                    using (var da = new OdbcDataAdapter(cmd))
                                    {
                                        da.SelectCommand.CommandTimeout = 120;
                                        da.Fill(dt);
                                    }
                                }
                            }

                            dtOK = FilterUsefulRows(dt);
                            nameOK = cand;
                            goto OK;
                        }
                        catch (Exception ex)
                        {
                            lastEx = ex;
                        }
                    }
                }

            OK:
                if (dtOK == null)
                {
                    MessageBox.Show($"读取表 [{tableName}] 失败：\n\n{GetDbErrorDetail(lastEx)}", "读取失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (dtOK.Rows.Count == 0)
                {
                    MessageBox.Show($"表 [{nameOK ?? tableName}] 无可用数据（全为空/无有效内容）。", "无数据", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                ShowCloudDataTableToMain(dtOK, nameOK ?? tableName);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        private void SafeOpenWithRetry(OdbcConnection conn, int tries)
        {
            Exception last = null;
            for (int i = 0; i < Math.Max(1, tries); i++)
            {
                try
                {
                    conn.Open();
                    return;
                }
                catch (Exception ex)
                {
                    last = ex;
                    System.Threading.Thread.Sleep(120);
                }
            }
            if (last != null) throw last;
            conn.Open();
        }

        private void DoLocalImport()
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "文本文件 (*.txt;*.csv)|*.txt;*.csv|所有文件|*.*";
                ofd.Multiselect = false;
                if (ofd.ShowDialog(this) != DialogResult.OK) return;
                string path = ofd.FileName;
                try
                {
                    Encoding detectedEnc;
                    var lines = ReadAllLinesWithAutoEncoding(path, out detectedEnc).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
                    lastInputFileEncoding = detectedEnc;

                    if (lines.Length == 0) { MessageBox.Show("文件为空。"); return; }

                    var sepPattern = "[,，\\t;]+";
                    var first = lines[0];
                    var headParts = Regex.Split(first, sepPattern).Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();

                    var dt = new DataTable();
                    bool firstIsHeader = headParts.Length > 0 && headParts.All(p => p.Any(c => char.IsLetterOrDigit(c) || c > 127));
                    if (firstIsHeader)
                    {
                        foreach (var h in headParts) dt.Columns.Add(h.Trim());
                        for (int i = 1; i < lines.Length; i++)
                        {
                            var parts = Regex.Split(lines[i], sepPattern);
                            var row = dt.NewRow();
                            for (int c = 0; c < dt.Columns.Count && c < parts.Length; c++) row[c] = parts[c].Trim();
                            dt.Rows.Add(row);
                        }
                    }
                    else
                    {
                        var parsed = lines.Select(l => Regex.Split(l, sepPattern)).ToArray();
                        int maxc = parsed.Max(arr => arr.Length);
                        for (int c = 0; c < maxc; c++) dt.Columns.Add("C" + (c + 1));
                        foreach (var p in parsed)
                        {
                            var r = dt.NewRow();
                            for (int c = 0; c < p.Length; c++) r[c] = p[c].Trim();
                            dt.Rows.Add(r);
                        }
                    }

                    currentDataTable = dt;
                    currentTableName = Path.GetFileName(path);
                    isAccessTable = false;
                    dataAdapter = null;
                    dataChanged = false;

                    dataGridViewMain.DataSource = dt;
                    dataGridViewMain.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

                    ApplyListGraphLayout(showGraph: lastGraphEdges != null && lastGraphEdges.Count > 0);

                    MessageBox.Show($"导入完成（检测编码：{(detectedEnc != null ? detectedEnc.WebName : "未知")}）。");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("本地导入失败：" + ex.Message);
                }
            }
        }

        private string[] ReadAllLinesWithAutoEncoding(string path, out Encoding detected)
        {
            detected = null;
            byte[] data = File.ReadAllBytes(path);

            Encoding[] tryEncodings;
            try
            {
                tryEncodings = new Encoding[]
                {
                    new UTF8Encoding(true),
                    new UTF8Encoding(false),
                    Encoding.GetEncoding("GB18030"),
                    Encoding.GetEncoding(936),
                    Encoding.Unicode,
                    Encoding.BigEndianUnicode,
                    Encoding.ASCII,
                    Encoding.Default
                };
            }
            catch
            {
                tryEncodings = new Encoding[] { Encoding.UTF8, Encoding.Default };
            }

            foreach (var enc in tryEncodings)
            {
                try
                {
                    string txt = enc.GetString(data);
                    if (txt.Contains('\uFFFD')) continue;
                    var firstLine = txt.Replace("\r\n", "\n").Split('\n')[0];
                    if (string.IsNullOrWhiteSpace(firstLine)) continue;
                    detected = enc;
                    return txt.Replace("\r\n", "\n").Split('\n');
                }
                catch { }
            }

            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                using (var sr = new StreamReader(fs, Encoding.UTF8, true))
                {
                    string txt = sr.ReadToEnd();
                    detected = sr.CurrentEncoding ?? Encoding.UTF8;
                    return txt.Replace("\r\n", "\n").Split('\n');
                }
            }
            catch
            {
                try
                {
                    string txt2 = Encoding.UTF8.GetString(data).Replace("\uFFFD", "");
                    detected = Encoding.UTF8;
                    return txt2.Replace("\r\n", "\n").Split('\n');
                }
                catch
                {
                    string fallback = Encoding.Default.GetString(data);
                    detected = Encoding.Default;
                    return fallback.Replace("\r\n", "\n").Split('\n');
                }
            }
        }

        private void OpenMapForSelection()
        {
            try
            {
                var map = new MapRouteForm(currentUser);
                map.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show("打开地图失败: " + ex.Message);
            }
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            try
            {
                loginShownByAction = true;
                var lf = new LoginForm();
                lf.StartPosition = FormStartPosition.CenterScreen;
                lf.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("注销失败: " + ex.Message);
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            if (currentDataTable == null)
            {
                MessageBox.Show("当前没有加载任何数据，请先导入数据。");
                return;
            }

            var add = new AddDataForm(currentDataTable);
            if (add.ShowDialog(this) == DialogResult.OK)
            {
                var nr = add.NewRow;
                if (nr != null)
                {
                    currentDataTable.Rows.Add(nr);
                    dataChanged = true;
                    dataGridViewMain.Refresh();
                    MessageBox.Show("新增数据已添加（尚未保存）。");
                }
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (currentDataTable == null)
            {
                MessageBox.Show("当前没有加载任何数据，请先导入数据。");
                return;
            }

            if (dataGridViewMain.SelectedRows.Count == 0)
            {
                MessageBox.Show("请先选择要删除的行（单行）。");
                return;
            }

            var idx = dataGridViewMain.SelectedRows[0].Index;
            if (idx < 0 || idx >= currentDataTable.Rows.Count) { MessageBox.Show("选择无效。"); return; }

            if (MessageBox.Show("确认删除所选行？（删除后可通过保存提交数据库/或在本地保存为文件）", "确认删除", MessageBoxButtons.YesNo) != DialogResult.Yes) return;

            try
            {
                var dr = ((DataRowView)dataGridViewMain.SelectedRows[0].DataBoundItem).Row;
                dr.Delete();
                dataChanged = true;
                dataGridViewMain.Refresh();
                MessageBox.Show("已从当前表中删除所选行（尚未保存）。");
            }
            catch (Exception ex)
            {
                MessageBox.Show("删除失败：" + ex.Message);
            }
        }

        private void BtnUpdate_Click(object sender, EventArgs e)
        {
            if (currentDataTable == null)
            {
                MessageBox.Show("当前没有加载任何数据，请先导入数据。");
                return;
            }

            if (dataGridViewMain.SelectedRows.Count == 0)
            {
                MessageBox.Show("请先选择要修改的行（单行）。");
                return;
            }

            try
            {
                var drv = dataGridViewMain.SelectedRows[0].DataBoundItem as DataRowView;
                if (drv == null) { MessageBox.Show("无法识别所选行的数据。"); return; }

                var edit = new EditDataForm(drv.Row);
                if (edit.ShowDialog(this) == DialogResult.OK)
                {
                    dataChanged = true;
                    dataGridViewMain.Refresh();
                    MessageBox.Show("修改已更新（尚未保存）。");

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("修改失败：" + ex.Message);
            }
        }

        // ✅ 点击“数据保存” -> SaveResultDialog（你后续把 SaveResultDialog.cs 发我，我也统一改为 DSN=OKS 的 ODBC/Access）
        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (currentDataTable == null || currentDataTable.Rows.Count == 0)
            {
                MessageBox.Show("当前无可保存的数据。");
                return;
            }

            try
            {
                EnsureConnStrReady();
                using (var dlg = new SaveResultDialog(currentDataTable, null, connStr))
                {
                    dlg.StartPosition = FormStartPosition.CenterParent;
                    dlg.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("打开保存数据窗口失败：" + ex.Message);
            }
        }

        private void BtnQuery_Click(object sender, EventArgs e)
        {
            if (currentDataTable == null)
            {
                MessageBox.Show("请先导入或打开一个表，然后再进行查询。");
                return;
            }

            try
            {
                var qf = new QueryForm(currentDataTable, dataGridViewMain, () => { MarkDataChanged(); });
                qf.StartPosition = FormStartPosition.CenterParent;
                qf.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show("打开查询窗体失败：" + ex.Message);
            }
        }

        public void MarkDataChanged()
        {
            dataChanged = true;
        }

        private void BtnCalc_Click(object sender, EventArgs e)
        {
            try
            {
                EnsureConnStrReady();
                var cf = new CalcChoiceForm(currentUser, connStr, this, currentDataTable?.Copy()) { StartPosition = FormStartPosition.CenterParent };
                cf.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show("打开计算窗体失败：" + ex.Message);
            }
        }

        private void BtnAbout_Click(object sender, EventArgs e)
        {
            try
            {
                MessageBox.Show(
                    "基于轨迹数据的时空最优出行系统\n\n" +
                    "功能：数据库/本地导入、增删改查、路径计算、结果可视化。\n\n" +
                    "提示：当前仅使用 Access ODBC（系统DSN=OKS）。",
                    "项目说明",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch { }
        }

        public void ShowCalculationResult(DataTable dt, string title = "计算结果")
        {
            if (dt == null) return;

            currentDataTable = dt;
            currentTableName = title;
            isAccessTable = false;
            dataAdapter = null;
            dataChanged = false;

            dataGridViewMain.DataSource = currentDataTable;
            dataGridViewMain.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            bool hasGraph = lastGraphEdges != null && lastGraphEdges.Count > 0;
            ApplyListGraphLayout(showGraph: hasGraph);

            try { MessageBox.Show("计算完成：结果已在主界面显示。", "计算完成", MessageBoxButtons.OK, MessageBoxIcon.Information); } catch { }
        }

        public void SetLastGraph(List<Tuple<string, string, double>> edges)
        {
            lastGraphEdges = edges == null ? null : new List<Tuple<string, string, double>>(edges);
            bool hasGraph = lastGraphEdges != null && lastGraphEdges.Count > 0;
            ApplyListGraphLayout(showGraph: hasGraph);
        }

        public Bitmap GetLastGraphBitmapCopy()
        {
            if (lastGraphEdges == null || lastGraphEdges.Count == 0) return null;
            CreateGraphBitmapToFitPictureBox();
            if (lastGraphBitmap == null) return null;
            return new Bitmap(lastGraphBitmap);
        }

        private void CreateGraphBitmapToFitPictureBox()
        {
            try
            {
                lastGraphBitmap?.Dispose();
                lastGraphBitmap = null;

                if (lastGraphEdges == null || lastGraphEdges.Count == 0) return;
                if (!pbGraph.Visible) return;

                int availableWidth = Math.Max(400, pbGraph.ClientSize.Width);
                int availableHeight = Math.Max(220, pbGraph.ClientSize.Height);

                var bmp = new Bitmap(availableWidth, availableHeight);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.Clear(Color.White);

                    HashSet<string> nodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var e in lastGraphEdges) { nodes.Add(e.Item1); nodes.Add(e.Item2); }
                    var nodeList = nodes.ToList();
                    int n = nodeList.Count;
                    if (n == 0) { lastGraphBitmap = bmp; return; }

                    int centerX = availableWidth / 2;
                    int centerY = availableHeight / 2;
                    int radius = Math.Max(60, Math.Min(centerX, centerY) - 60);
                    var pos = new Dictionary<string, PointF>(StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < n; i++)
                    {
                        double ang = 2 * Math.PI * i / n - Math.PI / 2;
                        int x = centerX + (int)(radius * Math.Cos(ang));
                        int y = centerY + (int)(radius * Math.Sin(ang));
                        pos[nodeList[i]] = new PointF(x, y);
                    }

                    var edgeDict = new Dictionary<(string, string), double>(new PairComparer());
                    foreach (var ed in lastGraphEdges) edgeDict[(ed.Item1, ed.Item2)] = ed.Item3;

                    float nodeR = Math.Max(12f, Math.Min(availableWidth, availableHeight) / 28f);
                    float arrowLen = Math.Max(12f, Math.Min(availableWidth, availableHeight) / 45f);
                    float offsetBase = Math.Max(8f, Math.Min(availableWidth, availableHeight) / 60f);

                    using (var pen = new Pen(Color.DarkBlue, Math.Max(1.6f, Math.Min(availableWidth, availableHeight) / 180f)))
                    using (var brush = new SolidBrush(Color.Black))
                    using (var font = new Font("微软雅黑", Math.Max(10, (int)(nodeR * 0.8f))))
                    {
                        var processedPairs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var kv in lastGraphEdges)
                        {
                            string a = kv.Item1, b = kv.Item2;
                            string pairKey = (string.Compare(a, b, StringComparison.OrdinalIgnoreCase) <= 0) ? $"{a}|{b}" : $"{b}|{a}";
                            if (processedPairs.Contains(pairKey)) continue;

                            bool hasAB = edgeDict.ContainsKey((a, b));
                            bool hasBA = edgeDict.ContainsKey((b, a));

                            PointF s = pos[a], t = pos[b];
                            if (hasAB && hasBA)
                            {
                                double wAB = edgeDict[(a, b)];
                                double wBA = edgeDict[(b, a)];
                                DrawOffsetDirectedEdgeToGraphics(g, s, t, +offsetBase, wAB.ToString("F0"), pen, brush, font, nodeR, arrowLen);
                                DrawOffsetDirectedEdgeToGraphics(g, t, s, -offsetBase, wBA.ToString("F0"), pen, brush, font, nodeR, arrowLen);
                            }
                            else if (hasAB)
                            {
                                double wAB = edgeDict[(a, b)];
                                DrawOffsetDirectedEdgeToGraphics(g, s, t, 0, wAB.ToString("F0"), pen, brush, font, nodeR, arrowLen);
                            }
                            else if (hasBA)
                            {
                                double wBA = edgeDict[(b, a)];
                                DrawOffsetDirectedEdgeToGraphics(g, t, s, 0, wBA.ToString("F0"), pen, brush, font, nodeR, arrowLen);
                            }

                            processedPairs.Add(pairKey);
                        }

                        foreach (var kv2 in pos)
                        {
                            var p = kv2.Value;
                            g.FillEllipse(Brushes.White, p.X - nodeR, p.Y - nodeR, nodeR * 2, nodeR * 2);
                            g.DrawEllipse(Pens.DarkGray, p.X - nodeR, p.Y - nodeR, nodeR * 2, nodeR * 2);

                            var nmFont = new Font("微软雅黑", Math.Max(9, (int)(nodeR * 0.7)), FontStyle.Bold);
                            var nmSize = g.MeasureString(kv2.Key, nmFont);
                            g.DrawString(kv2.Key, nmFont, Brushes.DarkRed, p.X - nmSize.Width / 2, p.Y - nmSize.Height / 2 - nodeR - 6);
                            nmFont.Dispose();
                        }
                    }
                }

                lastGraphBitmap = bmp;
            }
            catch
            {
                lastGraphBitmap?.Dispose();
                lastGraphBitmap = null;
            }
        }

        private void UpdateGraphPictureBox()
        {
            try
            {
                pbGraph.Image?.Dispose();
                pbGraph.Image = lastGraphBitmap != null ? new Bitmap(lastGraphBitmap) : null;
                pbGraph.Refresh();
            }
            catch { }
        }

        private void DrawOffsetDirectedEdgeToGraphics(Graphics g, PointF start, PointF end, double offsetPixels, string weightText, Pen pen, Brush textBrush, Font font, float nodeR, float arrowLen)
        {
            double dx = end.X - start.X, dy = end.Y - start.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 1e-6) return;

            double ux = dx / len, uy = dy / len;
            double nx = -uy, ny = ux;

            double ox = nx * offsetPixels, oy = ny * offsetPixels;
            PointF ps = new PointF((float)(start.X + ox), (float)(start.Y + oy));
            PointF pe = new PointF((float)(end.X + ox), (float)(end.Y + oy));
            PointF ps2 = new PointF(ps.X + (float)(ux * nodeR), ps.Y + (float)(uy * nodeR));
            PointF pe2 = new PointF(pe.X - (float)(ux * nodeR), pe.Y - (float)(uy * nodeR));

            g.DrawLine(pen, ps2, pe2);

            PointF mid = new PointF((ps2.X + pe2.X) / 2f, (ps2.Y + pe2.Y) / 2f);
            float textPad = (float)Math.Max(6, offsetPixels != 0 ? Math.Abs(offsetPixels) : 8f);
            PointF textPos = new PointF(mid.X + (float)(nx * textPad), mid.Y + (float)(ny * textPad));
            var sz = g.MeasureString(weightText, font);
            g.FillRectangle(Brushes.White, textPos.X - 2, textPos.Y - 2, sz.Width + 4, sz.Height + 4);
            g.DrawString(weightText, font, textBrush, textPos.X, textPos.Y);

            DrawArrowHeadToGraphics(g, pe2, ux, uy, pen.Brush, arrowLen);
        }

        private void DrawArrowHeadToGraphics(Graphics g, PointF tip, double ux, double uy, Brush brush, float arrowLen)
        {
            float side = arrowLen * 0.6f;
            PointF p1 = new PointF((float)(tip.X - ux * arrowLen + -uy * side), (float)(tip.Y - uy * arrowLen + ux * side));
            PointF p2 = new PointF((float)(tip.X - ux * arrowLen + uy * side), (float)(tip.Y - uy * arrowLen - ux * side));
            g.FillPolygon(brush, new[] { tip, p1, p2 });
        }

        private class PairComparer : IEqualityComparer<(string, string)>
        {
            public bool Equals((string, string) x, (string, string) y)
            {
                return string.Equals(x.Item1, y.Item1, StringComparison.OrdinalIgnoreCase) &&
                       string.Equals(x.Item2, y.Item2, StringComparison.OrdinalIgnoreCase);
            }
            public int GetHashCode((string, string) obj)
            {
                return (obj.Item1?.ToLowerInvariant() ?? "").GetHashCode() ^ (obj.Item2?.ToLowerInvariant() ?? "").GetHashCode();
            }
        }

        private void ResolveUserTableAndColumns()
        {
            if (!string.IsNullOrEmpty(userTableName)) return;

            EnsureConnStrReady();
            using (var conn = new OdbcConnection(connStr))
            {
                SafeOpenWithRetry(conn, 3);

                string[] candidates = new[] { "用户表", "用户登录表", "用户" };
                foreach (var t in candidates)
                {
                    if (TableExists(conn, t)) { userTableName = t; break; }
                }

                if (string.IsNullOrEmpty(userTableName))
                    throw new Exception("数据库中未找到 '用户表' 或 '用户登录表' 或 '用户'。");

                List<string> cols = GetTableColumns(conn, userTableName);

                string Find(params string[] tries)
                {
                    foreach (var s in tries)
                    {
                        var f = cols.FirstOrDefault(c => string.Equals(c?.Trim(), s, StringComparison.OrdinalIgnoreCase));
                        if (f != null) return f;
                    }
                    return null;
                }

                colUsername = Find("用户名", "user", "username", "UserName") ?? cols.FirstOrDefault() ?? "用户名";
                colPassword = Find("用户密码", "密码", "password", "pwd", "pass") ?? colPassword;
                colAvatar = Find("用户头像", "头像", "avatar", "useravatar") ?? colAvatar;
                colPhone = Find("手机号", "电话号码", "phone", "mobile") ?? colPhone;
                colWeChat = Find("WeChatOpenId", "WeChat", "微信OpenId", "WeChatopenid", "微信openid") ?? colWeChat;
                colQQ = Find("QQOpenId", "QQ", "QQOpenid", "qq_openid") ?? colQQ;
            }
        }

        private bool TableExists(OdbcConnection conn, string tableName)
        {
            try
            {
                DataTable schema = conn.GetSchema("Tables");
                foreach (DataRow r in schema.Rows)
                {
                    string type = (r.Table.Columns.Contains("TABLE_TYPE") ? (r["TABLE_TYPE"] as string) : "") ?? "";
                    string name = (r.Table.Columns.Contains("TABLE_NAME") ? (r["TABLE_NAME"] as string) : "") ?? "";
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    if (!string.Equals(type, "TABLE", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(type, "BASE TABLE", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (string.Equals(name, tableName, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            catch { }
            return false;
        }

        private List<string> GetTableColumns(OdbcConnection conn, string tableName)
        {
            var cols = new List<string>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $"SELECT TOP 1 * FROM {Q(tableName)}";
                using (var rd = cmd.ExecuteReader())
                {
                    for (int i = 0; i < rd.FieldCount; i++)
                        cols.Add(rd.GetName(i));
                }
            }
            return cols;
        }

        // Access 没有 CREATE TABLE IF NOT EXISTS；这里只给一个“参考 DDL”，真正创建应先判断 TableExists 再执行
        private string GetCreateUserTableSql_Access()
        {
            return
                "CREATE TABLE 用户表 (" +
                " 用户ID AUTOINCREMENT PRIMARY KEY, " +
                " 用户名 TEXT(64) NOT NULL, " +
                " 用户密码 TEXT(255) NOT NULL, " +
                " 用户头像 TEXT(255), " +
                " 手机号 TEXT(32), " +
                " WeChatOpenId TEXT(128), " +
                " QQOpenId TEXT(128) " +
                ");";
        }

        private static string GetDbErrorDetail(Exception ex)
        {
            if (ex == null) return "未知异常（ex=null）";
            var sb = new StringBuilder();
            sb.AppendLine(ex.Message ?? "");

            var ox = ex as OdbcException;
            if (ox != null)
            {
                sb.AppendLine("---- OdbcException ----");
                sb.AppendLine("Errors.Count=" + ox.Errors.Count);
                for (int i = 0; i < ox.Errors.Count; i++)
                {
                    var e = ox.Errors[i];
                    sb.AppendLine($"[{i}] SQLState={e.SQLState}, NativeError={e.NativeError}, Message={e.Message}");
                }
            }

            sb.AppendLine("---- Full ----");
            sb.AppendLine(ex.ToString());
            return sb.ToString();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try { lastGraphBitmap?.Dispose(); lastGraphBitmap = null; } catch { }

            if (e.CloseReason == CloseReason.UserClosing && !loginShownByAction)
            {
                try
                {
                    var lf = new LoginForm();
                    lf.StartPosition = FormStartPosition.CenterScreen;
                    lf.Show();
                    loginShownByAction = true;
                }
                catch { }
            }
            base.OnFormClosing(e);
        }
    }
}
