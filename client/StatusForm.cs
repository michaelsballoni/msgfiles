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

        public StatusForm(string token)
        {
            InitializeComponent();
            Text = "Message Files - Receiving Files";
            m_token = token;
        }

        private ClientMessage? m_msg;
        private string? m_token;

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
            Progress(0.0);
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
                            catch (Exception exp)
                            {
                                if (exp is SocketException || exp is NetworkException)
                                {
                                    Log("Authentication failed due to a network error, will retry...");
                                    for (int t = 1; t <= seconds_between_retries * 10; ++t)
                                    {
                                        Thread.Sleep(100);
                                        if (Cancelled)
                                            break;
                                    }
                                }
                                else
                                    throw;
                            }
                        }

                        if 
                        (
                            !string.IsNullOrEmpty(m_token) // receive
                            && 
                            !success 
                            && 
                            should_delete 
                        )
                        {
                            client.DeleteMessage(m_token);
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
                        else if (m_token != null)
                        {
                            if (client.GetMessage(m_token, out should_delete))
                            {
                                client.DeleteMessage(m_token);
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

                            if (!should_delete)
                                return;
                        }
                        else
                            throw new NullReferenceException("m_msg and m_pwd");
                    }
                }
                catch (Exception exp)
                {
                    if (exp is SocketException || exp is NetworkException)
                    {
                        Log("The operation failed due to a network error, will retry...");
                        for (int t = 1; t <= seconds_between_retries * 10; ++t)
                        {
                            Thread.Sleep(100);
                            if (Cancelled)
                                break;
                        }
                    }
                    else if (exp is InputException)
                    {
                        MessageBox.Show(exp.Message);
                        return;
                    }
                    else
                    {
                        MessageBox.Show($"Unexpected error: {exp.Message}");
                        return;
                    }
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
