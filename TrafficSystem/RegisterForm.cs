using System;
using System.Configuration;
using System.Data;
using System.Data.Odbc;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace TrafficSystem
{
    public partial class RegisterForm : Form
    {
        // ✅ 只允许 ODBC 系统 DSN 连接 Access（OKS）
        private const string DefaultDsnName = "OKS";
        private readonly string _connStr;

        private string avatarFullPath = "";
        public string RegUsernameAfterSuccess { get; private set; } = null;

        // 模拟短信验证码（内存）
        private static System.Collections.Generic.Dictionary<string, string> _verificationCodes =
            new System.Collections.Generic.Dictionary<string, string>();

        // 图形验证码文本（4位数字）
        private string _captchaText = "";

        // 用户表和列动态解析
        private string userTableName = null;
        private string colUsername = "用户名";
        private string colPassword = "用户密码";
        private string colAvatar = "用户头像";
        private string colPhone = "手机号";

        // ✅ 兼容 .NET Framework 4.7.2：自己实现安全随机整数
        private static readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();

        private static int RngNextInt(int minInclusive, int maxExclusive)
        {
            if (minInclusive >= maxExclusive) throw new ArgumentOutOfRangeException();
            uint range = (uint)(maxExclusive - minInclusive);

            // 拒绝采样，避免取模偏差
            uint limit = uint.MaxValue - (uint.MaxValue % range);
            uint r;
            byte[] buf = new byte[4];

            do
            {
                _rng.GetBytes(buf);
                r = BitConverter.ToUInt32(buf, 0);
            } while (r >= limit);

            return (int)(minInclusive + (r % range));
        }

        public RegisterForm()
        {
            InitializeComponent();
            _connStr = BuildDsnOnlyConnStr(); // ✅ 固化 DSN-only

            ApplyVisualStyle();

            // 事件绑定（避免重复绑定/遗漏绑定）
            if (btnChooseAvatar != null)
            {
                btnChooseAvatar.Click -= btnChooseAvatar_Click;
                btnChooseAvatar.Click += btnChooseAvatar_Click;
            }

            if (btnRegister != null)
            {
                btnRegister.Click -= btnRegister_Click;
                btnRegister.Click += btnRegister_Click;
            }

            if (btnGetRegCode != null)
            {
                btnGetRegCode.Click -= BtnGetRegCode_Click;
                btnGetRegCode.Click += BtnGetRegCode_Click;
            }

            if (picCaptcha != null)
            {
                picCaptcha.Click -= PicCaptcha_Click;
                picCaptcha.Click += PicCaptcha_Click;
            }

            if (btnRefreshCaptcha != null)
            {
                btnRefreshCaptcha.Click -= BtnRefreshCaptcha_Click;
                btnRefreshCaptcha.Click += BtnRefreshCaptcha_Click;
            }

            if (txtRegPassword != null)
            {
                txtRegPassword.TextChanged -= TxtRegPassword_TextChanged;
                txtRegPassword.TextChanged += TxtRegPassword_TextChanged;
            }

            // ✅ 你原来有处理函数，但这里也统一绑定（不改布局、不动 Designer）
            if (chkShowRegPassword != null)
            {
                chkShowRegPassword.CheckedChanged -= chkShowRegPassword_CheckedChanged;
                chkShowRegPassword.CheckedChanged += chkShowRegPassword_CheckedChanged;
            }

            if (panelRightCard != null)
            {
                panelRightCard.SizeChanged -= panelRightCard_SizeChanged;
                panelRightCard.SizeChanged += panelRightCard_SizeChanged;
            }

            // 初始化验证码（4位数字）
            GenerateCaptchaImage();
        }

        // ✅ 仅 DSN：从 App.config(appSettings: DbConn) 读取，但只允许 DSN 形式
        // 允许：
        //   DbConn=DSN=OKS;
        //   DbConn=OKS
        // 不允许：
        //   Driver=...;Dbq=...;
        //   Server=...;Database=...;User=...;Password=...
        private static string BuildDsnOnlyConnStr()
        {
            string raw = (ConfigurationManager.AppSettings["DbConn"] ?? "").Trim();

            if (string.IsNullOrWhiteSpace(raw))
                raw = "DSN=" + DefaultDsnName + ";";

            // 如果只写了 DSN 名（例如 OKS），自动补全
            if (!raw.Contains("="))
                raw = "DSN=" + raw + ";";

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
                    "数据库连接配置错误：本程序仅允许使用 ODBC 系统 DSN 方式连接 Access(OKS)。\n\n" +
                    "请在 App.config 的 appSettings 中配置：\n" +
                    "  <add key=\"DbConn\" value=\"DSN=OKS;\" />\n" +
                    "或直接不配（默认 DSN=OKS;）。\n\n" +
                    "禁止使用 Driver/Server/Database/UID/PWD 等其他连接方式。");
            }

            return raw.Trim();
        }

        private void TxtRegPassword_TextChanged(object sender, EventArgs e)
        {
            UpdatePasswordStrengthIndicator(txtRegPassword.Text);
        }

        private void BtnGetRegCode_Click(object sender, EventArgs e)
        {
            string phone = txtRegPhone.Text.Trim();
            if (string.IsNullOrEmpty(phone))
            {
                MessageBox.Show("请输入手机号以获取短信验证码。");
                return;
            }
            string code = Generate6DigitCode();
            _verificationCodes[phone] = code;
            MessageBox.Show($"模拟发送验证码：{code}（仅测试用）", "验证码发送");
        }

        private void PicCaptcha_Click(object sender, EventArgs e)
        {
            GenerateCaptchaImage();
        }

        private void BtnRefreshCaptcha_Click(object sender, EventArgs e)
        {
            GenerateCaptchaImage();
        }

        // ✅ 生成 4 位纯数字验证码图片
        private void GenerateCaptchaImage()
        {
            _captchaText = Generate4DigitCaptchaText();

            var bmp = new Bitmap(140, 40);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.White);

                // 噪点
                for (int i = 0; i < 30; i++)
                {
                    int x = RngNextInt(0, bmp.Width);
                    int y = RngNextInt(0, bmp.Height);
                    g.DrawEllipse(Pens.LightGray, x, y, 1, 1);
                }

                var fonts = new[] { "微软雅黑", "Arial", "Tahoma" };

                // 画 4 位数字
                for (int i = 0; i < _captchaText.Length; i++)
                {
                    char c = _captchaText[i];
                    var font = new Font(fonts[RngNextInt(0, fonts.Length)], 18 + RngNextInt(0, 3), FontStyle.Bold);

                    int r = RngNextInt(50, 180);
                    int gg = RngNextInt(50, 180);
                    int b = RngNextInt(50, 180);
                    using (var brush = new SolidBrush(Color.FromArgb(r, gg, b)))
                    {
                        float x = 10 + i * 28 + RngNextInt(-2, 3);
                        float y = 4 + RngNextInt(-2, 3);
                        g.DrawString(c.ToString(), font, brush, x, y);
                    }

                    font.Dispose();
                }

                // 干扰线
                for (int i = 0; i < 3; i++)
                {
                    int r = RngNextInt(100, 200);
                    int gg = RngNextInt(100, 200);
                    int b = RngNextInt(100, 200);
                    using (var pen = new Pen(Color.FromArgb(r, gg, b), 1f))
                    {
                        g.DrawLine(pen,
                            RngNextInt(0, bmp.Width), RngNextInt(0, bmp.Height),
                            RngNextInt(0, bmp.Width), RngNextInt(0, bmp.Height));
                    }
                }
            }

            if (picCaptcha.Image != null)
            {
                var old = picCaptcha.Image;
                picCaptcha.Image = null;
                old.Dispose();
            }
            picCaptcha.Image = bmp;
        }

        private string Generate4DigitCaptchaText()
        {
            int n = RngNextInt(0, 10000);
            return n.ToString("D4");
        }

        private void btnChooseAvatar_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    avatarFullPath = ofd.FileName;
                    try
                    {
                        if (picAvatarPreview.Image != null)
                        {
                            var old = picAvatarPreview.Image;
                            picAvatarPreview.Image = null;
                            old.Dispose();
                        }

                        picAvatarPreview.Image = Image.FromFile(avatarFullPath);
                        SetRoundedRegion(picAvatarPreview, picAvatarPreview.Width / 10);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("加载头像失败：" + ex.Message);
                    }
                }
            }
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            string username = txtRegUsername.Text.Trim();
            string pwd = txtRegPassword.Text ?? "";

            string captchaInput = txtRegCaptcha.Text.Trim();
            if (!string.Equals(captchaInput, _captchaText, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("图形验证码错误，请刷新后重试。");
                GenerateCaptchaImage();
                return;
            }

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(pwd))
            {
                MessageBox.Show("请填写用户名和密码。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (username.Length < 3 || username.Length > 64)
            {
                MessageBox.Show("用户名长度应在3到64字符之间。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string phone = txtRegPhone.Text.Trim();
            if (!string.IsNullOrEmpty(phone))
            {
                string codeInput = txtRegCode.Text.Trim();
                if (!_verificationCodes.TryGetValue(phone, out var real) || real != codeInput)
                {
                    MessageBox.Show("手机验证码不正确或未获取。");
                    return;
                }
            }

            try
            {
                if (string.IsNullOrWhiteSpace(_connStr))
                {
                    MessageBox.Show("未配置数据库连接（仅允许 DSN=OKS;），请检查 App.config。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 保存头像到本地目录（路径写入数据库）
                string savedPath = "";
                if (!string.IsNullOrEmpty(avatarFullPath) && File.Exists(avatarFullPath))
                {
                    string avatarsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "avatars");
                    if (!Directory.Exists(avatarsDir)) Directory.CreateDirectory(avatarsDir);

                    string ext = Path.GetExtension(avatarFullPath);
                    string dest = Path.Combine(avatarsDir, MakeSafeFileName(username + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ext));
                    File.Copy(avatarFullPath, dest, true);
                    savedPath = dest;
                }

                using (var conn = new OdbcConnection(_connStr))
                {
                    conn.Open();

                    // ✅ 确保用户表存在并解析列名（Access/ODBC）
                    EnsureUserTableExistsAndResolveAccess(conn);

                    // 检查用户名是否存在
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = $"SELECT COUNT(*) FROM {Q(userTableName)} WHERE {Q(colUsername)}=?";
                        cmd.Parameters.Add(new OdbcParameter { Value = username });

                        int cnt = Convert.ToInt32(cmd.ExecuteScalar());
                        if (cnt > 0)
                        {
                            MessageBox.Show("用户名已存在，请换一个。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                    }

                    // 插入
                    using (var ins = conn.CreateCommand())
                    {
                        string hashed = ComputeSha256Hex(pwd);

                        ins.CommandText =
                            $"INSERT INTO {Q(userTableName)} ({Q(colUsername)},{Q(colPassword)},{Q(colAvatar)},{Q(colPhone)}) " +
                            $"VALUES (?,?,?,?)";

                        ins.Parameters.Add(new OdbcParameter { Value = username });
                        ins.Parameters.Add(new OdbcParameter { Value = hashed });
                        ins.Parameters.Add(new OdbcParameter { Value = savedPath });
                        ins.Parameters.Add(new OdbcParameter { Value = phone });

                        int n = ins.ExecuteNonQuery();
                        if (n > 0)
                        {
                            MessageBox.Show("注册成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            RegUsernameAfterSuccess = username;
                            this.DialogResult = DialogResult.OK;
                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show("注册失败，请重试。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("注册失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void chkShowRegPassword_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                txtRegPassword.UseSystemPasswordChar = !chkShowRegPassword.Checked;
            }
            catch { }
        }

        private void ApplyVisualStyle()
        {
            try
            {
                Color primary = Color.FromArgb(40, 130, 210);
                this.BackColor = Color.FromArgb(248, 249, 250);
                panelLeft.BackColor = Color.White;
                panelRight.BackColor = this.BackColor;
                panelRightCard.BackColor = Color.White;
                panelRightCard.Padding = new Padding(16);

                lblProjectTitle.Font = new Font("微软雅黑", 20F, FontStyle.Bold);
                lblProjectTitle.ForeColor = Color.FromArgb(36, 36, 36);
                lblProjectDesc.ForeColor = Color.FromArgb(100, 100, 100);

                txtRegUsername.BorderStyle = BorderStyle.None;
                txtRegPassword.BorderStyle = BorderStyle.None;
                txtRegPassword.UseSystemPasswordChar = true;

                btnChooseAvatar.FlatStyle = FlatStyle.Flat;
                btnRegister.FlatStyle = FlatStyle.Flat;
                btnGetRegCode.FlatStyle = FlatStyle.Flat;

                btnChooseAvatar.BackColor = Color.FromArgb(120, 120, 120);
                btnRegister.BackColor = primary;
                btnGetRegCode.BackColor = Color.FromArgb(80, 140, 200);

                btnChooseAvatar.ForeColor = btnRegister.ForeColor = btnGetRegCode.ForeColor = Color.White;

                SetRoundedRegion(panelRightCard, 10);
            }
            catch { }
        }

        private void SetRoundedRegion(Control c, int radius)
        {
            try
            {
                if (c.Width <= 0 || c.Height <= 0) return;
                var r = new Rectangle(0, 0, c.Width, c.Height);
                var gp = new GraphicsPath();
                int d = radius * 2;
                gp.AddArc(r.X, r.Y, d, d, 180, 90);
                gp.AddArc(r.Right - d, r.Y, d, d, 270, 90);
                gp.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
                gp.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
                gp.CloseFigure();
                c.Region = new Region(gp);
                gp.Dispose();
            }
            catch { }
        }

        private void panelRightCard_SizeChanged(object sender, EventArgs e)
        {
            SetRoundedRegion(panelRightCard, 10);
        }

        private void UpdatePasswordStrengthIndicator(string pwd)
        {
            int score = 0;
            if (string.IsNullOrEmpty(pwd)) score = 0;
            else
            {
                bool hasDigit = false, hasLetter = false, hasSymbol = false;
                foreach (var c in pwd)
                {
                    if (char.IsDigit(c)) hasDigit = true;
                    else if (char.IsLetter(c)) hasLetter = true;
                    else hasSymbol = true;
                }
                if ((hasDigit && !hasLetter && !hasSymbol) || (!hasDigit && hasLetter && !hasSymbol)) score = 1;
                else if ((hasDigit && hasLetter && !hasSymbol) || (hasLetter && hasSymbol && !hasDigit) || (hasDigit && hasSymbol && !hasLetter)) score = 2;
                else if (hasDigit && hasLetter && hasSymbol) score = 3;
            }

            switch (score)
            {
                case 0:
                    lblPwdStrength.Text = "密码强度：";
                    lblPwdStrength.ForeColor = Color.Gray;
                    break;
                case 1:
                    lblPwdStrength.Text = "密码强度：弱";
                    lblPwdStrength.ForeColor = Color.Red;
                    break;
                case 2:
                    lblPwdStrength.Text = "密码强度：中";
                    lblPwdStrength.ForeColor = Color.Orange;
                    break;
                case 3:
                    lblPwdStrength.Text = "密码强度：强";
                    lblPwdStrength.ForeColor = Color.Green;
                    break;
            }
        }

        private string ComputeSha256Hex(string input)
        {
            using (var sha = SHA256.Create())
            {
                var bs = Encoding.UTF8.GetBytes(input ?? "");
                var hs = sha.ComputeHash(bs);
                var sb = new StringBuilder();
                foreach (var b in hs) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        private string MakeSafeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(name.Length);
            foreach (var c in name)
            {
                if (Array.IndexOf(invalid, c) >= 0) sb.Append('_'); else sb.Append(c);
            }
            return sb.ToString();
        }

        private string Generate6DigitCode()
        {
            int n = RngNextInt(0, 1000000);
            return n.ToString("D6");
        }

        private void RegisterForm_Load(object sender, EventArgs e)
        {
        }

        // ===================== Access / ODBC（仅 DSN） =====================

        // Access 标识符引用
        private static string Q(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier)) return "[]";
            return "[" + identifier.Replace("]", "]]") + "]";
        }

        private void EnsureUserTableExistsAndResolveAccess(OdbcConnection conn)
        {
            if (!string.IsNullOrEmpty(userTableName)) return;

            // 候选表名
            string[] candidates = new[] { "用户表", "用户登录表", "用户" };
            foreach (var t in candidates)
            {
                if (TableExistsAccess(conn, t))
                {
                    userTableName = t;
                    break;
                }
            }

            // 不存在就创建一个 用户
            if (string.IsNullOrEmpty(userTableName))
            {
                TryCreateUserTableAccess(conn);
                userTableName = "用户";
            }

            var cols = GetColumnNamesAccess(conn, userTableName);

            string Find(params string[] tries)
            {
                foreach (var s in tries)
                {
                    var f = cols.FirstOrDefault(c => string.Equals(c?.Trim(), s, StringComparison.OrdinalIgnoreCase));
                    if (f != null) return f;
                }
                return null;
            }

            colUsername = Find("用户名", "user", "username", "UserName", "账号", "账户") ?? cols.FirstOrDefault() ?? "用户名";
            colPassword = Find("用户密码", "密码", "password", "pwd", "pass") ?? "用户密码";
            colAvatar = Find("用户头像", "头像", "avatar", "useravatar", "头像路径") ?? "用户头像";
            colPhone = Find("手机号", "电话号码", "phone", "mobile") ?? "手机号";
        }

        private bool TableExistsAccess(OdbcConnection conn, string tableName)
        {
            try
            {
                var schema = conn.GetSchema("Tables");
                foreach (DataRow r in schema.Rows)
                {
                    var name = r["TABLE_NAME"] == DBNull.Value ? "" : Convert.ToString(r["TABLE_NAME"]);
                    if (string.Equals(name, tableName, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.IsNullOrEmpty(name) && name.StartsWith("MSys", StringComparison.OrdinalIgnoreCase))
                            return false;
                        return true;
                    }
                }
            }
            catch { }

            try
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $"SELECT TOP 1 * FROM {Q(tableName)}";
                    cmd.CommandTimeout = 10;
                    using (var rd = cmd.ExecuteReader())
                    {
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        private System.Collections.Generic.List<string> GetColumnNamesAccess(OdbcConnection conn, string tableName)
        {
            var cols = new System.Collections.Generic.List<string>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $"SELECT * FROM {Q(tableName)} WHERE 1=0";
                using (var rd = cmd.ExecuteReader())
                {
                    for (int i = 0; i < rd.FieldCount; i++)
                        cols.Add(rd.GetName(i));
                }
            }
            return cols;
        }

        private void TryCreateUserTableAccess(OdbcConnection conn)
        {
            // 注意：不同 Access ODBC 驱动对“多语句”支持不一致，所以拆开执行更稳
            try
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText =
                        "CREATE TABLE " + Q("用户") + " (" +
                        Q("用户ID") + " AUTOINCREMENT PRIMARY KEY, " +
                        Q("用户名") + " TEXT(64) NOT NULL, " +
                        Q("用户密码") + " TEXT(255) NOT NULL, " +
                        Q("用户头像") + " TEXT(255), " +
                        Q("手机号") + " TEXT(32), " +
                        Q("WeChatOpenId") + " TEXT(128), " +
                        Q("QQOpenId") + " TEXT(128), " +
                        Q("创建时间") + " DATETIME" +
                        ")";
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                // 表可能已存在或驱动限制：忽略
            }

            try
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "CREATE UNIQUE INDEX " + Q("UK_用户_用户名") + " ON " + Q("用户") + "(" + Q("用户名") + ")";
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }

            try
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "CREATE INDEX " + Q("IDX_用户_手机号") + " ON " + Q("用户") + "(" + Q("手机号") + ")";
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }
    }
}
