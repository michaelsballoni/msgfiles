using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace msgfiles
{
    public partial class AddressBookForm : Form
    {
        public AddressBookForm(List<string> toAddresses)
        {
            InitializeComponent();

            if (GlobalState.Settings == null)
                throw new NullReferenceException("GlobalState.Settings");
            Addresses = GlobalState.Settings.GetSeries("AddressBook", "Address");
            Addresses.Sort();

            m_initialToAddresses = toAddresses;
        }

        private List<string> Addresses;
        private List<string> m_initialToAddresses;

        public List<string> AddressesToSendTo
        {
            get
            {
                var ret_val = new List<string>();
                foreach (var checked_item in AddressCheckListBox.CheckedItems)
                {
                    string? checked_str = checked_item != null ? checked_item.ToString() : null;
                    if (checked_str != null)
                        ret_val.Add(checked_str);
                }
                ret_val.Sort();
                return ret_val;
            }
        }

        private void AddressBookForm_Load(object sender, EventArgs e)
        {
            foreach (string address in Addresses)
            {
                int idx = AddressCheckListBox.Items.Add(address);
                AddressCheckListBox.SetItemChecked(idx, m_initialToAddresses.Contains(address));
            }

        }

        private void AddAddressButton_Click(object sender, EventArgs e)
        {
            using (var dlg = new PromptForm("Enter the address you would like to add:"))
            {
                if (dlg.ShowDialog() != DialogResult.OK || dlg.ResultValue.Length == 0)
                    return;

                string new_address = dlg.ResultValue;
                if (!Addresses.Contains(new_address))
                {
                    Addresses.Add(new_address);
                    Addresses.Sort();

                    if (GlobalState.Settings == null)
                        throw new NullReferenceException("GlobalState.Settings");
                    GlobalState.Settings.SetSeries("AddressBook", "Address", Addresses);
                    GlobalState.Settings.Save();

                    AddressCheckListBox.Items.Add(new_address);
                }
            }
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            List<string> items_to_delete = new List<string>();
            foreach (var selected_item in AddressCheckListBox.SelectedItems)
            {
                string? selected_str = selected_item != null ? selected_item.ToString() : null;
                if (selected_str != null)
                    items_to_delete.Add(selected_str);
            }
            
            foreach (string to_delete in items_to_delete)
            {
                Addresses.Remove(to_delete);
                AddressCheckListBox.Items.Remove(to_delete);
            }

            if (GlobalState.Settings == null)
                throw new NullReferenceException("GlobalState.Settings");
            GlobalState.Settings.SetSeries("AddressBook", "Address", Addresses);
            GlobalState.Settings.Save();
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }
    }
}
