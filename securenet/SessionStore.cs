using System.Data.SQLite;

namespace msgfiles
{
    /// <summary>
    /// Manage user sessions
    /// </summary>
    public class SessionStore : IDisposable
    {
        public SessionStore(string dbFilePath)
        {
            if (!File.Exists(dbFilePath))
            {
                using (var db = new SQLiteConnection($"Data Source={dbFilePath}"))
                {
                    db.Open();

                    using (var cmd = new SQLiteCommand("PRAGMA journal_mode = WAL", db))
                        cmd.ExecuteNonQuery();
                    using (var cmd = new SQLiteCommand("PRAGMA synchronous = NORMAL", db))
                        cmd.ExecuteNonQuery();

                    string table_create_sql =
                        "CREATE TABLE sessions " +
                        "(token STRING CONSTRAINT sessions_primary_key PRIMARY KEY, " +
                        "display STRING NOT NULL, " +
                        "email STRING NOT NULL, " +
                        "lastaccessEpoch INTEGER NOT NULL, " +
                        "variables STRING NULL)";
                    using (var cmd = new SQLiteCommand(table_create_sql, db))
                        cmd.ExecuteNonQuery();

                    string index_create_sql =
                        "CREATE INDEX session_emails_idx ON sessions (email)";
                    using (var cmd = new SQLiteCommand(index_create_sql, db))
                        cmd.ExecuteNonQuery();
                }
            }

            m_db = new SQLiteConnection($"Data Source={dbFilePath}");
            m_db.Open();
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

        /// <summary>
        /// Given a session token, try to get out a session
        /// Returns null if session not found
        /// </summary>
        public Session? GetSession(string token)
        {
            lock (this)
            {
                // Bump the timestamp
                // If nothing affected, there must not be a session for this user
                string update_sql =
                    "UPDATE sessions SET lastaccessEpoch = @epochSeconds WHERE token = @token";
                using (var cmd = new SQLiteCommand(update_sql, m_db))
                {
                    cmd.Parameters.AddWithValue("@token", token);
                    cmd.Parameters.AddWithValue("@epochSeconds", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                    if (cmd.ExecuteNonQuery() == 0)
                        return null;
                }

                // Read out the session variables, just email and display for now
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

        /// <summary>
        /// Given user information, create and return a session
        /// </summary>
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

        /// <summary>
        /// Prune old sessions
        /// In seconds for unit tests
        /// </summary>
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
