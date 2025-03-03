using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using MySql.Data.MySqlClient;
using Npgsql;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagicEf.Scaffold.Helpers
{
    public class DatabaseHelper
    {
        public static bool ValidateConnectionString(string connectionString)
        {
            (string name, Func<string, DbConnection> factory)[] providers =
            {
            ("SQL Server", conn => new SqlConnection(conn)),
            ("PostgreSQL", conn => new NpgsqlConnection(conn)),
            ("MySQL", conn => new MySqlConnection(conn)),
            ("Oracle", conn => new OracleConnection(conn)),
            ("SQLite", conn => new SqliteConnection(conn))
        };

            foreach (var (name, factory) in providers)
            {
                try
                {
                    using (DbConnection connection = factory(connectionString))
                    {
                        // Set timeout in the connection string if not present
                        if (!connectionString.Contains("Timeout="))
                            connection.ConnectionString += ";Timeout=5"; // Default to 5 seconds

                        Console.WriteLine($"🔍 Testing connection with {name}...");

                        var task = Task.Run(() =>
                        {
                            connection.Open();
                            connection.Close();
                        });

                        if (!task.Wait(TimeSpan.FromSeconds(5))) // Ensure no infinite hang
                        {
                            Console.WriteLine($"⏳ {name} took too long, skipping.");
                            continue;
                        }

                        Console.WriteLine($"✅ Connection successful with {name}!");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ {name} failed: {ex.Message}");
                }
            }

            return false;
        }
    }
}
