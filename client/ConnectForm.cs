namespace msgfiles
{
    public partial class ConnectForm : Form, IClientApp
    {
        public ConnectForm()
        {
            InitializeComponent();

            NameTextBox.Text = GlobalState.DisplayName;
            EmailTextBox.Text = GlobalState.Email;

            ServerTextBox.Text = GlobalState.Server;
            if (!string.IsNullOrEmpty(ServerTextBox.Text) && GlobalState.Port != 9914)
                ServerTextBox.Text += ":" + GlobalState.Port;
        }

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
            StatusBarLabel.Text = msg;
        }

        private bool m_cancelled = false;

        private void CancelButton_Click(object sender, EventArgs e)
        {
            m_cancelled = true;
            
            while (!ConnectButton.Enabled)
                Application.DoEvents();

            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void ConnectButton_Click(object sender, EventArgs e)
        {
            try
            {
                ConnectButton.Enabled = false;
                DialogResult = DialogResult.None;

                if (string.IsNullOrWhiteSpace(NameTextBox.Text))
                {
                    MessageBox.Show("Fill in the Name field");
                    return;
                }

                if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
                {
                    MessageBox.Show("Fill in the Email field");
                    return;
                }

                if (string.IsNullOrWhiteSpace(ServerTextBox.Text))
                {
                    MessageBox.Show("Fill in the Server field");
                    return;
                }

                GlobalState.DisplayName = NameTextBox.Text.Trim();
                GlobalState.Email = EmailTextBox.Text.Trim();

                GlobalState.Server = ServerTextBox.Text.Trim();
                int colon = GlobalState.Server.IndexOf(':');
                if (colon > 0)
                {
                    if (!int.TryParse(GlobalState.Server.Substring(colon + 1).Trim(), out GlobalState.Port))
                    {
                        MessageBox.Show("Invalid server:port");
                        return;
                    }
                    GlobalState.Server = GlobalState.Server.Substring(0, colon).Trim();
                }

                GlobalState.SaveSettings();

                using (var client = new Client(this))
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
                        StatusBarLabel.Text = "...";

                        var prompt_dialog = new PromptForm("Submit Challenge Response", "Enter the 6-digit code from the email you just got:");
                        if (prompt_dialog.ShowDialog() != DialogResult.OK)
                            return;

                        string response = prompt_dialog.ResultTextBox.Text.Trim();
                        if (string.IsNullOrEmpty(response))
                            return;

                        client.ContinueConnect(response);
                        if (Cancelled)
                            return;
                    }

                    GlobalState.SessionToken = client.SessionToken;
                    GlobalState.SaveSettings();

                    DialogResult = DialogResult.OK;
                    Close();
                }
            }
            catch (Exception exp)
            {
                MessageBox.Show($"ERROR: {Utils.SumExp(exp)}");
            }
            finally
            {
                StatusBarLabel.Text = "Ready";
                ConnectButton.Enabled = true;
            }
        }
    }
}
