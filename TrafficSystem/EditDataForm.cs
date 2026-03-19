using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace TrafficSystem
{
    public partial class EditDataForm : Form
    {
        public DataRow EditedRow { get; private set; }
        private readonly DataRow _originalRow;

        public EditDataForm(DataRow row)
        {
            InitializeComponent();
            _originalRow = row ?? throw new ArgumentNullException(nameof(row));
            EditedRow = row;

            InitDynamicControls();

            // ✅ 动态控件都创建完 + ClientSize 已确定后，再注册缩放基准
            // “整个等比例放大” -> scaleFormClientSize:true
            UiZoom.Register(this, scaleFormClientSize: true);

            // ✅ 可选：Ctrl + 滚轮缩放（想要就保留）
            UiZoom.EnableCtrlWheelZoom(this);

            // ✅ 可选：Ctrl + +/-/0 快捷键
            this.KeyPreview = true;
            this.KeyDown += EditDataForm_KeyDown;
        }

        private void EditDataForm_KeyDown(object sender, KeyEventArgs e)
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

        private void InitDynamicControls()
        {
            this.Text = "修改数据";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = new Font("微软雅黑", 9F);

            int y = 20;
            // 两列布局以处理较多字段
            int leftXLabel = 20, leftXInput = 150;
            int rightXLabel = 400, rightXInput = 530;
            int perColumnHeight = 40;
            var cols = _originalRow.Table.Columns;
            int half = Math.Max(1, (cols.Count + 1) / 2);

            for (int i = 0; i < cols.Count; i++)
            {
                var col = cols[i];
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
                    Text = _originalRow[col.ColumnName] == DBNull.Value ? "" : _originalRow[col.ColumnName].ToString(),
                    Font = new Font("微软雅黑", 9F)
                };
                this.Controls.Add(lbl);
                this.Controls.Add(txt);
                y = Math.Max(y, posY + perColumnHeight);
            }

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

            Button btnQuit = new Button
            {
                Text = "退出",
                Location = new Point(250, y + 10),
                Size = new Size(80, 30),
                Font = new Font("微软雅黑", 9F),
                BackColor = Color.Gray,
                ForeColor = Color.White
            };
            btnQuit.Click += (s, e) => this.Close();
            this.Controls.Add(btnQuit);

            this.ClientSize = new Size(740, y + 60);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            foreach (DataColumn col in _originalRow.Table.Columns)
            {
                var controls = this.Controls.Find($"txt_{col.ColumnName}", false);
                if (controls.Length == 0) continue;
                TextBox txt = controls[0] as TextBox;
                var text = txt?.Text?.Trim();
                if (string.IsNullOrEmpty(text))
                    EditedRow[col.ColumnName] = DBNull.Value;
                else
                    EditedRow[col.ColumnName] = text;
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void EditDataForm_Load(object sender, EventArgs e)
        {

        }
    }
}
