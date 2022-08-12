namespace msgfiles
{
    partial class AddressBookForm
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
            this.AddressCheckListBox = new System.Windows.Forms.CheckedListBox();
            this.OkButton = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.AddAddressButton = new System.Windows.Forms.Button();
            this.DeleteButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // AddressCheckListBox
            // 
            this.AddressCheckListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.AddressCheckListBox.FormattingEnabled = true;
            this.AddressCheckListBox.Location = new System.Drawing.Point(12, 76);
            this.AddressCheckListBox.Name = "AddressCheckListBox";
            this.AddressCheckListBox.Size = new System.Drawing.Size(1169, 664);
            this.AddressCheckListBox.Sorted = true;
            this.AddressCheckListBox.TabIndex = 4;
            // 
            // OkButton
            // 
            this.OkButton.Location = new System.Drawing.Point(992, 12);
            this.OkButton.Name = "OkButton";
            this.OkButton.Size = new System.Drawing.Size(188, 58);
            this.OkButton.TabIndex = 3;
            this.OkButton.Text = "OK";
            this.OkButton.UseVisualStyleBackColor = true;
            this.OkButton.Click += new System.EventHandler(this.OkButton_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(18, 296);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(8, 8);
            this.button1.TabIndex = 2;
            this.button1.Text = "button1";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // AddAddressButton
            // 
            this.AddAddressButton.Location = new System.Drawing.Point(12, 12);
            this.AddAddressButton.Name = "AddAddressButton";
            this.AddAddressButton.Size = new System.Drawing.Size(228, 58);
            this.AddAddressButton.TabIndex = 1;
            this.AddAddressButton.Text = "Add...";
            this.AddAddressButton.UseVisualStyleBackColor = true;
            this.AddAddressButton.Click += new System.EventHandler(this.AddAddressButton_Click);
            // 
            // DeleteButton
            // 
            this.DeleteButton.Location = new System.Drawing.Point(456, 12);
            this.DeleteButton.Name = "DeleteButton";
            this.DeleteButton.Size = new System.Drawing.Size(251, 58);
            this.DeleteButton.TabIndex = 2;
            this.DeleteButton.Text = "Delete Selected";
            this.DeleteButton.UseVisualStyleBackColor = true;
            this.DeleteButton.Click += new System.EventHandler(this.DeleteButton_Click);
            // 
            // AddressBookForm
            // 
            this.AcceptButton = this.OkButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(17F, 41F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1206, 769);
            this.Controls.Add(this.DeleteButton);
            this.Controls.Add(this.AddAddressButton);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.OkButton);
            this.Controls.Add(this.AddressCheckListBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "AddressBookForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Message Files - Address Book";
            this.Load += new System.EventHandler(this.AddressBookForm_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private CheckedListBox AddressCheckListBox;
        private Button OkButton;
        private Button button1;
        private Button AddAddressButton;
        private Button DeleteButton;
    }
}