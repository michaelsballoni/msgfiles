namespace msgfiles
{
    public partial class PromptForm : Form
    {
        public PromptForm(string title, string prompt)
        {
            InitializeComponent();

            Text += title;
            PromptMessageLabel.Text = prompt;
        }

        private void PromptForm_Load(object sender, EventArgs e)
        {
            PromptMessageLabel.Focus();
        }
    }
}
