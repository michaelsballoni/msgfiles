using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Data.SQLite;

namespace msgfiles
{
    public class SessionDb : IDisposable
    {
        public SessionDb(string dbFilePath)
        {
            bool file_existed = File.Exists(dbFilePath);

            m_db = new SQLiteConnection($"Data Source={dbFilePath}");
            m_db.Open();

            if (!file_existed)
            {
                string table_create_sql =
                    "CREATE TABLE sessions " +
                    "(token STRING CONSTRAINT sessions_primary_key PRIMARY KEY, " +
                    "display STRING NOT NULL, " +
                    "email STRING NOT NULL, " +
                    "lastaccessEpoch INTEGER NOT NULL)";
                using (var cmd = new SQLiteCommand(table_create_sql, m_db))
                    cmd.ExecuteNonQuery();

                string index_create_sql =
                    "CREATE INDEX session_emails ON sessions (email)";
                using (var cmd = new SQLiteCommand(index_create_sql, m_db))
                    cmd.ExecuteNonQuery();
            }
        }

        public void Dispose()
        {
            lock (this)
            {
                if (m_db != null)
                {
                    m_db.Dispose();
                    m_db = null;
                }
            }
        }

        public Session? GetSession(string token)
        {
            lock (this)
            {
                bool exists = false;
                string update_sql =
                    "UPDATE sessions SET lastaccessEpoch = @epochSeconds WHERE token = @token";
                using (var cmd = new SQLiteCommand(update_sql, m_db))
                {
                    cmd.Parameters.AddWithValue("@token", token);
                    cmd.Parameters.AddWithValue("@epochSeconds", DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                    int affected = cmd.ExecuteNonQuery();
                    if (affected > 0)
                        exists = true;
                }
                if (!exists)
                    return null;

                string select_sql = "SELECT email, display FROM sessions WHERE token = @token";
                using (var cmd = new SQLiteCommand(select_sql, m_db))
                {
                    cmd.Parameters.AddWithValue("@token", token);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return
                                new Session()
                                {
                                    token = token,
                                    email = reader.GetString(0),
                                    display = reader.GetString(1)
                                };
                        }
                        else
                            return null;
                    }
                }
            }
        }

        public Session CreateSession(string email, string display)
        {
            string token = Guid.NewGuid().ToString();
            lock (this)
            {
                string insert_sql =
                    "INSERT INTO sessions (token, email, display, lastaccessEpoch) " +
                                 "VALUES (@token, @email, @display, @epochSeconds)";
                using (var cmd = new SQLiteCommand(insert_sql, m_db))
                {
                    cmd.Parameters.AddWithValue("@token", token);
                    cmd.Parameters.AddWithValue("@email", email);
                    cmd.Parameters.AddWithValue("@display", display);
                    cmd.Parameters.AddWithValue("@epochSeconds", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                    cmd.ExecuteNonQuery();
                }
            }
            return
                new Session()
                {
                    token = token,
                    email = email,
                    display = display
                };
        }

        public bool DropSession(string token)
        {
            lock (this)
            {
                string delete_sql = "DELETE FROM sessions WHERE token = @token";
                using (var cmd = new SQLiteCommand(delete_sql, m_db))
                {
                    cmd.Parameters.AddWithValue("@token", token);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        private SQLiteConnection? m_db;
    }
}
