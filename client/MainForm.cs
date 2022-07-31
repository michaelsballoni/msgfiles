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

            ConnectForm dlg = new ConnectForm();
            if (dlg.ShowDialog() != DialogResult.OK)
                return false;

            ConnectAsLabel.Text =
                GlobalState.DisplayName + " <" + GlobalState.Email + "> on " +
                GlobalState.Server + ":" + GlobalState.Port;

            return true;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            GlobalState.Init();
            Connect();
        }

        private void ConnectAsButton_Click(object sender, EventArgs e)
        {
            Connect();
        }

        private void SendFilesButton_Click(object sender, EventArgs e)
        {
            var dlg = new SendFilesForm();
            dlg.Show();
        }
    }
}
