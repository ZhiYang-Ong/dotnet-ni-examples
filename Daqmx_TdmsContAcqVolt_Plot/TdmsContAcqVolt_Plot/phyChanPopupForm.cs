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
            int thisSearchIndex, lastSearchIndex;
            string thisItem, tempItem, lastItem, lastSyntax; 
            tempItem = lastItem = lastSyntax = "";
            foreach (var item in phyChanListBox.SelectedItems)
            {
                thisItem = item.ToString();
                if (first == true)
                {
                    phyChanSyntax = lastItem = lastSyntax = thisItem;
                    first = false;
                }
                else
                {
                    // If the current device is same as previous one, and the channel number follows, concatenate the channels
                    thisSearchIndex = thisItem.IndexOf(chanSearchKey);
                    lastSearchIndex = lastItem.IndexOf(chanSearchKey);
                    if (
                        thisItem.Substring(0, thisSearchIndex) == lastItem.Substring(0, lastSearchIndex) 
                        && int.Parse(thisItem.Substring(thisSearchIndex+3)) == int.Parse(lastItem.Substring(lastSearchIndex+3))+1
                        )
                    {
                        phyChanSyntax = phyChanSyntax.Substring(0, phyChanSyntax.IndexOf(lastSyntax));
                        if (lastSyntax.Contains(":"))
                        {
                            tempItem = lastSyntax.Substring(0, lastSyntax.IndexOf(":") + 1)
                                + thisItem.Substring(thisItem.IndexOf(chanSearchKey) + 3);
                        }
                        else
                        {
                            tempItem = lastItem + ":" + thisItem.Substring(thisItem.IndexOf(chanSearchKey) + 3);
                        }
                        phyChanSyntax += tempItem;
                    }
                    else
                    {
                        tempItem = thisItem;
                        phyChanSyntax += ", " + tempItem;
                    }                  
                    lastSyntax = tempItem;
                    lastItem = thisItem;
                }
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
