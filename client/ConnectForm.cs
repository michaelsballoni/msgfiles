using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace msgfiles
{
    public partial class ConnectForm : Form, IClientApp
    {
        public ConnectForm()
        {
            InitializeComponent();

            NameTextBox.Text = GlobalState.DisplayName;
            EmailTextBox.Text = GlobalState.Email;
            ServerTextBox.Text = GlobalState.Server + ":" + GlobalState.Port;
        }

        public bool Cancelled => m_cancelled;

        public void Log(string msg)
        {
            Application.DoEvents();
            ProgressLabel.Text = msg;
        }

        private bool m_cancelled = false;

        private void CancelButton_Click(object sender, EventArgs e)
        {
            m_cancelled = true;
            Close();
        }

        private async void ConnectButton_Click(object sender, EventArgs e)
        {
            try
            {
                ConnectButton.Enabled = false;

                GlobalState.DisplayName = NameTextBox.Text.Trim();
                GlobalState.Email = EmailTextBox.Text.Trim();

                GlobalState.Server = ServerTextBox.Text.Trim();
                int colon = GlobalState.Server.IndexOf(':');
                if (colon > 0)
                {
                    if (!int.TryParse(GlobalState.Server.Substring(colon + 1), out GlobalState.Port))
                    {
                        MessageBox.Show("Invalid server:port");
                        DialogResult = DialogResult.Cancel;
                        return;
                    }
                    GlobalState.Server = GlobalState.Server.Substring(0, colon);
                }

                using (var client = new Client(this))
                {
                    client.SessionToken = GlobalState.SessionToken;

                    bool challenged = 
                        await client.BeginConnectAsync(GlobalState.Server, GlobalState.Port, GlobalState.DisplayName, GlobalState.Email);
                    if (Cancelled)
                    {
                        DialogResult = DialogResult.Cancel;
                        return;
                    }

                    if (!challenged)
                    {
                        GlobalState.SessionToken = client.SessionToken;
                        return;
                    }

                    var prompt_dialog = new PromptForm("Submit Challenge Response", "Enter the 6-digit code from the email you just got:");
                    if (prompt_dialog.ShowDialog() != DialogResult.OK)
                    {
                        DialogResult = DialogResult.Cancel;
                        return;
                    }

                    await client.ContinueConnectAsync(prompt_dialog.ResultTextBox.Text);

                    GlobalState.SessionToken = client.SessionToken;
                }
            }
            catch (Exception exp)
            {
                MessageBox.Show($"ERROR: {exp.GetType().FullName}: {exp.Message}");
            }
            finally
            {
                ProgressLabel.Text = "";
                ConnectButton.Enabled = true;
            }
        }
    }
}
