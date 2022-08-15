using System.Diagnostics;
using System.Net.Sockets;

namespace msgfiles
{
    public partial class StatusForm : Form, IClientApp
    {
        public StatusForm(ClientMessage msg)
        {
            InitializeComponent();
            Text = "Message Files - Sending Files";
            m_msg = msg;
        }

        public StatusForm(string pwd)
        {
            InitializeComponent();
            Text = "Message Files - Receiving Files";
            m_pwd = pwd;
        }

        private ClientMessage? m_msg;
        private string? m_pwd;

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
            LogOutputTextBox.AppendText(msg + "\r\n");
            Application.DoEvents();
        }
        public void Progress(double progress)
        {
            StatusProgressBar.Value = (int)Math.Round(progress * StatusProgressBar.Maximum);
            Application.DoEvents();
        }
        public bool ConfirmDownload
        (
            string from, 
            string message, 
            out bool shouldDelete
        )
        {
            shouldDelete = false;

            string to_confirm =
                $"From:\r\n{from}\r\n\r\nMessage:\r\n\r\n{message}";
            using (var dlg = new ConfirmForm("Does this message look good?", to_confirm))
            {
                bool should_continue = dlg.ShowDialog() == DialogResult.OK;
                shouldDelete = !should_continue && dlg.ShouldDelete;
                return should_continue;
            }
        }
        public bool ConfirmExtraction
        (
            string manifest, 
            out bool shouldDelete,
            out string extractionFolder)
        {
            shouldDelete = false;
            extractionFolder = "";

            using (var dlg = new ConfirmForm("Do these file contents look good?", manifest))
            {
                bool should_continue = dlg.ShowDialog() == DialogResult.OK;
                shouldDelete = !should_continue && dlg.ShouldDelete;
                if (!should_continue)
                    return false;
            }

            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Where folder do you want to extract the files into?";
                dlg.UseDescriptionForTitle = true;
                if (dlg.ShowDialog() != DialogResult.OK || !Directory.Exists(dlg.SelectedPath))
                    return false;

                extractionFolder = dlg.SelectedPath;
                m_extractionFolderDirPath = extractionFolder;
            }

            return true;
        }

        private bool m_cancelled = false;
        private string m_extractionFolderDirPath = "";

        private void DoIt()
        {
            CancelSendButton.Text = "Cancel";
            int seconds_between_retries = 3;

            bool success = false;
            string token = "";
            bool should_delete = false;
            while (!Cancelled)
            {
                try
                {
                    using (var client = new MsgClient(this))
                    {
                        while (!Cancelled)
                        {
                            try
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
                                else
                                {
                                    break;
                                }
                            }
                            catch (NetworkException)
                            {
                                Log("Authentication failed due to a network error, will retry");
                                Thread.Sleep(seconds_between_retries * 1000);
                            }
                        }

                        if 
                        (
                            !string.IsNullOrEmpty(m_pwd) 
                            && 
                            !success 
                            && 
                            should_delete 
                            && 
                            !string.IsNullOrEmpty(token)
                        )
                        {
                            client.DeleteMessage(token);
                            Close();
                            return;
                        }

                        if (m_msg != null)
                        {
                            if (client.SendMsg(m_msg.To, m_msg.Message, m_msg.Paths))
                            {
                                MessageBox.Show("Files sent!");
                                success = true;
                                Close();
                            }
                            else
                                MessageBox.Show($"Sending files failed!");
                            return;
                        }
                        else if (m_pwd != null)
                        {
                            if (client.GetMessage(m_pwd, out token, out should_delete))
                            {
                                client.DeleteMessage(token);
                                MessageBox.Show("Files received!");
                                success = true;
                                Close();

                                if (Directory.Exists(m_extractionFolderDirPath))
                                {
                                    ProcessStartInfo si = new ProcessStartInfo
                                    {
                                        Arguments = m_extractionFolderDirPath,
                                        FileName = "explorer.exe"
                                    };
                                    Process.Start(si);
                                }

                                return;
                            }
                            else if (should_delete)
                                MessageBox.Show("Receiving files failed!");

                            if (string.IsNullOrEmpty(token) || !should_delete)
                                return;
                        }
                        else
                            throw new NullReferenceException("m_msg and m_pwd");
                    }
                }
                catch (SocketException)
                {
                    Log("The operation failed due to a network error, will retry...");
                    Thread.Sleep(seconds_between_retries * 1000);
                }
                catch (NetworkException)
                {
                    Log("The operation failed due to a network error, will retry...");
                    Thread.Sleep(seconds_between_retries * 1000);
                }
                catch (Exception exp)
                {
                    exp = Utils.SmashExp(exp);
                    if (exp is InputException)
                        MessageBox.Show(exp.Message);
                    else
                        MessageBox.Show($"ERROR: {Utils.SumExp(exp)}");
                    return;
                }
                finally
                {
                    if (!success)
                    {
                        CancelSendButton.Text = "Retry";
                    }
                }
            }
        }

        private void StatusForm_Load(object sender, EventArgs e)
        {
            Show();
            DoIt();
        }

        private void CancelSendButton_Click(object sender, EventArgs e)
        {
            if (CancelSendButton.Text == "Cancel")
                m_cancelled = true;
            else
                DoIt();
        }
    }
}
