namespace msgfiles
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private bool Connect()
        {
            ConnectAsLabel.Text = "...";

            using (ConnectForm dlg = new ConnectForm())
            {
                if (dlg.ShowDialog() != DialogResult.OK)
                    return false;
            }

            ConnectAsLabel.Text =
                GlobalState.DisplayName + " <" + GlobalState.Email + "> on " +
                GlobalState.Server + ":" + GlobalState.Port;

            return true;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            GlobalState.Init();
            if (!Connect())
                Close();
        }

        private void ConnectAsButton_Click(object sender, EventArgs e)
        {
            Connect();
        }

        private void SendFilesButton_Click(object sender, EventArgs e)
        {
            ClientMessage client_message;
            using (var dlg = new SendFilesForm())
            {
                if (dlg.ShowDialog() != DialogResult.OK || dlg.Message == null)
                    return;
                else
                    client_message = dlg.Message;
            }

            using (var dlg = new StatusForm(client_message))
                dlg.ShowDialog();
        }

        private void GetFilesButton_Click(object sender, EventArgs e)
        {
            string pwd = "";
            using (var dlg = new PromptForm("Paste the access token you received in the email"))
            {
                if (dlg.ShowDialog() != DialogResult.OK || string.IsNullOrWhiteSpace(dlg.ResultValue))
                    return;
                else
                    pwd = dlg.ResultValue;
            }

            using (var status_dlg = new StatusForm(pwd))
                status_dlg.ShowDialog();
        }
    }
}
