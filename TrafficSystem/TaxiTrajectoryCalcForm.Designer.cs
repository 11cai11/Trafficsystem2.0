// 文件：TaxiTrajectoryCalcForm.Designer.cs
namespace TrafficSystem
{
    partial class TaxiTrajectoryCalcForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // TaxiTrajectoryCalcForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(920, 620);
            this.Name = "TaxiTrajectoryCalcForm";
            this.Text = "出租车轨迹计算（速度/方位角 + 距离）";
            this.Load += new System.EventHandler(this.TaxiTrajectoryCalcForm_Load);
            this.ResumeLayout(false);
        }
        #endregion
    }
}
