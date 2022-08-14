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

        public bool ShouldDelete = false;

        private void LooksBadButton_Click(object sender, EventArgs e)
        {
            ShouldDelete = true;
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void LooksGoodButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
