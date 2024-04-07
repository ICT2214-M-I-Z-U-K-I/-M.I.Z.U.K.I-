using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Mizuki
{
    public partial class AddFriend : Form
    {

        private Button OKButton;
        public string UUID { get; private set; }
        public string IPAddress { get; private set; }
        public string FriendName { get; private set; }


        public AddFriend(string UUID, string IPAddress, string FriendName)
        {
            InitializeComponent();

            UUIDText.Text = UUID;
            NameText.Text = FriendName;
            IPText.Text = IPAddress;

        }


        private Button button1;

        private void ConfirmButton_Click(object sender, EventArgs e)
        {
            FriendName = NameText.Text;
            IPAddress = IPText.Text;
            UUID = UUIDText.Text;
            DialogResult = DialogResult.OK;
            Close();
        }
    }

}

