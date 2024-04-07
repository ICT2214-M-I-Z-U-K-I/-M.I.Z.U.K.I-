using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Mizuki
{
    public partial class FriendDropDown : Form
    {
        public int FriendNumber { get; private set; }
        public FriendDropDown(int FriendNumber)
        {
            InitializeComponent();
            if (FriendNumber == 1)
            {
                FriendNumberCombo.Items.Add(2);
            }
            else
            {
                for (int i = 2; i <= FriendNumber + 1; i++)
                {
                    FriendNumberCombo.Items.Add(i);
                }
            }
            // Set default selection to the first item
            FriendNumberCombo.SelectedIndex = 0;
        }


        private void OK_Click(object sender, EventArgs e)
        {
            FriendNumber = (int)FriendNumberCombo.SelectedItem;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
