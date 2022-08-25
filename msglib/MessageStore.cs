using System.Data.SQLite;
using Newtonsoft.Json;

namespace msgfiles
{
    /// <summary>
    /// MessageStore manages the set of messages, to deliver, and delivered
    /// </summary>
    public class MessageStore : IDisposable
    {
        public MessageStore(string dbFilePath)
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

                    string table_sql =
                        "CREATE TABLE messages " +
                        "(token STRING CONSTRAINT message_key PRIMARY KEY, " +
                        "fromAddress STRING NOT NULL, " +
                        "toAddress STRING NOT NULL, " +
                        "message STRING NOT NULL, " +
                        "path STRING NOT NULL, " +
                        "createdEpoch INTEGER NOT NULL, " +
                        "deleted INTEGER NOT NULL, " +
                        "metadata STRING NOT NULL)";
                    using (var cmd = new SQLiteCommand(table_sql, db))
                        cmd.ExecuteNonQuery();

                    string to_index_sql = "CREATE INDEX message_to_idx ON messages (toAddress)";
                    using (var cmd = new SQLiteCommand(to_index_sql, db))
                        cmd.ExecuteNonQuery();

                    string created_index_sql = "CREATE INDEX message_created_idx ON messages (createdEpoch)";
                    using (var cmd = new SQLiteCommand(created_index_sql, db))
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
        /// Store a message given the path to the payload file, 
        /// and the file's hash
        /// </summary>
        public string StoreMessage(msg msg, string path, string hash)
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
                        "(token, fromAddress, toAddress, message, path, createdEpoch, deleted, metadata) " +
                    "VALUES " +
                     "(@token, @fromAddress, @toAddress, @message, @path, @createdEpoch, 0, @metadata)";
                using (var cmd = new SQLiteCommand(insert_sql, m_db))
                {
                    cmd.Parameters.AddWithValue("@token", token);
                    cmd.Parameters.AddWithValue("@fromAddress", msg.from);
                    cmd.Parameters.AddWithValue("@toAddress", Utils.PrepEmailForLookup(msg.to));
                    cmd.Parameters.AddWithValue("@message", msg.message);
                    cmd.Parameters.AddWithValue("@path", path);
                    cmd.Parameters.AddWithValue("@createdEpoch", created_epoch);
                    cmd.Parameters.AddWithValue("@metadata", JsonConvert.SerializeObject(metadata));
                    cmd.ExecuteNonQuery();
                }
                return token;
            }
        }

        /// <summary>
        /// Given a To: address and a message token, get the message
        /// and the path to the message's payload and the payload's hash
        /// </summary>
        public msg? GetMessage(string to, string token, out string path, out string hash)
        {
            path = "";
            hash = "";

            lock (this)
            {
                msg? msg = null;
                string select_sql =
                    "SELECT fromAddress, message, createdEpoch, path, metadata " +
                    "FROM messages " +
                    "WHERE toAddress = @to AND token = @token AND deleted = 0";
                using (var cmd = new SQLiteCommand(select_sql, m_db))
                {
                    cmd.Parameters.AddWithValue("@to", Utils.PrepEmailForLookup(to));
                    cmd.Parameters.AddWithValue("@token", token);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            msg =
                                new msg()
                                {
                                    token = token,
                                    from = reader.GetString(0),
                                    message = reader.GetString(1),
                                    created = DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(2))
                                };

                            path = reader.GetString(3);

                            var dict = Utils.GetMetadata(reader.GetString(4));
                            if (dict.ContainsKey("hash"))
                                hash = dict["hash"];
                        }
                    }
                }
                return msg;
            }
        }

        /// <summary>
        /// Delete a message given its token and who it's To;
        /// </summary>
        public bool DeleteMessage(string token, string to)
        {
            lock (this)
            {
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
                return any_deleted;
            }
        }

        /// <summary>
        /// Prune old messages
        /// In seconds for unit tests
        /// </summary>
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
