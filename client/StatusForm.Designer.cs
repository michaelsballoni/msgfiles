namespace client
{
    partial class StatusForm
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
            this.StatusProgressBar = new System.Windows.Forms.ProgressBar();
            this.LogOutputTextBox = new System.Windows.Forms.TextBox();
            this.CancelSendButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // StatusProgressBar
            // 
            this.StatusProgressBar.Location = new System.Drawing.Point(12, 625);
            this.StatusProgressBar.Name = "StatusProgressBar";
            this.StatusProgressBar.Size = new System.Drawing.Size(896, 80);
            this.StatusProgressBar.TabIndex = 3;
            // 
            // LogOutputTextBox
            // 
            this.LogOutputTextBox.BackColor = System.Drawing.SystemColors.Window;
            this.LogOutputTextBox.Location = new System.Drawing.Point(12, 12);
            this.LogOutputTextBox.Multiline = true;
            this.LogOutputTextBox.Name = "LogOutputTextBox";
            this.LogOutputTextBox.ReadOnly = true;
            this.LogOutputTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.LogOutputTextBox.Size = new System.Drawing.Size(1204, 607);
            this.LogOutputTextBox.TabIndex = 1;
            this.LogOutputTextBox.WordWrap = false;
            // 
            // CancelSendButton
            // 
            this.CancelSendButton.Location = new System.Drawing.Point(914, 625);
            this.CancelSendButton.Name = "CancelSendButton";
            this.CancelSendButton.Size = new System.Drawing.Size(302, 80);
            this.CancelSendButton.TabIndex = 2;
            this.CancelSendButton.Text = "Cancel Send";
            this.CancelSendButton.UseVisualStyleBackColor = true;
            // 
            // StatusForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(17F, 41F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1228, 717);
            this.Controls.Add(this.CancelSendButton);
            this.Controls.Add(this.StatusProgressBar);
            this.Controls.Add(this.LogOutputTextBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "StatusForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Message Files - Send Message Status";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private ProgressBar StatusProgressBar;
        private TextBox LogOutputTextBox;
        private Button CancelSendButton;
    }
}