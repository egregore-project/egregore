using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Data.Sqlite;

namespace egregore.Data
{
    public class SqliteProjection
    {
        static SqliteProjection()
        {
            SqlMapper.AddTypeHandler(new UInt64TypeHandler());
            SqlMapper.AddTypeHandler(new UInt128TypeHandler());
        }

        public string DataFile { get; private set; }

        public void Init()
        {
            CreateIfNotExists(DataFile);
        }

        internal SqliteProjection(string filePath)
        {
            DataFile = filePath;
        }

        private void CreateIfNotExists(string filePath)
        {
            var baseDirectory = Path.GetDirectoryName(filePath);
            if(!string.IsNullOrWhiteSpace(baseDirectory))
                Directory.CreateDirectory(baseDirectory);
            DataFile = filePath;
        }

        public void Visit(Record record)
        {
            record.Columns.Sort(RecordColumn.IndexComparer);

            var db = OpenConnection();

            var t = db.BeginTransaction();

            var tableInfoList = db.Query<TableInfo>(
                    $"SELECT * " +
                    $"FROM sqlite_master " +
                    $"WHERE type='table' " +
                    $"AND name LIKE :name " +
                    $"ORDER BY name DESC ", new {name = $"{record.Type}%",}, t)
                .AsList();

            int revision;
            if (tableInfoList.Count == 0)
            {
                revision = 1;
                db.Execute(CreateTableSql(record, revision), transaction: t);
            }
            else
            {
                var tableInfo = tableInfoList[0];
                var revisionString = Regex.Split(tableInfo.name, "([0-9])+$",
                    RegexOptions.IgnoreCase | RegexOptions.Compiled)[1];
                if (!int.TryParse(revisionString, out revision))
                    throw new FormatException("invalid revision mask");

                revision++;

                db.Execute(CreateTableSql(record, revision), transaction: t);

                RebuildView(record, db, t, tableInfo, tableInfoList, revision);
            }

            if (record.Uuid == default)
            {
                record.Uuid = Guid.NewGuid();

                var insert = new StringBuilder();
                insert.Append("INSERT INTO '");
                insert.Append(record.Type);
                insert.Append("_V");
                insert.Append(revision);
                insert.Append("' (");
                for (var i = 0; i < record.Columns.Count; i++)
                {
                    if (i != 0)
                        insert.Append(", ");
                    var column = record.Columns[i];
                    insert.Append("\"");
                    insert.Append(column.Name);
                    insert.Append("\"");
                }
                insert.Append(") VALUES (");
                for (var i = 0; i < record.Columns.Count; i++)
                {
                    if (i != 0)
                        insert.Append(", ");
                    var column = record.Columns[i];
                    insert.Append(":");
                    insert.Append(column.Name);
                }
                insert.Append(')');

                var hash = new Dictionary<string, object>();
                foreach (var column in record.Columns)
                {
                    hash.Add(column.Name, column.Value);
                }

                var insertSql = insert.ToString();
                db.Execute(insertSql, hash, t); 
            }
            else
            {
                throw new NotImplementedException("when the row may already exist");
            }
            
            t.Commit();
        }

        private static void RebuildView(Record record, IDbConnection db, IDbTransaction t, TableInfo tableInfo, List<TableInfo> tableInfoList, int revision)
        {
            db.Execute("DROP VIEW IF EXISTS \"" + record.Type + "\"");
            
            var view = new StringBuilder();
            view.Append("CREATE VIEW \"");
            view.Append(record.Type);
            view.Append("\" (");
            for (var i = 0; i < record.Columns.Count; i++)
            {
                if (i != 0)
                    view.Append(", ");
                var column = record.Columns[i];
                view.Append("\"");
                view.Append(column.Name);
                view.Append("\"");
            }

            view.Append(") AS SELECT ");
            for (var i = 0; i < record.Columns.Count; i++)
            {
                if (i != 0)
                    view.Append(", ");
                var column = record.Columns[i];
                view.Append("\"");
                view.Append(column.Name);
                view.Append("\"");
            }
            view.Append(" FROM \"");
            view.Append(record.Type);
            view.Append("_V");
            view.Append(revision);
            view.Append("\" ");

            for (var i = 0; i < tableInfoList.Count; i++)
            {
                var entry = tableInfoList[i];
                var hash = BuildSqlHash(entry);

                view.Append("UNION SELECT ");
                var j = 0;
                foreach (var column in record.Columns)
                {
                    if (j != 0)
                        view.Append(", ");

                    if (!hash.ContainsKey(column.Name))
                    {
                        view.Append(ResolveColumnDefaultValue(column));
                        view.Append(" AS \"");
                        view.Append(column.Name);
                        view.Append("\"");
                        j++;
                        continue;
                    }

                    view.Append("\"");
                    view.Append(column.Name);
                    view.Append("\"");
                    j++;
                }

                view.Append(" FROM \"");
                view.Append(entry.name);
                view.Append("\" ");
            }

            var viewSql = view.ToString(); 
            db.Execute(viewSql, transaction: t);
        }

        private static Dictionary<string, (string, string)> BuildSqlHash(TableInfo tableInfo)
        {
            var clauses = Regex.Replace(tableInfo.sql, @"[^\(]+\(([^\)]+)\)", "$1",
                RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

            var columns = clauses[1..]
                .Split(',', StringSplitOptions.RemoveEmptyEntries);

            var sqlHash = new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase);
            foreach (var column in columns)
            {
                var pair = column.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var name = pair[0].Trim('\"');
                var type = pair[1];
                var defaultValue = pair[3];
                sqlHash.Add(name, (type, defaultValue));
            }

            return sqlHash;
        }

        private static string CreateTableSql(Record record, int revision)
        {
            var create = new StringBuilder();
            create.Append("CREATE TABLE \"");
            create.Append(record.Type);
            create.Append("_V");
            create.Append(revision);
            create.Append("\" (");
            for (var i = 0; i < record.Columns.Count; i++)
            {
                if (i != 0)
                    create.Append(", ");
                var column = record.Columns[i];
                create.Append(" \"");
                create.Append(column.Name);
                create.Append("\" ");
                create.Append(ResolveColumnTypeToDbType(column));
                create.Append(" DEFAULT ");
                create.Append(ResolveColumnDefaultValue(column));
            }
            create.Append(")");
            var createSql = create.ToString();
            return createSql;
        }

        internal IDbConnection OpenConnection()
        {
            var db = new SqliteConnection($"Data Source={DataFile}");
            db.Open();
            return db;
        }

        private static string ResolveColumnTypeToDbType(RecordColumn column)
        {
            var type = column.Type?.ToUpperInvariant();
            return type switch
            {
                "INT" => "INTEGER",
                "STRING" => "TEXT",
                _ => "BLOB"
            };
        }

        private static string ResolveColumnDefaultValue(RecordColumn column)
        {
            if (string.IsNullOrWhiteSpace(column.Default))
                return "NULL";

            // FIXME: Dapper crashes if the default value starts with "?"
            var type = column.Type?.ToUpperInvariant();
            return type == "STRING" ? $"\"{column.Default}\"" : column.Default;
        }

        private struct TableInfo
        {
            public string name;
            public string sql;
        }
    }
}
