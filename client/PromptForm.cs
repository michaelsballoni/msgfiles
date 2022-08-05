namespace msgfiles
{
    public partial class PromptForm : Form
    {
        public PromptForm(string prompt)
        {
            InitializeComponent();
            PromptMessageLabel.Text = prompt;
        }

        public string ResultValue => ResultTextBox.Text.Trim();

        private void PromptForm_Load(object sender, EventArgs e)
        {
            PromptMessageLabel.Focus();
        }
    }
}
