/*using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using Microsoft.Data.Sqlite;
using System.IO;
using System.Diagnostics;

namespace QTIParserApp
{
    public class DatabaseHelper
    {
        //private const string ConnectionString = "Data Source=Database/quiz.db";
        private static readonly string DatabasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "quiz.db");
        private static readonly string ConnectionString = $"Data Source={DatabasePath};";


        public static IDbConnection GetConnection()
        {
            Debug.WriteLine($"[DEBUG] SQLite is looking for: {DatabasePath}");
            FileInfo dbFile = new FileInfo(DatabasePath);
            Debug.WriteLine($"[DEBUG] Database Exists: {dbFile.Exists}");
            Debug.WriteLine($"[DEBUG] Database Path Valid: {dbFile.Directory.Exists}");
            return new SqliteConnection(ConnectionString);
        }

        public static void Execute(string sql, object parameters = null)
        {
            using (var connection = GetConnection())
            {
                connection.Execute(sql, parameters);
            }
        }

        public static T QuerySingle<T>(string sql, object parameters = null)
        {
            using (var connection = GetConnection())
            {
                return connection.QuerySingleOrDefault<T>(sql, parameters);
            }
        }

        public static List<T> Query<T>(string sql, object parameters = null)
        {
            using (var connection = GetConnection())
            {
                return connection.Query<T>(sql, parameters).ToList();
            }
        }
    }
}
*/