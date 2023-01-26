using NationalInstruments.DAQmx;
using NationalInstruments.Restricted;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Windows.Forms;
using static System.Windows.Forms.ListBox;

namespace PhyChanPopup
{
    public partial class phyChanPopupForm : Form
    {
        public phyChanPopupForm()
        {
            InitializeComponent();
        }

        public string phyChannels
        {
            get { return phyChanSyntax; }
        }

        private string phyChanSyntax = "";

        private void okButton_Click(object sender, EventArgs e)
        {
            const string chanSearchKey = "/ai";
            bool first = true;
            int i = 1;
            string thisItem, tempItem = "", lastItem = "";
            foreach (var item in phyChanListBox.SelectedItems)
            {
                thisItem = item.ToString();
                if (first == true)
                {
                    phyChanSyntax = thisItem;
                    lastItem = thisItem;
                    first = false;
                }
                else
                {
                    // If the current device is same as previous one, concatenate the channels
                    if (thisItem.Substring(0, thisItem.IndexOf(chanSearchKey)) == lastItem.Substring(0, lastItem.IndexOf(chanSearchKey)))
                    {
                        tempItem = lastItem;
                        phyChanSyntax = phyChanSyntax.Substring(0, phyChanSyntax.Length - lastItem.Length - 2);
                        if (tempItem.Contains(":"))
                        {
                            tempItem = tempItem.Substring(0, tempItem.IndexOf(":") + 1)
                                + thisItem.Substring(thisItem.IndexOf(chanSearchKey) + 3);
                        }
                        else
                        {
                            tempItem = lastItem + ":" + thisItem.Substring(thisItem.IndexOf(chanSearchKey) + 3);
                        }
                        phyChanSyntax += tempItem;
                    }
                    else
                        phyChanSyntax += thisItem;
                    lastItem = tempItem;
                }

                if (i != phyChanListBox.SelectedItems.Count)
                    phyChanSyntax += ", ";
                i++;
            }
            Close();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void phyChanPopupForm_Load(object sender, EventArgs e)
        {
            phyChanListBox.Items.Clear();
            phyChanListBox.Items.AddRange(DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.AI, PhysicalChannelAccess.External));
        }
    }
}
