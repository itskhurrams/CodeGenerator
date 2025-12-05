using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Data.SqlClient;

namespace EntityGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Data.SqlClient;

    namespace EntityGenerator.Wpf
    {
        public class ColumnInfo
        {
            public string ColumnName { get; set; } = "";
            public string SqlType { get; set; } = "";
            public bool IsNullable { get; set; }
            public bool IsPrimaryKey { get; set; }
        }

        public static class SchemaReader
        {
            public static List<string> GetTables(SqlConnection conn)
            {
                var tables = new List<string>();

                using var cmd = new SqlCommand(@"
                        SELECT TABLE_NAME
                        FROM INFORMATION_SCHEMA.TABLES
                        WHERE TABLE_TYPE = 'BASE TABLE'
                          AND TABLE_SCHEMA = 'dbo'
                          AND TABLE_NAME NOT LIKE 'spt[_]%'
                          AND TABLE_NAME NOT IN ('sysdiagrams')
                        ORDER BY TABLE_NAME", conn);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    tables.Add(reader.GetString(0));
                }

                return tables;
            }


            public static List<ColumnInfo> GetColumns(SqlConnection conn, string tableName)
            {
                var columns = new List<ColumnInfo>();

                using (var cmd = new SqlCommand(@"
                SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = @table
                ORDER BY ORDINAL_POSITION", conn))
                {
                    cmd.Parameters.AddWithValue("@table", tableName);

                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        columns.Add(new ColumnInfo
                        {
                            ColumnName = reader.GetString(0),
                            SqlType = reader.GetString(1),
                            IsNullable = string.Equals(reader.GetString(2), "YES",
                                StringComparison.OrdinalIgnoreCase),
                            IsPrimaryKey = false
                        });
                    }
                }

                // primary keys
                var pkColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                using (var cmdPk = new SqlCommand(@"
                SELECT kcu.COLUMN_NAME
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
                    ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
                    AND tc.TABLE_NAME = kcu.TABLE_NAME
                WHERE tc.TABLE_NAME = @table
                    AND tc.CONSTRAINT_TYPE = 'PRIMARY KEY'", conn))
                {
                    cmdPk.Parameters.AddWithValue("@table", tableName);

                    using var reader = cmdPk.ExecuteReader();
                    while (reader.Read())
                    {
                        pkColumns.Add(reader.GetString(0));
                    }
                }

                foreach (var col in columns)
                {
                    if (pkColumns.Contains(col.ColumnName))
                    {
                        col.IsPrimaryKey = true;
                        col.IsNullable = false;
                    }
                }

                return columns;
            }
        }
    }

}
