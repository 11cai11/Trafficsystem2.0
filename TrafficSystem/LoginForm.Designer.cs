namespace TrafficSystem
{
    partial class LoginForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Panel panelLeft;
        private System.Windows.Forms.PictureBox picLogo;
        private System.Windows.Forms.Label lblProjectTitle;
        private System.Windows.Forms.Label lblProjectDesc;

        private System.Windows.Forms.Panel panelRight;
        private System.Windows.Forms.Panel panelRightCard;

        // credential 区（用户名/密码 或 手机验证码）
        private System.Windows.Forms.Panel panelCredential; // 新：承载 credential 的容器
        private System.Windows.Forms.Panel panelCredPassword; // 用户名/密码表单
        private System.Windows.Forms.Label lblUsername;
        private System.Windows.Forms.TextBox txtUsername;
        private System.Windows.Forms.Label lblPassword;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.CheckBox chkShowPassword;
        private System.Windows.Forms.Label lblPwdStrength;
        private System.Windows.Forms.Label lblHint;

        private System.Windows.Forms.Panel panelPhoneLogin; // 手机验证码表单（与密码表单同位置切换）
        private System.Windows.Forms.Label lblLoginPhone;
        private System.Windows.Forms.TextBox txtLoginPhone;
        private System.Windows.Forms.Label lblLoginCode;
        private System.Windows.Forms.TextBox txtLoginCode;
        private System.Windows.Forms.Button btnGetLoginCode;

        // credential 操作按钮（登录 / 注册or取消）
        private System.Windows.Forms.Panel panelCredButtons;
        private System.Windows.Forms.Button btnLogin;
        private System.Windows.Forms.Button btnRegister;

        // 按钮行（在 credential 下面）
        private System.Windows.Forms.Panel panelButtonsRow;
        private System.Windows.Forms.Button btnPhoneModeToggle;
        private System.Windows.Forms.Button btnWeChat;
        private System.Windows.Forms.Button btnQQ;

        // 第三方展示区（在按钮行下面）
        private System.Windows.Forms.Panel panelThirdPartyArea;
        private System.Windows.Forms.Label lblThirdPartyTitle;
        private System.Windows.Forms.WebBrowser webBrowserThirdParty;
        private System.Windows.Forms.PictureBox pictureBoxThirdParty;
        private System.Windows.Forms.Button btnCloseThirdParty;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.panelLeft = new System.Windows.Forms.Panel();
            this.picLogo = new System.Windows.Forms.PictureBox();
            this.lblProjectTitle = new System.Windows.Forms.Label();
            this.lblProjectDesc = new System.Windows.Forms.Label();
            this.panelRight = new System.Windows.Forms.Panel();
            this.panelRightCard = new System.Windows.Forms.Panel();
            this.panelCredential = new System.Windows.Forms.Panel();
            this.panelCredPassword = new System.Windows.Forms.Panel();
            this.lblUsername = new System.Windows.Forms.Label();
            this.txtUsername = new System.Windows.Forms.TextBox();
            this.lblPassword = new System.Windows.Forms.Label();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.chkShowPassword = new System.Windows.Forms.CheckBox();
            this.lblPwdStrength = new System.Windows.Forms.Label();
            this.lblHint = new System.Windows.Forms.Label();
            this.panelPhoneLogin = new System.Windows.Forms.Panel();
            this.lblLoginPhone = new System.Windows.Forms.Label();
            this.txtLoginPhone = new System.Windows.Forms.TextBox();
            this.lblLoginCode = new System.Windows.Forms.Label();
            this.txtLoginCode = new System.Windows.Forms.TextBox();
            this.btnGetLoginCode = new System.Windows.Forms.Button();
            this.panelCredButtons = new System.Windows.Forms.Panel();
            this.btnLogin = new System.Windows.Forms.Button();
            this.btnRegister = new System.Windows.Forms.Button();
            this.panelButtonsRow = new System.Windows.Forms.Panel();
            this.btnPhoneModeToggle = new System.Windows.Forms.Button();
            this.btnWeChat = new System.Windows.Forms.Button();
            this.btnQQ = new System.Windows.Forms.Button();
            this.panelThirdPartyArea = new System.Windows.Forms.Panel();
            this.lblThirdPartyTitle = new System.Windows.Forms.Label();
            this.webBrowserThirdParty = new System.Windows.Forms.WebBrowser();
            this.pictureBoxThirdParty = new System.Windows.Forms.PictureBox();
            this.btnCloseThirdParty = new System.Windows.Forms.Button();
            this.panelLeft.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picLogo)).BeginInit();
            this.panelRight.SuspendLayout();
            this.panelRightCard.SuspendLayout();
            this.panelCredential.SuspendLayout();
            this.panelCredPassword.SuspendLayout();
            this.panelPhoneLogin.SuspendLayout();
            this.panelCredButtons.SuspendLayout();
            this.panelButtonsRow.SuspendLayout();
            this.panelThirdPartyArea.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxThirdParty)).BeginInit();
            this.SuspendLayout();
            // 
            // panelLeft
            // 
            this.panelLeft.BackColor = System.Drawing.Color.White;
            this.panelLeft.Controls.Add(this.picLogo);
            this.panelLeft.Controls.Add(this.lblProjectTitle);
            this.panelLeft.Controls.Add(this.lblProjectDesc);
            this.panelLeft.Dock = System.Windows.Forms.DockStyle.Left;
            this.panelLeft.Location = new System.Drawing.Point(0, 0);
            this.panelLeft.Name = "panelLeft";
            this.panelLeft.Padding = new System.Windows.Forms.Padding(30);
            this.panelLeft.Size = new System.Drawing.Size(540, 693);
            this.panelLeft.TabIndex = 1;
            // 
            // picLogo
            // 
            this.picLogo.Location = new System.Drawing.Point(33, 96);
            this.picLogo.Name = "picLogo";
            this.picLogo.Size = new System.Drawing.Size(160, 206);
            this.picLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picLogo.TabIndex = 0;
            this.picLogo.TabStop = false;
            // 
            // lblProjectTitle
            // 
            this.lblProjectTitle.Font = new System.Drawing.Font("微软雅黑", 20F, System.Drawing.FontStyle.Bold);
            this.lblProjectTitle.Location = new System.Drawing.Point(207, 96);
            this.lblProjectTitle.Name = "lblProjectTitle";
            this.lblProjectTitle.Size = new System.Drawing.Size(300, 80);
            this.lblProjectTitle.TabIndex = 1;
            this.lblProjectTitle.Text = "基于轨迹数据的\n时空最优出行系统";
            // 
            // lblProjectDesc
            // 
            this.lblProjectDesc.Font = new System.Drawing.Font("微软雅黑", 10F);
            this.lblProjectDesc.Location = new System.Drawing.Point(207, 196);
            this.lblProjectDesc.Name = "lblProjectDesc";
            this.lblProjectDesc.Size = new System.Drawing.Size(300, 160);
            this.lblProjectDesc.TabIndex = 2;
            this.lblProjectDesc.Text = "用途：轨迹读取、最短路径/时间计算、可视化展示与基本用户管理。";
            // 
            // panelRight
            // 
            this.panelRight.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            this.panelRight.Controls.Add(this.panelRightCard);
            this.panelRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelRight.Location = new System.Drawing.Point(540, 0);
            this.panelRight.Name = "panelRight";
            this.panelRight.Padding = new System.Windows.Forms.Padding(36);
            this.panelRight.Size = new System.Drawing.Size(407, 693);
            this.panelRight.TabIndex = 0;
            // 
            // panelRightCard
            // 
            this.panelRightCard.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.panelRightCard.Controls.Add(this.panelCredential);
            this.panelRightCard.Controls.Add(this.panelButtonsRow);
            this.panelRightCard.Controls.Add(this.panelThirdPartyArea);
            this.panelRightCard.Location = new System.Drawing.Point(28, 0);
            this.panelRightCard.Name = "panelRightCard";
            this.panelRightCard.Size = new System.Drawing.Size(340, 693);
            this.panelRightCard.TabIndex = 0;
            // 
            // panelCredential
            // 
            this.panelCredential.Controls.Add(this.panelCredPassword);
            this.panelCredential.Controls.Add(this.panelPhoneLogin);
            this.panelCredential.Controls.Add(this.panelCredButtons);
            this.panelCredential.Location = new System.Drawing.Point(10, 10);
            this.panelCredential.Name = "panelCredential";
            this.panelCredential.Size = new System.Drawing.Size(320, 240);
            this.panelCredential.TabIndex = 0;
            // 
            // panelCredPassword
            // 
            this.panelCredPassword.Controls.Add(this.lblUsername);
            this.panelCredPassword.Controls.Add(this.txtUsername);
            this.panelCredPassword.Controls.Add(this.lblPassword);
            this.panelCredPassword.Controls.Add(this.txtPassword);
            this.panelCredPassword.Controls.Add(this.chkShowPassword);
            this.panelCredPassword.Controls.Add(this.lblPwdStrength);
            this.panelCredPassword.Controls.Add(this.lblHint);
            this.panelCredPassword.Location = new System.Drawing.Point(0, 0);
            this.panelCredPassword.Name = "panelCredPassword";
            this.panelCredPassword.Size = new System.Drawing.Size(320, 180);
            this.panelCredPassword.TabIndex = 0;
            // 
            // lblUsername
            // 
            this.lblUsername.Location = new System.Drawing.Point(8, 6);
            this.lblUsername.Name = "lblUsername";
            this.lblUsername.Size = new System.Drawing.Size(80, 22);
            this.lblUsername.TabIndex = 0;
            this.lblUsername.Text = "用户名：";
            // 
            // txtUsername
            // 
            this.txtUsername.Location = new System.Drawing.Point(8, 32);
            this.txtUsername.Name = "txtUsername";
            this.txtUsername.Size = new System.Drawing.Size(300, 21);
            this.txtUsername.TabIndex = 1;
            // 
            // lblPassword
            // 
            this.lblPassword.Location = new System.Drawing.Point(8, 66);
            this.lblPassword.Name = "lblPassword";
            this.lblPassword.Size = new System.Drawing.Size(80, 22);
            this.lblPassword.TabIndex = 2;
            this.lblPassword.Text = "密码：";
            // 
            // txtPassword
            // 
            this.txtPassword.Location = new System.Drawing.Point(8, 92);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.Size = new System.Drawing.Size(300, 21);
            this.txtPassword.TabIndex = 3;
            this.txtPassword.UseSystemPasswordChar = true;
            // 
            // chkShowPassword
            // 
            this.chkShowPassword.Location = new System.Drawing.Point(8, 122);
            this.chkShowPassword.Name = "chkShowPassword";
            this.chkShowPassword.Size = new System.Drawing.Size(88, 22);
            this.chkShowPassword.TabIndex = 4;
            this.chkShowPassword.Text = "显示密码";
            // 
            // lblPwdStrength
            // 
            this.lblPwdStrength.Location = new System.Drawing.Point(110, 122);
            this.lblPwdStrength.Name = "lblPwdStrength";
            this.lblPwdStrength.Size = new System.Drawing.Size(200, 22);
            this.lblPwdStrength.TabIndex = 11;
            // 
            // lblHint
            // 
            this.lblHint.Location = new System.Drawing.Point(8, 150);
            this.lblHint.Name = "lblHint";
            this.lblHint.Size = new System.Drawing.Size(300, 22);
            this.lblHint.TabIndex = 5;
            this.lblHint.Text = "未注册？请点击下面的“注册新用户”创建账号。";
            // 
            // panelPhoneLogin
            // 
            this.panelPhoneLogin.Controls.Add(this.lblLoginPhone);
            this.panelPhoneLogin.Controls.Add(this.txtLoginPhone);
            this.panelPhoneLogin.Controls.Add(this.lblLoginCode);
            this.panelPhoneLogin.Controls.Add(this.txtLoginCode);
            this.panelPhoneLogin.Controls.Add(this.btnGetLoginCode);
            this.panelPhoneLogin.Location = new System.Drawing.Point(0, 0);
            this.panelPhoneLogin.Name = "panelPhoneLogin";
            this.panelPhoneLogin.Size = new System.Drawing.Size(320, 140);
            this.panelPhoneLogin.TabIndex = 1;
            this.panelPhoneLogin.Visible = false;
            // 
            // lblLoginPhone
            // 
            this.lblLoginPhone.Location = new System.Drawing.Point(8, 6);
            this.lblLoginPhone.Name = "lblLoginPhone";
            this.lblLoginPhone.Size = new System.Drawing.Size(300, 22);
            this.lblLoginPhone.TabIndex = 0;
            this.lblLoginPhone.Text = "手机号/用户名（用于接收验证码）：";
            // 
            // txtLoginPhone
            // 
            this.txtLoginPhone.Location = new System.Drawing.Point(8, 32);
            this.txtLoginPhone.Name = "txtLoginPhone";
            this.txtLoginPhone.Size = new System.Drawing.Size(210, 21);
            this.txtLoginPhone.TabIndex = 1;
            // 
            // lblLoginCode
            // 
            this.lblLoginCode.Location = new System.Drawing.Point(8, 66);
            this.lblLoginCode.Name = "lblLoginCode";
            this.lblLoginCode.Size = new System.Drawing.Size(80, 22);
            this.lblLoginCode.TabIndex = 2;
            this.lblLoginCode.Text = "验证码：";
            // 
            // txtLoginCode
            // 
            this.txtLoginCode.Location = new System.Drawing.Point(8, 92);
            this.txtLoginCode.Name = "txtLoginCode";
            this.txtLoginCode.Size = new System.Drawing.Size(140, 21);
            this.txtLoginCode.TabIndex = 3;
            // 
            // btnGetLoginCode
            // 
            this.btnGetLoginCode.Location = new System.Drawing.Point(160, 88);
            this.btnGetLoginCode.Name = "btnGetLoginCode";
            this.btnGetLoginCode.Size = new System.Drawing.Size(120, 28);
            this.btnGetLoginCode.TabIndex = 4;
            this.btnGetLoginCode.Text = "获取验证码";
            // 
            // panelCredButtons
            // 
            this.panelCredButtons.Controls.Add(this.btnLogin);
            this.panelCredButtons.Controls.Add(this.btnRegister);
            this.panelCredButtons.Location = new System.Drawing.Point(0, 180);
            this.panelCredButtons.Name = "panelCredButtons";
            this.panelCredButtons.Size = new System.Drawing.Size(320, 56);
            this.panelCredButtons.TabIndex = 2;
            // 
            // btnLogin
            // 
            this.btnLogin.Location = new System.Drawing.Point(8, 10);
            this.btnLogin.Name = "btnLogin";
            this.btnLogin.Size = new System.Drawing.Size(150, 36);
            this.btnLogin.TabIndex = 6;
            this.btnLogin.Text = "登录";
            // 
            // btnRegister
            // 
            this.btnRegister.Location = new System.Drawing.Point(168, 10);
            this.btnRegister.Name = "btnRegister";
            this.btnRegister.Size = new System.Drawing.Size(140, 36);
            this.btnRegister.TabIndex = 7;
            this.btnRegister.Text = "注册新用户";
            // 
            // panelButtonsRow
            // 
            this.panelButtonsRow.Controls.Add(this.btnPhoneModeToggle);
            this.panelButtonsRow.Controls.Add(this.btnWeChat);
            this.panelButtonsRow.Controls.Add(this.btnQQ);
            this.panelButtonsRow.Location = new System.Drawing.Point(10, 260);
            this.panelButtonsRow.Name = "panelButtonsRow";
            this.panelButtonsRow.Size = new System.Drawing.Size(320, 44);
            this.panelButtonsRow.TabIndex = 1;
            // 
            // btnPhoneModeToggle
            // 
            this.btnPhoneModeToggle.Location = new System.Drawing.Point(0, 6);
            this.btnPhoneModeToggle.Name = "btnPhoneModeToggle";
            this.btnPhoneModeToggle.Size = new System.Drawing.Size(100, 34);
            this.btnPhoneModeToggle.TabIndex = 8;
            this.btnPhoneModeToggle.Text = "手机登录";
            // 
            // btnWeChat
            // 
            this.btnWeChat.Location = new System.Drawing.Point(110, 6);
            this.btnWeChat.Name = "btnWeChat";
            this.btnWeChat.Size = new System.Drawing.Size(100, 34);
            this.btnWeChat.TabIndex = 9;
            this.btnWeChat.Text = "微信登录";
            // 
            // btnQQ
            // 
            this.btnQQ.Location = new System.Drawing.Point(220, 6);
            this.btnQQ.Name = "btnQQ";
            this.btnQQ.Size = new System.Drawing.Size(100, 34);
            this.btnQQ.TabIndex = 10;
            this.btnQQ.Text = "QQ 登录";
            // 
            // panelThirdPartyArea
            // 
            this.panelThirdPartyArea.Controls.Add(this.lblThirdPartyTitle);
            this.panelThirdPartyArea.Controls.Add(this.webBrowserThirdParty);
            this.panelThirdPartyArea.Controls.Add(this.pictureBoxThirdParty);
            this.panelThirdPartyArea.Controls.Add(this.btnCloseThirdParty);
            this.panelThirdPartyArea.Location = new System.Drawing.Point(10, 312);
            this.panelThirdPartyArea.Name = "panelThirdPartyArea";
            this.panelThirdPartyArea.Size = new System.Drawing.Size(320, 369);
            this.panelThirdPartyArea.TabIndex = 2;
            this.panelThirdPartyArea.Visible = false;
            // 
            // lblThirdPartyTitle
            // 
            this.lblThirdPartyTitle.Font = new System.Drawing.Font("微软雅黑", 11F, System.Drawing.FontStyle.Bold);
            this.lblThirdPartyTitle.Location = new System.Drawing.Point(8, 6);
            this.lblThirdPartyTitle.Name = "lblThirdPartyTitle";
            this.lblThirdPartyTitle.Size = new System.Drawing.Size(304, 22);
            this.lblThirdPartyTitle.TabIndex = 20;
            this.lblThirdPartyTitle.Text = "第三方登录";
            this.lblThirdPartyTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // webBrowserThirdParty
            // 
            this.webBrowserThirdParty.Location = new System.Drawing.Point(8, 30);
            this.webBrowserThirdParty.MinimumSize = new System.Drawing.Size(20, 20);
            this.webBrowserThirdParty.Name = "webBrowserThirdParty";
            this.webBrowserThirdParty.Size = new System.Drawing.Size(304, 280);
            this.webBrowserThirdParty.TabIndex = 21;
            // 
            // pictureBoxThirdParty
            // 
            this.pictureBoxThirdParty.Location = new System.Drawing.Point(8, 30);
            this.pictureBoxThirdParty.Name = "pictureBoxThirdParty";
            this.pictureBoxThirdParty.Size = new System.Drawing.Size(304, 50);
            this.pictureBoxThirdParty.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxThirdParty.TabIndex = 22;
            this.pictureBoxThirdParty.TabStop = false;
            this.pictureBoxThirdParty.Visible = false;
            // 
            // btnCloseThirdParty
            // 
            this.btnCloseThirdParty.Location = new System.Drawing.Point(8, 316);
            this.btnCloseThirdParty.Name = "btnCloseThirdParty";
            this.btnCloseThirdParty.Size = new System.Drawing.Size(304, 26);
            this.btnCloseThirdParty.TabIndex = 23;
            this.btnCloseThirdParty.Text = "关闭";
            // 
            // LoginForm
            // 
            this.ClientSize = new System.Drawing.Size(947, 693);
            this.Controls.Add(this.panelRight);
            this.Controls.Add(this.panelLeft);
            this.Name = "LoginForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "系统登录 - 基于轨迹数据的时空最优出行系统";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.LoginForm_FormClosed);
            this.Load += new System.EventHandler(this.LoginForm_Load);
            this.panelLeft.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picLogo)).EndInit();
            this.panelRight.ResumeLayout(false);
            this.panelRightCard.ResumeLayout(false);
            this.panelCredential.ResumeLayout(false);
            this.panelCredPassword.ResumeLayout(false);
            this.panelCredPassword.PerformLayout();
            this.panelPhoneLogin.ResumeLayout(false);
            this.panelPhoneLogin.PerformLayout();
            this.panelCredButtons.ResumeLayout(false);
            this.panelButtonsRow.ResumeLayout(false);
            this.panelThirdPartyArea.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxThirdParty)).EndInit();
            this.ResumeLayout(false);

        }
    }
}