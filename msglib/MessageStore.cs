using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Data.SQLite;

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
                    "manifest STRING NOT NULL, " +
                    "createdEpoch INTEGER NOT NULL, " +
                    "payloadFilePath STRING NOT NULL)";
                using (var cmd = new SQLiteCommand(table_sql, m_db))
                    cmd.ExecuteNonQuery();

                string to_index_sql = "CREATE INDEX message_to_idx ON messages (toAddress)";
                using (var cmd = new SQLiteCommand(to_index_sql, m_db))
                    cmd.ExecuteNonQuery();

                string created_index_sql = "CREATE INDEX message_created_idx ON messages (createdEpoch)";
                using (var cmd = new SQLiteCommand(created_index_sql, m_db))
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

        public string StoreMessage(msg msg, string payloadFilePath)
        {
            if (m_db == null)
                throw new NullReferenceException("m_db");

            string token = Utils.GenToken();
            long created_epoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string insert_sql =
                "INSERT INTO messages " +
                        "(token, fromAddress, toAddress, subject, body, manifest, createdEpoch, payloadFilePath) " +
                "VALUES (@token, @from, @to, @subject, @body, @manifest, @createdEpoch, @payloadFilePath)";
            using (var cmd = new SQLiteCommand(insert_sql, m_db))
            {
                cmd.Parameters.AddWithValue("@token", token);
                cmd.Parameters.AddWithValue("@from", msg.from);
                cmd.Parameters.AddWithValue("@to", msg.to);
                cmd.Parameters.AddWithValue("@subject", msg.subject);
                cmd.Parameters.AddWithValue("@body", msg.body);
                cmd.Parameters.AddWithValue("@manifest", msg.manifest);
                cmd.Parameters.AddWithValue("@createdEpoch", created_epoch);
                cmd.Parameters.AddWithValue("@payloadFilePath", payloadFilePath);
                lock (this)
                    cmd.ExecuteNonQuery();
            }
            return token;
        }

        public List<msg> GetMessages(string to)
        {
            var output = new List<msg>();
            string select_sql =
                "SELECT token, fromAddress, subject, createdEpoch " +
                "FROM messages WHERE toAddress = @to ORDER BY createdEpoch DESC";
            using (var cmd = new SQLiteCommand(select_sql, m_db))
            {
                cmd.Parameters.AddWithValue("@to", Utils.PrepEmailForLookup(to));
                lock (this)
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            msg msg =
                                new msg()
                                {
                                    token = reader.GetString(0),
                                    from = reader.GetString(1),
                                    subject = reader.GetString(2),
                                    created = DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(3))
                                };
                            output.Add(msg);
                        }
                    }
                }
            }
            return output;
        }

        public msg? GetMessage(string token, string to, out string payloadFilePath)
        {
            payloadFilePath = "";

            string select_sql =
                "SELECT token, fromAddress, toAddress, subject, body, manifest, createdEpoch, payloadFilePath " +
                "FROM messages " +
                "WHERE token = @token AND toAddress = @to";
            using (var cmd = new SQLiteCommand(select_sql, m_db))
            {
                cmd.Parameters.AddWithValue("@token", token);
                cmd.Parameters.AddWithValue("@to", Utils.PrepEmailForLookup(to));
                lock (this)
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            msg msg =
                                new msg()
                                {
                                    token = reader.GetString(0),
                                    from = reader.GetString(1),
                                    to = reader.GetString(2),
                                    subject = reader.GetString(3),
                                    body = reader.GetString(4),
                                    manifest = reader.GetString(5),
                                    created = DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(6))
                                };
                            payloadFilePath = reader.GetString(7);
                            return msg;
                        }
                    }
                }
            }
            return null;
        }

        public bool DeleteMessage(string token, string to)
        {
            string select_sql = "SELECT payloadFilePath FROM messages WHERE token = @token";
            string? file_path = null;
            using (var cmd = new SQLiteCommand(select_sql, m_db))
            {
                cmd.Parameters.AddWithValue("@token", token);
                lock (this)
                {
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                        file_path = (string)result;
                }
            }
            if (file_path == null)
                return false;

            bool any_deleted = false;
            string delete_sql = "DELETE FROM messages WHERE token = @token AND toAddress = @to";
            using (var cmd = new SQLiteCommand(delete_sql, m_db))
            {
                cmd.Parameters.AddWithValue("@token", token);
                cmd.Parameters.AddWithValue("@to", Utils.PrepEmailForLookup(to));
                lock (this)
                    any_deleted = cmd.ExecuteNonQuery() > 0;
            }
            if (!any_deleted)
                return false;

            bool any_msgs_use_file;
            string count_remaining_sql = 
                "SELECT COUNT(*) FROM messages WHERE payloadFilePath = @filePath";
            using (var cmd = new SQLiteCommand(count_remaining_sql, m_db))
            {
                cmd.Parameters.AddWithValue("@filePath", file_path);
                lock (this)
                {
                    object result = cmd.ExecuteScalar();
                    any_msgs_use_file =
                        !(result == null || result == DBNull.Value || (long)result == 0);
                }
            }
            if (!any_msgs_use_file && File.Exists(file_path))
                File.Delete(file_path);

            return true;
        }

        public int DeleteOldMessages(int maxAgeSeconds)
        {
            var old_token_twos = new Dictionary<string, string>();
            {
                var now = DateTimeOffset.UtcNow;
                var then = now - new TimeSpan(0, 0, maxAgeSeconds);
                long oldest_epoch_seconds = then.ToUnixTimeSeconds();
                string select_sql = "SELECT token, toAddress FROM messages WHERE createdEpoch < @oldestEpoch";
                using (var cmd = new SQLiteCommand(select_sql, m_db))
                {
                    cmd.Parameters.AddWithValue("@oldestEpoch", oldest_epoch_seconds);
                    lock (this)
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                                old_token_twos.Add(reader.GetString(0), reader.GetString(1));
                        }
                    }
                }
            }

            int messages_deleted = 0;
            foreach (var kvp in old_token_twos)
            {
                if (DeleteMessage(kvp.Key, kvp.Value))
                    ++messages_deleted;
            }
            return messages_deleted;
        }

        private SQLiteConnection? m_db;
    }
}
