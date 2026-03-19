using System;
using System.Collections.Generic;

namespace TrafficSystem
{
    public class SimplePriorityQueue<T>
    {
        private readonly List<Tuple<T, double>> heap = new List<Tuple<T, double>>();

        public int Count => heap.Count;

        public void Enqueue(T item, double priority)
        {
            heap.Add(Tuple.Create(item, priority));
            SiftUp(heap.Count - 1);
        }

        public Tuple<T, double> Dequeue()
        {
            if (heap.Count == 0) throw new InvalidOperationException("队列为空");

            var root = heap[0];
            var last = heap[heap.Count - 1];
            heap.RemoveAt(heap.Count - 1);

            if (heap.Count > 0)
            {
                heap[0] = last;
                SiftDown(0);
            }
            return root;
        }

        private void SiftUp(int i)
        {
            while (i > 0)
            {
                int p = (i - 1) / 2;
                if (heap[i].Item2 < heap[p].Item2)
                {
                    Swap(i, p);
                    i = p;
                }
                else break;
            }
        }

        private void SiftDown(int i)
        {
            int n = heap.Count;
            while (true)
            {
                int l = 2 * i + 1;
                int r = 2 * i + 2;
                int smallest = i;

                if (l < n && heap[l].Item2 < heap[smallest].Item2) smallest = l;
                if (r < n && heap[r].Item2 < heap[smallest].Item2) smallest = r;

                if (smallest != i)
                {
                    Swap(i, smallest);
                    i = smallest;
                }
                else break;
            }
        }

        private void Swap(int i, int j)
        {
            var tmp = heap[i];
            heap[i] = heap[j];
            heap[j] = tmp;
        }
    }

    public static class Dijkstra
    {
        /// <summary>
        /// 返回：(路径节点列表, 总权重)
        /// 若不可达：路径空、总权重为 PositiveInfinity
        /// </summary>
        public static Tuple<List<string>, double> Run(
            Dictionary<string, List<Tuple<string, double>>> graph,
            string start,
            string end)
        {
            if (graph == null || graph.Count == 0)
                return Tuple.Create(new List<string>(), double.PositiveInfinity);

            if (string.IsNullOrWhiteSpace(start) || string.IsNullOrWhiteSpace(end))
                return Tuple.Create(new List<string>(), double.PositiveInfinity);

            var dist = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var prev = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // 收集所有节点（包括只出现在边终点的）
            foreach (var kv in graph)
            {
                if (!dist.ContainsKey(kv.Key))
                    dist[kv.Key] = double.PositiveInfinity;

                if (kv.Value != null)
                {
                    foreach (var edge in kv.Value)
                    {
                        if (!dist.ContainsKey(edge.Item1))
                            dist[edge.Item1] = double.PositiveInfinity;
                    }
                }
            }

            if (!dist.ContainsKey(start) || !dist.ContainsKey(end))
                return Tuple.Create(new List<string>(), double.PositiveInfinity);

            dist[start] = 0;

            var pq = new SimplePriorityQueue<string>();
            pq.Enqueue(start, 0);

            while (pq.Count > 0)
            {
                var nodeWithPri = pq.Dequeue();
                string u = nodeWithPri.Item1;
                double currentPri = nodeWithPri.Item2;

                // 旧条目直接跳过（因为我们会重复入队）
                if (currentPri > dist[u]) continue;

                if (string.Equals(u, end, StringComparison.OrdinalIgnoreCase))
                    break;

                if (!graph.ContainsKey(u) || graph[u] == null) continue;

                foreach (var edge in graph[u])
                {
                    string v = edge.Item1;
                    double w = edge.Item2;

                    // Dijkstra 不支持负权
                    if (w < 0) continue;

                    double alt = dist[u] + w;
                    if (alt < dist[v])
                    {
                        dist[v] = alt;
                        prev[v] = u;
                        pq.Enqueue(v, alt);
                    }
                }
            }

            if (!dist.ContainsKey(end) || double.IsPositiveInfinity(dist[end]))
                return Tuple.Create(new List<string>(), double.PositiveInfinity);

            // 回溯路径
            var path = new List<string>();
            string curNode = end;
            while (curNode != null)
            {
                path.Insert(0, curNode);
                if (!prev.ContainsKey(curNode)) break;
                curNode = prev[curNode];
            }

            if (path.Count == 0 || !string.Equals(path[0], start, StringComparison.OrdinalIgnoreCase))
                return Tuple.Create(new List<string>(), double.PositiveInfinity);

            return Tuple.Create(path, dist[end]);
        }
    }
}
