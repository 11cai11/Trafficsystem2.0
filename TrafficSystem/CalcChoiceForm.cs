// 文件：CalcChoiceForm.cs
// ✅ 修复：不要在本文件里重复定义 Dijkstra / SimplePriorityQueue（会导致 CS0101/CS0111/CS0121）
// ✅ 默认连接串改回 Access DSN 形式（你可以在外部传入覆盖）
// ✅ 新增：本文件内三个窗体全部“引用全局等比例缩放算法 UiZoom”（不改布局、不删控件、不省略代码）
// ✅ 新增：最短路径窗体支持“算法下拉选择”，点击开始计算按选择算法运行
// 兼容：.NET Framework 4.7.2 + C# 7.3

using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO;

namespace TrafficSystem
{
    public partial class CalcChoiceForm : Form
    {
        private string _currentUser = "未登录用户";

        // ✅ 默认改回 Access DSN 形式（可被外部传入覆盖）
        private string _connStr = "DSN=OKS;";

        private MainForm _ownerMain = null;
        private DataTable _sourceTable = null;

        private Label lblUser;

        public CalcChoiceForm()
        {
            InitializeComponent();
            InitFormStyle();

            // ✅ 引用“全局等比例缩放算法”（注册基准）
            // 注意：本窗体在 InitFormStyle 里会 Clear+重建控件，所以必须放在 InitFormStyle 之后注册
            UiZoom.Register(this, scaleFormClientSize: true);
        }

        public CalcChoiceForm(string currentUser, string connStr, MainForm owner, DataTable sourceTable = null) : this()
        {
            _currentUser = string.IsNullOrWhiteSpace(currentUser) ? "未登录用户" : currentUser;
            _connStr = string.IsNullOrWhiteSpace(connStr) ? _connStr : connStr;
            _ownerMain = owner;
            _sourceTable = sourceTable;

            if (lblUser != null) lblUser.Text = $"当前用户：{_currentUser}";
        }

