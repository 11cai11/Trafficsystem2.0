using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.Data.Odbc;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;

namespace TrafficSystem
{
    public partial class LoginForm : Form
    {
        // ✅ 只允许 ODBC 系统 DSN 连接 Access（按你的要求：不需要其他连接方式）
        // 默认 DSN 名：OKS（你同学电脑也要建立同名 64位系统 DSN 并指向同一个 .mdb/.accdb）
        private const string DefaultDsnName = "OKS";
        private readonly string _connStr;

        // 第三方凭证（示例）
        private const string WeChatAppId = "替换为你的微信AppID";
        private const string WeChatAppSecret = "替换为你的微信AppSecret";
        private const string WeChatRedirectUri = "https://yourdomain.com/wechat_callback";

        private const string QqAppId = "替换为你的QQ AppID";
        private const string QqAppKey = "替换为你的QQ AppKey";
        private const string QqRedirectUri = "https://yourdomain.com/qq_callback";

        private static readonly HttpClient _http = new HttpClient();

        // 验证码存储
        private static Dictionary<string, (string Code, DateTime Expire)> _verificationCodes =
            new Dictionary<string, (string, DateTime)>(StringComparer.OrdinalIgnoreCase);

        private Timer smsTimer;
        private int smsCountdown = 0;
        private string currentThirdParty = null;

        // 动态识别的表名与列名（运行时解析）
        private string userTableName = null;
        private string colUsername = "用户名";
        private string colPassword = "用户密码"; // 或 密码
        private string colAvatar = "用户头像";   // 或 头像
        private string colPhone = "手机号";
        private string colWeChat = "WeChatOpenId";
        private string colQQ = "QQOpenId";

        public LoginForm()
        {
            InitializeComponent();

            // ✅ 固化连接串：仅允许 DSN
            _connStr = BuildDsnOnlyConnStr();

            ApplyVisualStyle();

            // 事件绑定（防重复）
            btnLogin.Click -= BtnLogin_Click; btnLogin.Click += BtnLogin_Click;
            btnRegister.Click -= BtnRegister_Click; btnRegister.Click += BtnRegister_Click;
            btnPhoneModeToggle.Click -= BtnPhoneModeToggle_Click; btnPhoneModeToggle.Click += BtnPhoneModeToggle_Click;
            btnGetLoginCode.Click -= BtnGetLoginCode_Click; btnGetLoginCode.Click += BtnGetLoginCode_Click;
            btnWeChat.Click -= BtnWeChat_Click; btnWeChat.Click += BtnWeChat_Click;
            btnQQ.Click -= BtnQQ_Click; btnQQ.Click += BtnQQ_Click;
            btnCloseThirdParty.Click -= BtnCloseThirdParty_Click; btnCloseThirdParty.Click += BtnCloseThirdParty_Click;
            chkShowPassword.CheckedChanged -= ChkShowPassword_CheckedChanged; chkShowPassword.CheckedChanged += ChkShowPassword_CheckedChanged;
            txtPassword.TextChanged -= TxtPassword_TextChanged; txtPassword.TextChanged += TxtPassword_TextChanged;

            webBrowserThirdParty.DocumentCompleted -= WebBrowserThirdParty_DocumentCompleted;
            webBrowserThirdParty.DocumentCompleted += WebBrowserThirdParty_DocumentCompleted;

            smsTimer = new Timer { Interval = 1000 };
            smsTimer.Tick += SmsTimer_Tick;

            SetPhoneMode(false);
            panelThirdPartyArea.Visible = false;

            // ✅ ✅ ✅ 引用“全局等比例缩放算法”（不改 Designer，不改布局）
            // 建议：LoginForm 属于 Designer 布局，所以在构造函数末尾注册缩放基准最稳
            UiZoom.Register(this, scaleFormClientSize: true);

            // ✅ 可选：Ctrl + 鼠标滚轮缩放
            UiZoom.EnableCtrlWheelZoom(this);

            // ✅ 可选：Ctrl + / - / 0 快捷键
            this.KeyPreview = true;
            this.KeyDown -= LoginForm_KeyDown;
            this.KeyDown += LoginForm_KeyDown;
        }

