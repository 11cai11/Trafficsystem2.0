using System;
using System.Data;
using System.Data.Odbc;
using System.Configuration;
using System.IO;
using System.Text;

namespace TrafficSystem
{
    public static class DBHelper
    {
        // ✅ 只允许这个 DSN（名字必须完全一致：OKS）
        private const string OnlyDsnConnStr = "DSN=OKS;";

        // ✅ 给其它窗体读取（即使 App.config 没配，也强制用 DSN=OKS;）
        public static string ConnStr
        {
            get
            {
                // 允许你在 App.config 写 DbConn，但只接受 DSN=OKS;
                string cfg = "";
                try { cfg = ConfigurationManager.AppSettings["DbConn"] ?? ""; } catch { }

                if (!string.IsNullOrWhiteSpace(cfg))
                {
                    // 只要不是 DSN=OKS; 形式，一律忽略，强制用 OnlyDsnConnStr
                    if (cfg.Trim().IndexOf("DSN=OKS", StringComparison.OrdinalIgnoreCase) >= 0)
                        return OnlyDsnConnStr;
                }

                return OnlyDsnConnStr;
            }
        }

        // MySQL 表/列名安全引用（支持中文表名）：反引号包裹
        private static string Q(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                throw new ArgumentException("identifier 不能为空");
            return "`" + identifier.Replace("`", "``") + "`";
        }

        private static OdbcConnection OpenConn()
        {
            // ✅ 只用 DSN=OKS;
            var conn = new OdbcConnection(OnlyDsnConnStr);
            conn.Open();
            return conn;
        }

        public static DataTable GetTable(string tableName)
        {
            using (var conn = OpenConn())
            {
                string sql = $"SELECT * FROM {Q(tableName)}";
                using (var da = new OdbcDataAdapter(sql, conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    return dt;
                }
            }
        }

        public static int ExecuteNonQuery(string sql, params OdbcParameter[] pars)
        {
            using (var conn = OpenConn())
            using (var cmd = new OdbcCommand(sql, conn))
            {
                if (pars != null)
                {
                    foreach (var p in pars) cmd.Parameters.Add(p);
                }
                return cmd.ExecuteNonQuery();
            }
        }

        public static DataTable QueryWithWhere(string tableName, string whereClause, params OdbcParameter[] pars)
        {
            using (var conn = OpenConn())
            {
                string sql = $"SELECT * FROM {Q(tableName)}";
                if (!string.IsNullOrWhiteSpace(whereClause))
                    sql += " WHERE " + whereClause;

                using (var cmd = new OdbcCommand(sql, conn))
                {
                    if (pars != null && pars.Length > 0)
                    {
                        foreach (var p in pars) cmd.Parameters.Add(p);
                    }

                    using (var da = new OdbcDataAdapter(cmd))
                    {
                        var dt = new DataTable();
                        da.Fill(dt);
                        return dt;
                    }
                }
            }
        }

        public static void ExportToCsv(DataTable dt, string path)
        {
            if (dt == null) throw new ArgumentNullException(nameof(dt));
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("path 不能为空");

            // ✅ UTF8 带 BOM（Excel 友好）
            using (var sw = new StreamWriter(path, false, new UTF8Encoding(true)))
            {
                // header
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    if (i > 0) sw.Write(",");
                    sw.Write("\"" + (dt.Columns[i].ColumnName ?? "").Replace("\"", "\"\"") + "\"");
                }
                sw.WriteLine();

                // rows
                foreach (DataRow r in dt.Rows)
                {
                    for (int i = 0; i < dt.Columns.Count; i++)
                    {
                        if (i > 0) sw.Write(",");
                        var s = r[i]?.ToString() ?? "";
                        s = s.Replace("\"", "\"\"");
                        sw.Write("\"" + s + "\"");
                    }
                    sw.WriteLine();
                }
            }
        }
    }
}
