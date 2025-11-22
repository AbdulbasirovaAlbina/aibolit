using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using Npgsql;

namespace Aibolit
{
    public class DatabaseHelper
    {
        private string connectionString;

        public DatabaseHelper()
        {
            connectionString = ConfigurationManager.ConnectionStrings["AibolitDB"]?.ConnectionString 
                ?? "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres";
        }

        public NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(connectionString);
        }

        public bool TestConnection()
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public bool AuthenticateUser(string username, string password)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(
                        "SELECT COUNT(*) FROM user_cooperator WHERE username = @username AND password = @password", conn))
                    {
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@password", password);
                        var result = cmd.ExecuteScalar();
                        return Convert.ToInt32(result) > 0;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        public DataTable ExecuteQuery(string query, params NpgsqlParameter[] parameters)
        {
            DataTable dt = new DataTable();
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        if (parameters != null)
                        {
                            foreach (var param in parameters)
                            {
                                cmd.Parameters.Add(param);
                            }
                        }
                        using (var adapter = new NpgsqlDataAdapter(cmd))
                        {
                            adapter.Fill(dt);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка выполнения запроса: {ex.Message}", ex);
            }
            return dt;
        }

        public int ExecuteNonQuery(string query, params NpgsqlParameter[] parameters)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        if (parameters != null)
                        {
                            foreach (var param in parameters)
                            {
                                cmd.Parameters.Add(param);
                            }
                        }
                        return cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка выполнения команды: {ex.Message}", ex);
            }
        }

        public object ExecuteScalar(string query, params NpgsqlParameter[] parameters)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        if (parameters != null)
                        {
                            foreach (var param in parameters)
                            {
                                cmd.Parameters.Add(param);
                            }
                        }
                        return cmd.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка выполнения скалярного запроса: {ex.Message}", ex);
            }
        }
    }
}


