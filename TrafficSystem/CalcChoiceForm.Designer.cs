// 文件：CalcChoiceForm.Designer.cs
namespace TrafficSystem
{
    partial class CalcChoiceForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // CalcChoiceForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(360, 220);
            this.Name = "CalcChoiceForm";
            this.Text = "轨迹数据计算 - 算法选择";
            this.Load += new System.EventHandler(this.CalcChoiceForm_Load);
            this.ResumeLayout(false);
        }


        #endregion
    }
}
