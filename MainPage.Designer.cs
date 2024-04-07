using System;

namespace Mizuki
{
    partial class MainPage
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
            openFileDialog1 = new OpenFileDialog();
            browseFileButton = new Button();
            browseFileTextBox = new TextBox();
            friendsListView = new ListView();
            friendsListViewNameColumnHeader = new ColumnHeader();
            friendsListViewUUIDColumnHeader = new ColumnHeader();
            populateFriendsButton = new Button();
            friendsListLabel = new Label();
            profileUUIDLabel = new Label();
            splitter1 = new Splitter();
            encryptionSectionLabel = new Label();
            decryptionSectionLabel = new Label();
            encryptFileButton = new Button();
            decryptFileButton = new Button();
            UUIDLabel = new Label();
            AddFriendButton = new Button();
            SuspendLayout();
            // 
            // openFileDialog1
            // 
            openFileDialog1.FileName = "openFileDialog1";
            // 
            // browseFileButton
            // 
            browseFileButton.Location = new Point(1084, 86);
            browseFileButton.Margin = new Padding(3, 2, 3, 2);
            browseFileButton.Name = "browseFileButton";
            browseFileButton.Size = new Size(131, 37);
            browseFileButton.TabIndex = 1;
            browseFileButton.Text = "Browse";
            browseFileButton.UseVisualStyleBackColor = true;
            browseFileButton.Click += browseFileButton_Click;
            // 
            // browseFileTextBox
            // 
            browseFileTextBox.Enabled = false;
            browseFileTextBox.Location = new Point(563, 86);
            browseFileTextBox.Margin = new Padding(3, 2, 3, 2);
            browseFileTextBox.Multiline = true;
            browseFileTextBox.Name = "browseFileTextBox";
            browseFileTextBox.Size = new Size(517, 38);
            browseFileTextBox.TabIndex = 2;
            // 
            // friendsListView
            // 
            friendsListView.Columns.AddRange(new ColumnHeader[] { friendsListViewNameColumnHeader, friendsListViewUUIDColumnHeader });
            friendsListView.FullRowSelect = true;
            friendsListView.Location = new Point(10, 32);
            friendsListView.Margin = new Padding(3, 2, 3, 2);
            friendsListView.Name = "friendsListView";
            friendsListView.Size = new Size(390, 459);
            friendsListView.TabIndex = 3;
            friendsListView.UseCompatibleStateImageBehavior = false;
            friendsListView.View = View.Details;
            friendsListView.MouseDown += friendsListView_MouseDown;
            // 
            // friendsListViewNameColumnHeader
            // 
            friendsListViewNameColumnHeader.Text = "Name";
            friendsListViewNameColumnHeader.Width = 120;
            // 
            // friendsListViewUUIDColumnHeader
            // 
            friendsListViewUUIDColumnHeader.Text = "UUID";
            friendsListViewUUIDColumnHeader.Width = 320;
            // 
            // populateFriendsButton
            // 
            populateFriendsButton.Location = new Point(405, 483);
            populateFriendsButton.Margin = new Padding(3, 2, 3, 2);
            populateFriendsButton.Name = "populateFriendsButton";
            populateFriendsButton.Size = new Size(129, 28);
            populateFriendsButton.TabIndex = 4;
            populateFriendsButton.Text = "Refresh Friends";
            populateFriendsButton.UseVisualStyleBackColor = true;
            populateFriendsButton.Click += populateFriendsButton_Click;
            // 
            // friendsListLabel
            // 
            friendsListLabel.BackColor = SystemColors.Control;
            friendsListLabel.BorderStyle = BorderStyle.Fixed3D;
            friendsListLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold | FontStyle.Underline, GraphicsUnit.Point, 0);
            friendsListLabel.Location = new Point(10, 10);
            friendsListLabel.Name = "friendsListLabel";
            friendsListLabel.Size = new Size(389, 19);
            friendsListLabel.TabIndex = 5;
            friendsListLabel.Text = "Friends List";
            friendsListLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // profileUUIDLabel
            // 
            profileUUIDLabel.BorderStyle = BorderStyle.Fixed3D;
            profileUUIDLabel.Location = new Point(83, 492);
            profileUUIDLabel.Name = "profileUUIDLabel";
            profileUUIDLabel.Size = new Size(316, 19);
            profileUUIDLabel.TabIndex = 6;
            profileUUIDLabel.Text = "00000000-0000-0000-0000-000000000000";
            profileUUIDLabel.Click += profileUUIDLabel_Click;
            // 
            // splitter1
            // 
            splitter1.Location = new Point(0, 0);
            splitter1.Margin = new Padding(3, 2, 3, 2);
            splitter1.Name = "splitter1";
            splitter1.Size = new Size(4, 518);
            splitter1.TabIndex = 8;
            splitter1.TabStop = false;
            // 
            // encryptionSectionLabel
            // 
            encryptionSectionLabel.BorderStyle = BorderStyle.Fixed3D;
            encryptionSectionLabel.Location = new Point(563, 144);
            encryptionSectionLabel.Name = "encryptionSectionLabel";
            encryptionSectionLabel.Size = new Size(422, 367);
            encryptionSectionLabel.TabIndex = 9;
            // 
            // decryptionSectionLabel
            // 
            decryptionSectionLabel.BorderStyle = BorderStyle.Fixed3D;
            decryptionSectionLabel.Location = new Point(1010, 144);
            decryptionSectionLabel.Name = "decryptionSectionLabel";
            decryptionSectionLabel.Size = new Size(422, 367);
            decryptionSectionLabel.TabIndex = 10;
            // 
            // encryptFileButton
            // 
            encryptFileButton.Location = new Point(858, 473);
            encryptFileButton.Name = "encryptFileButton";
            encryptFileButton.Size = new Size(127, 27);
            encryptFileButton.TabIndex = 11;
            encryptFileButton.Text = "Encrypt File";
            encryptFileButton.UseVisualStyleBackColor = true;
            encryptFileButton.Click += encryptFileButton_Click;
            // 
            // decryptFileButton
            // 
            decryptFileButton.Location = new Point(1290, 464);
            decryptFileButton.Name = "decryptFileButton";
            decryptFileButton.Size = new Size(127, 27);
            decryptFileButton.TabIndex = 12;
            decryptFileButton.Text = "Decrypt File";
            decryptFileButton.UseVisualStyleBackColor = true;
            decryptFileButton.Click += decryptFileButton_Click;
            // 
            // UUIDLabel
            // 
            UUIDLabel.BorderStyle = BorderStyle.Fixed3D;
            UUIDLabel.Location = new Point(10, 492);
            UUIDLabel.Name = "UUIDLabel";
            UUIDLabel.Size = new Size(76, 19);
            UUIDLabel.TabIndex = 14;
            UUIDLabel.Text = "Profile UUID:";
            // 
            // AddFriendButton
            // 
            AddFriendButton.Location = new Point(405, 451);
            AddFriendButton.Name = "AddFriendButton";
            AddFriendButton.Size = new Size(129, 27);
            AddFriendButton.TabIndex = 15;
            AddFriendButton.Text = "Add Friends";
            AddFriendButton.UseVisualStyleBackColor = true;
            AddFriendButton.Click += AddFriendButton_Click;
            // 
            // MainPage
            // 
            AllowDrop = true;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1600, 518);
            Controls.Add(AddFriendButton);
            Controls.Add(UUIDLabel);
            Controls.Add(decryptFileButton);
            Controls.Add(encryptFileButton);
            Controls.Add(decryptionSectionLabel);
            Controls.Add(encryptionSectionLabel);
            Controls.Add(splitter1);
            Controls.Add(profileUUIDLabel);
            Controls.Add(friendsListLabel);
            Controls.Add(populateFriendsButton);
            Controls.Add(friendsListView);
            Controls.Add(browseFileTextBox);
            Controls.Add(browseFileButton);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Margin = new Padding(3, 2, 3, 2);
            Name = "MainPage";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Mizuki";
            DragDrop += dragDrop;
            DragOver += dragOver;
            MouseDoubleClick += mouseDoubleClick;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private OpenFileDialog openFileDialog1;
        private Button browseFileButton;
        private TextBox browseFileTextBox;
        private ListView friendsListView;
        private ColumnHeader friendsListViewNameColumnHeader;
        private ColumnHeader friendsListViewUUIDColumnHeader;
        private Button populateFriendsButton;
        private Label friendsListLabel;
        private Label profileUUIDLabel;
        private Splitter splitter1;
        private Label encryptionSectionLabel;
        private Label decryptionSectionLabel;
        private Button encryptFileButton;
        private Button decryptFileButton;
        private Label UUIDLabel;
        private Button AddFriendButton;
    }
}