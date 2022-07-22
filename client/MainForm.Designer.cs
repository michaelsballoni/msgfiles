namespace WinFormsApp1
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
            this.MesssagesTreeView = new System.Windows.Forms.TreeView();
            this.NewMessageFilesButton = new System.Windows.Forms.Button();
            this.OpenMessageButton = new System.Windows.Forms.Button();
            this.DeleteMessageButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.ChangeConnectAsButton = new System.Windows.Forms.Button();
            this.ConnectAsLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // MesssagesTreeView
            // 
            this.MesssagesTreeView.Location = new System.Drawing.Point(9, 146);
            this.MesssagesTreeView.Name = "MesssagesTreeView";
            this.MesssagesTreeView.Size = new System.Drawing.Size(982, 742);
            this.MesssagesTreeView.TabIndex = 1;
            // 
            // NewMessageFilesButton
            // 
            this.NewMessageFilesButton.Location = new System.Drawing.Point(9, 76);
            this.NewMessageFilesButton.Name = "NewMessageFilesButton";
            this.NewMessageFilesButton.Size = new System.Drawing.Size(319, 64);
            this.NewMessageFilesButton.TabIndex = 2;
            this.NewMessageFilesButton.Text = "Send Files";
            this.NewMessageFilesButton.UseVisualStyleBackColor = true;
            // 
            // OpenMessageButton
            // 
            this.OpenMessageButton.Location = new System.Drawing.Point(659, 76);
            this.OpenMessageButton.Name = "OpenMessageButton";
            this.OpenMessageButton.Size = new System.Drawing.Size(332, 64);
            this.OpenMessageButton.TabIndex = 3;
            this.OpenMessageButton.Text = "Open Message";
            this.OpenMessageButton.UseVisualStyleBackColor = true;
            // 
            // DeleteMessageButton
            // 
            this.DeleteMessageButton.Location = new System.Drawing.Point(334, 76);
            this.DeleteMessageButton.Name = "DeleteMessageButton";
            this.DeleteMessageButton.Size = new System.Drawing.Size(319, 64);
            this.DeleteMessageButton.TabIndex = 4;
            this.DeleteMessageButton.Text = "Delete Message";
            this.DeleteMessageButton.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 21);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(226, 41);
            this.label1.TabIndex = 5;
            this.label1.Text = "Connecting As: ";
            // 
            // ChangeConnectAsButton
            // 
            this.ChangeConnectAsButton.Location = new System.Drawing.Point(803, 12);
            this.ChangeConnectAsButton.Name = "ChangeConnectAsButton";
            this.ChangeConnectAsButton.Size = new System.Drawing.Size(188, 58);
            this.ChangeConnectAsButton.TabIndex = 6;
            this.ChangeConnectAsButton.Text = "Change";
            this.ChangeConnectAsButton.UseVisualStyleBackColor = true;
            // 
            // ConnectAsLabel
            // 
            this.ConnectAsLabel.AutoSize = true;
            this.ConnectAsLabel.Location = new System.Drawing.Point(241, 21);
            this.ConnectAsLabel.Name = "ConnectAsLabel";
            this.ConnectAsLabel.Size = new System.Drawing.Size(322, 41);
            this.ConnectAsLabel.TabIndex = 7;
            this.ConnectAsLabel.Text = "Display - Email - Server";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(17F, 41F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1003, 900);
            this.Controls.Add(this.ConnectAsLabel);
            this.Controls.Add(this.ChangeConnectAsButton);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.DeleteMessageButton);
            this.Controls.Add(this.OpenMessageButton);
            this.Controls.Add(this.NewMessageFilesButton);
            this.Controls.Add(this.MesssagesTreeView);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MainForm";
            this.Text = "Message Files - Home";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private TreeView MesssagesTreeView;
        private Button NewMessageFilesButton;
        private Button OpenMessageButton;
        private Button DeleteMessageButton;
        private Label label1;
        private Button ChangeConnectAsButton;
        private Label ConnectAsLabel;
    }
}