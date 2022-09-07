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
            this.ConnectAsButton = new System.Windows.Forms.Button();
            this.ConnectAsLabel = new System.Windows.Forms.Label();
            this.GetFilesButton = new System.Windows.Forms.Button();
            this.TheLinkLabel = new System.Windows.Forms.LinkLabel();
            this.SuspendLayout();
            // 
            // SendFilesButton
            // 
            this.SendFilesButton.Location = new System.Drawing.Point(73, 170);
            this.SendFilesButton.Name = "SendFilesButton";
            this.SendFilesButton.Size = new System.Drawing.Size(611, 255);
            this.SendFilesButton.TabIndex = 4;
            this.SendFilesButton.Text = "Send Files!";
            this.SendFilesButton.UseVisualStyleBackColor = true;
            this.SendFilesButton.Click += new System.EventHandler(this.SendFilesButton_Click);
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
            // GetFilesButton
            // 
            this.GetFilesButton.Location = new System.Drawing.Point(748, 170);
            this.GetFilesButton.Name = "GetFilesButton";
            this.GetFilesButton.Size = new System.Drawing.Size(611, 255);
            this.GetFilesButton.TabIndex = 8;
            this.GetFilesButton.Text = "Recieve Files!";
            this.GetFilesButton.UseVisualStyleBackColor = true;
            this.GetFilesButton.Click += new System.EventHandler(this.GetFilesButton_Click);
            // 
            // TheLinkLabel
            // 
            this.TheLinkLabel.AutoSize = true;
            this.TheLinkLabel.Location = new System.Drawing.Point(250, 473);
            this.TheLinkLabel.Name = "TheLinkLabel";
            this.TheLinkLabel.Size = new System.Drawing.Size(924, 41);
            this.TheLinkLabel.TabIndex = 9;
            this.TheLinkLabel.TabStop = true;
            this.TheLinkLabel.Text = "Visit msgfiles.io for details on this application and setting up a server";
            this.TheLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.TheLinkLabel_LinkClicked);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(17F, 41F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1432, 562);
            this.Controls.Add(this.TheLinkLabel);
            this.Controls.Add(this.GetFilesButton);
            this.Controls.Add(this.ConnectAsLabel);
            this.Controls.Add(this.ConnectAsButton);
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
        private Button ConnectAsButton;
        private Label ConnectAsLabel;
        private Button GetFilesButton;
        private LinkLabel TheLinkLabel;
    }
}