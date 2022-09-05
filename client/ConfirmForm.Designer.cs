namespace msgfiles
{
    partial class ConfirmForm
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
            this.ContentTextBox = new System.Windows.Forms.TextBox();
            this.ConfirmMessageLabel = new System.Windows.Forms.Label();
            this.LooksGoodButton = new System.Windows.Forms.Button();
            this.LooksBadButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // ContentTextBox
            // 
            this.ContentTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ContentTextBox.BackColor = System.Drawing.SystemColors.Window;
            this.ContentTextBox.Location = new System.Drawing.Point(12, 273);
            this.ContentTextBox.Multiline = true;
            this.ContentTextBox.Name = "ContentTextBox";
            this.ContentTextBox.ReadOnly = true;
            this.ContentTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.ContentTextBox.Size = new System.Drawing.Size(1329, 868);
            this.ContentTextBox.TabIndex = 3;
            // 
            // ConfirmMessageLabel
            // 
            this.ConfirmMessageLabel.AutoSize = true;
            this.ConfirmMessageLabel.Location = new System.Drawing.Point(12, 21);
            this.ConfirmMessageLabel.Name = "ConfirmMessageLabel";
            this.ConfirmMessageLabel.Size = new System.Drawing.Size(250, 41);
            this.ConfirmMessageLabel.TabIndex = 1;
            this.ConfirmMessageLabel.Text = "Confirm Message";
            // 
            // LooksGoodButton
            // 
            this.LooksGoodButton.Location = new System.Drawing.Point(883, 89);
            this.LooksGoodButton.Name = "LooksGoodButton";
            this.LooksGoodButton.Size = new System.Drawing.Size(412, 146);
            this.LooksGoodButton.TabIndex = 2;
            this.LooksGoodButton.Text = "Looks Good, Continue...";
            this.LooksGoodButton.UseVisualStyleBackColor = true;
            this.LooksGoodButton.Click += new System.EventHandler(this.LooksGoodButton_Click);
            // 
            // LooksBadButton
            // 
            this.LooksBadButton.Location = new System.Drawing.Point(50, 89);
            this.LooksBadButton.Name = "LooksBadButton";
            this.LooksBadButton.Size = new System.Drawing.Size(412, 146);
            this.LooksBadButton.TabIndex = 1;
            this.LooksBadButton.Text = "Looks Bad, Delete It!";
            this.LooksBadButton.UseVisualStyleBackColor = true;
            this.LooksBadButton.Click += new System.EventHandler(this.LooksBadButton_Click);
            // 
            // ConfirmForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(17F, 41F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1353, 1161);
            this.Controls.Add(this.LooksBadButton);
            this.Controls.Add(this.LooksGoodButton);
            this.Controls.Add(this.ConfirmMessageLabel);
            this.Controls.Add(this.ContentTextBox);
            this.Name = "ConfirmForm";
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Message Files - Confirm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private TextBox ContentTextBox;
        private Label ConfirmMessageLabel;
        private Button LooksGoodButton;
        private Button LooksBadButton;
    }
}