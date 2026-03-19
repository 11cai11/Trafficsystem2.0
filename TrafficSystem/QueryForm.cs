using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace TrafficSystem
{
    /// <summary>
    /// 查找 / 替换 窗体（类似 Word 的查询窗口）
    /// 使用方式：new QueryForm(dataTable, dataGridView, markChangedAction)
    /// </summary>
    public partial class QueryForm : Form
    {
        private DataTable dt;
        private DataGridView dgv;
        private Action markChanged;

        private List<int> matchedRowIndices = new List<int>();
        private int currentMatchIndex = -1;

        public QueryForm(DataTable table, DataGridView dataGridView, Action markChangedCallback)
        {
            InitializeComponent();
            dt = table ?? throw new ArgumentNullException(nameof(table));
            dgv = dataGridView ?? throw new ArgumentNullException(nameof(dataGridView));
            markChanged = markChangedCallback;

            // 初始化列选择（第一项为 所有列）
            cmbColumns.Items.Clear();
            cmbColumns.Items.Add("所有列");
            foreach (DataColumn c in dt.Columns) cmbColumns.Items.Add(c.ColumnName);
            cmbColumns.SelectedIndex = 0;

            btnFindNext.Click += BtnFindNext_Click;
            btnReplace.Click += BtnReplace_Click;
            btnReplaceAll.Click += BtnReplaceAll_Click;
            btnCancel.Click += (s, e) => this.Close();
        }

        private void BtnFindNext_Click(object sender, EventArgs e)
        {
            string find = txtFind.Text ?? "";
            if (string.IsNullOrEmpty(find))
            {
                MessageBox.Show("请输入要查找的内容。");
                return;
            }

            BuildMatches();

            if (matchedRowIndices.Count == 0)
            {
                MessageBox.Show("未找到匹配项。");
                ClearHighlights();
                return;
            }

            // 循环到下一项
            currentMatchIndex++;
            if (currentMatchIndex >= matchedRowIndices.Count) currentMatchIndex = 0;

            HighlightMatches();
            ScrollToRow(matchedRowIndices[currentMatchIndex]);
        }

        private void BuildMatches()
        {
            matchedRowIndices.Clear();
            currentMatchIndex = -1;

            string find = txtFind.Text ?? "";
            bool caseSensitive = chkMatchCase.Checked;
            string selCol = cmbColumns.SelectedItem?.ToString() ?? "所有列";

            StringComparison cmp = caseSensitive ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                var row = dt.Rows[i];
                if (row.RowState == DataRowState.Deleted) continue;

                bool matched = false;
                if (selCol == "所有列")
                {
                    foreach (DataColumn c in dt.Columns)
                    {
                        var val = row[c];
                        if (val == DBNull.Value) continue;
                        string s = val.ToString();
                        if (s.IndexOf(find, cmp) >= 0) { matched = true; break; }
                    }
                }
                else
                {
                    if (!dt.Columns.Contains(selCol)) continue;
                    var val = row[selCol];
                    if (val != DBNull.Value)
                    {
                        string s = val.ToString();
                        if (s.IndexOf(find, cmp) >= 0) matched = true;
                    }
                }

                if (matched) matchedRowIndices.Add(i);
            }
        }

        private void HighlightMatches()
        {
            // 清除先前样式
            ClearHighlights();

            // 先给所有匹配行浅黄，再给当前行橙色
            foreach (int rIdx in matchedRowIndices)
            {
                if (rIdx >= 0 && rIdx < dgv.Rows.Count)
                {
                    dgv.Rows[rIdx].DefaultCellStyle.BackColor = Color.LightYellow;
                }
            }

            if (currentMatchIndex >= 0 && currentMatchIndex < matchedRowIndices.Count)
            {
                int cur = matchedRowIndices[currentMatchIndex];
                if (cur >= 0 && cur < dgv.Rows.Count)
                {
                    dgv.ClearSelection();
                    dgv.Rows[cur].Selected = true;
                    dgv.CurrentCell = dgv.Rows[cur].Cells.Cast<DataGridViewCell>().FirstOrDefault(c => c.Visible) ?? dgv.Rows[cur].Cells[0];
                    dgv.Rows[cur].DefaultCellStyle.BackColor = Color.Orange;
                }
            }
        }

        private void ClearHighlights()
        {
            try
            {
                foreach (DataGridViewRow r in dgv.Rows)
                {
                    r.DefaultCellStyle.BackColor = Color.White;
                }
            }
            catch
            {
                // 忽略可能的异常（例如虚拟模式等）
            }
        }

        private void ScrollToRow(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= dgv.Rows.Count) return;
            dgv.FirstDisplayedScrollingRowIndex = Math.Max(0, Math.Min(rowIndex, Math.Max(0, dgv.Rows.Count - 1)));
        }

        private void BtnReplace_Click(object sender, EventArgs e)
        {
            string find = txtFind.Text ?? "";
            if (string.IsNullOrEmpty(find))
            {
                MessageBox.Show("请输入要查找的内容。");
                return;
            }
            if (matchedRowIndices.Count == 0)
            {
                BuildMatches();
                if (matchedRowIndices.Count == 0)
                {
                    MessageBox.Show("未找到匹配项。");
                    return;
                }
                currentMatchIndex = 0;
            }
            if (currentMatchIndex < 0 || currentMatchIndex >= matchedRowIndices.Count)
            {
                currentMatchIndex = 0;
            }

            int rowIdx = matchedRowIndices[currentMatchIndex];
            if (rowIdx < 0 || rowIdx >= dt.Rows.Count)
            {
                MessageBox.Show("当前匹配行无效。");
                return;
            }

            DoReplaceOnRow(rowIdx, find, txtReplace.Text ?? "");
            // after replace, rebuild matches because content changed
            BuildMatches();
            if (matchedRowIndices.Count > 0)
            {
                // try to keep index within range
                currentMatchIndex = Math.Min(currentMatchIndex, matchedRowIndices.Count - 1);
            }
            else
            {
                currentMatchIndex = -1;
            }

            HighlightMatches();
            markChanged?.Invoke();
        }

        private void BtnReplaceAll_Click(object sender, EventArgs e)
        {
            string find = txtFind.Text ?? "";
            if (string.IsNullOrEmpty(find))
            {
                MessageBox.Show("请输入要查找的内容。");
                return;
            }

            BuildMatches();
            if (matchedRowIndices.Count == 0)
            {
                MessageBox.Show("未找到匹配项。");
                return;
            }

            // 将索引复制一份以防修改时发生变化
            var targets = matchedRowIndices.ToArray();
            for (int i = 0; i < targets.Length; i++)
            {
                int rowIdx = targets[i];
                if (rowIdx < 0 || rowIdx >= dt.Rows.Count) continue;
                DoReplaceOnRow(rowIdx, find, txtReplace.Text ?? "");
            }

            // 重建匹配列表
            BuildMatches();
            currentMatchIndex = matchedRowIndices.Count > 0 ? 0 : -1;
            HighlightMatches();
            markChanged?.Invoke();
            MessageBox.Show($"已完成全部替换（共 {targets.Length} 行尝试替换）。");
        }

        /// <summary>
        /// 在指定 DataTable 行上执行替换（支持指定列或所有列）。
        /// 替换为在字符串中做子串替换（不改变非字符串类型的值）。
        /// </summary>
        private void DoReplaceOnRow(int rowIndex, string findText, string replaceText)
        {
            if (rowIndex < 0 || rowIndex >= dt.Rows.Count) return;
            var row = dt.Rows[rowIndex];
            bool caseSensitive = chkMatchCase.Checked;
            bool replaceAllColumns = (cmbColumns.SelectedItem?.ToString() ?? "") == "所有列";
            StringComparison cmp = caseSensitive ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;

            if (replaceAllColumns)
            {
                foreach (DataColumn col in dt.Columns)
                {
                    if (!col.DataType.Equals(typeof(string)) && col.DataType != typeof(object)) continue;
                    var val = row[col];
                    if (val == DBNull.Value) continue;
                    string s = val.ToString();
                    if (s.IndexOf(findText, cmp) >= 0)
                    {
                        string newVal = ReplaceStringWithComparison(s, findText, replaceText, cmp);
                        row[col] = newVal;
                    }
                }
            }
            else
            {
                string sel = cmbColumns.SelectedItem?.ToString() ?? "";
                if (!dt.Columns.Contains(sel)) return;
                var col = dt.Columns[sel];
                if (!col.DataType.Equals(typeof(string)) && col.DataType != typeof(object))
                {
                    // 尝试对非字符串列进行直接替换（如果完全相等）
                    var val = row[col];
                    if (val != DBNull.Value)
                    {
                        string s = val.ToString();
                        if (string.Equals(s, findText, cmp))
                        {
                            row[col] = Convert.ChangeType(replaceText, col.DataType);
                        }
                    }
                }
                else
                {
                    var val = row[col];
                    if (val == DBNull.Value) return;
                    string s = val.ToString();
                    if (s.IndexOf(findText, cmp) >= 0)
                    {
                        string newVal = ReplaceStringWithComparison(s, findText, replaceText, cmp);
                        row[col] = newVal;
                    }
                }
            }

            // 立即刷新 DataGridView 展示
            try
            {
                dgv.Refresh();
            }
            catch { }
        }

        private string ReplaceStringWithComparison(string source, string find, string replace, StringComparison cmp)
        {
            // 如果不区分大小写，手动逐个替换保持简单逻辑
            if (cmp == StringComparison.CurrentCultureIgnoreCase)
            {
                // 使用循环替换（避免正则）
                int idx = source.IndexOf(find, cmp);
                if (idx < 0) return source;
                var sb = new System.Text.StringBuilder();
                int start = 0;
                while (idx >= 0)
                {
                    sb.Append(source.Substring(start, idx - start));
                    sb.Append(replace);
                    start = idx + find.Length;
                    if (start >= source.Length) break;
                    idx = source.IndexOf(find, start, cmp);
                }
                if (start < source.Length) sb.Append(source.Substring(start));
                return sb.ToString();
            }
            else
            {
                // 区分大小写可以直接使用 Replace
                return source.Replace(find, replace);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // 关闭窗体时清理高亮，避免留下样式
            ClearHighlights();
            base.OnFormClosing(e);
        }

        private void QueryForm_Load(object sender, EventArgs e)
        {

        }
    }
}