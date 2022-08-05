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
        public AddressBookForm()
        {
            InitializeComponent();

            if (GlobalState.Settings == null)
                throw new NullReferenceException("GlobalState.Settings");
            Addresses = GlobalState.Settings.GetSeries("AddressBook", "Address");
            Addresses.Sort();
        }

        private List<string> Addresses;

        public List<string> AddressesToAdd
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
                AddressCheckListBox.Items.Add(address);
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
                }
            }
        }
    }
}
