namespace msgfiles
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.SendFilesButton = new System.Windows.Forms.Button();
            this.OpenMessageButton = new System.Windows.Forms.Button();
            this.DeleteMessageButton = new System.Windows.Forms.Button();
            this.ConnectAsButton = new System.Windows.Forms.Button();
            this.ConnectAsLabel = new System.Windows.Forms.Label();
            this.MessagesListBox = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            // 
            // SendFilesButton
            // 
            this.SendFilesButton.Location = new System.Drawing.Point(672, 78);
            this.SendFilesButton.Name = "SendFilesButton";
            this.SendFilesButton.Size = new System.Drawing.Size(319, 64);
            this.SendFilesButton.TabIndex = 4;
            this.SendFilesButton.Text = "Send Files";
            this.SendFilesButton.UseVisualStyleBackColor = true;
            this.SendFilesButton.Click += new System.EventHandler(this.SendFilesButton_Click);
            // 
            // OpenMessageButton
            // 
            this.OpenMessageButton.Location = new System.Drawing.Point(12, 76);
            this.OpenMessageButton.Name = "OpenMessageButton";
            this.OpenMessageButton.Size = new System.Drawing.Size(332, 64);
            this.OpenMessageButton.TabIndex = 2;
            this.OpenMessageButton.Text = "Open Message";
            this.OpenMessageButton.UseVisualStyleBackColor = true;
            // 
            // DeleteMessageButton
            // 
            this.DeleteMessageButton.Location = new System.Drawing.Point(350, 78);
            this.DeleteMessageButton.Name = "DeleteMessageButton";
            this.DeleteMessageButton.Size = new System.Drawing.Size(319, 64);
            this.DeleteMessageButton.TabIndex = 3;
            this.DeleteMessageButton.Text = "Delete Message";
            this.DeleteMessageButton.UseVisualStyleBackColor = true;
            // 
            // ConnectAsButton
            // 
            this.ConnectAsButton.Location = new System.Drawing.Point(12, 12);
            this.ConnectAsButton.Name = "ConnectAsButton";
            this.ConnectAsButton.Size = new System.Drawing.Size(188, 58);
            this.ConnectAsButton.TabIndex = 1;
            this.ConnectAsButton.Text = "Connect As:";
            this.ConnectAsButton.UseVisualStyleBackColor = true;
            this.ConnectAsButton.Click += new System.EventHandler(this.ConnectAsButton_Click);
            // 
            // ConnectAsLabel
            // 
            this.ConnectAsLabel.AutoSize = true;
            this.ConnectAsLabel.Location = new System.Drawing.Point(206, 21);
            this.ConnectAsLabel.Name = "ConnectAsLabel";
            this.ConnectAsLabel.Size = new System.Drawing.Size(322, 41);
            this.ConnectAsLabel.TabIndex = 7;
            this.ConnectAsLabel.Text = "Display - Email - Server";
            // 
            // MessagesListBox
            // 
            this.MessagesListBox.FormattingEnabled = true;
            this.MessagesListBox.ItemHeight = 41;
            this.MessagesListBox.Location = new System.Drawing.Point(9, 148);
            this.MessagesListBox.Name = "MessagesListBox";
            this.MessagesListBox.Size = new System.Drawing.Size(982, 742);
            this.MessagesListBox.TabIndex = 5;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(17F, 41F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1003, 904);
            this.Controls.Add(this.MessagesListBox);
            this.Controls.Add(this.ConnectAsLabel);
            this.Controls.Add(this.ConnectAsButton);
            this.Controls.Add(this.DeleteMessageButton);
            this.Controls.Add(this.OpenMessageButton);
            this.Controls.Add(this.SendFilesButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Message Files - Home";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private Button SendFilesButton;
        private Button OpenMessageButton;
        private Button DeleteMessageButton;
        private Button ConnectAsButton;
        private Label ConnectAsLabel;
        private ListBox MessagesListBox;
    }
}