namespace msgfiles
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Show();

            GlobalState.Init();
            ConnectForm dlg = new ConnectForm();
            if (dlg.ShowDialog() != DialogResult.OK)
            {
                Close();
                return;
            }
        }

    }
}
