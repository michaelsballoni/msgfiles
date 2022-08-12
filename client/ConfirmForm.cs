namespace msgfiles
{
    public partial class ConfirmForm : Form
    {
        public ConfirmForm(string confirmText, string toConfirmBodyText)
        {
            InitializeComponent();
            ConfirmMessageLabel.Text = confirmText;
            ContentTextBox.Text = toConfirmBodyText;
        }

        private void ConfirmForm_Load(object sender, EventArgs e)
        {

        }
    }
}
