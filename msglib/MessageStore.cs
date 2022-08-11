using System.Data.SQLite;
using Newtonsoft.Json;

namespace msgfiles
{
    public class MessageStore : IDisposable
    {
        public MessageStore(string dbFilePath)
        {
            bool file_existed = File.Exists(dbFilePath);

            m_db = new SQLiteConnection($"Data Source={dbFilePath}");
            m_db.Open();

            if (!file_existed)
            {
                string table_sql =
                    "CREATE TABLE messages " +
                    "(token STRING CONSTRAINT message_key PRIMARY KEY, " + 
                    "fromAddress STRING NOT NULL, " +
                    "toAddress STRING NOT NULL, " +
                    "subject STRING NOT NULL, " +
                    "body STRING NOT NULL, " +
                    "pwd STRING NOT NULL, " +
                    "path STRING NOT NULL, " +
                    "createdEpoch INTEGER NOT NULL, " +
                    "deleted INTEGER NOT NULL, " +
                    "metadata STRING NOT NULL)";
                using (var cmd = new SQLiteCommand(table_sql, m_db))
                    cmd.ExecuteNonQuery();

                string to_index_sql = "CREATE INDEX message_to_idx ON messages (toAddress)";
                using (var cmd = new SQLiteCommand(to_index_sql, m_db))
                    cmd.ExecuteNonQuery();

                string created_index_sql = "CREATE INDEX message_created_idx ON messages (createdEpoch)";
                using (var cmd = new SQLiteCommand(created_index_sql, m_db))
                    cmd.ExecuteNonQuery();

                string path_index_sql = "CREATE INDEX message_path_idx ON messages (path)";
                using (var cmd = new SQLiteCommand(path_index_sql, m_db))
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

        public string StoreMessage(msg msg, string pwd, string path, string hash)
        {
            lock (this)
            {
                if (m_db == null)
                    throw new NullReferenceException("m_db");

                var metadata = new Dictionary<string, string>() { { "hash", hash } };

                string token = Utils.GenToken();

                long created_epoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                string insert_sql =
                    "INSERT INTO messages " +
                        "(token, fromAddress, toAddress, subject, body, pwd, path, createdEpoch, deleted, metadata) " +
                    "VALUES " +
                     "(@token, @fromAddress, @toAddress, @subject, @body, @pwd, @path, @createdEpoch, 0, @metadata)";
                using (var cmd = new SQLiteCommand(insert_sql, m_db))
                {
                    cmd.Parameters.AddWithValue("@token", token);
                    cmd.Parameters.AddWithValue("@fromAddress", msg.from);
                    cmd.Parameters.AddWithValue("@toAddress", Utils.PrepEmailForLookup(msg.to));
                    cmd.Parameters.AddWithValue("@subject", msg.subject);
                    cmd.Parameters.AddWithValue("@body", msg.body);
                    cmd.Parameters.AddWithValue("@pwd", pwd);
                    cmd.Parameters.AddWithValue("@path", path);
                    cmd.Parameters.AddWithValue("@createdEpoch", created_epoch);
                    cmd.Parameters.AddWithValue("@metadata", JsonConvert.SerializeObject(metadata));
                    cmd.ExecuteNonQuery();
                }
                return token;
            }
        }

        public msg? GetMessage(string to, string pwd, out string path, out string hash)
        {
            path = "";
            hash = "";

            lock (this)
            {
                msg? msg = null;
                string select_sql =
                    "SELECT token, fromAddress, subject, body, createdEpoch, path, metadata " +
                    "FROM messages " +
                    "WHERE toAddress = @to AND pwd = @pwd AND deleted = 0";
                using (var cmd = new SQLiteCommand(select_sql, m_db))
                {
                    cmd.Parameters.AddWithValue("@to", Utils.PrepEmailForLookup(to));
                    cmd.Parameters.AddWithValue("@pwd", pwd);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            msg =
                                new msg()
                                {
                                    token = reader.GetString(0),
                                    from = reader.GetString(1),
                                    subject = reader.GetString(2),
                                    body = reader.GetString(3),
                                    created = DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(4))
                                };

                            path = reader.GetString(5);

                            var dict = Utils.GetMetadata(reader.GetString(6));
                            if (dict.ContainsKey("hash"))
                                hash = dict["hash"];
                        }
                    }
                }
                return msg;
            }
        }

        public bool DeleteMessage(string token, string to)
        {
            lock (this)
            {
                string? file_path = null;
                string select_sql = "SELECT path FROM messages WHERE token = @token";
                using (var cmd = new SQLiteCommand(select_sql, m_db))
                {
                    cmd.Parameters.AddWithValue("@token", token);
                    object result = cmd.ExecuteScalar();
                    if (result != DBNull.Value)
                        file_path = (string)result;
                }

                bool any_deleted = false;
                string delete_sql = 
                    "UPDATE messages SET deleted = 1 " +
                    "WHERE token = @token AND toAddress = @to AND deleted = 0";
                using (var cmd = new SQLiteCommand(delete_sql, m_db))
                {
                    cmd.Parameters.AddWithValue("@token", token);
                    cmd.Parameters.AddWithValue("@to", Utils.PrepEmailForLookup(to));
                    any_deleted = cmd.ExecuteNonQuery() > 0;
                }

                if (!string.IsNullOrEmpty(file_path))
                {
                    bool any_msgs_use_file = true;
                    string count_remaining_sql =
                        "SELECT COUNT(*) FROM messages WHERE path = @path AND deleted = 0";
                    using (var cmd = new SQLiteCommand(count_remaining_sql, m_db))
                    {
                        cmd.Parameters.AddWithValue("@path", file_path);
                        object result = cmd.ExecuteScalar();
                        if (result == null || result == DBNull.Value || (long)result == 0)
                            any_msgs_use_file = false;
                    }
                    if (!any_msgs_use_file && File.Exists(file_path))
                        File.Delete(file_path);
                }

                return any_deleted;
            }
        }

        public int DeleteOldMessages(int maxAgeSeconds)
        {
            var old_token_toos = new Dictionary<string, string>();
            lock (this)
            {
                var now = DateTimeOffset.UtcNow;
                var then = now - new TimeSpan(0, 0, maxAgeSeconds);
                long oldest_epoch_seconds = then.ToUnixTimeSeconds();
                string select_sql = "SELECT token, toAddress FROM messages WHERE createdEpoch <= @oldestEpoch";
                using (var cmd = new SQLiteCommand(select_sql, m_db))
                {
                    cmd.Parameters.AddWithValue("@oldestEpoch", oldest_epoch_seconds);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            old_token_toos.Add(reader.GetString(0), reader.GetString(1));
                    }
                }
            }

            int messages_deleted = 0;
            foreach (var kvp in old_token_toos)
            {
                if (DeleteMessage(kvp.Key, kvp.Value))
                    ++messages_deleted;
            }
            return messages_deleted;
        }

        private SQLiteConnection? m_db;
    }
}
