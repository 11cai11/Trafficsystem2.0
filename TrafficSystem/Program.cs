using System;
using System.Threading;
using System.Windows.Forms;

namespace TrafficSystem
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 让未处理异常弹窗显示出来（否则就是“打不开”）
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            Application.ThreadException += (s, e) =>
            {
                MessageBox.Show(e.Exception.ToString(), "ThreadException", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                MessageBox.Show(e.ExceptionObject?.ToString() ?? "Unknown", "UnhandledException", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            try
            {
                // ✅ 全局等比例缩放：拖大/拖小 -> 控件与字体同步缩放（不改 Designer）
                // 前提：项目里已添加 UiZoom.cs（命名空间同为 TrafficSystem）
                UiZoom.EnableForApplication(keepAspect: true);

                // 这里保持你真实启动窗体（比如 LoginForm / MainForm）
                Application.Run(new LoginForm());
                // Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Main Crash", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
