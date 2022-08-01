using Ionic.Zip;

namespace msgfiles
{
    public partial class StatusForm : Form, IClientApp
    {
        public StatusForm(Msg msg)
        {
            m_msg = msg;
            InitializeComponent();
        }

        private Msg m_msg;

        public bool Cancelled
        {
            get
            {
                Application.DoEvents();
                return m_cancelled;
            }
        }
        public void Log(string msg)
        {
            Application.DoEvents();
            LogOutputTextBox.Text += msg + "\r";
        }
        public void Progress(int cur, int total)
        {
            Application.DoEvents();
            StatusProgressBar.Value = (int)((double)cur / total) * StatusProgressBar.Maximum;
        }

        private bool m_cancelled = false;

        private void StatusForm_Load(object sender, EventArgs e)
        {
            Show();

            string pwd = Utils.GenToken();
            string zip_file_path = 
                Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip");

            try
            {
                using (var zip = new ZipFile(zip_file_path))
                {
                    Log("Adding files to package...");

                    zip.CompressionLevel = Ionic.Zlib.CompressionLevel.BestSpeed;
                    zip.Password = pwd;

                    foreach (var path in m_msg.Paths)
                    {
                        if (File.Exists(path))
                            zip.AddFile(path);
                        else if (Directory.Exists(path))
                            zip.AddDirectory(path);
                        else
                            throw new Exception($"Item to send not found: {path}");
                    }

                    Log("Saving package...");
                    zip.Save();
                }

                using (var client = new Client(this))
                {
                    do
                    {
                        client.SessionToken = GlobalState.SessionToken;
                        bool challenged =
                            client.BeginConnect
                            (
                                GlobalState.Server,
                                GlobalState.Port,
                                GlobalState.DisplayName,
                                GlobalState.Email
                            );
                        if (Cancelled)
                            return;

                        if (challenged)
                        {
                            var connecter = new ConnectForm();
                            if (connecter.ShowDialog() != DialogResult.OK)
                                return;
                            else
                                continue;
                        }
                        if (Cancelled)
                            return;
                    } while (false);

                    if (client.SendMsg(m_msg, pwd, zip_file_path))
                    {
                        MessageBox.Show("Message sent!");
                        Close();
                    }
                    else
                        MessageBox.Show("Sending message failed!");
                }
            }
            catch (Exception exp)
            {
                MessageBox.Show($"ERROR: {Utils.SumExp(exp)}");
            }
            finally
            {
                try
                {
                    File.Delete(zip_file_path);
                }
                catch { }
            }
        }
    }
}
