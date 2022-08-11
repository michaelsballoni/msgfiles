using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Data.SQLite;
using Newtonsoft.Json;

namespace msgfiles
{
    public class SessionStore : IDisposable
    {
        public SessionStore(string dbFilePath)
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
                    "lastaccessEpoch INTEGER NOT NULL, "  +
                    "variables STRING NULL)";
                using (var cmd = new SQLiteCommand(table_create_sql, m_db))
                    cmd.ExecuteNonQuery();

                string index_create_sql =
                    "CREATE INDEX session_emails_idx ON sessions (email)";
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
                string update_sql =
                    "UPDATE sessions SET lastaccessEpoch = @epochSeconds WHERE token = @token";
                using (var cmd = new SQLiteCommand(update_sql, m_db))
                {
                    cmd.Parameters.AddWithValue("@token", token);
                    cmd.Parameters.AddWithValue("@epochSeconds", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                    if (cmd.ExecuteNonQuery() == 0)
                        return null;
                }

                string select_sql = "SELECT email, display, variables FROM sessions WHERE token = @token";
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
                                    display = reader.GetString(1),
                                    variables =
                                        (
                                            reader.IsDBNull(2) 
                                            ? null 
                                            : Utils.GetMetadata(reader.GetString(2)) 
                                        )
                                        ?? 
                                        new Dictionary<string, string>()
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
            lock (this)
            {
                string token = Guid.NewGuid().ToString();

                string insert_sql =
                    "INSERT INTO sessions (token, email, display, lastaccessEpoch, variables) " +
                                    "VALUES (@token, @email, @display, @epochSeconds, NULL)";
                using (var cmd = new SQLiteCommand(insert_sql, m_db))
                {
                    cmd.Parameters.AddWithValue("@token", token);
                    cmd.Parameters.AddWithValue("@email", email);
                    cmd.Parameters.AddWithValue("@display", display);
                    cmd.Parameters.AddWithValue("@epochSeconds", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                    cmd.ExecuteNonQuery();
                }

                return
                    new Session()
                    {
                        token = token,
                        email = email,
                        display = display
                    };
            }
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

        public int DropOldSessions(int maxAgeSeconds)
        {
            List<string> old_session_tokens = new List<string>();
            lock (this)
            {
                var now = DateTimeOffset.UtcNow;
                var then = now - new TimeSpan(0, 0, maxAgeSeconds);
                long oldest_epoch_seconds = then.ToUnixTimeSeconds();
                string select_sql = "SELECT token FROM sessions WHERE lastaccessEpoch <= @oldestEpoch";
                using (var cmd = new SQLiteCommand(select_sql, m_db))
                {
                    cmd.Parameters.AddWithValue("@oldestEpoch", oldest_epoch_seconds);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            old_session_tokens.Add(reader.GetString(0));
                    }
                }
            }

            int sessions_deleted = 0;
            foreach (var token in old_session_tokens)
                sessions_deleted += DropSession(token) ? 1 : 0;
            return sessions_deleted;
        }

        private SQLiteConnection? m_db;
    }
}
