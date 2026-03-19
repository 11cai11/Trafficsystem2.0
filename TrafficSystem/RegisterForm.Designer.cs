namespace TrafficSystem
{
    partial class RegisterForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Panel panelLeft;
        private System.Windows.Forms.PictureBox picLogo;
        private System.Windows.Forms.Label lblProjectTitle;
        private System.Windows.Forms.Label lblProjectDesc;

        private System.Windows.Forms.Panel panelRight;
        private System.Windows.Forms.Panel panelRightCard;

        private System.Windows.Forms.Label lblRegUsername;
        private System.Windows.Forms.TextBox txtRegUsername;
        private System.Windows.Forms.Label lblRegPassword;
        private System.Windows.Forms.TextBox txtRegPassword;
        private System.Windows.Forms.CheckBox chkShowRegPassword;
        private System.Windows.Forms.PictureBox picAvatarPreview;
        private System.Windows.Forms.Button btnChooseAvatar;
        private System.Windows.Forms.Button btnRegister;

        // 手机/验证码相关控件
        private System.Windows.Forms.Label lblRegPhone;
        private System.Windows.Forms.TextBox txtRegPhone;
        private System.Windows.Forms.Button btnGetRegCode;
        private System.Windows.Forms.TextBox txtRegCode;

        // 图形验证码控件 + 密码强度提示
        private System.Windows.Forms.PictureBox picCaptcha;
        private System.Windows.Forms.Button btnRefreshCaptcha;
        private System.Windows.Forms.TextBox txtRegCaptcha;
        private System.Windows.Forms.Label lblPwdStrength;

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
            this.label1 = new System.Windows.Forms.Label();
            this.lblRegUsername = new System.Windows.Forms.Label();
            this.txtRegUsername = new System.Windows.Forms.TextBox();
            this.lblRegPassword = new System.Windows.Forms.Label();
            this.txtRegPassword = new System.Windows.Forms.TextBox();
            this.chkShowRegPassword = new System.Windows.Forms.CheckBox();
            this.lblPwdStrength = new System.Windows.Forms.Label();
            this.picCaptcha = new System.Windows.Forms.PictureBox();
            this.btnRefreshCaptcha = new System.Windows.Forms.Button();
            this.txtRegCaptcha = new System.Windows.Forms.TextBox();
            this.lblRegPhone = new System.Windows.Forms.Label();
            this.txtRegPhone = new System.Windows.Forms.TextBox();
            this.btnGetRegCode = new System.Windows.Forms.Button();
            this.txtRegCode = new System.Windows.Forms.TextBox();
            this.picAvatarPreview = new System.Windows.Forms.PictureBox();
            this.btnChooseAvatar = new System.Windows.Forms.Button();
            this.btnRegister = new System.Windows.Forms.Button();
            this.panelLeft.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picLogo)).BeginInit();
            this.panelRight.SuspendLayout();
            this.panelRightCard.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picCaptcha)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picAvatarPreview)).BeginInit();
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
            this.panelLeft.Size = new System.Drawing.Size(540, 629);
            this.panelLeft.TabIndex = 1;
            // 
            // picLogo
            // 
            this.picLogo.Location = new System.Drawing.Point(12, 122);
            this.picLogo.Name = "picLogo";
            this.picLogo.Size = new System.Drawing.Size(160, 206);
            this.picLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picLogo.TabIndex = 2;
            this.picLogo.TabStop = false;
            // 
            // lblProjectTitle
            // 
            this.lblProjectTitle.Location = new System.Drawing.Point(207, 122);
            this.lblProjectTitle.Name = "lblProjectTitle";
            this.lblProjectTitle.Size = new System.Drawing.Size(300, 80);
            this.lblProjectTitle.TabIndex = 0;
            this.lblProjectTitle.Text = "基于轨迹数据的\n时空最优出行系统";
            // 
            // lblProjectDesc
            // 
            this.lblProjectDesc.Location = new System.Drawing.Point(207, 222);
            this.lblProjectDesc.Name = "lblProjectDesc";
            this.lblProjectDesc.Size = new System.Drawing.Size(300, 160);
            this.lblProjectDesc.TabIndex = 1;
            this.lblProjectDesc.Text = "在此注册新用户，上传头像用于主界面显示。";
            // 
            // panelRight
            // 
            this.panelRight.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            this.panelRight.Controls.Add(this.panelRightCard);
            this.panelRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelRight.Location = new System.Drawing.Point(540, 0);
            this.panelRight.Name = "panelRight";
            this.panelRight.Padding = new System.Windows.Forms.Padding(36);
            this.panelRight.Size = new System.Drawing.Size(517, 629);
            this.panelRight.TabIndex = 0;
            // 
            // panelRightCard
            // 
            this.panelRightCard.Controls.Add(this.label1);
            this.panelRightCard.Controls.Add(this.lblRegUsername);
            this.panelRightCard.Controls.Add(this.txtRegUsername);
            this.panelRightCard.Controls.Add(this.lblRegPassword);
            this.panelRightCard.Controls.Add(this.txtRegPassword);
            this.panelRightCard.Controls.Add(this.chkShowRegPassword);
            this.panelRightCard.Controls.Add(this.lblPwdStrength);
            this.panelRightCard.Controls.Add(this.picCaptcha);
            this.panelRightCard.Controls.Add(this.btnRefreshCaptcha);
            this.panelRightCard.Controls.Add(this.txtRegCaptcha);
            this.panelRightCard.Controls.Add(this.lblRegPhone);
            this.panelRightCard.Controls.Add(this.txtRegPhone);
            this.panelRightCard.Controls.Add(this.btnGetRegCode);
            this.panelRightCard.Controls.Add(this.txtRegCode);
            this.panelRightCard.Controls.Add(this.picAvatarPreview);
            this.panelRightCard.Controls.Add(this.btnChooseAvatar);
            this.panelRightCard.Controls.Add(this.btnRegister);
            this.panelRightCard.Location = new System.Drawing.Point(16, 49);
            this.panelRightCard.Name = "panelRightCard";
            this.panelRightCard.Size = new System.Drawing.Size(489, 549);
            this.panelRightCard.TabIndex = 0;
            this.panelRightCard.SizeChanged += new System.EventHandler(this.panelRightCard_SizeChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(22, 185);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(101, 12);
            this.label1.TabIndex = 21;
            this.label1.Text = "图片验证码如下：";
            // 
            // lblRegUsername
            // 
            this.lblRegUsername.Location = new System.Drawing.Point(22, 18);
            this.lblRegUsername.Name = "lblRegUsername";
            this.lblRegUsername.Size = new System.Drawing.Size(80, 22);
            this.lblRegUsername.TabIndex = 0;
            this.lblRegUsername.Text = "用户名：";
            // 
            // txtRegUsername
            // 
            this.txtRegUsername.Location = new System.Drawing.Point(22, 44);
            this.txtRegUsername.Name = "txtRegUsername";
            this.txtRegUsername.Size = new System.Drawing.Size(260, 21);
            this.txtRegUsername.TabIndex = 1;
            // 
            // lblRegPassword
            // 
            this.lblRegPassword.Location = new System.Drawing.Point(22, 74);
            this.lblRegPassword.Name = "lblRegPassword";
            this.lblRegPassword.Size = new System.Drawing.Size(80, 22);
            this.lblRegPassword.TabIndex = 2;
            this.lblRegPassword.Text = "密码：";
            // 
            // txtRegPassword
            // 
            this.txtRegPassword.Location = new System.Drawing.Point(22, 100);
            this.txtRegPassword.Name = "txtRegPassword";
            this.txtRegPassword.Size = new System.Drawing.Size(260, 21);
            this.txtRegPassword.TabIndex = 3;
            this.txtRegPassword.UseSystemPasswordChar = true;
            // 
            // chkShowRegPassword
            // 
            this.chkShowRegPassword.Location = new System.Drawing.Point(22, 128);
            this.chkShowRegPassword.Name = "chkShowRegPassword";
            this.chkShowRegPassword.Size = new System.Drawing.Size(88, 22);
            this.chkShowRegPassword.TabIndex = 4;
            this.chkShowRegPassword.Text = "显示密码";
            this.chkShowRegPassword.CheckedChanged += new System.EventHandler(this.chkShowRegPassword_CheckedChanged);
            // 
            // lblPwdStrength
            // 
            this.lblPwdStrength.Location = new System.Drawing.Point(22, 150);
            this.lblPwdStrength.Name = "lblPwdStrength";
            this.lblPwdStrength.Size = new System.Drawing.Size(260, 22);
            this.lblPwdStrength.TabIndex = 13;
            this.lblPwdStrength.Text = "密码强度：";
            // 
            // picCaptcha
            // 
            this.picCaptcha.Location = new System.Drawing.Point(22, 213);
            this.picCaptcha.Name = "picCaptcha";
            this.picCaptcha.Size = new System.Drawing.Size(144, 40);
            this.picCaptcha.TabIndex = 11;
            this.picCaptcha.TabStop = false;
            // 
            // btnRefreshCaptcha
            // 
            this.btnRefreshCaptcha.Location = new System.Drawing.Point(172, 221);
            this.btnRefreshCaptcha.Name = "btnRefreshCaptcha";
            this.btnRefreshCaptcha.Size = new System.Drawing.Size(32, 28);
            this.btnRefreshCaptcha.TabIndex = 14;
            this.btnRefreshCaptcha.Text = "↻";
            // 
            // txtRegCaptcha
            // 
            this.txtRegCaptcha.Location = new System.Drawing.Point(210, 226);
            this.txtRegCaptcha.Name = "txtRegCaptcha";
            this.txtRegCaptcha.Size = new System.Drawing.Size(92, 21);
            this.txtRegCaptcha.TabIndex = 12;
            // 
            // lblRegPhone
            // 
            this.lblRegPhone.Location = new System.Drawing.Point(22, 273);
            this.lblRegPhone.Name = "lblRegPhone";
            this.lblRegPhone.Size = new System.Drawing.Size(260, 22);
            this.lblRegPhone.TabIndex = 20;
            this.lblRegPhone.Text = "手机号（用于接收短信验证码）：";
            // 
            // txtRegPhone
            // 
            this.txtRegPhone.Location = new System.Drawing.Point(22, 297);
            this.txtRegPhone.Name = "txtRegPhone";
            this.txtRegPhone.Size = new System.Drawing.Size(180, 21);
            this.txtRegPhone.TabIndex = 8;
            // 
            // btnGetRegCode
            // 
            this.btnGetRegCode.Location = new System.Drawing.Point(210, 295);
            this.btnGetRegCode.Name = "btnGetRegCode";
            this.btnGetRegCode.Size = new System.Drawing.Size(82, 26);
            this.btnGetRegCode.TabIndex = 9;
            this.btnGetRegCode.Text = "获取验证码";
            // 
            // txtRegCode
            // 
            this.txtRegCode.Location = new System.Drawing.Point(22, 329);
            this.txtRegCode.Name = "txtRegCode";
            this.txtRegCode.Size = new System.Drawing.Size(270, 21);
            this.txtRegCode.TabIndex = 10;
            // 
            // picAvatarPreview
            // 
            this.picAvatarPreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picAvatarPreview.Location = new System.Drawing.Point(332, 18);
            this.picAvatarPreview.Name = "picAvatarPreview";
            this.picAvatarPreview.Size = new System.Drawing.Size(140, 192);
            this.picAvatarPreview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picAvatarPreview.TabIndex = 5;
            this.picAvatarPreview.TabStop = false;
            // 
            // btnChooseAvatar
            // 
            this.btnChooseAvatar.Location = new System.Drawing.Point(332, 216);
            this.btnChooseAvatar.Name = "btnChooseAvatar";
            this.btnChooseAvatar.Size = new System.Drawing.Size(140, 33);
            this.btnChooseAvatar.TabIndex = 6;
            this.btnChooseAvatar.Text = "选择头像";
            // 
            // btnRegister
            // 
            this.btnRegister.Location = new System.Drawing.Point(108, 394);
            this.btnRegister.Name = "btnRegister";
            this.btnRegister.Size = new System.Drawing.Size(302, 36);
            this.btnRegister.TabIndex = 7;
            this.btnRegister.Text = "注册";
            // 
            // RegisterForm
            // 
            this.ClientSize = new System.Drawing.Size(1057, 629);
            this.Controls.Add(this.panelRight);
            this.Controls.Add(this.panelLeft);
            this.Name = "RegisterForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "用户注册 - 基于轨迹数据的时空最优出行系统";
            this.Load += new System.EventHandler(this.RegisterForm_Load);
            this.panelLeft.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picLogo)).EndInit();
            this.panelRight.ResumeLayout(false);
            this.panelRightCard.ResumeLayout(false);
            this.panelRightCard.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picCaptcha)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picAvatarPreview)).EndInit();
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.Label label1;
    }
}
