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
            this.ReturnButton = new System.Windows.Forms.Button();
            this.AddAddressButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // AddressCheckListBox
            // 
            this.AddressCheckListBox.FormattingEnabled = true;
            this.AddressCheckListBox.Location = new System.Drawing.Point(12, 12);
            this.AddressCheckListBox.Name = "AddressCheckListBox";
            this.AddressCheckListBox.Size = new System.Drawing.Size(887, 708);
            this.AddressCheckListBox.TabIndex = 1;
            // 
            // OkButton
            // 
            this.OkButton.Location = new System.Drawing.Point(711, 726);
            this.OkButton.Name = "OkButton";
            this.OkButton.Size = new System.Drawing.Size(188, 58);
            this.OkButton.TabIndex = 3;
            this.OkButton.Text = "OK";
            this.OkButton.UseVisualStyleBackColor = true;
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
            // ReturnButton
            // 
            this.ReturnButton.Location = new System.Drawing.Point(12, 726);
            this.ReturnButton.Name = "ReturnButton";
            this.ReturnButton.Size = new System.Drawing.Size(188, 58);
            this.ReturnButton.TabIndex = 2;
            this.ReturnButton.Text = "Cancel";
            this.ReturnButton.UseVisualStyleBackColor = true;
            // 
            // AddAddressButton
            // 
            this.AddAddressButton.Location = new System.Drawing.Point(299, 726);
            this.AddAddressButton.Name = "AddAddressButton";
            this.AddAddressButton.Size = new System.Drawing.Size(295, 58);
            this.AddAddressButton.TabIndex = 4;
            this.AddAddressButton.Text = "Add Address";
            this.AddAddressButton.UseVisualStyleBackColor = true;
            this.AddAddressButton.Click += new System.EventHandler(this.AddAddressButton_Click);
            // 
            // AddressBookForm
            // 
            this.AcceptButton = this.OkButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(17F, 41F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.ReturnButton;
            this.ClientSize = new System.Drawing.Size(911, 802);
            this.Controls.Add(this.AddAddressButton);
            this.Controls.Add(this.ReturnButton);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.OkButton);
            this.Controls.Add(this.AddressCheckListBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AddressBookForm";
            this.ShowInTaskbar = false;
            this.Text = "Message Files - Address Book";
            this.Load += new System.EventHandler(this.AddressBookForm_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private CheckedListBox AddressCheckListBox;
        private Button OkButton;
        private Button button1;
        private Button ReturnButton;
        private Button AddAddressButton;
    }
}