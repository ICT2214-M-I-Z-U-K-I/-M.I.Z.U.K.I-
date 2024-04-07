using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using VTDev.Libraries.CEXEngine.Crypto.Cipher.Asymmetric.Encrypt.NTRU;
using VTDev.Libraries.CEXEngine.Crypto.Cipher.Asymmetric.Interfaces;
using Mizuki.Classes;
using System.Collections;
using System.Runtime.Versioning;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Mizuki
{
    [RequiresPreviewFeatures]
    public partial class MainPage : Form
    {
        private Controller _currentController;

        public MainPage()
        {
            InitializeComponent();
            encryptFileButton.Enabled = false;
            decryptFileButton.Enabled = false;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        public string EditedName { get; private set; }


        public async Task InitializeAsync()
        {
            AllocConsole();
            _currentController = new Controller();
            EnderChest.controller = _currentController;
            EnderChest.mainPage = this;
            if (_currentController.HasProfile)
            {
                profileUUIDLabel.Text = $" {_currentController.UUID}";
            }

            HandleFriendChange();
        }

        private void mouseDoubleClick(object sender, MouseEventArgs e)
        {
            string rawData = "Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book. It has survived not only five centuries, but also the leap into electronic typesetting, remaining essentially unchanged. It was popularised in the 1960s with the release of Letraset sheets containing Lorem Ipsum passages, and more recently with desktop publishing software like Aldus PageMaker including versions of Lorem Ipsum.";

            Encryption instanceOne = new Encryption(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            string cert = instanceOne.GenerateKeyPair();
            /*
            Encryption instanceTwo = new Encryption(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            instanceTwo.GenerateKeyPair(cert);
            */
            //Random rand = new Random();
            List<Encryption> members = new List<Encryption>();
            for (int i = 0; i < 5; i++)
            {
                string uuid = Guid.NewGuid().ToString();
                Encryption memberInstance = new Encryption(Guid.NewGuid().ToString(), uuid);
                memberInstance.GenerateKeyPair();
                instanceOne.LoadMembersKey(uuid, memberInstance.PublicKey);
                members.Add(memberInstance);
            }

            byte[] rawDataEncoded = Encoding.ASCII.GetBytes(rawData);
            var membersData = instanceOne.Encrypt(rawDataEncoded, 4);

            //////////////////////////////////////////////////////////////////////////////////////////

            List<string> shares = new List<string>();
            foreach (Encryption member in members)
            {
                byte[] decryptedData = member.NtruDecrypt(membersData[member.UUID]);
                string decryptedShares = Encoding.ASCII.GetString(decryptedData);
                shares.Add(decryptedShares);
            }
            shares.RemoveAt(shares.Count - 1);
            shares.RemoveAt(shares.Count - 1);
            byte[] extractedDataEncoded = instanceOne.Decrypt(instanceOne.EncryptedData, shares);
            string extractedData = Encoding.ASCII.GetString(extractedDataEncoded);
            Console.WriteLine($"Original Secret: {rawData}\n");
            Console.WriteLine($"Encrypted Secret: {instanceOne.EncryptedData}\n");
            Console.WriteLine($"Decrypted Secret: {extractedData}\n");
        }

        private void browseFileButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "All files (*.*)|*.*";
            fileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                string filename = fileDialog.FileName;
                string safeFileName = fileDialog.FileName;
                browseFileTextBox.Text = safeFileName;
                browseFileTextBox.Tag = filename;

                HandleFileBoxUpdate();
            }
        }

        private void HandleFriendChange()
        {
            friendsListView.Items.Clear();
            Dictionary<string, string> friendsList = _currentController.GetFriendsList();
            foreach (var friend in friendsList)
            {
                ListViewItem friendItem = new ListViewItem(friend.Value);
                friendItem.SubItems.Add(friend.Key);
                friendsListView.Items.Add(friendItem);
            }
        }

        public void HandleFileBoxUpdate()
        {
            if (browseFileTextBox.Text.Substring(browseFileTextBox.Text.Length - 4) == ".mzk")
            {
                encryptFileButton.Enabled = false;
                decryptFileButton.Enabled = true;
            }
            else
            {
                decryptFileButton.Enabled = false;
                if (friendsListView.SelectedItems.Count > 0)
                {
                    encryptFileButton.Enabled = true;
                }
            }
        }

        private void populateFriendsButton_Click(object sender, EventArgs e)
        {
            
            /*Temporary Here, Need Add Friend Function*/
            EnderChest.controller.Database.InsertFriend("2bc1ca2e-3d6b-48b2-92e6-cb20ae149dd4", "uyou", null, "10.42.185.13");
            EnderChest.controller.Database.InsertFriend("fdecfe5e-8b71-4abe-8ba2-4a4437b2f959", "Javier", null, "192.168.91.128");
            EnderChest.controller.Database.InsertFriend("a18c40ed-9a59-499f-8366-4759b1d05364", "J2", null, "10.5.16.2");
            EnderChest.controller.Database.InsertFriend("cf73a92e-f315-43e3-9569-d08d78ccfbfa", "J4", null, "192.168.91.131");
            /**/
            HandleFriendChange();
        }

        private async void encryptFileButton_Click(object sender, EventArgs e)

        {
            int FriendNumber = friendsListView.SelectedItems.Count;
            using (var FriendDropDown = new FriendDropDown(FriendNumber))
            {
                if (FriendDropDown.ShowDialog() == DialogResult.OK)
                {
                    int numberOfFriends = FriendDropDown.FriendNumber;

                    // Use the selected number of friends as needed
                    MessageBox.Show($"Choose the Minimum Amount of Peers Needed for Decryption: {numberOfFriends}");

                    string filePath = (string) browseFileTextBox.Tag;
                    List<Guid> allGuids = new List<Guid>();
                    allGuids.Add(new Guid(_currentController.UUID));
                    foreach (ListViewItem friendItem in friendsListView.SelectedItems)
                    {
                        allGuids.Add(new Guid(friendItem.SubItems[1].Text));
                    }

                    string outputText = await _currentController.EncryptFile(allGuids, numberOfFriends, filePath);
                    MessageBox.Show(outputText);
                }
            }
        }

        private async void decryptFileButton_Click(object sender, EventArgs e)
        {
            string filePath = (string) browseFileTextBox.Tag;
            string outputText;
            string decryptResult;
            (outputText, decryptResult)  = await _currentController.DecryptFile(filePath);
            if (decryptResult != string.Empty) 
            { 
                
            }
            MessageBox.Show(outputText);
        }

        private void profileUUIDLabel_Click(object sender, EventArgs e)
        {
            Label label = sender as Label;

            if (label != null && !string.IsNullOrEmpty(label.Text))
            {
                Clipboard.SetText(label.Text.Substring(1));
                MessageBox.Show("UUID Copied");
            }
        }

        private void dragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Any()) Console.WriteLine(files.First());
            browseFileTextBox.Text = files.First();

            HandleFileBoxUpdate();
        }

        private void dragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Link;
            else
                e.Effect = DragDropEffects.None;
        }
        private void EditFriend(object sender, EventArgs e)
        {

            if (friendsListView.SelectedItems.Count > 0)
            {
                ListViewItem selectedItem = friendsListView.SelectedItems[0];
                //pls change the IP
                string friendIP = "192.168.1.1";
                string currentName = selectedItem.SubItems[0].Text;

                using (var editPage = new EditPage(currentName, friendIP))
                {
                    if (editPage.ShowDialog() == DialogResult.OK)
                    {

                        string editedName = editPage.editedName;
                        string editedIP = editPage.editedIP;
                        selectedItem.SubItems[0].Text = editedName;

                        MessageBox.Show($"Friend with IP {editedIP} updated. New name: {editedName}");
                    }
                }
            }
        }

        private void DeleteFriend(object sender, EventArgs e)
        {
            if (friendsListView.SelectedItems.Count > 0)
            {
                ListViewItem selectedItem = friendsListView.SelectedItems[0];
                string friendId = selectedItem.SubItems[1].Text;
                _currentController.Database.DeleteFriendWithUUID(new Guid(friendId));
                HandleFriendChange();
                MessageBox.Show($"Deleted Friend with ID: {friendId}");
            }
        }
        private void friendsListView_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (friendsListView.FocusedItem != null)
                {
                    friendsListView.FocusedItem = friendsListView.GetItemAt(e.X, e.Y);

                    ContextMenuStrip contextMenu = new ContextMenuStrip();
                    contextMenu.Items.Add("Edit", null, EditFriend);
                    contextMenu.Items.Add("Delete", null, DeleteFriend);
                    contextMenu.Show(friendsListView, e.Location);
                }
            }
        }

        private void AddFriendButton_Click(object sender, EventArgs e)
        {
            String UUID = "";
            String IPAddress = "";
            String FriendName = "";
            using (var AddFriend = new AddFriend(UUID, IPAddress, FriendName))
            {
                if (AddFriend.ShowDialog() == DialogResult.OK)
                {
                    string id = AddFriend.UUID;
                    string ipAddress = AddFriend.IPAddress;
                    string name = AddFriend.FriendName;

                    try
                    {
                        if (id != "") { Guid guid = new Guid(id); }
                        if (ipAddress != "") { System.Net.IPAddress.Parse(ipAddress); }

                        _currentController.Database.InsertFriend(id, name, null, ipAddress);
                        HandleFriendChange();
                        MessageBox.Show($"UUID: {id}\nIP Address: {ipAddress}\nName: {name}");
                        return;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Either GUID or IP Address is Invalid");
                    }
                }
                else
                {
                    MessageBox.Show("No friends added.");
                }
            }
        }


    }
}
