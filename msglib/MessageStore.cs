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
                string create_sql =
                    "CREATE TABLE messages " +
                    "(token STRING CONSTRAINT message_key PRIMARY KEY, " + 
                    "from STRING NOT NULL, " +
                    "to STRING NOT NULL, " +
                    "subject STRING NOT NULL, " +
                    "body STRING NOT NULL, " +
                    "manifest STRING NOT NULL, " +
                    "createdEpoch INTEGER NOT NULL)\n";
                create_sql += "CREATE INDEX message_to ON sessions (to)";
                using (var cmd = new SQLiteCommand(create_sql, m_db))
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

            string insert_sql =
                "INSERT INTO messages (token, from, to, subject, body, manifest, payloadFilePath) " +
                             "VALUES (@token, @from, @to, @subject, @body, @manifest, @payloadFilePath)";
            lock (this)
            {
                using (var cmd = new SQLiteCommand(insert_sql, m_db))
                {
                    cmd.Parameters.AddWithValue("@token", token);
                    cmd.Parameters.AddWithValue("@from", msg.from);
                    cmd.Parameters.AddWithValue("@to", msg.to);
                    cmd.Parameters.AddWithValue("@subject", msg.subject);
                    cmd.Parameters.AddWithValue("@body", msg.body);
                    cmd.Parameters.AddWithValue("@manifest", msg.manifest);
                    cmd.Parameters.AddWithValue("@payloadFilePath", payloadFilePath);
                    cmd.ExecuteNonQuery();
                }
            }

            return token;
        }

        public List<ServerMessage> GetMessages(string to)
        {
            var output = new List<ServerMessage>();
            // FORNOW string select_sql = "SELECT token, from,"
            return output;
        }

        public int DeleteOldMessages(int maxAgeSeconds)
        {
            var old_row_ids = new List<long>();
            {
                var now = DateTimeOffset.UtcNow;
                var then = now - new TimeSpan(0, 0, maxAgeSeconds);
                long oldest_epoch_seconds = then.ToUnixTimeSeconds();
                string select_sql = "SELECT row_id FROM messages WHERE createdEpoch < @oldestEpoch";
                lock (this)
                {
                    using (var cmd = new SQLiteCommand(select_sql, m_db))
                    {
                        cmd.Parameters.AddWithValue("@oldestEpoch", oldest_epoch_seconds);

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
                lock (this)
                {
                    using (var cmd = new SQLiteCommand(delete_sql, m_db))
                    {
                        cmd.Parameters.AddWithValue("@rowId", row_id);
                        messages_deleted += cmd.ExecuteNonQuery();
                    }
                }
            }
            return messages_deleted;
        }

        private SQLiteConnection? m_db;
    }
}
