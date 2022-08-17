using System.Data;

namespace msgfiles
{
    public partial class SendFilesForm : Form
    {
        public SendFilesForm()
        {
            InitializeComponent();
        }

        public ClientMessage? Message;

        private List<string> ToAddresses
        {
            get
            {
                return
                    ToEmailsTextBox.Text
                    .Split(';')
                    .Select(to => to.Trim())
                    .Where(to => to.Length > 0)
                    .ToList();
            }
            set
            {
                ToEmailsTextBox.Text = string.Join("; ", value);
            }
        }

        private void AddFilesButton_Click(object sender, EventArgs e)
        {
            HashSet<string> existing_paths = new HashSet<string>();
            foreach (var item in FilesListBox.Items)
            {
                if (item == null)
                    continue;
                var path = item.ToString();
                if (path == null)
                    continue;
                existing_paths.Add(path);
            }

            using (var dlg = new OpenFileDialog())
            {
                dlg.CheckFileExists = true;
                dlg.CheckPathExists = true;
                dlg.Multiselect = true;
                dlg.ValidateNames = true;
                if (dlg.ShowDialog() != DialogResult.OK)
                    return;

                foreach (var new_path in dlg.FileNames)
                {
                    if (!existing_paths.Contains(new_path))
                        FilesListBox.Items.Add(new_path);
                }
            }
        }

        private void RemoveFilesButton_Click(object sender, EventArgs e)
        {
            HashSet<string> selected_paths = new HashSet<string>();
            foreach (var item in FilesListBox.SelectedItems)
            {
                if (item == null)
                    continue;
                var path = item.ToString();
                if (path == null)
                    continue;
                selected_paths.Add(path);
            }

            foreach (var item in selected_paths)
                FilesListBox.Items.Remove(item);
        }

        private void AddFolderButton_Click(object sender, EventArgs e)
        {
            HashSet<string> existing_paths = new HashSet<string>();
            foreach (var item in FilesListBox.Items)
            {
                if (item == null)
                    continue;
                var path = item.ToString();
                if (path == null)
                    continue;
                existing_paths.Add(path);
            }

            using (var dlg = new FolderBrowserDialog())
            {
                dlg.ShowNewFolderButton = false;
                if (dlg.ShowDialog() == DialogResult.OK)
                    FilesListBox.Items.Add(dlg.SelectedPath);
            }
        }

        private void SendFilesForm_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data == null)
            {
                MessageBox.Show("Nothing was dropped");
                return;
            }

            var formats = new HashSet<string>(e.Data.GetFormats());
            if (!formats.Contains(DataFormats.FileDrop))
            {
                MessageBox.Show("You can only drop files and folders here");
                return;
            }

            HashSet<string> existing_paths = new HashSet<string>();
            foreach (var item in FilesListBox.Items)
            {
                if (item == null)
                    continue;
                var path = item.ToString();
                if (path == null)
                    continue;
                existing_paths.Add(path);
            }

            var dropped = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var path in dropped)
            {
                if (!existing_paths.Contains(path))
                    FilesListBox.Items.Add(path);
            }
        }

        private void SendFilesForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e == null || e.Data == null)
                return;

            var formats = new HashSet<string>(e.Data.GetFormats());
            if (!formats.Contains(DataFormats.FileDrop))
            {
                MessageBox.Show("You can only drop files and folders here");
                return;
            }

            e.Effect = DragDropEffects.Copy;
        }

        private void SendFilesButton_Click(object sender, EventArgs e)
        {
            var to_addresses = ToAddresses;
            if (to_addresses.Count == 0)
            {
                MessageBox.Show("Specify email addresses to send the files to");
                ToEmailsTextBox.Focus();
                return;
            }

            var invalid_addressses = to_addresses.Where(to => Utils.GetValidEmail(to) == "");
            if (invalid_addressses.Count() > 0)
            {
                MessageBox.Show($"Invalid email addresses:\r\r{string.Join('\r', invalid_addressses)}");
                ToEmailsTextBox.Focus();
                return;
            }

            var message = MessageTextBox.Text.Trim();
            if (message.Length == 0)
            {
                MessageBox.Show("Specify the message");
                MessageTextBox.Focus();
                return;
            }

            if (FilesListBox.Items.Count == 0)
            {
                MessageBox.Show("Add files or folders that you want to send");
                return;
            }
            var paths = new List<string>();
            foreach (var item in FilesListBox.Items)
            {
                string? item_str = item == null ? "" : item.ToString();
                if (item_str == null)
                    continue;
                else
                    paths.Add(item_str);
            }

            Message = new ClientMessage(to_addresses, message, paths);

            DialogResult = DialogResult.OK;
            Close();
        }

        private void AddressButton_Click(object sender, EventArgs e)
        {
            using (var dlg = new AddressBookForm(ToAddresses))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                    ToAddresses = dlg.AddressesToSendTo;
            }
        }

        private void SendFilesForm_Load(object sender, EventArgs e)
        {
            Show();
            AddressButton_Click(sender, e);
        }
    }
}
