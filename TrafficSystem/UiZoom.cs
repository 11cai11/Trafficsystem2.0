// 文件：UiZoom.cs（最终版）
// ✅ 全局缩放：拖动窗体 -> 控件/字体同步等比缩放
// ✅ 输入框字体同步变大（TextBox）
// ✅ 按钮不裁字：TableLayoutPanel 中按钮 Dock=Fill + MinimumSize
// ✅ 自动修复布局：把“关键 TableLayoutPanel”的 Absolute/AutoSize 列行改 Percent（解决你截图里按钮小块）
// ✅ 不改 Designer
// 兼容：.NET Framework 4.7.2 + C# 7.3

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace TrafficSystem
{
    public static class UiZoom
    {
        private sealed class FormState
        {
            public bool Inited;
            public bool InApply;

            public Size BaseClientSize;
            public float ManualScale = 1f;
            public float LastScale = 1f;

            // true: min(sx,sy)；false: sqrt(sx*sy) 更“填满”
            public bool KeepAspect = true;

            public bool WheelZoomEnabled;
            public Timer DebounceTimer;

            // 防止重复“修布局”
            public bool LayoutFixedOnce;
            public bool ButtonTablesFixedOnce;
        }

        private sealed class ControlState
        {
            public Font BaseFont;
            public Font LastScaledFont;

            // DataGridView 基准
            public bool IsDgv;
            public int DgvHeaderHeight;
            public int DgvRowHeight;
            public int[] DgvColWidths;
            public Font DgvBaseDefaultFont;
            public Font DgvBaseHeaderFont;
            public Font DgvLastDefaultFont;
            public Font DgvLastHeaderFont;

            // ListView 基准
            public bool IsLv;
            public int[] LvColWidths;
        }

        private static readonly ConditionalWeakTable<Form, FormState> _fs =
            new ConditionalWeakTable<Form, FormState>();

        private static readonly ConditionalWeakTable<Control, ControlState> _cs =
            new ConditionalWeakTable<Control, ControlState>();

        private static Timer _scanTimer;
        private static bool _globalEnabled;

        private static bool _wheelFilterInstalled;
        private static readonly CtrlWheelFilter _wheelFilter = new CtrlWheelFilter();

        // =========================
        // 对外 API：全局启用
        // =========================
        public static void EnableForApplication(bool keepAspect = true, int scanIntervalMs = 250)
        {
            if (_globalEnabled) return;
            _globalEnabled = true;

            _scanTimer = new Timer { Interval = Math.Max(100, scanIntervalMs) };
            _scanTimer.Tick += (s, e) =>
            {
                try
                {
                    foreach (Form f in Application.OpenForms)
                    {
                        if (f == null) continue;
                        Attach(f, keepAspect);
                    }
                }
                catch { }
            };
            _scanTimer.Start();
        }

        // 兼容你项目里已有调用
        public static void Register(Form form, bool keepAspect = true, bool scaleFormClientSize = true, bool enableCtrlWheelZoom = false)
        {
            Attach(form, keepAspect);
            if (enableCtrlWheelZoom) EnableCtrlWheelZoom(form);
        }

        public static void EnableCtrlWheelZoom(Form form)
        {
            if (form == null) return;
            Attach(form, keepAspect: true);

            if (_fs.TryGetValue(form, out var fs))
                fs.WheelZoomEnabled = true;

            EnsureWheelFilterInstalled();
        }

        public static void ZoomIn(Form form, float step = 0.1f)
        {
            if (form == null) return;
            if (!_fs.TryGetValue(form, out var fs)) return;

            fs.ManualScale = Clamp(fs.ManualScale * (1f + Math.Max(0.01f, step)), 0.2f, 5f);
            ApplyScale(form);
        }

        public static void ZoomOut(Form form, float step = 0.1f)
        {
            if (form == null) return;
            if (!_fs.TryGetValue(form, out var fs)) return;

            fs.ManualScale = Clamp(fs.ManualScale / (1f + Math.Max(0.01f, step)), 0.2f, 5f);
            ApplyScale(form);
        }

        public static void Reset(Form form)
        {
            if (form == null) return;
            if (!_fs.TryGetValue(form, out var fs)) return;

            fs.ManualScale = 1f;
            ApplyScale(form);
        }

        public static void ZoomIn() => ZoomIn(Form.ActiveForm);
        public static void ZoomOut() => ZoomOut(Form.ActiveForm);
        public static void Reset() => Reset(Form.ActiveForm);

        // =========================
        // 核心：Attach
        // =========================
        public static void Attach(Form form, bool keepAspect = true)
        {
            if (form == null) return;

            if (_fs.TryGetValue(form, out var exist))
            {
                exist.KeepAspect = keepAspect;
                return;
            }

            var fs = new FormState { KeepAspect = keepAspect };
            _fs.Add(form, fs);

            // 避免 WinForms AutoScale 叠加
            form.AutoScaleMode = AutoScaleMode.None;

            fs.DebounceTimer = new Timer { Interval = 60 };
            fs.DebounceTimer.Tick += (s, e) =>
            {
                fs.DebounceTimer.Stop();
                ApplyScale(form);
            };

            form.Shown += (s, e) =>
            {
                InitIfNeeded(form);

                // ✅ 第一次显示时就把布局修一遍（解决 TableLayout 里按钮变小块）
                FixRootLayoutsOnce(form);
                FixButtonTablesOnce(form);

                ApplyScale(form);
            };

            form.Resize += (s, e) =>
            {
                if (!fs.Inited) return;
                if (form.WindowState == FormWindowState.Minimized) return;

                fs.DebounceTimer.Stop();
                fs.DebounceTimer.Start();
            };

            form.FormClosed += (s, e) =>
            {
                try { fs.DebounceTimer?.Stop(); fs.DebounceTimer?.Dispose(); } catch { }
                ReleaseScaledFonts(form);
            };
        }

        private static void InitIfNeeded(Form form)
        {
            if (!_fs.TryGetValue(form, out var fs)) return;
            if (fs.Inited) return;

            if (form.ClientSize.Width <= 0 || form.ClientSize.Height <= 0) return;

            // 最大化启动用 RestoreBounds 估基准
            if (form.WindowState == FormWindowState.Maximized && form.RestoreBounds.Width > 0)
            {
                fs.BaseClientSize = new Size(
                    Math.Max(1, form.RestoreBounds.Width - 16),
                    Math.Max(1, form.RestoreBounds.Height - 39));
            }
            else
            {
                fs.BaseClientSize = form.ClientSize;
            }

            CaptureControlTree(form);

            fs.LastScale = 1f;
            fs.Inited = true;
        }

        // =========================
        // 缩放主流程
        // =========================
        private static void ApplyScale(Form form)
        {
            if (form == null) return;
            if (!_fs.TryGetValue(form, out var fs)) return;
            if (!fs.Inited) return;
            if (fs.InApply) return;

            float sx = (float)form.ClientSize.Width / Math.Max(1, fs.BaseClientSize.Width);
            float sy = (float)form.ClientSize.Height / Math.Max(1, fs.BaseClientSize.Height);

            float baseScale = fs.KeepAspect ? Math.Min(sx, sy) : (float)Math.Sqrt(Math.Max(0.0001, sx * sy));
            baseScale = Clamp(baseScale, 0.2f, 5f);

            float targetScale = Clamp(baseScale * fs.ManualScale, 0.2f, 5f);
            float ratio = targetScale / Math.Max(0.0001f, fs.LastScale);

            // 缩放变化很小就不做 Scale，但仍要确保 TableLayout 不回弹
            if (Math.Abs(ratio - 1f) < 0.002f)
            {
                // 拖动窗口时，有些 TableLayout 会重新 Layout，把按钮挤回去
                FixButtonTablesOnce(form, force: true);
                return;
            }

            fs.InApply = true;
            try
            {
                form.SuspendLayout();

                // 1) 控件树几何缩放（增量）
                foreach (Control top in form.Controls.Cast<Control>().ToArray())
                {
                    try { top.Scale(new SizeF(ratio, ratio)); } catch { }
                }

                // 2) 字体按“基准字体 * targetScale”统一重算（输入框字也会变大）
                ApplyScaledFonts(form, targetScale);

                // 3) 特殊控件（DGV/LV 列宽行高）
                ApplyScaledSpecialControls(form, targetScale);

                // 4) 修复按钮裁切（TableLayout/FlowLayout 内 Dock=Fill）
                AutoFitButtons(form, targetScale);

                // 5) 防止布局回弹：再修一次按钮表格（必要）
                FixButtonTablesOnce(form, force: true);

                fs.LastScale = targetScale;
            }
            finally
            {
                form.ResumeLayout(true);
                fs.InApply = false;
            }
        }

        // =========================
        // ✅ 关键：把根布局 TableLayout 改成 Percent（减少中间空白、让右侧区可变宽）
        // =========================
        private static void FixRootLayoutsOnce(Form form)
        {
            if (!_fs.TryGetValue(form, out var fs)) return;
            if (fs.LayoutFixedOnce) return;

            // 只修“占据窗体大部分面积”的顶层 TableLayoutPanel（很稳）
            foreach (Control c in form.Controls)
            {
                if (c is TableLayoutPanel tlp)
                {
                    if (tlp.Width >= form.ClientSize.Width * 0.7 && tlp.Height >= form.ClientSize.Height * 0.7)
                    {
                        FixTableToPercent(tlp, alsoDockChildrenFill: true, onlyIfFewChildren: false);
                        fs.LayoutFixedOnce = true;
                        break;
                    }
                }
            }
        }

        // =========================
        // ✅ 关键：修复“按钮表格” TableLayout：Absolute -> Percent + 按钮 Dock=Fill
        // =========================
        private static void FixButtonTablesOnce(Form form, bool force = false)
        {
            if (!_fs.TryGetValue(form, out var fs)) return;
            if (fs.ButtonTablesFixedOnce && !force) return;

            WalkAndFix(form);

            if (!force) fs.ButtonTablesFixedOnce = true;

            void WalkAndFix(Control node)
            {
                foreach (Control child in node.Controls)
                {
                    if (child is TableLayoutPanel tlp)
                    {
                        // 只处理“确实放了按钮”的表格，避免误伤其它排版
                        bool hasButton = tlp.Controls.Cast<Control>().Any(x => x is ButtonBase);
                        if (hasButton)
                        {
                            FixTableToPercent(tlp, alsoDockChildrenFill: true, onlyIfFewChildren: false);
                            foreach (Control x in tlp.Controls)
                            {
                                if (x is ButtonBase b)
                                {
                                    b.AutoSize = false;
                                    b.Dock = DockStyle.Fill;
                                    b.TextAlign = ContentAlignment.MiddleCenter;
                                }
                            }
                        }
                    }

                    if (child.HasChildren) WalkAndFix(child);
                }
            }
        }

        private static void FixTableToPercent(TableLayoutPanel tlp, bool alsoDockChildrenFill, bool onlyIfFewChildren)
        {
            if (tlp == null || tlp.IsDisposed) return;
            if (onlyIfFewChildren && tlp.Controls.Count > 20) return;

            // 让表格随父容器伸缩
            if (tlp.Dock == DockStyle.None)
                tlp.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            // 确保 styles 数量足够
            while (tlp.ColumnStyles.Count < tlp.ColumnCount)
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / Math.Max(1, tlp.ColumnCount)));

            while (tlp.RowStyles.Count < tlp.RowCount)
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / Math.Max(1, tlp.RowCount)));

            // 用运行时实际宽高比例，转成 Percent（最贴近你原布局）
            int[] colW = null;
            int[] rowH = null;
            try { colW = tlp.GetColumnWidths(); } catch { }
            try { rowH = tlp.GetRowHeights(); } catch { }

            if (colW != null && colW.Length == tlp.ColumnCount)
            {
                int sum = colW.Sum(); if (sum <= 0) sum = 1;
                for (int i = 0; i < tlp.ColumnCount; i++)
                {
                    tlp.ColumnStyles[i].SizeType = SizeType.Percent;
                    tlp.ColumnStyles[i].Width = colW[i] * 100f / sum;
                }
            }
            else
            {
                for (int i = 0; i < tlp.ColumnCount; i++)
                {
                    tlp.ColumnStyles[i].SizeType = SizeType.Percent;
                    tlp.ColumnStyles[i].Width = 100f / Math.Max(1, tlp.ColumnCount);
                }
            }

            if (rowH != null && rowH.Length == tlp.RowCount)
            {
                int sum = rowH.Sum(); if (sum <= 0) sum = 1;
                for (int i = 0; i < tlp.RowCount; i++)
                {
                    tlp.RowStyles[i].SizeType = SizeType.Percent;
                    tlp.RowStyles[i].Height = rowH[i] * 100f / sum;
                }
            }
            else
            {
                for (int i = 0; i < tlp.RowCount; i++)
                {
                    tlp.RowStyles[i].SizeType = SizeType.Percent;
                    tlp.RowStyles[i].Height = 100f / Math.Max(1, tlp.RowCount);
                }
            }

            if (alsoDockChildrenFill)
            {
                foreach (Control c in tlp.Controls)
                {
                    // 重要：让单元格里的内容跟着变大
                    if (c is Panel || c is GroupBox || c is TableLayoutPanel || c is FlowLayoutPanel)
                        c.Dock = DockStyle.Fill;
                }
            }

            tlp.PerformLayout();
        }

        // =========================
        // 捕获控件树基准
        // =========================
        private static void CaptureControlTree(Control root)
        {
            foreach (Control c in root.Controls)
            {
                CaptureControl(c);
                if (c.HasChildren) CaptureControlTree(c);
            }
        }

        private static void CaptureControl(Control c)
        {
            if (_cs.TryGetValue(c, out var _)) return;

            var cs = new ControlState { BaseFont = c.Font };

            if (c is DataGridView dgv)
            {
                cs.IsDgv = true;
                cs.DgvHeaderHeight = dgv.ColumnHeadersHeight;
                cs.DgvRowHeight = dgv.RowTemplate != null ? dgv.RowTemplate.Height : 22;
                cs.DgvColWidths = dgv.Columns.Cast<DataGridViewColumn>().Select(col => col.Width).ToArray();

                cs.DgvBaseDefaultFont = (dgv.DefaultCellStyle?.Font) ?? dgv.Font;
                cs.DgvBaseHeaderFont = (dgv.ColumnHeadersDefaultCellStyle?.Font) ?? dgv.Font;
            }

            if (c is ListView lv)
            {
                cs.IsLv = true;
                cs.LvColWidths = lv.Columns.Cast<ColumnHeader>().Select(ch => ch.Width).ToArray();
            }

            _cs.Add(c, cs);
        }

        // =========================
        // 字体缩放（输入框字变大）
        // =========================
        private static void ApplyScaledFonts(Control root, float targetScale)
        {
            foreach (Control c in root.Controls)
            {
                if (!_cs.TryGetValue(c, out var cs))
                {
                    CaptureControl(c);
                    _cs.TryGetValue(c, out cs);
                }

                if (cs != null && cs.BaseFont != null)
                {
                    float newSize = Math.Max(1f, cs.BaseFont.SizeInPoints * targetScale);

                    if (cs.LastScaledFont == null || Math.Abs(cs.LastScaledFont.SizeInPoints - newSize) >= 0.1f)
                    {
                        var nf = new Font(cs.BaseFont.FontFamily, newSize, cs.BaseFont.Style, GraphicsUnit.Point,
                                          cs.BaseFont.GdiCharSet, cs.BaseFont.GdiVerticalFont);

                        if (cs.LastScaledFont != null) { try { cs.LastScaledFont.Dispose(); } catch { } }
                        cs.LastScaledFont = nf;
                        c.Font = nf;
                    }

                    // ✅ 单行 TextBox：高度随字体适配
                    if (c is TextBox tb && !tb.Multiline)
                    {
                        try { tb.AutoSize = true; } catch { }
                    }
                }

                if (c.HasChildren) ApplyScaledFonts(c, targetScale);
            }
        }

        // =========================
        // 特殊控件（DGV/LV）
        // =========================
        private static void ApplyScaledSpecialControls(Control root, float targetScale)
        {
            foreach (Control c in root.Controls)
            {
                if (_cs.TryGetValue(c, out var cs) && cs != null)
                {
                    if (cs.IsDgv && c is DataGridView dgv)
                    {
                        try
                        {
                            dgv.ColumnHeadersHeight = Math.Max(10, (int)Math.Round(cs.DgvHeaderHeight * targetScale));
                            if (dgv.RowTemplate != null)
                                dgv.RowTemplate.Height = Math.Max(10, (int)Math.Round(cs.DgvRowHeight * targetScale));

                            if (cs.DgvColWidths != null && cs.DgvColWidths.Length == dgv.Columns.Count)
                            {
                                for (int i = 0; i < dgv.Columns.Count; i++)
                                    dgv.Columns[i].Width = Math.Max(10, (int)Math.Round(cs.DgvColWidths[i] * targetScale));
                            }

                            // DGV 字体
                            if (cs.DgvBaseDefaultFont != null)
                            {
                                float sz = Math.Max(1f, cs.DgvBaseDefaultFont.SizeInPoints * targetScale);
                                if (cs.DgvLastDefaultFont == null || Math.Abs(cs.DgvLastDefaultFont.SizeInPoints - sz) >= 0.1f)
                                {
                                    var nf = new Font(cs.DgvBaseDefaultFont.FontFamily, sz, cs.DgvBaseDefaultFont.Style);
                                    if (cs.DgvLastDefaultFont != null) try { cs.DgvLastDefaultFont.Dispose(); } catch { }
                                    cs.DgvLastDefaultFont = nf;
                                    dgv.DefaultCellStyle.Font = nf;
                                }
                            }

                            if (cs.DgvBaseHeaderFont != null)
                            {
                                float sz = Math.Max(1f, cs.DgvBaseHeaderFont.SizeInPoints * targetScale);
                                if (cs.DgvLastHeaderFont == null || Math.Abs(cs.DgvLastHeaderFont.SizeInPoints - sz) >= 0.1f)
                                {
                                    var nf = new Font(cs.DgvBaseHeaderFont.FontFamily, sz, cs.DgvBaseHeaderFont.Style);
                                    if (cs.DgvLastHeaderFont != null) try { cs.DgvLastHeaderFont.Dispose(); } catch { }
                                    cs.DgvLastHeaderFont = nf;
                                    dgv.ColumnHeadersDefaultCellStyle.Font = nf;
                                }
                            }
                        }
                        catch { }
                    }

                    if (cs.IsLv && c is ListView lv)
                    {
                        try
                        {
                            if (cs.LvColWidths != null && cs.LvColWidths.Length == lv.Columns.Count)
                            {
                                for (int i = 0; i < lv.Columns.Count; i++)
                                    lv.Columns[i].Width = Math.Max(10, (int)Math.Round(cs.LvColWidths[i] * targetScale));
                            }
                        }
                        catch { }
                    }
                }

                if (c.HasChildren) ApplyScaledSpecialControls(c, targetScale);
            }
        }

        // =========================
        // ✅ 按钮防裁切（TableLayout/FlowLayout：Dock=Fill + MinimumSize）
        // =========================
        private static void AutoFitButtons(Control root, float targetScale)
        {
            foreach (Control c in root.Controls)
            {
                if (c is ButtonBase btn && !string.IsNullOrEmpty(btn.Text))
                {
                    bool inLayout = (btn.Parent is TableLayoutPanel) || (btn.Parent is FlowLayoutPanel);

                    try
                    {
                        var textSize = TextRenderer.MeasureText(
                            btn.Text, btn.Font,
                            new Size(int.MaxValue, int.MaxValue),
                            TextFormatFlags.SingleLine | TextFormatFlags.NoClipping);

                        int extraW = (int)Math.Round(28 * targetScale);
                        int extraH = (int)Math.Round(16 * targetScale);

                        int needW = Math.Max(1, textSize.Width + extraW);
                        int needH = Math.Max(1, textSize.Height + extraH);

                        if (inLayout)
                        {
                            // 关键：TableLayout 里不要 AutoSize=true（会被单元格压缩）
                            btn.AutoSize = false;
                            btn.Dock = DockStyle.Fill;
                            btn.MinimumSize = new Size(needW, needH);
                            btn.TextAlign = ContentAlignment.MiddleCenter;
                        }
                        else
                        {
                            if (btn.Width < needW) btn.Width = needW;
                            if (btn.Height < needH) btn.Height = needH;
                        }
                    }
                    catch { }
                }

                if (c.HasChildren) AutoFitButtons(c, targetScale);
            }
        }

        // =========================
        // 释放字体资源
        // =========================
        private static void ReleaseScaledFonts(Form form)
        {
            try { ReleaseScaledFontsInTree(form); } catch { }
        }

        private static void ReleaseScaledFontsInTree(Control root)
        {
            foreach (Control c in root.Controls)
            {
                if (_cs.TryGetValue(c, out var cs) && cs != null)
                {
                    if (cs.LastScaledFont != null) { try { cs.LastScaledFont.Dispose(); } catch { } cs.LastScaledFont = null; }
                    if (cs.DgvLastDefaultFont != null) { try { cs.DgvLastDefaultFont.Dispose(); } catch { } cs.DgvLastDefaultFont = null; }
                    if (cs.DgvLastHeaderFont != null) { try { cs.DgvLastHeaderFont.Dispose(); } catch { } cs.DgvLastHeaderFont = null; }
                }

                if (c.HasChildren) ReleaseScaledFontsInTree(c);
            }
        }

        // =========================
        // Ctrl + 鼠标滚轮缩放（可选）
        // =========================
        private static void EnsureWheelFilterInstalled()
        {
            if (_wheelFilterInstalled) return;
            _wheelFilterInstalled = true;
            Application.AddMessageFilter(_wheelFilter);
        }

        private sealed class CtrlWheelFilter : IMessageFilter
        {
            private const int WM_MOUSEWHEEL = 0x020A;

            public bool PreFilterMessage(ref Message m)
            {
                try
                {
                    if (m.Msg != WM_MOUSEWHEEL) return false;
                    if ((Control.ModifierKeys & Keys.Control) != Keys.Control) return false;

                    Control ctrl = Control.FromHandle(m.HWnd);
                    Form form = ctrl?.FindForm() ?? Form.ActiveForm;
                    if (form == null) return false;

                    if (!_fs.TryGetValue(form, out var fs)) return false;
                    if (!fs.WheelZoomEnabled) return false;

                    int w = m.WParam.ToInt32();
                    short delta = (short)((w >> 16) & 0xFFFF);

                    if (delta > 0) ZoomIn(form);
                    else if (delta < 0) ZoomOut(form);

                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        private static float Clamp(float v, float min, float max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }
    }
}
