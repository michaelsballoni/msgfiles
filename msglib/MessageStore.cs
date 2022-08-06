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
                    "from STRING NOT NULL, " +
                    "to STRING NOT NULL, " +
                    "subject STRING NOT NULL, " +
                    "body STRING NOT NULL, " +
                    "manifest STRING NOT NULL, " +
                    "createdEpoch INTEGER NOT NULL, " +
                    "payloadFilePath STRING NOT NULL)";
                using (var cmd = new SQLiteCommand(table_sql, m_db))
                    cmd.ExecuteNonQuery();

                string to_index_sql = "CREATE INDEX message_to_idx ON messages (to)";
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

        public string StoreMessage(ServerMessage msg, string payloadFilePath)
        {
            if (m_db == null)
                throw new NullReferenceException("m_db");

            string token = Utils.GenToken();
            long created_epoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string insert_sql =
                "INSERT INTO messages " +
                        "(token, from, to, subject, body, manifest, createdEpoch, payloadFilePath) " +
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

        public List<ServerMessage> GetMessages(string to)
        {
            var output = new List<ServerMessage>();
            string select_sql = 
                "SELECT token, from, to, subject, body, manifest, createdEpoch FROM messages WHERE to = @to ORDER BY ";
            using (var cmd = new SQLiteCommand(select_sql, m_db))
            {
                cmd.Parameters.AddWithValue("@to", Utils.PrepEmailForLookup(to));
                lock (this)
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ServerMessage msg =
                                new ServerMessage()
                                {
                                    from = reader.GetString(0),
                                    to = reader.GetString(1),
                                    subject = reader.GetString(2),
                                    body = reader.GetString(3),
                                    manifest = reader.GetString(4),
                                    created = DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(5))
                                };
                            output.Add(msg);
                        }
                    }
                }
            }
            return output;
        }

        public ServerMessage GetMessage(string token, string to, out string payloadFilePath)
        {
            string select_sql =
                "SELECT token, from, to, subject, body, manifest, createdEpoch, payloadFilePath " +
                "FROM messages " +
                "WHERE token = @token AND to = @to";
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
                            ServerMessage msg =
                                new ServerMessage()
                                {
                                    from = reader.GetString(0),
                                    to = reader.GetString(1),
                                    subject = reader.GetString(2),
                                    body = reader.GetString(3),
                                    manifest = reader.GetString(4),
                                    created = DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(5))
                                };
                            payloadFilePath = reader.GetString(6);
                            return msg;
                        }
                    }
                }
            }
            throw new InputException("Message not found");
        }

        public bool DeleteMessage(string token, string to)
        {
            string select_sql = "SELETE payloadFilePath FROM messages WHERE token = @token";
            string file_path;
            using (var cmd = new SQLiteCommand(select_sql, m_db))
            {
                lock (this)
                    file_path = (string)cmd.ExecuteScalar();
            }
            if (file_path == null)
                return false;

            bool any_deleted = false;
            string delete_sql = "DELETE FROM messages WHERE token = @token AND to = @to";
            using (var cmd = new SQLiteCommand(delete_sql, m_db))
            {
                cmd.Parameters.AddWithValue("@token", token);
                cmd.Parameters.AddWithValue("@to", Utils.PrepEmailForLookup(to));
                lock (this)
                    any_deleted = cmd.ExecuteNonQuery() > 0;
            }
            if (!any_deleted)
                return false;

            int remaining_count;
            string count_remaining_sql = 
                "SELECT COUNT(*) FROM messages WHERE payloadFilePath = @filePath";
            using (var cmd = new SQLiteCommand(count_remaining_sql, m_db))
            {
                cmd.Parameters.AddWithValue("@filePath", file_path);
                lock (this)
                    remaining_count = (int)cmd.ExecuteScalar();
            }
            if (remaining_count == 0 && File.Exists(file_path))
                File.Delete(file_path);

            return true;
        }

        public int DeleteOldMessages(int maxAgeSeconds)
        {
            var old_row_ids = new List<long>();
            {
                var now = DateTimeOffset.UtcNow;
                var then = now - new TimeSpan(0, 0, maxAgeSeconds);
                long oldest_epoch_seconds = then.ToUnixTimeSeconds();
                string select_sql = "SELECT row_id FROM messages WHERE createdEpoch < @oldestEpoch";
                using (var cmd = new SQLiteCommand(select_sql, m_db))
                {
                    cmd.Parameters.AddWithValue("@oldestEpoch", oldest_epoch_seconds);
                    lock (this)
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                                old_row_ids.Add(reader.GetInt64(0));
                        }
                    }
                }
            }

            string delete_sql = "DELETE FROM messages WHERE row_id = @rowId";
            int messages_deleted = 0;
            foreach (var row_id in old_row_ids)
            {
                using (var cmd = new SQLiteCommand(delete_sql, m_db))
                {
                    cmd.Parameters.AddWithValue("@rowId", row_id);
                    lock (this)
                        messages_deleted += cmd.ExecuteNonQuery();
                }
            }
            return messages_deleted;
        }

        private SQLiteConnection? m_db;
    }
}
