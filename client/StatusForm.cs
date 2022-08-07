using Ionic.Zip;

namespace msgfiles
{
    public partial class StatusForm : Form, IClientApp
    {
        public StatusForm(ClientMessage msg)
        {
            m_msg = msg;
            InitializeComponent();
        }

        private ClientMessage m_msg;

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
            LogOutputTextBox.AppendText(msg + "\r");
        }
        public void Progress(double progress)
        {
            Application.DoEvents();
            StatusProgressBar.Value = (int)Math.Round(progress * StatusProgressBar.Maximum);
        }

        private bool m_cancelled = false;

        private void DoSend()
        {
            CancelSendButton.Text = "Cancel Send";
            int seconds_between_retries = 3;

            bool success = false;
            while (!Cancelled)
            {
                try
                {
                    using (var client = new Client(this))
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

                        MsgClient msg_client = new MsgClient(client);
                        if (msg_client.SendMsg(m_msg.To, m_msg.Subject, m_msg.Body, m_msg.Paths))
                        {
                            MessageBox.Show("Message sent!");
                            success = true;
                            Close();
                        }
                        else
                            MessageBox.Show("Sending message failed!");
                        return;
                    }
                }
                catch (NetworkException)
                {
                    Log("Sending the message failed due to a network error, will retry");
                    Thread.Sleep(seconds_between_retries * 1000);
                }
                catch (Exception exp)
                {
                    // FORNOW - If InputException, show just Message
                    MessageBox.Show($"ERROR: {Utils.SumExp(exp)}");
                    return;
                }
                finally
                {
                    if (!success)
                    {
                        CancelSendButton.Text = "Retry Send";
                    }
                }
            }
        }

        private void StatusForm_Load(object sender, EventArgs e)
        {
            Show();
            DoSend();
        }

        private void CancelSendButton_Click(object sender, EventArgs e)
        {
            if (CancelSendButton.Text == "Cancel Send")
                m_cancelled = true;
            else
                DoSend();
        }
    }
}
