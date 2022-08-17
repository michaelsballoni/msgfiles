namespace msgfiles
{
    partial class SendFilesForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.AddFilesButton = new System.Windows.Forms.Button();
            this.RemoveFilesButton = new System.Windows.Forms.Button();
            this.FilesListBox = new System.Windows.Forms.ListBox();
            this.button3 = new System.Windows.Forms.Button();
            this.AddFolderButton = new System.Windows.Forms.Button();
            this.button4 = new System.Windows.Forms.Button();
            this.SendFilesButton = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.MessageTextBox = new System.Windows.Forms.TextBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.AddressButton = new System.Windows.Forms.Button();
            this.ToEmailsTextBox = new System.Windows.Forms.TextBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // AddFilesButton
            // 
            this.AddFilesButton.Location = new System.Drawing.Point(499, 548);
            this.AddFilesButton.Name = "AddFilesButton";
            this.AddFilesButton.Size = new System.Drawing.Size(237, 58);
            this.AddFilesButton.TabIndex = 11;
            this.AddFilesButton.Text = "Add Files";
            this.AddFilesButton.UseVisualStyleBackColor = true;
            this.AddFilesButton.Click += new System.EventHandler(this.AddFilesButton_Click);
            // 
            // RemoveFilesButton
            // 
            this.RemoveFilesButton.Location = new System.Drawing.Point(243, 548);
            this.RemoveFilesButton.Name = "RemoveFilesButton";
            this.RemoveFilesButton.Size = new System.Drawing.Size(228, 58);
            this.RemoveFilesButton.TabIndex = 9;
            this.RemoveFilesButton.Text = "Remove Files";
            this.RemoveFilesButton.UseVisualStyleBackColor = true;
            this.RemoveFilesButton.Click += new System.EventHandler(this.RemoveFilesButton_Click);
            // 
            // FilesListBox
            // 
            this.FilesListBox.FormattingEnabled = true;
            this.FilesListBox.ItemHeight = 41;
            this.FilesListBox.Location = new System.Drawing.Point(21, 46);
            this.FilesListBox.Name = "FilesListBox";
            this.FilesListBox.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.FilesListBox.Size = new System.Drawing.Size(1227, 496);
            this.FilesListBox.Sorted = true;
            this.FilesListBox.TabIndex = 7;
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(400, 940);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(8, 8);
            this.button3.TabIndex = 5;
            this.button3.Text = "button3";
            this.button3.UseVisualStyleBackColor = true;
            // 
            // AddFolderButton
            // 
            this.AddFolderButton.Location = new System.Drawing.Point(21, 548);
            this.AddFolderButton.Name = "AddFolderButton";
            this.AddFolderButton.Size = new System.Drawing.Size(194, 58);
            this.AddFolderButton.TabIndex = 10;
            this.AddFolderButton.Text = "Add Folder";
            this.AddFolderButton.UseVisualStyleBackColor = true;
            this.AddFolderButton.Click += new System.EventHandler(this.AddFolderButton_Click);
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(942, 1044);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(8, 8);
            this.button4.TabIndex = 7;
            this.button4.Text = "button4";
            this.button4.UseVisualStyleBackColor = true;
            // 
            // SendFilesButton
            // 
            this.SendFilesButton.Location = new System.Drawing.Point(753, 548);
            this.SendFilesButton.Name = "SendFilesButton";
            this.SendFilesButton.Size = new System.Drawing.Size(495, 58);
            this.SendFilesButton.TabIndex = 12;
            this.SendFilesButton.Text = "Send Files!";
            this.SendFilesButton.UseVisualStyleBackColor = true;
            this.SendFilesButton.Click += new System.EventHandler(this.SendFilesButton_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.MessageTextBox);
            this.groupBox1.Location = new System.Drawing.Point(12, 194);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(1254, 309);
            this.groupBox1.TabIndex = 9;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Message";
            // 
            // MessageTextBox
            // 
            this.MessageTextBox.Location = new System.Drawing.Point(15, 46);
            this.MessageTextBox.Multiline = true;
            this.MessageTextBox.Name = "MessageTextBox";
            this.MessageTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.MessageTextBox.Size = new System.Drawing.Size(1227, 245);
            this.MessageTextBox.TabIndex = 4;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.FilesListBox);
            this.groupBox2.Controls.Add(this.SendFilesButton);
            this.groupBox2.Controls.Add(this.AddFilesButton);
            this.groupBox2.Controls.Add(this.AddFolderButton);
            this.groupBox2.Controls.Add(this.RemoveFilesButton);
            this.groupBox2.Location = new System.Drawing.Point(12, 511);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(1248, 623);
            this.groupBox2.TabIndex = 10;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Files To Send - Drag-and-Drop files or folders to add or push buttons below";
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.AddressButton);
            this.groupBox3.Controls.Add(this.ToEmailsTextBox);
            this.groupBox3.Location = new System.Drawing.Point(12, 12);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(1254, 163);
            this.groupBox3.TabIndex = 11;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Email Addresses To Send To";
            // 
            // AddressButton
            // 
            this.AddressButton.Location = new System.Drawing.Point(1100, 46);
            this.AddressButton.Name = "AddressButton";
            this.AddressButton.Size = new System.Drawing.Size(142, 106);
            this.AddressButton.TabIndex = 2;
            this.AddressButton.Text = "...";
            this.AddressButton.UseVisualStyleBackColor = true;
            this.AddressButton.Click += new System.EventHandler(this.AddressButton_Click);
            // 
            // ToEmailsTextBox
            // 
            this.ToEmailsTextBox.BackColor = System.Drawing.SystemColors.Window;
            this.ToEmailsTextBox.Location = new System.Drawing.Point(15, 46);
            this.ToEmailsTextBox.Multiline = true;
            this.ToEmailsTextBox.Name = "ToEmailsTextBox";
            this.ToEmailsTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.ToEmailsTextBox.Size = new System.Drawing.Size(1072, 106);
            this.ToEmailsTextBox.TabIndex = 1;
            // 
            // SendFilesForm
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(17F, 41F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1280, 1161);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.button3);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SendFilesForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Message Files - Send Files";
            this.Load += new System.EventHandler(this.SendFilesForm_Load);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.SendFilesForm_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.SendFilesForm_DragEnter);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private Button AddFilesButton;
        private Button RemoveFilesButton;
        private ListBox FilesListBox;
        private Button button3;
        private Button AddFolderButton;
        private Button button4;
        private Button SendFilesButton;
        private GroupBox groupBox1;
        private TextBox MessageTextBox;
        private GroupBox groupBox2;
        private GroupBox groupBox3;
        private TextBox ToEmailsTextBox;
        private Button AddressButton;
    }
}