        private void InitFormStyle()
        {
            this.BackColor = Color.FromArgb(245, 247, 250);
            this.Text = "轨迹数据计算 - 算法选择";
            this.Size = new Size(360, 220);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            this.Controls.Clear();

            Label lblTip = new Label
            {
                Text = "请选择要执行的计算算法：",
                Location = new Point(20, 20),
                Size = new Size(300, 25),
                Font = new Font("微软雅黑", 10F)
            };
            this.Controls.Add(lblTip);

            lblUser = new Label
            {
                Text = $"当前用户：{_currentUser}",
                Location = new Point(20, 50),
                Size = new Size(300, 20),
                Font = new Font("微软雅黑", 9F),
                ForeColor = Color.DarkGray
            };
            this.Controls.Add(lblUser);

            Button btnOpenCalc = new Button
            {
                Text = "打开算法计算窗口",
                Location = new Point(40, 90),
                Size = new Size(260, 36),
                Font = new Font("微软雅黑", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnOpenCalc.FlatAppearance.BorderSize = 0;
            btnOpenCalc.Click += (s, e) =>
            {
                try
                {
                    var calcForm = new TaxiPathCalculation.CalcAlgorithmMainForm(_currentUser, _connStr, _ownerMain, _sourceTable);
                    calcForm.ShowDialog(this);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("打开算法计算窗口失败：\r\n" + ex.Message, "错误",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            this.Controls.Add(btnOpenCalc);
        }

        private void CalcChoiceForm_Load(object sender, EventArgs e) { }
    }

    // ======================================================================
    // ✅ 新增：最短路径算法集合（新命名，避免与你工程里已有 Dijkstra / Queue 冲突）
    // ======================================================================
    public enum ShortestPathAlgoKind
    {
        Dijkstra = 0,
        AStar = 1,
        BiDijkstra = 2,
        BiAStar = 3,
        BellmanFord = 4,
        SPFA = 5,
        DagShortestPath = 6,
        FloydWarshall = 7,
        JohnsonReweight = 8
    }

    public sealed class ShortestPathResult
    {
        public bool Ok { get; set; }
        public List<string> Path { get; set; }
        public double Total { get; set; }
        public string Message { get; set; }

        public static ShortestPathResult Fail(string msg)
        {
            return new ShortestPathResult { Ok = false, Path = null, Total = double.PositiveInfinity, Message = msg ?? "失败" };
        }

        public static ShortestPathResult Success(List<string> path, double total, string msg = null)
        {
            return new ShortestPathResult { Ok = true, Path = path, Total = total, Message = msg ?? "" };
        }
    }

    internal sealed class MinHeapPriorityQueue<T>
    {
        private struct Node
        {
            public double Key;
            public int Seq;
            public T Item;
        }

        private readonly List<Node> _heap = new List<Node>();
        private int _seq = 0;

        public int Count { get { return _heap.Count; } }

        public void Push(double key, T item)
        {
            _heap.Add(new Node { Key = key, Seq = _seq++, Item = item });
            SiftUp(_heap.Count - 1);
        }

        public T Pop(out double key)
        {
            if (_heap.Count == 0) throw new InvalidOperationException("empty");
            var root = _heap[0];
            key = root.Key;

            int last = _heap.Count - 1;
            _heap[0] = _heap[last];
            _heap.RemoveAt(last);
            if (_heap.Count > 0) SiftDown(0);

            return root.Item;
        }

        public bool TryPop(out T item, out double key)
        {
            if (_heap.Count == 0)
            {
                item = default(T);
                key = 0;
                return false;
            }
            item = Pop(out key);
            return true;
        }

        private bool Less(int i, int j)
        {
            // key 小优先；key相同按 seq 小优先（稳定）
            if (_heap[i].Key < _heap[j].Key) return true;
            if (_heap[i].Key > _heap[j].Key) return false;
            return _heap[i].Seq < _heap[j].Seq;
        }

        private void SiftUp(int i)
        {
            while (i > 0)
            {
                int p = (i - 1) / 2;
                if (!Less(i, p)) break;
                Swap(i, p);
                i = p;
            }
        }

        private void SiftDown(int i)
        {
            int n = _heap.Count;
            while (true)
            {
                int l = i * 2 + 1;
                int r = l + 1;
                int best = i;

                if (l < n && Less(l, best)) best = l;
                if (r < n && Less(r, best)) best = r;

                if (best == i) break;
                Swap(i, best);
                i = best;
            }
        }

        private void Swap(int i, int j)
        {
            var t = _heap[i];
            _heap[i] = _heap[j];
            _heap[j] = t;
        }
    }

    public static class ShortestPathAlgorithms
    {
        private static Dictionary<string, List<Tuple<string, double>>> BuildReverseGraph(Dictionary<string, List<Tuple<string, double>>> g)
        {
            var rev = new Dictionary<string, List<Tuple<string, double>>>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in g)
            {
                var u = kv.Key;
                if (!rev.ContainsKey(u)) rev[u] = new List<Tuple<string, double>>();
                foreach (var e in kv.Value)
                {
                    var v = e.Item1;
                    var w = e.Item2;
                    if (!rev.ContainsKey(v)) rev[v] = new List<Tuple<string, double>>();
                    rev[v].Add(Tuple.Create(u, w));
                }
            }
            return rev;
        }

        private static List<string> ReconstructPath(Dictionary<string, string> prev, string start, string end)
        {
            if (prev == null) return null;
            var path = new List<string>();
            string cur = end;
            int guard = 0;
            while (!string.Equals(cur, start, StringComparison.OrdinalIgnoreCase))
            {
                path.Add(cur);
                if (!prev.ContainsKey(cur)) return null;
                cur = prev[cur];
                guard++;
                if (guard > 200000) return null;
            }
            path.Add(start);
            path.Reverse();
            return path;
        }

        private static List<string> ReconstructPathBi(Dictionary<string, string> prevF, Dictionary<string, string> prevB, string start, string end, string meet)
        {
            // prevF: node -> parent toward start
            // prevB: node -> next toward end
            var left = new List<string>();
            string cur = meet;
            int guard = 0;
            while (!string.Equals(cur, start, StringComparison.OrdinalIgnoreCase))
            {
                left.Add(cur);
                if (!prevF.ContainsKey(cur)) return null;
                cur = prevF[cur];
                guard++;
                if (guard > 200000) return null;
            }
            left.Add(start);
            left.Reverse();

            var right = new List<string>();
            cur = meet;
            guard = 0;
            while (!string.Equals(cur, end, StringComparison.OrdinalIgnoreCase))
            {
                if (!prevB.ContainsKey(cur)) return null;
                cur = prevB[cur];
                right.Add(cur);
                guard++;
                if (guard > 200000) return null;
            }

            left.AddRange(right);
            return left;
        }

        public static ShortestPathResult AStar(
            Dictionary<string, List<Tuple<string, double>>> g,
            string start,
            string end,
            Func<string, string, double> heuristic // h(u,end)
        )
        {
            if (g == null || g.Count == 0) return ShortestPathResult.Fail("图为空。");
            if (string.IsNullOrWhiteSpace(start) || string.IsNullOrWhiteSpace(end)) return ShortestPathResult.Fail("起点/终点为空。");
            if (string.Equals(start, end, StringComparison.OrdinalIgnoreCase))
                return ShortestPathResult.Success(new List<string> { start }, 0, "起点=终点");

            if (heuristic == null) heuristic = (a, b) => 0;

            var dist = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var prev = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var closed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var pq = new MinHeapPriorityQueue<string>();
            dist[start] = 0;
            pq.Push(heuristic(start, end), start);

            while (pq.Count > 0)
            {
                double f;
                string u = pq.Pop(out f);
                if (closed.Contains(u)) continue;
                closed.Add(u);

                if (string.Equals(u, end, StringComparison.OrdinalIgnoreCase))
                    break;

                double du;
                if (!dist.TryGetValue(u, out du)) continue;

                List<Tuple<string, double>> edges;
                if (!g.TryGetValue(u, out edges)) continue;

                for (int i = 0; i < edges.Count; i++)
                {
                    var v = edges[i].Item1;
                    var w = edges[i].Item2;

                    double nd = du + w;
                    double dv;
                    if (!dist.TryGetValue(v, out dv) || nd < dv)
                    {
                        dist[v] = nd;
                        prev[v] = u;
                        double fv = nd + heuristic(v, end);
                        pq.Push(fv, v);
                    }
                }
            }

            double total;
            if (!dist.TryGetValue(end, out total) || double.IsInfinity(total))
                return ShortestPathResult.Fail("不可达。");

            var path = ReconstructPath(prev, start, end);
            if (path == null || path.Count == 0) return ShortestPathResult.Fail("路径重建失败。");
            return ShortestPathResult.Success(path, total);
        }

        public static ShortestPathResult BidirectionalDijkstra(Dictionary<string, List<Tuple<string, double>>> g, string start, string end)
        {
            if (g == null || g.Count == 0) return ShortestPathResult.Fail("图为空。");
            if (string.IsNullOrWhiteSpace(start) || string.IsNullOrWhiteSpace(end)) return ShortestPathResult.Fail("起点/终点为空。");
            if (string.Equals(start, end, StringComparison.OrdinalIgnoreCase))
                return ShortestPathResult.Success(new List<string> { start }, 0, "起点=终点");

            var rg = BuildReverseGraph(g);

            var distF = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var distB = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var prevF = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var prevB = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var visF = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var visB = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var pqF = new MinHeapPriorityQueue<string>();
            var pqB = new MinHeapPriorityQueue<string>();

            distF[start] = 0;
            distB[end] = 0;
            pqF.Push(0, start);
            pqB.Push(0, end);

            double best = double.PositiveInfinity;
            string meet = null;

            while (pqF.Count > 0 && pqB.Count > 0)
            {
                // 取当前更小的一侧扩展
                double kf, kb;
                string peekF, peekB;

                // 用 TryPop + 回推 的方式太麻烦；这里直接 Pop 后判断“过期”即可
                // 通过比较 dist 的最小值：简单做法：各 Pop 一次看 key，但会改变队列
                // 为了稳定：每轮各扩展一次（工程够用）
                // --- Forward ---
                if (pqF.TryPop(out peekF, out kf))
                {
                    if (!visF.Contains(peekF))
                    {
                        double df;
                        if (distF.TryGetValue(peekF, out df))
                        {
                            if (kf > df) { /*过期*/ }
                            else
                            {
                                visF.Add(peekF);
                                if (distB.ContainsKey(peekF))
                                {
                                    double cand = df + distB[peekF];
                                    if (cand < best) { best = cand; meet = peekF; }
                                }

                                List<Tuple<string, double>> edges;
                                if (g.TryGetValue(peekF, out edges))
                                {
                                    for (int i = 0; i < edges.Count; i++)
                                    {
                                        var v = edges[i].Item1;
                                        var w = edges[i].Item2;
                                        double nd = df + w;

                                        double dv;
                                        if (!distF.TryGetValue(v, out dv) || nd < dv)
                                        {
                                            distF[v] = nd;
                                            prevF[v] = peekF;
                                            pqF.Push(nd, v);
                                        }

                                        if (distB.ContainsKey(v))
                                        {
                                            double cand = nd + distB[v];
                                            if (cand < best) { best = cand; meet = v; }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // --- Backward (on reverse graph) ---
                if (pqB.TryPop(out peekB, out kb))
                {
                    if (!visB.Contains(peekB))
                    {
                        double db;
                        if (distB.TryGetValue(peekB, out db))
                        {
                            if (kb > db) { /*过期*/ }
                            else
                            {
                                visB.Add(peekB);
                                if (distF.ContainsKey(peekB))
                                {
                                    double cand = db + distF[peekB];
                                    if (cand < best) { best = cand; meet = peekB; }
                                }

                                List<Tuple<string, double>> edges;
                                if (rg.TryGetValue(peekB, out edges))
                                {
                                    for (int i = 0; i < edges.Count; i++)
                                    {
                                        var v = edges[i].Item1; // v -> peekB in original
                                        var w = edges[i].Item2;
                                        double nd = db + w;

                                        double dv;
                                        if (!distB.TryGetValue(v, out dv) || nd < dv)
                                        {
                                            distB[v] = nd;
                                            // 关键：prevB[v] = peekB 代表从 v 往终点走下一步是 peekB
                                            prevB[v] = peekB;
                                            pqB.Push(nd, v);
                                        }

                                        if (distF.ContainsKey(v))
                                        {
                                            double cand = nd + distF[v];
                                            if (cand < best) { best = cand; meet = v; }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // 剪枝：如果两边当前最优都已经不可能改善
                if (meet != null)
                {
                    // 简单剪枝：当已找到 best，且队列继续扩展意义不大时可 break
                    // 这里不取 peek key 做严格剪枝，保持实现简单稳定
                    // （工程上可直接继续循环直到队列空）
                }
            }

            if (meet == null || double.IsInfinity(best))
                return ShortestPathResult.Fail("不可达。");

            var path = ReconstructPathBi(prevF, prevB, start, end, meet);
            if (path == null || path.Count == 0) return ShortestPathResult.Fail("路径重建失败。");
            return ShortestPathResult.Success(path, best);
        }

        public static ShortestPathResult BidirectionalAStar(
            Dictionary<string, List<Tuple<string, double>>> g,
            string start,
            string end,
            Func<string, string, double> heuristic // h(u,target)
        )
        {
            // 简化实现：在双向 Dijkstra 的基础上，用 f=g+h 作为优先级
            // 若 heuristic=0 则等价双向Dijkstra
            if (heuristic == null) heuristic = (a, b) => 0;

            if (g == null || g.Count == 0) return ShortestPathResult.Fail("图为空。");
            if (string.IsNullOrWhiteSpace(start) || string.IsNullOrWhiteSpace(end)) return ShortestPathResult.Fail("起点/终点为空。");
            if (string.Equals(start, end, StringComparison.OrdinalIgnoreCase))
                return ShortestPathResult.Success(new List<string> { start }, 0, "起点=终点");

            var rg = BuildReverseGraph(g);

            var distF = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var distB = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var prevF = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var prevB = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var closedF = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var closedB = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var pqF = new MinHeapPriorityQueue<string>();
            var pqB = new MinHeapPriorityQueue<string>();

            distF[start] = 0;
            distB[end] = 0;

            pqF.Push(heuristic(start, end), start);
            pqB.Push(heuristic(end, start), end);

            double best = double.PositiveInfinity;
            string meet = null;

            while (pqF.Count > 0 && pqB.Count > 0)
            {
                // Forward expand
                double fkey;
                string u = pqF.Pop(out fkey);
                if (!closedF.Contains(u))
                {
                    double du;
                    if (!distF.TryGetValue(u, out du)) du = double.PositiveInfinity;

                    // 过期
                    if (fkey > du + heuristic(u, end))
                    {
                        // skip
                    }
                    else
                    {
                        closedF.Add(u);
                        if (distB.ContainsKey(u))
                        {
                            double cand = du + distB[u];
                            if (cand < best) { best = cand; meet = u; }
                        }

                        List<Tuple<string, double>> edges;
                        if (g.TryGetValue(u, out edges))
                        {
                            for (int i = 0; i < edges.Count; i++)
                            {
                                var v = edges[i].Item1;
                                var w = edges[i].Item2;
                                double nd = du + w;

                                double dv;
                                if (!distF.TryGetValue(v, out dv) || nd < dv)
                                {
                                    distF[v] = nd;
                                    prevF[v] = u;
                                    pqF.Push(nd + heuristic(v, end), v);
                                }

                                if (distB.ContainsKey(v))
                                {
                                    double cand = nd + distB[v];
                                    if (cand < best) { best = cand; meet = v; }
                                }
                            }
                        }
                    }
                }

                // Backward expand (reverse graph)
                double bkey;
                string x = pqB.Pop(out bkey);
                if (!closedB.Contains(x))
                {
                    double dx;
                    if (!distB.TryGetValue(x, out dx)) dx = double.PositiveInfinity;

                    if (bkey > dx + heuristic(x, start))
                    {
                        // skip
                    }
                    else
                    {
                        closedB.Add(x);
                        if (distF.ContainsKey(x))
                        {
                            double cand = dx + distF[x];
                            if (cand < best) { best = cand; meet = x; }
                        }

                        List<Tuple<string, double>> edges;
                        if (rg.TryGetValue(x, out edges))
                        {
                            for (int i = 0; i < edges.Count; i++)
                            {
                                var v = edges[i].Item1; // v -> x (original)
                                var w = edges[i].Item2;
                                double nd = dx + w;

                                double dv;
                                if (!distB.TryGetValue(v, out dv) || nd < dv)
                                {
                                    distB[v] = nd;
                                    prevB[v] = x; // from v go next to x toward end
                                    pqB.Push(nd + heuristic(v, start), v);
                                }

                                if (distF.ContainsKey(v))
                                {
                                    double cand = nd + distF[v];
                                    if (cand < best) { best = cand; meet = v; }
                                }
                            }
                        }
                    }
                }
            }

            if (meet == null || double.IsInfinity(best))
                return ShortestPathResult.Fail("不可达。");

            var path = ReconstructPathBi(prevF, prevB, start, end, meet);
            if (path == null || path.Count == 0) return ShortestPathResult.Fail("路径重建失败。");
            return ShortestPathResult.Success(path, best);
        }

        public static ShortestPathResult BellmanFord(Dictionary<string, List<Tuple<string, double>>> g, List<string> nodes, string start, string end)
        {
            if (g == null || g.Count == 0) return ShortestPathResult.Fail("图为空。");
            if (nodes == null || nodes.Count == 0) return ShortestPathResult.Fail("节点为空。");
            if (string.IsNullOrWhiteSpace(start) || string.IsNullOrWhiteSpace(end)) return ShortestPathResult.Fail("起点/终点为空。");
            if (string.Equals(start, end, StringComparison.OrdinalIgnoreCase))
                return ShortestPathResult.Success(new List<string> { start }, 0, "起点=终点");

            var edges = new List<Tuple<string, string, double>>();
            foreach (var kv in g)
            {
                var u = kv.Key;
                for (int i = 0; i < kv.Value.Count; i++)
                    edges.Add(Tuple.Create(u, kv.Value[i].Item1, kv.Value[i].Item2));
            }

            var dist = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var prev = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < nodes.Count; i++)
                dist[nodes[i]] = double.PositiveInfinity;

            dist[start] = 0;

            bool changed = false;
            for (int it = 0; it < nodes.Count - 1; it++)
            {
                changed = false;
                for (int i = 0; i < edges.Count; i++)
                {
                    var u = edges[i].Item1;
                    var v = edges[i].Item2;
                    var w = edges[i].Item3;

                    double du;
                    if (!dist.TryGetValue(u, out du) || double.IsInfinity(du)) continue;

                    double nd = du + w;
                    double dv;
                    if (!dist.TryGetValue(v, out dv) || nd < dv)
                    {
                        dist[v] = nd;
                        prev[v] = u;
                        changed = true;
                    }
                }
                if (!changed) break;
            }

            // 负环检测：若还能松弛，说明存在负环（且从 start 可达）
            for (int i = 0; i < edges.Count; i++)
            {
                var u = edges[i].Item1;
                var v = edges[i].Item2;
                var w = edges[i].Item3;

                double du;
                if (!dist.TryGetValue(u, out du) || double.IsInfinity(du)) continue;

                double nd = du + w;
                double dv;
                if (dist.TryGetValue(v, out dv) && nd < dv)
                    return ShortestPathResult.Fail("检测到负环（从起点可达），最短路径无定义。");
            }

            double total;
            if (!dist.TryGetValue(end, out total) || double.IsInfinity(total))
                return ShortestPathResult.Fail("不可达。");

            var path = ReconstructPath(prev, start, end);
            if (path == null || path.Count == 0) return ShortestPathResult.Fail("路径重建失败。");
            return ShortestPathResult.Success(path, total);
        }

        public static ShortestPathResult SPFA(Dictionary<string, List<Tuple<string, double>>> g, List<string> nodes, string start, string end)
        {
            if (g == null || g.Count == 0) return ShortestPathResult.Fail("图为空。");
            if (nodes == null || nodes.Count == 0) return ShortestPathResult.Fail("节点为空。");
            if (string.IsNullOrWhiteSpace(start) || string.IsNullOrWhiteSpace(end)) return ShortestPathResult.Fail("起点/终点为空。");
            if (string.Equals(start, end, StringComparison.OrdinalIgnoreCase))
                return ShortestPathResult.Success(new List<string> { start }, 0, "起点=终点");

            var dist = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var prev = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var inq = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var relaxCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < nodes.Count; i++)
            {
                dist[nodes[i]] = double.PositiveInfinity;
                relaxCount[nodes[i]] = 0;
            }
            dist[start] = 0;

            var q = new Queue<string>();
            q.Enqueue(start);
            inq.Add(start);

            while (q.Count > 0)
            {
                var u = q.Dequeue();
                inq.Remove(u);

                double du;
                if (!dist.TryGetValue(u, out du)) continue;

                List<Tuple<string, double>> edges;
                if (!g.TryGetValue(u, out edges)) continue;

                for (int i = 0; i < edges.Count; i++)
                {
                    var v = edges[i].Item1;
                    var w = edges[i].Item2;

                    double nd = du + w;
                    double dv;
                    if (!dist.TryGetValue(v, out dv) || nd < dv)
                    {
                        dist[v] = nd;
                        prev[v] = u;

                        relaxCount[v] = relaxCount[v] + 1;
                        if (relaxCount[v] > nodes.Count)
                            return ShortestPathResult.Fail("检测到负环（可能），最短路径无定义。");

                        if (!inq.Contains(v))
                        {
                            q.Enqueue(v);
                            inq.Add(v);
                        }
                    }
                }
            }

            double total;
            if (!dist.TryGetValue(end, out total) || double.IsInfinity(total))
                return ShortestPathResult.Fail("不可达。");

            var path = ReconstructPath(prev, start, end);
            if (path == null || path.Count == 0) return ShortestPathResult.Fail("路径重建失败。");
            return ShortestPathResult.Success(path, total);
        }

        public static ShortestPathResult DagShortestPath(Dictionary<string, List<Tuple<string, double>>> g, List<string> nodes, string start, string end)
        {
            if (g == null || g.Count == 0) return ShortestPathResult.Fail("图为空。");
            if (nodes == null || nodes.Count == 0) return ShortestPathResult.Fail("节点为空。");
            if (string.IsNullOrWhiteSpace(start) || string.IsNullOrWhiteSpace(end)) return ShortestPathResult.Fail("起点/终点为空。");
            if (string.Equals(start, end, StringComparison.OrdinalIgnoreCase))
                return ShortestPathResult.Success(new List<string> { start }, 0, "起点=终点");

            var indeg = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < nodes.Count; i++) indeg[nodes[i]] = 0;

            foreach (var kv in g)
            {
                var u = kv.Key;
                for (int i = 0; i < kv.Value.Count; i++)
                {
                    var v = kv.Value[i].Item1;
                    if (!indeg.ContainsKey(v)) indeg[v] = 0;
                    indeg[v] = indeg[v] + 1;
                }
            }

            var q = new Queue<string>();
            foreach (var kv in indeg) if (kv.Value == 0) q.Enqueue(kv.Key);

            var topo = new List<string>();
            while (q.Count > 0)
            {
                var u = q.Dequeue();
                topo.Add(u);

                List<Tuple<string, double>> edges;
                if (!g.TryGetValue(u, out edges)) continue;

                for (int i = 0; i < edges.Count; i++)
                {
                    var v = edges[i].Item1;
                    indeg[v] = indeg[v] - 1;
                    if (indeg[v] == 0) q.Enqueue(v);
                }
            }

            if (topo.Count != indeg.Count)
                return ShortestPathResult.Fail("当前图不是 DAG（存在环），无法使用 DAG 最短路算法。");

            var dist = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var prev = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var n in indeg.Keys) dist[n] = double.PositiveInfinity;
            dist[start] = 0;

            for (int i = 0; i < topo.Count; i++)
            {
                var u = topo[i];
                double du;
                if (!dist.TryGetValue(u, out du) || double.IsInfinity(du)) continue;

                List<Tuple<string, double>> edges;
                if (!g.TryGetValue(u, out edges)) continue;

                for (int j = 0; j < edges.Count; j++)
                {
                    var v = edges[j].Item1;
                    var w = edges[j].Item2;

                    double nd = du + w;
                    double dv;
                    if (!dist.TryGetValue(v, out dv) || nd < dv)
                    {
                        dist[v] = nd;
                        prev[v] = u;
                    }
                }
            }

            double total;
            if (!dist.TryGetValue(end, out total) || double.IsInfinity(total))
                return ShortestPathResult.Fail("不可达。");

            var path = ReconstructPath(prev, start, end);
            if (path == null || path.Count == 0) return ShortestPathResult.Fail("路径重建失败。");
            return ShortestPathResult.Success(path, total);
        }

        public static ShortestPathResult FloydWarshallSinglePair(Dictionary<string, List<Tuple<string, double>>> g, List<string> nodes, string start, string end)
        {
            if (g == null || g.Count == 0) return ShortestPathResult.Fail("图为空。");
            if (nodes == null || nodes.Count == 0) return ShortestPathResult.Fail("节点为空。");
            if (string.IsNullOrWhiteSpace(start) || string.IsNullOrWhiteSpace(end)) return ShortestPathResult.Fail("起点/终点为空。");
            if (string.Equals(start, end, StringComparison.OrdinalIgnoreCase))
                return ShortestPathResult.Success(new List<string> { start }, 0, "起点=终点");

            var idx = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < nodes.Count; i++) idx[nodes[i]] = i;

            int n = nodes.Count;
            var dist = new double[n, n];
            var next = new int[n, n];

            double INF = double.PositiveInfinity;

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    dist[i, j] = (i == j) ? 0 : INF;
                    next[i, j] = -1;
                }
            }

            foreach (var kv in g)
            {
                int iu;
                if (!idx.TryGetValue(kv.Key, out iu)) continue;
                for (int i = 0; i < kv.Value.Count; i++)
                {
                    var v = kv.Value[i].Item1;
                    var w = kv.Value[i].Item2;
                    int iv;
                    if (!idx.TryGetValue(v, out iv)) continue;

                    if (w < dist[iu, iv])
                    {
                        dist[iu, iv] = w;
                        next[iu, iv] = iv;
                    }
                }
            }

            // Floyd
            for (int k = 0; k < n; k++)
            {
                for (int i = 0; i < n; i++)
                {
                    if (double.IsInfinity(dist[i, k])) continue;
                    for (int j = 0; j < n; j++)
                    {
                        if (double.IsInfinity(dist[k, j])) continue;

                        double nd = dist[i, k] + dist[k, j];
                        if (nd < dist[i, j])
                        {
                            dist[i, j] = nd;
                            next[i, j] = next[i, k];
                        }
                    }
                }
            }

            int sIdx, eIdx;
            if (!idx.TryGetValue(start, out sIdx) || !idx.TryGetValue(end, out eIdx))
                return ShortestPathResult.Fail("起点/终点不在节点集合中。");

            if (next[sIdx, eIdx] == -1)
                return ShortestPathResult.Fail("不可达。");

            // reconstruct
            var path = new List<string>();
            int cur = sIdx;
            path.Add(nodes[cur]);

            int guard = 0;
            while (cur != eIdx)
            {
                cur = next[cur, eIdx];
                if (cur < 0) return ShortestPathResult.Fail("路径重建失败。");
                path.Add(nodes[cur]);
                guard++;
                if (guard > n + 5) return ShortestPathResult.Fail("路径重建失败（循环保护触发）。");
            }

            return ShortestPathResult.Success(path, dist[sIdx, eIdx]);
        }

        public static ShortestPathResult JohnsonReweightSinglePair(Dictionary<string, List<Tuple<string, double>>> g, List<string> nodes, string start, string end)
        {
            // Johnson：通过 Bellman-Ford 求势能 h(v)，对边重加权后用 Dijkstra（可处理负权边，但不能有负环）
            if (g == null || g.Count == 0) return ShortestPathResult.Fail("图为空。");
            if (nodes == null || nodes.Count == 0) return ShortestPathResult.Fail("节点为空。");
            if (string.IsNullOrWhiteSpace(start) || string.IsNullOrWhiteSpace(end)) return ShortestPathResult.Fail("起点/终点为空。");
            if (string.Equals(start, end, StringComparison.OrdinalIgnoreCase))
                return ShortestPathResult.Success(new List<string> { start }, 0, "起点=终点");

            // 构造超级源 S，S->v 权重0
            string super = "__SUPER_SOURCE__";
            var gg = new Dictionary<string, List<Tuple<string, double>>>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in g)
                gg[kv.Key] = new List<Tuple<string, double>>(kv.Value);

            gg[super] = new List<Tuple<string, double>>();
            for (int i = 0; i < nodes.Count; i++)
            {
                gg[super].Add(Tuple.Create(nodes[i], 0.0));
            }

            var allNodes = new List<string>(nodes);
            if (!allNodes.Contains(super, StringComparer.OrdinalIgnoreCase)) allNodes.Add(super);

            // Bellman-Ford from super to get h(v)
            var bf = BellmanFord(gg, allNodes, super, end: super); // end 不用，借用 dist
            // 上面 BellmanFord 返回的是到 super 的结果，不合适；这里我们需要 dist 数组
            // 为避免改动 BellmanFord 的签名，下面单独跑一遍求势能
            var edges = new List<Tuple<string, string, double>>();
            foreach (var kv in gg)
            {
                var u = kv.Key;
                for (int i = 0; i < kv.Value.Count; i++)
                    edges.Add(Tuple.Create(u, kv.Value[i].Item1, kv.Value[i].Item2));
            }

            var h = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < allNodes.Count; i++) h[allNodes[i]] = double.PositiveInfinity;
            h[super] = 0;

            for (int it = 0; it < allNodes.Count - 1; it++)
            {
                bool changed = false;
                for (int i = 0; i < edges.Count; i++)
                {
                    var u = edges[i].Item1;
                    var v = edges[i].Item2;
                    var w = edges[i].Item3;

                    double hu;
                    if (!h.TryGetValue(u, out hu) || double.IsInfinity(hu)) continue;

                    double nd = hu + w;
                    double hv;
                    if (!h.TryGetValue(v, out hv) || nd < hv)
                    {
                        h[v] = nd;
                        changed = true;
                    }
                }
                if (!changed) break;
            }

            for (int i = 0; i < edges.Count; i++)
            {
                var u = edges[i].Item1;
                var v = edges[i].Item2;
                var w = edges[i].Item3;

                double hu;
                if (!h.TryGetValue(u, out hu) || double.IsInfinity(hu)) continue;

                double nd = hu + w;
                double hv;
                if (h.TryGetValue(v, out hv) && nd < hv)
                    return ShortestPathResult.Fail("检测到负环（Johnson 不可用），最短路径无定义。");
            }

            // 重加权
            var rw = new Dictionary<string, List<Tuple<string, double>>>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in g)
            {
                var u = kv.Key;
                if (!rw.ContainsKey(u)) rw[u] = new List<Tuple<string, double>>();
                for (int i = 0; i < kv.Value.Count; i++)
                {
                    var v = kv.Value[i].Item1;
                    var w = kv.Value[i].Item2;

                    double hu = h.ContainsKey(u) ? h[u] : 0;
                    double hv = h.ContainsKey(v) ? h[v] : 0;

                    if (double.IsInfinity(hu) || double.IsInfinity(hv))
                    {
                        // 不可达势能节点，仍然放入（但可能不可达）
                        rw[u].Add(Tuple.Create(v, w));
                    }
                    else
                    {
                        double wp = w + hu - hv; // >=0
                        rw[u].Add(Tuple.Create(v, wp));
                    }
                }
            }

            // ✅ 这里直接调用你工程里唯一的 Dijkstra.Run（不会重复定义）
            var dij = TrafficSystem.Dijkstra.Run(rw, start, end);
            var path = dij.Item1;
            var totalPrime = dij.Item2;

            if (path == null || path.Count == 0 || double.IsInfinity(totalPrime))
                return ShortestPathResult.Fail("不可达。");

            double hs = (h.ContainsKey(start) ? h[start] : 0);
            double ht = (h.ContainsKey(end) ? h[end] : 0);
            double total = totalPrime - hs + ht;

            return ShortestPathResult.Success(path, total);
        }
    }
}

namespace TaxiPathCalculation
{
    public class CalcAlgorithmMainForm : Form
    {
        private readonly string _currentUser;
        private readonly string _connStr;
        private readonly TrafficSystem.MainForm _owner;
        private readonly DataTable _sourceTable;

        public CalcAlgorithmMainForm(string currentUser, string connStr, TrafficSystem.MainForm owner, DataTable sourceTable = null)
        {
            _currentUser = string.IsNullOrWhiteSpace(currentUser) ? "未登录用户" : currentUser;
            _connStr = string.IsNullOrWhiteSpace(connStr) ? "DSN=TrafficDSN;" : connStr;

            _owner = owner;
            _sourceTable = sourceTable;

            InitMainFormStyle();
            AddAlgorithmButtons();

            // ✅ 引用“全局等比例缩放算法”（注册基准）
            // 该窗体控件在 AddAlgorithmButtons 已经创建完成，所以放在其后注册
            TrafficSystem.UiZoom.Register(this, scaleFormClientSize: true);
        }

        private void InitMainFormStyle()
        {
            this.BackColor = Color.FromArgb(245, 247, 250);
            this.Text = "轨迹数据计算 - 算法执行";
            this.Size = new Size(420, 160);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
        }

        private void AddAlgorithmButtons()
        {
            Button btnTaxiCalc = new Button
            {
                Text = "出租车轨迹计算（速度/方位角 + 距离）",
                Size = new Size(180, 40),
                Location = new Point(20, 40),
                Font = new Font("微软雅黑", 10F),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnTaxiCalc.FlatAppearance.BorderSize = 0;
            btnTaxiCalc.Click += (s, e) =>
            {
                try
                {
                    using (var f = new TrafficSystem.TaxiTrajectoryCalcForm(_currentUser, _connStr, _owner, _sourceTable))
                    {
                        f.ShowDialog(this);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("打开出租车轨迹计算窗口失败：\r\n" + ex.Message, "错误",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            this.Controls.Add(btnTaxiCalc);

            Button btnDijkstra = new Button
            {
                Text = "最短路径（算法可选）",
                Size = new Size(180, 40),
                Location = new Point(220, 40),
                Font = new Font("微软雅黑", 10F),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnDijkstra.FlatAppearance.BorderSize = 0;
            btnDijkstra.Click += (s, e) =>
            {
                try
                {
                    using (var dijkstraForm = new DijkstraForm(_currentUser, _connStr, _sourceTable))
                    {
                        dijkstraForm.ShowDialog(this);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("打开 最短路径 窗口失败：\r\n" + ex.Message, "错误",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            this.Controls.Add(btnDijkstra);
        }
    }

    public class DijkstraForm : Form
    {
        private readonly string _currentUser;
        private readonly string _connStr;
        private DataTable _sourceTable;

        private Panel panelTop;
        private ComboBox cbStart;
        private ComboBox cbEnd;
        private ComboBox cbWeight;
        private CheckBox chkUndirected;
        private Button btnRun;
        private Button btnSaveImg;
        private Label lblTotal;

        // ✅ 新增：算法选择
        private Label lblAlgo;
        private ComboBox cbAlgo;

        private SplitContainer split;
        private DataGridView dgvPath;
        private PictureBox pbGraph;

        private Dictionary<string, List<Tuple<string, double>>> _graph;
        private Dictionary<Tuple<string, string>, double> _edgeWeightMin;
        private List<string> _nodes;
        private List<string> _lastPathNodes = null;

        // ✅ 可选：如果数据表里带节点坐标，可用于 A* 启发（仅当权重列像“距离”时使用）
        private Dictionary<string, PointF> _nodeCoords = null;

        // ✅ 用于保证“注册缩放基准”只做一次（且在初始布局/分割条设置完成之后）
        private bool _uiZoomRegistered = false;

        public DijkstraForm(string currentUser, string connStr, DataTable sourceTable)
        {
            _currentUser = string.IsNullOrWhiteSpace(currentUser) ? "未登录用户" : currentUser;
            _connStr = string.IsNullOrWhiteSpace(connStr) ? "DSN=TrafficDSN;" : connStr;
            _sourceTable = sourceTable;

            InitUI();

            this.Shown += (s, e) =>
            {
                BeginInvoke(new Action(() =>
                {
                    split.Panel1MinSize = 140;
                    split.Panel2MinSize = 160;
                    SafeSetSplitterDistance(260);

                    LayoutTopBar();
                    TryInitData();

                    // ✅ 引用“全局等比例缩放算法”（注册基准）
                    // 关键点：DijkstraForm 会在 Shown 里调整 SplitterDistance 等布局，
                    // 所以把 Register 放到这里，保证“基准值”是最终初始布局，而不是默认布局
                    if (!_uiZoomRegistered)
                    {
                        TrafficSystem.UiZoom.Register(this, scaleFormClientSize: true);
                        _uiZoomRegistered = true;
                    }
                }));
            };

            this.Resize += (s, e) =>
            {
                LayoutTopBar();
                RedrawGraph();
            };

            panelTop.Resize += (s, e) => LayoutTopBar();
        }

        private void InitUI()
        {
            this.Text = "最短路径（算法可选）";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(980, 620);
            this.MinimumSize = new Size(820, 520);
            this.BackColor = Color.FromArgb(245, 247, 250);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            panelTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 74,
                BackColor = Color.White
            };
            this.Controls.Add(panelTop);

            var lblUser = new Label
            {
                Text = $"当前用户：{_currentUser}",
                Location = new Point(12, 10),
                Size = new Size(260, 18),
                ForeColor = Color.Gray,
                Font = new Font("微软雅黑", 9F)
            };
            panelTop.Controls.Add(lblUser);

            lblTotal = new Label
            {
                Text = "总权重：-",
                Location = new Point(280, 10),
                Size = new Size(520, 18),
                AutoEllipsis = true,
                ForeColor = Color.FromArgb(60, 60, 60),
                Font = new Font("微软雅黑", 9F)
            };
            panelTop.Controls.Add(lblTotal);

            // ✅ 新增：算法下拉（放在右上角，不挤压第二行控件）
            lblAlgo = new Label
            {
                Text = "算法：",
                Location = new Point(560, 10), // 具体位置由 LayoutTopBar 重算
                Size = new Size(46, 18),
                ForeColor = Color.FromArgb(60, 60, 60),
                Font = new Font("微软雅黑", 9F)
            };
            panelTop.Controls.Add(lblAlgo);

            cbAlgo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Size = new Size(220, 26),
                Location = new Point(610, 6) // 具体位置由 LayoutTopBar 重算
            };
            panelTop.Controls.Add(cbAlgo);

            // 填充算法列表（显示名 + Kind）
            cbAlgo.Items.Clear();
            cbAlgo.Items.Add(new AlgoItem("Dijkstra（非负权，常用）", TrafficSystem.ShortestPathAlgoKind.Dijkstra));
            cbAlgo.Items.Add(new AlgoItem("A*（有坐标更快；默认等价Dijkstra）", TrafficSystem.ShortestPathAlgoKind.AStar));
            cbAlgo.Items.Add(new AlgoItem("双向Dijkstra（大图更快）", TrafficSystem.ShortestPathAlgoKind.BiDijkstra));
            cbAlgo.Items.Add(new AlgoItem("双向A*（有坐标更快）", TrafficSystem.ShortestPathAlgoKind.BiAStar));
            cbAlgo.Items.Add(new AlgoItem("Bellman-Ford（可负权）", TrafficSystem.ShortestPathAlgoKind.BellmanFord));
            cbAlgo.Items.Add(new AlgoItem("SPFA（可负权，可能退化）", TrafficSystem.ShortestPathAlgoKind.SPFA));
            cbAlgo.Items.Add(new AlgoItem("DAG最短路（仅DAG）", TrafficSystem.ShortestPathAlgoKind.DagShortestPath));
            cbAlgo.Items.Add(new AlgoItem("Floyd-Warshall（全点对，O(n^3)）", TrafficSystem.ShortestPathAlgoKind.FloydWarshall));
            cbAlgo.Items.Add(new AlgoItem("Johnson（重加权，支持负权无负环）", TrafficSystem.ShortestPathAlgoKind.JohnsonReweight));
            cbAlgo.SelectedIndex = 0;

            var lblS = new Label { Text = "起点：", Location = new Point(12, 40), Size = new Size(46, 22), Font = new Font("微软雅黑", 9F) };
            panelTop.Controls.Add(lblS);

            cbStart = new ComboBox
            {
                Location = new Point(58, 38),
                Size = new Size(180, 26),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            panelTop.Controls.Add(cbStart);

            var lblE = new Label { Text = "终点：", Location = new Point(250, 40), Size = new Size(46, 22), Font = new Font("微软雅黑", 9F) };
            panelTop.Controls.Add(lblE);

            cbEnd = new ComboBox
            {
                Location = new Point(296, 38),
                Size = new Size(180, 26),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            panelTop.Controls.Add(cbEnd);

            var lblW = new Label { Text = "权重列：", Location = new Point(486, 40), Size = new Size(60, 22), Font = new Font("微软雅黑", 9F) };
            panelTop.Controls.Add(lblW);

            cbWeight = new ComboBox
            {
                Location = new Point(546, 38),
                Size = new Size(170, 26),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            panelTop.Controls.Add(cbWeight);

            chkUndirected = new CheckBox
            {
                Text = "按无向图计算",
                Location = new Point(724, 40),
                Size = new Size(110, 22),
                Font = new Font("微软雅黑", 9F),
                Checked = true
            };
            panelTop.Controls.Add(chkUndirected);

            btnRun = new Button
            {
                Text = "开始计算",
                Size = new Size(110, 32),
                Font = new Font("微软雅黑", 9F, FontStyle.Bold),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnRun.FlatAppearance.BorderSize = 0;
            btnRun.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnRun.Click += (s, e) => RunDijkstraAndRender(); // 保留原方法名（内部按算法选择执行）
            panelTop.Controls.Add(btnRun);

            btnSaveImg = new Button
            {
                Text = "保存图片",
                Size = new Size(110, 26),
                Font = new Font("微软雅黑", 9F),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(30, 30, 30)
            };
            btnSaveImg.FlatAppearance.BorderSize = 1;
            btnSaveImg.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            btnSaveImg.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSaveImg.Click += (s, e) => SaveGraphImageToLocal();
            panelTop.Controls.Add(btnSaveImg);

            split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterWidth = 6,
                BackColor = Color.FromArgb(245, 247, 250),
                Panel1MinSize = 0,
                Panel2MinSize = 0
            };
            this.Controls.Add(split);

            dgvPath = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                BackgroundColor = Color.White
            };
            split.Panel1.Controls.Add(dgvPath);

            pbGraph = new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            split.Panel2.Controls.Add(pbGraph);

            var cms = new ContextMenuStrip();
            var miSave = new ToolStripMenuItem("保存当前图到本地");
            miSave.Click += (s, e) => SaveGraphImageToLocal();
            cms.Items.Add(miSave);
            pbGraph.ContextMenuStrip = cms;

            LayoutTopBar();
        }

        private void LayoutTopBar()
        {
            if (panelTop == null || btnRun == null || btnSaveImg == null) return;

            int rightMargin = 12;

            btnSaveImg.Location = new Point(panelTop.ClientSize.Width - rightMargin - btnSaveImg.Width, 6);
            btnRun.Location = new Point(panelTop.ClientSize.Width - rightMargin - btnRun.Width, panelTop.Height - btnRun.Height - 8);

            btnSaveImg.BringToFront();
            btnRun.BringToFront();

            // ✅ 算法下拉：放在保存按钮左侧
            if (cbAlgo != null && lblAlgo != null)
            {
                int algoRight = btnSaveImg.Left - 10;
                int algoW = cbAlgo.Width;
                int algoLeft = algoRight - algoW;
                if (algoLeft < 420) // 兜底：窗口太窄时缩一下
                {
                    algoLeft = 420;
                    algoW = Math.Max(140, algoRight - algoLeft);
                    cbAlgo.Width = algoW;
                }
                cbAlgo.Location = new Point(algoLeft, 6);
                lblAlgo.Location = new Point(cbAlgo.Left - 46, 10);
            }

            // lblTotal 自动避让到 “算法标签” 左边
            int rightBlockLeft = (lblAlgo != null) ? (lblAlgo.Left - 10) : (btnSaveImg.Left - 10);
            int labelLeft = lblTotal.Left;
            int newWidth = Math.Max(180, rightBlockLeft - labelLeft);
            lblTotal.Width = newWidth;
            lblTotal.AutoEllipsis = true;
        }

        private void SaveGraphImageToLocal()
        {
            try
            {
                if (pbGraph == null || pbGraph.Image == null)
                {
                    MessageBox.Show("当前没有可保存的图。\r\n请先生成最短路径并显示图后再保存。", "提示",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                string start = cbStart?.SelectedItem != null ? cbStart.SelectedItem.ToString() : "Start";
                string end = cbEnd?.SelectedItem != null ? cbEnd.SelectedItem.ToString() : "End";
                string wcol = cbWeight?.SelectedItem != null ? cbWeight.SelectedItem.ToString() : "Weight";
                string algo = GetSelectedAlgoNameForFile();
                string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");

                string safe(string s)
                {
                    if (string.IsNullOrEmpty(s)) return "X";
                    foreach (var ch in Path.GetInvalidFileNameChars()) s = s.Replace(ch, '_');
                    s = s.Replace(' ', '_');
                    return s;
                }

                string defaultName = $"ShortestPath_{safe(algo)}_{safe(start)}_{safe(end)}_{safe(wcol)}_{ts}.png";

                using (var sfd = new SaveFileDialog())
                {
                    sfd.Title = "保存生成的图";
                    sfd.Filter = "PNG 图片 (*.png)|*.png|JPG 图片 (*.jpg)|*.jpg|BMP 图片 (*.bmp)|*.bmp";
                    sfd.FileName = defaultName;
                    sfd.RestoreDirectory = true;

                    if (sfd.ShowDialog(this) != DialogResult.OK) return;

                    ImageFormat fmt = ImageFormat.Png;
                    string ext = (Path.GetExtension(sfd.FileName) ?? "").ToLowerInvariant();
                    if (ext == ".jpg" || ext == ".jpeg") fmt = ImageFormat.Jpeg;
                    else if (ext == ".bmp") fmt = ImageFormat.Bmp;

                    using (var bmp = new Bitmap(pbGraph.Image))
                    {
                        bmp.Save(sfd.FileName, fmt);
                    }

                    MessageBox.Show("图片已保存：\r\n" + sfd.FileName, "保存成功",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存图片失败：\r\n" + ex.Message, "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetSelectedAlgoNameForFile()
        {
            var it = cbAlgo != null ? cbAlgo.SelectedItem as AlgoItem : null;
            if (it == null) return "Dijkstra";
            return it.Text;
        }

        private void SafeSetSplitterDistance(int desired)
        {
            if (split == null) return;

            int total = (split.Orientation == Orientation.Horizontal)
                ? split.ClientSize.Height
                : split.ClientSize.Width;

            int min1 = split.Panel1MinSize;
            int min2 = split.Panel2MinSize;
            int sw = split.SplitterWidth;

            if (total <= sw + 2) return;

            int max = total - min2 - sw;
            if (max < min1) max = min1;

            int val = desired;
            if (val < min1) val = min1;
            if (val > max) val = max;

            split.SplitterDistance = val;
        }

        private void TryInitData()
        {
            if (_sourceTable == null)
            {
                MessageBox.Show("当前没有传入路径数据。\r\n请先在主界面从云端/本地导入“路径数据表”，然后再点【计算数据】进入本窗口。");
                return;
            }

            if (_sourceTable.Columns.Count == 0 || _sourceTable.Rows.Count == 0)
            {
                MessageBox.Show("路径数据表为空，无法计算。");
                return;
            }

            cbWeight.Items.Clear();
            foreach (DataColumn c in _sourceTable.Columns) cbWeight.Items.Add(c.ColumnName);

            string defaultWeight = FindColumn(_sourceTable,
                new[] { "行驶时间分钟", "行驶时间", "路程时间", "时间", "耗时", "cost", "weight", "距离", "distance" });

            if (!string.IsNullOrEmpty(defaultWeight) && cbWeight.Items.Contains(defaultWeight))
                cbWeight.SelectedItem = defaultWeight;
            else if (cbWeight.Items.Count > 0)
                cbWeight.SelectedIndex = 0;

            BuildGraphFromTable();

            // ✅ 尝试从表里提取节点坐标（若存在，用于 A* 启发）
            _nodeCoords = TryBuildNodeCoordsFromTable(_sourceTable);

            cbStart.Items.Clear();
            cbEnd.Items.Clear();
            foreach (var n in _nodes)
            {
                cbStart.Items.Add(n);
                cbEnd.Items.Add(n);
            }

            if (cbStart.Items.Count > 0) cbStart.SelectedIndex = 0;
            if (cbEnd.Items.Count > 1) cbEnd.SelectedIndex = 1;
            else if (cbEnd.Items.Count > 0) cbEnd.SelectedIndex = 0;

            cbWeight.SelectedIndexChanged += (s, e) =>
            {
                BuildGraphFromTable();
                _nodeCoords = TryBuildNodeCoordsFromTable(_sourceTable);

                _lastPathNodes = null;
                dgvPath.DataSource = null;
                lblTotal.Text = "总权重：-";
                RedrawGraph();
            };

            chkUndirected.CheckedChanged += (s, e) =>
            {
                BuildGraphFromTable();
                _nodeCoords = TryBuildNodeCoordsFromTable(_sourceTable);

                _lastPathNodes = null;
                dgvPath.DataSource = null;
                lblTotal.Text = "总权重：-";
                RedrawGraph();
            };

            RedrawGraph();
        }

        private string FindColumn(DataTable dt, IEnumerable<string> names)
        {
            foreach (var n in names)
            {
                foreach (DataColumn c in dt.Columns)
                {
                    if (string.Equals(c.ColumnName?.Trim(), n, StringComparison.OrdinalIgnoreCase))
                        return c.ColumnName;
                }
            }
            return null;
        }

        private void BuildGraphFromTable()
        {
            _graph = new Dictionary<string, List<Tuple<string, double>>>(StringComparer.OrdinalIgnoreCase);
            _edgeWeightMin = new Dictionary<Tuple<string, string>, double>(new PairComparer());

            string colStart = FindColumn(_sourceTable, new[] { "起点名称", "起点", "Start", "from", "from_name", "起点地名" })
                              ?? _sourceTable.Columns[0].ColumnName;

            string colEnd = FindColumn(_sourceTable, new[] { "终点名称", "终点", "End", "to", "to_name", "终点地名" })
                            ?? (_sourceTable.Columns.Count > 1 ? _sourceTable.Columns[1].ColumnName : _sourceTable.Columns[0].ColumnName);

            string colWeight = cbWeight.SelectedItem != null ? cbWeight.SelectedItem.ToString() : null;
            if (string.IsNullOrEmpty(colWeight))
                colWeight = FindColumn(_sourceTable, new[] { "行驶时间分钟", "行驶时间", "路程时间", "时间", "cost", "weight" });

            bool undirected = chkUndirected.Checked;

            foreach (DataRow r in _sourceTable.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;

                string s = SafeStr(r[colStart]);
                string t = SafeStr(r[colEnd]);
                if (string.IsNullOrWhiteSpace(s) || string.IsNullOrWhiteSpace(t)) continue;

                double w = SafeDouble(r[colWeight], 1);

                AddEdge(s, t, w);
                if (undirected) AddEdge(t, s, w);
            }

            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in _graph)
            {
                set.Add(kv.Key);
                foreach (var e in kv.Value) set.Add(e.Item1);
            }
            _nodes = set.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private void AddEdge(string s, string t, double w)
        {
            if (!_graph.ContainsKey(s)) _graph[s] = new List<Tuple<string, double>>();
            _graph[s].Add(Tuple.Create(t, w));

            var key = Tuple.Create(s, t);
            double old;
            if (_edgeWeightMin.TryGetValue(key, out old))
            {
                if (w < old) _edgeWeightMin[key] = w;
            }
            else _edgeWeightMin[key] = w;
        }

        private string SafeStr(object v)
        {
            if (v == null || v == DBNull.Value) return "";
            return (v.ToString() ?? "").Trim();
        }

        private double SafeDouble(object v, double fallback)
        {
            if (v == null || v == DBNull.Value) return fallback;
            string s = (v.ToString() ?? "").Trim();
            if (string.IsNullOrWhiteSpace(s)) return fallback;

            s = s.Replace("，", ".").Replace(",", ".").Trim();

            double d;
            if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out d)) return d;
            if (double.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out d)) return d;
            return fallback;
        }

        // ✅ 新增：从边表中尝试提取节点坐标（起点/终点坐标列）
        private Dictionary<string, PointF> TryBuildNodeCoordsFromTable(DataTable dt)
        {
            try
            {
                if (dt == null || dt.Columns.Count == 0 || dt.Rows.Count == 0) return null;

                string colStart = FindColumn(dt, new[] { "起点名称", "起点", "Start", "from", "from_name", "起点地名" }) ?? dt.Columns[0].ColumnName;
                string colEnd = FindColumn(dt, new[] { "终点名称", "终点", "End", "to", "to_name", "终点地名" })
                                ?? (dt.Columns.Count > 1 ? dt.Columns[1].ColumnName : dt.Columns[0].ColumnName);

                // 常见坐标列名
                string sx = FindColumn(dt, new[] { "起点X", "起点x", "起点经度", "起点lng", "起点lon", "起点longitude" });
                string sy = FindColumn(dt, new[] { "起点Y", "起点y", "起点纬度", "起点lat", "起点latitude" });
                string ex = FindColumn(dt, new[] { "终点X", "终点x", "终点经度", "终点lng", "终点lon", "终点longitude" });
                string ey = FindColumn(dt, new[] { "终点Y", "终点y", "终点纬度", "终点lat", "终点latitude" });

                if (string.IsNullOrEmpty(sx) || string.IsNullOrEmpty(sy) || string.IsNullOrEmpty(ex) || string.IsNullOrEmpty(ey))
                    return null;

                var acc = new Dictionary<string, List<PointF>>(StringComparer.OrdinalIgnoreCase);

                foreach (DataRow r in dt.Rows)
                {
                    if (r.RowState == DataRowState.Deleted) continue;

                    string sName = SafeStr(r[colStart]);
                    string eName = SafeStr(r[colEnd]);
                    if (!string.IsNullOrWhiteSpace(sName))
                    {
                        double x = SafeDouble(r[sx], double.NaN);
                        double y = SafeDouble(r[sy], double.NaN);
                        if (!double.IsNaN(x) && !double.IsNaN(y))
                        {
                            if (!acc.ContainsKey(sName)) acc[sName] = new List<PointF>();
                            acc[sName].Add(new PointF((float)x, (float)y));
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(eName))
                    {
                        double x = SafeDouble(r[ex], double.NaN);
                        double y = SafeDouble(r[ey], double.NaN);
                        if (!double.IsNaN(x) && !double.IsNaN(y))
                        {
                            if (!acc.ContainsKey(eName)) acc[eName] = new List<PointF>();
                            acc[eName].Add(new PointF((float)x, (float)y));
                        }
                    }
                }

                if (acc.Count == 0) return null;

                var res = new Dictionary<string, PointF>(StringComparer.OrdinalIgnoreCase);
                foreach (var kv in acc)
                {
                    float ax = 0, ay = 0;
                    int c = kv.Value.Count;
                    for (int i = 0; i < c; i++) { ax += kv.Value[i].X; ay += kv.Value[i].Y; }
                    res[kv.Key] = new PointF(ax / c, ay / c);
                }
                return res;
            }
            catch
            {
                return null;
            }
        }

        // ✅ 核心：不再固定 Dijkstra，而是按下拉框选择运行
        private void RunDijkstraAndRender()
        {
            if (_graph == null || _graph.Count == 0)
            {
                MessageBox.Show("当前图为空，无法计算。请检查路径数据是否包含起点/终点/权重。");
                return;
            }

            if (cbStart.SelectedItem == null || cbEnd.SelectedItem == null)
            {
                MessageBox.Show("请选择起点和终点。");
                return;
            }

            string start = cbStart.SelectedItem.ToString();
            string end = cbEnd.SelectedItem.ToString();

            var algoItem = cbAlgo != null ? cbAlgo.SelectedItem as AlgoItem : null;
            var algoKind = (algoItem != null) ? algoItem.Kind : TrafficSystem.ShortestPathAlgoKind.Dijkstra;
            var algoName = (algoItem != null) ? algoItem.Text : "Dijkstra";

            if (string.Equals(start, end, StringComparison.OrdinalIgnoreCase))
            {
                _lastPathNodes = new List<string> { start };
                dgvPath.DataSource = BuildResultTable(_lastPathNodes, 0);
                lblTotal.Text = $"[{algoName}] 总权重：0（起点=终点）";
                RedrawGraph();
                return;
            }

            TrafficSystem.ShortestPathResult r = null;

            try
            {
                // A* / 双向A* 可用启发：只有当权重像“距离”，并且我们提取到了节点坐标，才用欧氏距离；否则启发=0（保证最优）
                Func<string, string, double> heuristic = (a, b) => 0;
                bool weightLooksDistance = false;
                string wcol = cbWeight.SelectedItem != null ? cbWeight.SelectedItem.ToString() : "";
                if (!string.IsNullOrEmpty(wcol))
                {
                    string ww = wcol.ToLowerInvariant();
                    if (ww.Contains("距离") || ww.Contains("distance") || ww.Contains("length") || ww.Contains("len"))
                        weightLooksDistance = true;
                }

                if (_nodeCoords != null && _nodeCoords.Count > 0 && weightLooksDistance)
                {
                    heuristic = (a, b) =>
                    {
                        PointF pa, pb;
                        if (!_nodeCoords.TryGetValue(a, out pa)) return 0;
                        if (!_nodeCoords.TryGetValue(b, out pb)) return 0;
                        double dx = pa.X - pb.X;
                        double dy = pa.Y - pb.Y;
                        return Math.Sqrt(dx * dx + dy * dy);
                    };
                }

                switch (algoKind)
                {
                    case TrafficSystem.ShortestPathAlgoKind.Dijkstra:
                        // ✅ 只调用唯一一份算法：TrafficSystem.Dijkstra.Run
                        var res = TrafficSystem.Dijkstra.Run(_graph, start, end);
                        var path = res.Item1;
                        double total = res.Item2;

                        if (path == null || path.Count == 0 || double.IsInfinity(total))
                            r = TrafficSystem.ShortestPathResult.Fail("不可达。");
                        else
                            r = TrafficSystem.ShortestPathResult.Success(path, total);
                        break;

                    case TrafficSystem.ShortestPathAlgoKind.AStar:
                        r = TrafficSystem.ShortestPathAlgorithms.AStar(_graph, start, end, heuristic);
                        break;

                    case TrafficSystem.ShortestPathAlgoKind.BiDijkstra:
                        r = TrafficSystem.ShortestPathAlgorithms.BidirectionalDijkstra(_graph, start, end);
                        break;

                    case TrafficSystem.ShortestPathAlgoKind.BiAStar:
                        r = TrafficSystem.ShortestPathAlgorithms.BidirectionalAStar(_graph, start, end, heuristic);
                        break;

                    case TrafficSystem.ShortestPathAlgoKind.BellmanFord:
                        r = TrafficSystem.ShortestPathAlgorithms.BellmanFord(_graph, _nodes, start, end);
                        break;

                    case TrafficSystem.ShortestPathAlgoKind.SPFA:
                        r = TrafficSystem.ShortestPathAlgorithms.SPFA(_graph, _nodes, start, end);
                        break;

                    case TrafficSystem.ShortestPathAlgoKind.DagShortestPath:
                        r = TrafficSystem.ShortestPathAlgorithms.DagShortestPath(_graph, _nodes, start, end);
                        break;

                    case TrafficSystem.ShortestPathAlgoKind.FloydWarshall:
                        r = TrafficSystem.ShortestPathAlgorithms.FloydWarshallSinglePair(_graph, _nodes, start, end);
                        break;

                    case TrafficSystem.ShortestPathAlgoKind.JohnsonReweight:
                        r = TrafficSystem.ShortestPathAlgorithms.JohnsonReweightSinglePair(_graph, _nodes, start, end);
                        break;

                    default:
                        r = TrafficSystem.ShortestPathResult.Fail("未知算法选择。");
                        break;
                }
            }
            catch (Exception ex)
            {
                r = TrafficSystem.ShortestPathResult.Fail("计算异常：" + ex.Message);
            }

            if (r == null || !r.Ok || r.Path == null || r.Path.Count == 0 || double.IsInfinity(r.Total))
            {
                _lastPathNodes = null;
                dgvPath.DataSource = null;
                lblTotal.Text = $"[{algoName}] 总权重：∞（不可达/失败）";
                MessageBox.Show($"[{algoName}] 从【{start}】到【{end}】不可达或计算失败。\r\n{(r != null ? r.Message : "")}");
                RedrawGraph();
                return;
            }

            _lastPathNodes = r.Path;
            dgvPath.DataSource = BuildResultTable(r.Path, r.Total);
            lblTotal.Text = $"[{algoName}] 总权重：{r.Total:F3}（起点：{start} → 终点：{end}）";
            RedrawGraph();
        }

        private DataTable BuildResultTable(List<string> path, double total)
        {
            var dt = new DataTable();
            dt.Columns.Add("序号");
            dt.Columns.Add("起点名称");
            dt.Columns.Add("终点名称");
            dt.Columns.Add("权重");
            dt.Columns.Add("累计");

            double cum = 0;
            for (int i = 0; i < path.Count - 1; i++)
            {
                string s = path[i];
                string t = path[i + 1];

                double w = 0;
                double ww;
                if (_edgeWeightMin.TryGetValue(Tuple.Create(s, t), out ww)) w = ww;

                cum += w;

                var row = dt.NewRow();
                row["序号"] = (i + 1).ToString();
                row["起点名称"] = s;
                row["终点名称"] = t;
                row["权重"] = w.ToString("F3");
                row["累计"] = cum.ToString("F3");
                dt.Rows.Add(row);
            }

            var sum = dt.NewRow();
            sum["序号"] = "";
            sum["起点名称"] = "总计";
            sum["终点名称"] = "";
            sum["权重"] = total.ToString("F3");
            sum["累计"] = total.ToString("F3");
            dt.Rows.Add(sum);

            return dt;
        }

        private void RedrawGraph()
        {
            if (pbGraph == null) return;

            if (_graph == null || _graph.Count == 0)
            {
                if (pbGraph.Image != null)
                {
                    var old = pbGraph.Image;
                    pbGraph.Image = null;
                    old.Dispose();
                }
                return;
            }

            int w = Math.Max(400, pbGraph.ClientSize.Width);
            int h = Math.Max(240, pbGraph.ClientSize.Height);

            var bmp = new Bitmap(w, h);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.White);

                var nodes = _nodes ?? new List<string>();
                int n = nodes.Count;
                if (n == 0)
                {
                    g.DrawString("无节点", new Font("微软雅黑", 12), Brushes.Gray, 10, 10);
                    goto OUT;
                }

                int cx = w / 2, cy = h / 2;
                int radius = Math.Max(80, Math.Min(cx, cy) - 90);

                var pos = new Dictionary<string, PointF>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < n; i++)
                {
                    double ang = 2 * Math.PI * i / n - Math.PI / 2;
                    float x = cx + (float)(radius * Math.Cos(ang));
                    float y = cy + (float)(radius * Math.Sin(ang));
                    pos[nodes[i]] = new PointF(x, y);
                }

                var allEdges = new List<Tuple<string, string, double>>();
                foreach (var kv in _edgeWeightMin)
                    allEdges.Add(Tuple.Create(kv.Key.Item1, kv.Key.Item2, kv.Value));

                var pathEdges = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (_lastPathNodes != null && _lastPathNodes.Count >= 2)
                {
                    for (int i = 0; i < _lastPathNodes.Count - 1; i++)
                        pathEdges.Add(_lastPathNodes[i] + "->" + _lastPathNodes[i + 1]);
                }

                float nodeR = Math.Max(12f, Math.Min(w, h) / 30f);
                using (var penEdge = new Pen(Color.LightGray, 1.4f))
                using (var penPath = new Pen(Color.Red, 3.0f))
                using (var penNode = new Pen(Color.DarkGray, 1.4f))
                using (var font = new Font("微软雅黑", 9F))
                {
                    foreach (var e in allEdges)
                    {
                        if (!pos.ContainsKey(e.Item1) || !pos.ContainsKey(e.Item2)) continue;

                        var p1 = pos[e.Item1];
                        var p2 = pos[e.Item2];

                        var v = Normalize(p2.X - p1.X, p2.Y - p1.Y);
                        var a = new PointF(p1.X + v.X * nodeR, p1.Y + v.Y * nodeR);
                        var b = new PointF(p2.X - v.X * nodeR, p2.Y - v.Y * nodeR);

                        bool isPath = pathEdges.Contains(e.Item1 + "->" + e.Item2);
                        g.DrawLine(isPath ? penPath : penEdge, a, b);

                        var mid = new PointF((a.X + b.X) / 2f, (a.Y + b.Y) / 2f);
                        string wt = e.Item3.ToString("F0");
                        var sz = g.MeasureString(wt, font);
                        g.FillRectangle(Brushes.White, mid.X - sz.Width / 2f - 2, mid.Y - sz.Height / 2f - 1, sz.Width + 4, sz.Height + 2);
                        g.DrawString(wt, font, Brushes.Black, mid.X - sz.Width / 2f, mid.Y - sz.Height / 2f);
                    }

                    foreach (var kv in pos)
                    {
                        var p = kv.Value;
                        g.FillEllipse(Brushes.White, p.X - nodeR, p.Y - nodeR, nodeR * 2, nodeR * 2);
                        g.DrawEllipse(penNode, p.X - nodeR, p.Y - nodeR, nodeR * 2, nodeR * 2);

                        var name = kv.Key;
                        var sz = g.MeasureString(name, font);
                        g.DrawString(name, font, Brushes.DarkBlue, p.X - sz.Width / 2f, p.Y - sz.Height / 2f - nodeR - 6);
                    }
                }
            }

        OUT:
            if (pbGraph.Image != null)
            {
                var old = pbGraph.Image;
                pbGraph.Image = null;
                old.Dispose();
            }
            pbGraph.Image = bmp;
        }

        private PointF Normalize(float x, float y)
        {
            float len = (float)Math.Sqrt(x * x + y * y);
            if (len < 1e-6) return new PointF(0, 0);
            return new PointF(x / len, y / len);
        }

        private class PairComparer : IEqualityComparer<Tuple<string, string>>
        {
            public bool Equals(Tuple<string, string> x, Tuple<string, string> y)
            {
                return string.Equals(x.Item1, y.Item1, StringComparison.OrdinalIgnoreCase) &&
                       string.Equals(x.Item2, y.Item2, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(Tuple<string, string> obj)
            {
                unchecked
                {
                    int h1 = (obj.Item1 ?? "").ToLowerInvariant().GetHashCode();
                    int h2 = (obj.Item2 ?? "").ToLowerInvariant().GetHashCode();
                    return (h1 * 397) ^ h2;
                }
            }
        }

        private sealed class AlgoItem
        {
            public string Text;
            public TrafficSystem.ShortestPathAlgoKind Kind;
            public AlgoItem(string text, TrafficSystem.ShortestPathAlgoKind kind)
            {
                Text = text;
                Kind = kind;
            }
            public override string ToString() { return Text; }
        }
    }
}
