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

        private void DoSend()
        {
            CancelSendButton.Text = "Cancel Send";

            bool success = false;
            try
            {
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

                    if (client.SendMsg(m_msg))
                    {
                        MessageBox.Show("Message sent!");
                        success = true;
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
                if (!success)
                {
                    CancelSendButton.Text = "Retry Send";
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