        private void LoginForm_KeyDown(object sender, KeyEventArgs e)
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

            // 强制只允许 DSN 连接（按你的要求：删除/不允许其他连接方式）
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
                    "数据库连接配置错误：本程序仅允许使用 ODBC 系统 DSN 方式连接 Access。\n\n" +
                    "请在 App.config 的 appSettings 中配置：\n" +
                    "  <add key=\"DbConn\" value=\"DSN=OKS;\" />\n" +
                    "或直接不配（默认 DSN=OKS;）。\n\n" +
                    "禁止使用 Driver/Server/Database/UID/PWD 等其他连接方式。");
            }

            return raw.Trim();
        }

        #region 登录 / 注册 / 切换 / 获取验证码 / 第三方入口
        private async void BtnLogin_Click(object sender, EventArgs e)
        {
            try
            {
                await EnsureUserTableAndColumnsResolvedAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "登录失败：找不到用户表（用户表 / 用户登录表 / 用户）。\n\n" +
                    "请确认：\n" +
                    "1）同学电脑已创建 系统DSN：OKS（64位）并指向正确的 .mdb/.accdb\n" +
                    "2）Access 里存在 用户/用户表/用户登录表\n\n" +
                    "可参考建表 SQL（Access 语法）：\n\n" +
                    GetCreateUserTableSql() +
                    "\n\n错误信息: " + ex.Message,
                    "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 手机验证码模式
            if (panelPhoneLogin.Visible)
            {
                string key = txtLoginPhone.Text.Trim();
                string code = txtLoginCode.Text.Trim();
                if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(code))
                {
                    MessageBox.Show("请输入手机号/用户名和验证码。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!_verificationCodes.TryGetValue(key, out var entry) || entry.Code != code)
                {
                    MessageBox.Show("验证码错误或不存在。", "登录失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (DateTime.UtcNow > entry.Expire)
                {
                    _verificationCodes.Remove(key);
                    MessageBox.Show("验证码已过期。", "登录失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    string foundUsername = null;

                    using (var conn = new OdbcConnection(_connStr))
                    {
                        conn.Open();
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = $"SELECT TOP 1 {Q(colUsername)} FROM {Q(userTableName)} WHERE {Q(colUsername)}=? OR {Q(colPhone)}=?";
                            cmd.Parameters.Add(new OdbcParameter { Value = key });
                            cmd.Parameters.Add(new OdbcParameter { Value = key });

                            var v = cmd.ExecuteScalar();
                            foundUsername = (v == null || v == DBNull.Value) ? null : Convert.ToString(v);
                        }
                    }

                    if (string.IsNullOrEmpty(foundUsername))
                    {
                        MessageBox.Show("用户不存在或手机号未绑定。", "登录失败", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    OpenMainAndHide(foundUsername);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("登录失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    _verificationCodes.Remove(txtLoginPhone.Text.Trim());
                }

                return;
            }

            // 普通用户名密码登录
            string username = txtUsername.Text.Trim();
            string pwd = txtPassword.Text ?? "";
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(pwd))
            {
                MessageBox.Show("请填写用户名和密码。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string storedPwd = null;

                using (var conn = new OdbcConnection(_connStr))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = $"SELECT TOP 1 {Q(colPassword)} FROM {Q(userTableName)} WHERE {Q(colUsername)}=?";
                        cmd.Parameters.Add(new OdbcParameter { Value = username });

                        var v = cmd.ExecuteScalar();
                        if (v == null || v == DBNull.Value)
                        {
                            MessageBox.Show("用户名不存在。", "登录失败", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                        storedPwd = Convert.ToString(v) ?? "";
                    }

                    string hashedInput = ComputeSha256Hex(pwd);
                    bool match = false;

                    if (IsLikelyHexSha256(storedPwd))
                    {
                        match = string.Equals(storedPwd, hashedInput, StringComparison.OrdinalIgnoreCase);
                    }
                    else
                    {
                        // 兼容旧明文并尝试升级为哈希存储
                        if (string.Equals(storedPwd, pwd))
                        {
                            match = true;
                            try
                            {
                                using (var upd = conn.CreateCommand())
                                {
                                    upd.CommandText = $"UPDATE {Q(userTableName)} SET {Q(colPassword)}=? WHERE {Q(colUsername)}=?";
                                    upd.Parameters.Add(new OdbcParameter { Value = hashedInput });
                                    upd.Parameters.Add(new OdbcParameter { Value = username });
                                    upd.ExecuteNonQuery();
                                }
                            }
                            catch { }
                        }
                        else
                        {
                            match = string.Equals(storedPwd, hashedInput, StringComparison.OrdinalIgnoreCase);
                        }
                    }

                    if (!match)
                    {
                        MessageBox.Show("密码错误。", "登录失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                OpenMainAndHide(username);
            }
            catch (Exception ex)
            {
                MessageBox.Show("登录失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnRegister_Click(object sender, EventArgs e)
        {
            if (panelPhoneLogin.Visible)
            {
                SetPhoneMode(false);
                return;
            }

            using (var reg = new RegisterForm())
            {
                var dr = reg.ShowDialog(this);
                if (dr == DialogResult.OK && !string.IsNullOrEmpty(reg.RegUsernameAfterSuccess))
                {
                    txtUsername.Text = reg.RegUsernameAfterSuccess;
                    txtPassword.Focus();
                    SetPhoneMode(false);
                }
            }
        }

        private void BtnPhoneModeToggle_Click(object sender, EventArgs e)
        {
            SetPhoneMode(!panelPhoneLogin.Visible);
        }

        private void BtnGetLoginCode_Click(object sender, EventArgs e)
        {
            string key = txtLoginPhone.Text.Trim();
            if (string.IsNullOrEmpty(key))
            {
                MessageBox.Show("请输入手机号或用户名以获取验证码。");
                return;
            }

            bool looksLikePhone = Regex.IsMatch(key, @"^\d{6,15}$");
            bool looksLikeUsername = Regex.IsMatch(key, @"^[\w\-\.@]{3,64}$");

            if (!looksLikePhone && !looksLikeUsername)
            {
                var r = MessageBox.Show("输入既不像手机号也不像用户名，是否继续模拟发送？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (r != DialogResult.Yes) return;
            }

            string code = Generate6DigitCode();
            _verificationCodes[key] = (code, DateTime.UtcNow.AddMinutes(5));

            smsCountdown = 60;
            btnGetLoginCode.Enabled = false;
            btnGetLoginCode.Text = $"重新获取({smsCountdown}s)";
            smsTimer.Start();

            MessageBox.Show($"模拟发送验证码：{code}（仅测试）", "验证码已发送");
        }

        private void BtnWeChat_Click(object sender, EventArgs e) => StartWeChatOAuth();
        private void BtnQQ_Click(object sender, EventArgs e) => StartQqOAuth();
        #endregion

        #region 第三方 OAuth 展示
        private void StartWeChatOAuth()
        {
            currentThirdParty = "wechat";
            string state = "wechat_login_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            string redirect = Uri.EscapeDataString(WeChatRedirectUri);
            string url = $"https://open.weixin.qq.com/connect/qrconnect?appid={WeChatAppId}&redirect_uri={redirect}&response_type=code&scope=snsapi_login&state={state}#wechat_redirect";
            ShowThirdPartyArea("微信登录", url);
        }

        private void StartQqOAuth()
        {
            currentThirdParty = "qq";
            string state = "qq_login_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            string redirect = Uri.EscapeDataString(QqRedirectUri);
            string url = $"https://graph.qq.com/oauth2.0/authorize?response_type=code&client_id={QqAppId}&redirect_uri={redirect}&state={state}&display=pc";
            ShowThirdPartyArea("QQ 登录", url);
        }

        private void ShowThirdPartyArea(string title, string authUrl)
        {
            lblThirdPartyTitle.Text = title;
            pictureBoxThirdParty.Visible = false;
            webBrowserThirdParty.Navigate(authUrl);
            panelThirdPartyArea.Visible = true;
            panelThirdPartyArea.BringToFront();
        }

        private void BtnCloseThirdParty_Click(object sender, EventArgs e)
        {
            panelThirdPartyArea.Visible = false;
            webBrowserThirdParty.Stop();
            pictureBoxThirdParty.Image?.Dispose();
            pictureBoxThirdParty.Image = null;
            currentThirdParty = null;
        }

        private async void WebBrowserThirdParty_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            try
            {
                var url = webBrowserThirdParty.Url?.ToString() ?? "";
                if (string.IsNullOrEmpty(url)) return;

                if (url.Contains("code="))
                {
                    var uri = new Uri(url);
                    var qs = ParseQuery(uri.Query);
                    var code = qs["code"];
                    var state = qs["state"];
                    if (string.IsNullOrEmpty(code)) return;

                    if (currentThirdParty == "wechat" || (state != null && state.StartsWith("wechat_login")))
                    {
                        webBrowserThirdParty.Stop();
                        await HandleWeChatCodeAsync(code);
                    }
                    else if (currentThirdParty == "qq" || (state != null && state.StartsWith("qq_login")))
                    {
                        webBrowserThirdParty.Stop();
                        await HandleQqCodeAsync(code);
                    }

                    panelThirdPartyArea.Visible = false;
                    currentThirdParty = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("处理回调出错: " + ex.Message);
            }
        }

        private async Task HandleWeChatCodeAsync(string code)
        {
            try
            {
                string tokenUrl = $"https://api.weixin.qq.com/sns/oauth2/access_token?appid={WeChatAppId}&secret={WeChatAppSecret}&code={code}&grant_type=authorization_code";
                var tokenResp = await _http.GetStringAsync(tokenUrl);
                var tokenJson = JObject.Parse(tokenResp);
                if (tokenJson["errcode"] != null)
                {
                    MessageBox.Show("获取微信 token 失败: " + tokenJson.ToString());
                    return;
                }

                string accessToken = tokenJson.Value<string>("access_token"); // 保留（示例）
                string openid = tokenJson.Value<string>("openid");
                if (string.IsNullOrEmpty(openid))
                {
                    MessageBox.Show("未获取到微信 openid，请检查配置。");
                    return;
                }

                await EnsureUserTableAndColumnsResolvedAsync();

                string foundUser = null;
                using (var conn = new OdbcConnection(_connStr))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = $"SELECT TOP 1 {Q(colUsername)} FROM {Q(userTableName)} WHERE {Q(colWeChat)}=?";
                        cmd.Parameters.Add(new OdbcParameter { Value = openid });
                        var v = cmd.ExecuteScalar();
                        foundUser = (v == null || v == DBNull.Value) ? null : Convert.ToString(v);
                    }
                }

                if (!string.IsNullOrEmpty(foundUser))
                {
                    OpenMainAndHide(foundUser);
                    return;
                }

                string newUser = "wx_" + openid.Substring(0, Math.Min(8, openid.Length));
                string hashed = ComputeSha256Hex(Guid.NewGuid().ToString("N").Substring(0, 12));

                try
                {
                    using (var conn = new OdbcConnection(_connStr))
                    {
                        conn.Open();
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = $"INSERT INTO {Q(userTableName)} ({Q(colUsername)},{Q(colPassword)},{Q(colWeChat)}) VALUES (?,?,?)";
                            cmd.Parameters.Add(new OdbcParameter { Value = newUser });
                            cmd.Parameters.Add(new OdbcParameter { Value = hashed });
                            cmd.Parameters.Add(new OdbcParameter { Value = openid });
                            cmd.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show($"已为微信用户创建本地账号：{newUser}，请完善资料。");
                    OpenMainAndHide(newUser);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("创建本地用户失败: " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("微信登录失败: " + ex.Message);
            }
        }

        private async Task HandleQqCodeAsync(string code)
        {
            try
            {
                string tokenUrl = $"https://graph.qq.com/oauth2.0/token?grant_type=authorization_code&client_id={QqAppId}&client_secret={QqAppKey}&code={code}&redirect_uri={Uri.EscapeDataString(QqRedirectUri)}";
                var tokenResp = await _http.GetStringAsync(tokenUrl);
                var tokenQs = ParseQuery(tokenResp);
                string accessToken = tokenQs["access_token"];
                if (string.IsNullOrEmpty(accessToken))
                {
                    MessageBox.Show("获取 QQ access_token 失败: " + tokenResp);
                    return;
                }

                string meUrl = $"https://graph.qq.com/oauth2.0/me?access_token={accessToken}";
                var meResp = await _http.GetStringAsync(meUrl);
                var mstart = meResp.IndexOf('{');
                var mend = meResp.LastIndexOf('}');
                if (mstart < 0 || mend < 0)
                {
                    MessageBox.Show("解析 QQ openid 失败: " + meResp);
                    return;
                }
                var meJson = JObject.Parse(meResp.Substring(mstart, mend - mstart + 1));
                string openid = meJson.Value<string>("openid");

                await EnsureUserTableAndColumnsResolvedAsync();

                string foundUser = null;
                using (var conn = new OdbcConnection(_connStr))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = $"SELECT TOP 1 {Q(colUsername)} FROM {Q(userTableName)} WHERE {Q(colQQ)}=?";
                        cmd.Parameters.Add(new OdbcParameter { Value = openid });
                        var v = cmd.ExecuteScalar();
                        foundUser = (v == null || v == DBNull.Value) ? null : Convert.ToString(v);
                    }
                }

                if (!string.IsNullOrEmpty(foundUser))
                {
                    OpenMainAndHide(foundUser);
                    return;
                }

                string newUser = "qq_" + openid.Substring(0, Math.Min(8, openid.Length));
                string hashed = ComputeSha256Hex(Guid.NewGuid().ToString("N").Substring(0, 12));

                try
                {
                    using (var conn = new OdbcConnection(_connStr))
                    {
                        conn.Open();
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = $"INSERT INTO {Q(userTableName)} ({Q(colUsername)},{Q(colPassword)},{Q(colQQ)}) VALUES (?,?,?)";
                            cmd.Parameters.Add(new OdbcParameter { Value = newUser });
                            cmd.Parameters.Add(new OdbcParameter { Value = hashed });
                            cmd.Parameters.Add(new OdbcParameter { Value = openid });
                            cmd.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show($"已为 QQ 用户创建本地账号：{newUser}，请完善资料。");
                    OpenMainAndHide(newUser);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("创建本地用户失败: " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("QQ 登录失败: " + ex.Message);
            }
        }
        #endregion

        #region 表/列自动解析（Access/ODBC）
        private async Task EnsureUserTableAndColumnsResolvedAsync()
        {
            if (!string.IsNullOrEmpty(userTableName)) return;

            await Task.Run(() =>
            {
                using (var conn = new OdbcConnection(_connStr))
                {
                    conn.Open();

                    string[] candidates = new[] { "用户表", "用户登录表", "用户" };
                    foreach (var t in candidates)
                    {
                        if (TableExistsAccess(conn, t))
                        {
                            userTableName = t;
                            break;
                        }
                    }

                    if (string.IsNullOrEmpty(userTableName))
                        throw new Exception("未找到 用户表/用户登录表/用户。");

                    var cols = GetColumnNamesAccess(conn, userTableName);

                    string Find(params string[] tries)
                    {
                        foreach (var s in tries)
                        {
                            var f = cols.FirstOrDefault(c => string.Equals(c, s, StringComparison.OrdinalIgnoreCase));
                            if (f != null) return f;
                        }
                        return null;
                    }

                    colUsername = Find("用户名", "user", "username", "UserName", "账号", "账户") ?? cols.FirstOrDefault() ?? "用户名";
                    colPassword = Find("用户密码", "密码", "password", "pwd", "pass") ?? colPassword;
                    colAvatar = Find("用户头像", "头像", "avatar", "useravatar", "头像路径") ?? colAvatar;
                    colPhone = Find("手机号", "电话号码", "phone", "mobile") ?? colPhone;
                    colWeChat = Find("WeChatOpenId", "WeChat", "微信OpenId", "微信openid") ?? colWeChat;
                    colQQ = Find("QQOpenId", "QQ", "QQOpenid", "qq_openid") ?? colQQ;
                }
            });
        }

        // Access 标识符引用
        private static string Q(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier)) return "[]";
            return "[" + identifier.Replace("]", "]]") + "]";
        }

        private bool TableExistsAccess(OdbcConnection conn, string tableName)
        {
            // 优先用 schema（更稳），拿不到再尝试 SELECT
            try
            {
                var schema = conn.GetSchema("Tables");
                foreach (DataRow r in schema.Rows)
                {
                    var name = r["TABLE_NAME"] == DBNull.Value ? "" : Convert.ToString(r["TABLE_NAME"]);
                    var type = r.Table.Columns.Contains("TABLE_TYPE") ? Convert.ToString(r["TABLE_TYPE"]) : "";

                    if (string.Equals(name, tableName, StringComparison.OrdinalIgnoreCase))
                    {
                        // 过滤系统表
                        if (!string.IsNullOrEmpty(name) && name.StartsWith("MSys", StringComparison.OrdinalIgnoreCase))
                            return false;

                        // 常见：TABLE / VIEW。这里不强制限制，避免驱动返回不一致
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

        private List<string> GetColumnNamesAccess(OdbcConnection conn, string tableName)
        {
            var cols = new List<string>();
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

        private string GetCreateUserTableSql()
        {
            // Access/Jet/ACE 常见建表（不保证所有驱动都支持 IF NOT EXISTS）
            return
                "CREATE TABLE 用户 (" +
                "用户ID AUTOINCREMENT PRIMARY KEY, " +
                "用户名 TEXT(64) NOT NULL, " +
                "用户密码 TEXT(255) NOT NULL, " +
                "用户头像 TEXT(255), " +
                "手机号 TEXT(32), " +
                "WeChatOpenId TEXT(128), " +
                "QQOpenId TEXT(128), " +
                "创建时间 DATETIME" +
                ");\n" +
                "CREATE UNIQUE INDEX UK_用户_用户名 ON 用户(用户名);";
        }
        #endregion

        #region UI / 样式 / 辅助（未改动）
        private void SetPhoneMode(bool phoneMode)
        {
            panelPhoneLogin.Visible = phoneMode;
            panelCredPassword.Visible = !phoneMode;
            btnRegister.Text = phoneMode ? "取消" : "注册新用户";
        }

        private void ApplyVisualStyle()
        {
            try
            {
                Color primary = Color.FromArgb(35, 120, 215);
                Color light = Color.FromArgb(248, 249, 250);
                this.BackColor = light;

                panelLeft.BackColor = Color.White;
                panelRight.BackColor = light;
                panelRightCard.BackColor = Color.White;
                panelRightCard.Padding = new Padding(18);

                lblProjectTitle.Font = new Font("微软雅黑", 20F, FontStyle.Bold);
                lblProjectTitle.ForeColor = Color.FromArgb(36, 36, 36);
                lblProjectDesc.Font = new Font("微软雅黑", 10F);
                lblProjectDesc.ForeColor = Color.FromArgb(100, 100, 100);

                txtUsername.BorderStyle = BorderStyle.None;
                txtPassword.BorderStyle = BorderStyle.None;
                txtLoginPhone.BorderStyle = BorderStyle.FixedSingle;
                txtLoginCode.BorderStyle = BorderStyle.FixedSingle;

                StyleButton(btnLogin, primary);
                StyleButton(btnRegister, Color.FromArgb(120, 120, 120));
                StyleButton(btnPhoneModeToggle, Color.FromArgb(90, 90, 90));
                StyleButton(btnGetLoginCode, Color.FromArgb(50, 130, 200));
                StyleButton(btnWeChat, Color.FromArgb(72, 191, 114));
                StyleButton(btnQQ, Color.FromArgb(60, 120, 240));
                StyleButton(btnCloseThirdParty, Color.FromArgb(150, 150, 150));

                SetRoundedRegion(panelRightCard, 12);
                panelRightCard.SizeChanged += PanelRightCard_SizeChanged;

                if (lblPwdStrength != null)
                {
                    lblPwdStrength.Text = "";
                    lblPwdStrength.ForeColor = Color.Gray;
                }
            }
            catch { }
        }

        private void StyleButton(Button btn, Color bg)
        {
            if (btn == null) return;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = bg;
            btn.ForeColor = Color.White;
            btn.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            btn.Height = 34;
            btn.MouseEnter += (s, e) => btn.BackColor = ControlPaint.Light(bg, 0.12f);
            btn.MouseLeave += (s, e) => btn.BackColor = bg;
            btn.MouseDown += (s, e) => btn.BackColor = ControlPaint.Dark(bg, 0.08f);
            btn.MouseUp += (s, e) => btn.BackColor = bg;
            try { SetRoundedRegion(btn, 6); } catch { }
        }

        private void SetRoundedRegion(Control c, int radius)
        {
            if (c == null) return;
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

        private void PanelRightCard_SizeChanged(object sender, EventArgs e)
        {
            SetRoundedRegion(panelRightCard, 12);
        }
        #endregion

        #region 密码强度 / 显示密码 / 倒计时
        private void TxtPassword_TextChanged(object sender, EventArgs e)
        {
            UpdatePasswordStrengthIndicator(txtPassword.Text);
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
                    lblPwdStrength.Text = "";
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

        private void ChkShowPassword_CheckedChanged(object sender, EventArgs e)
        {
            txtPassword.UseSystemPasswordChar = !chkShowPassword.Checked;
        }

        private void SmsTimer_Tick(object sender, EventArgs e)
        {
            smsCountdown--;
            if (smsCountdown <= 0)
            {
                smsTimer.Stop();
                btnGetLoginCode.Enabled = true;
                btnGetLoginCode.Text = "获取验证码";
            }
            else
            {
                btnGetLoginCode.Text = $"重新获取({smsCountdown}s)";
            }
        }
        #endregion

        #region 辅助方法（哈希 / 生成验证码 / 打开主窗体 / 解析 Query）
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

        private bool IsLikelyHexSha256(string s)
        {
            if (string.IsNullOrEmpty(s) || s.Length != 64) return false;
            foreach (var ch in s)
            {
                bool ok = (ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F');
                if (!ok) return false;
            }
            return true;
        }

        private string Generate6DigitCode()
        {
            var r = new Random();
            return r.Next(0, 999999).ToString("D6");
        }

        private void OpenMainAndHide(string username)
        {
            var main = new MainForm(username);
            main.Show();
            this.Hide();
        }

        private NameValueCollection ParseQuery(string query)
        {
            var nvc = new NameValueCollection();
            if (string.IsNullOrEmpty(query)) return nvc;
            if (query.StartsWith("?")) query = query.Substring(1);
            foreach (var part in query.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var kv = part.Split(new[] { '=' }, 2);
                var key = Uri.UnescapeDataString(kv[0]);
                var val = kv.Length > 1 ? Uri.UnescapeDataString(kv[1]) : "";
                nvc.Add(key, val);
            }
            return nvc;
        }
        #endregion

        #region 窗体事件
        private void LoginForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            try
            {
                smsTimer?.Stop();
                smsTimer?.Dispose();
                smsTimer = null;
            }
            catch { }

            if (Application.OpenForms["MainForm"] == null)
                Application.Exit();
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {
        }
        #endregion
    }
}
