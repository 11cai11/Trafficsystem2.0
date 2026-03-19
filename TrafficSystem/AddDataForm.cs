using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace TrafficSystem
{
    public partial class AddDataForm : Form
    {
        public DataRow NewRow { get; private set; }
        private readonly DataTable _dataTable;

        public AddDataForm(DataTable dt)
        {
            InitializeComponent();
            _dataTable = dt;

            InitDynamicControls();

            // ✅ 关键：动态控件都创建完 + ClientSize 也定好了，再注册缩放基准
            // scaleFormClientSize:true -> 连对话框本身也等比变大/变小（你说“整个等比例放大”就是这个）
            UiZoom.Register(this, scaleFormClientSize: true);

            // ✅ 可选：启用 Ctrl + 鼠标滚轮缩放（想要就留着）
            UiZoom.EnableCtrlWheelZoom(this);

            // ✅ 可选：快捷键：Ctrl + 加号/减号/0
            this.KeyPreview = true;
            this.KeyDown += AddDataForm_KeyDown;
        }

        private void AddDataForm_KeyDown(object sender, KeyEventArgs e)
        {
            if ((Control.ModifierKeys & Keys.Control) != Keys.Control) return;

            // Ctrl + '+' / '=' 放大
            if (e.KeyCode == Keys.Oemplus || e.KeyCode == Keys.Add)
            {
                UiZoom.ZoomIn(this);
                e.Handled = true;
            }
            // Ctrl + '-' 缩小
            else if (e.KeyCode == Keys.OemMinus || e.KeyCode == Keys.Subtract)
            {
                UiZoom.ZoomOut(this);
                e.Handled = true;
            }
            // Ctrl + '0' 还原
            else if (e.KeyCode == Keys.D0 || e.KeyCode == Keys.NumPad0)
            {
                UiZoom.Reset(this);
                e.Handled = true;
            }
        }

        private void InitDynamicControls()
        {
            this.Text = "增加数据";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = new Font("微软雅黑", 9F);

            int y = 20;
            // 若列名过多自动换行布局：两列布局
            int leftXLabel = 20, leftXInput = 150;
            int rightXLabel = 400, rightXInput = 530;
            int perColumnHeight = 40;
            int half = Math.Max(1, (_dataTable.Columns.Count + 1) / 2);

            for (int i = 0; i < _dataTable.Columns.Count; i++)
            {
                var col = _dataTable.Columns[i];
                int columnIndex = i < half ? 0 : 1;
                int verticalIndex = i < half ? i : (i - half);

                int lblX = columnIndex == 0 ? leftXLabel : rightXLabel;
                int txtX = columnIndex == 0 ? leftXInput : rightXInput;
                int posY = 20 + verticalIndex * perColumnHeight;

                Label lbl = new Label
                {
                    Text = col.ColumnName,
                    Location = new Point(lblX, posY),
                    Size = new Size(120, 25),
                    Font = new Font("微软雅黑", 9F)
                };
                TextBox txt = new TextBox
                {
                    Name = $"txt_{col.ColumnName}",
                    Location = new Point(txtX, posY),
                    Size = new Size(180, 25),
                    Font = new Font("微软雅黑", 9F)
                };
                this.Controls.Add(lbl);
                this.Controls.Add(txt);
                y = Math.Max(y, posY + perColumnHeight);
            }

            // 按钮
            Button btnSave = new Button
            {
                Text = "保存",
                Location = new Point(150, y + 10),
                Size = new Size(80, 30),
                Font = new Font("微软雅黑", 9F),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White
            };
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            Button btnCancel = new Button
            {
                Text = "取消",
                Location = new Point(250, y + 10),
                Size = new Size(80, 30),
                Font = new Font("微软雅黑", 9F),
                BackColor = Color.Gray,
                ForeColor = Color.White
            };
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);

            this.ClientSize = new Size(740, y + 60);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            NewRow = _dataTable.NewRow();
            foreach (DataColumn col in _dataTable.Columns)
            {
                var controls = this.Controls.Find($"txt_{col.ColumnName}", false);
                if (controls.Length == 0) { NewRow[col.ColumnName] = DBNull.Value; continue; }
                TextBox txt = controls[0] as TextBox;
                var text = txt?.Text?.Trim();
                if (string.IsNullOrEmpty(text))
                    NewRow[col.ColumnName] = DBNull.Value;
                else
                    NewRow[col.ColumnName] = text;
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void AddDataForm_Load(object sender, EventArgs e)
        {

        }
    }
}
