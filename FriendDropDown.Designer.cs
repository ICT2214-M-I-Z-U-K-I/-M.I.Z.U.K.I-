namespace Mizuki
{
    partial class FriendDropDown
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
            NumberLabel = new Label();
            OK = new Button();
            FriendNumberCombo = new ComboBox();
            SuspendLayout();
            // 
            // NumberLabel
            // 
            NumberLabel.AutoSize = true;
            NumberLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            NumberLabel.Location = new Point(12, 25);
            NumberLabel.Name = "NumberLabel";
            NumberLabel.Size = new Size(219, 15);
            NumberLabel.TabIndex = 0;
            NumberLabel.Text = "Minimum Decryption Threshold Count";
            // 
            // OK
            // 
            OK.Location = new Point(79, 99);
            OK.Name = "OK";
            OK.Size = new Size(75, 31);
            OK.TabIndex = 2;
            OK.Text = "OK";
            OK.UseVisualStyleBackColor = true;
            OK.Click += OK_Click;
            // 
            // FriendNumberCombo
            // 
            FriendNumberCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            FriendNumberCombo.FormattingEnabled = true;
            FriendNumberCombo.Location = new Point(54, 58);
            FriendNumberCombo.Name = "FriendNumberCombo";
            FriendNumberCombo.Size = new Size(121, 23);
            FriendNumberCombo.TabIndex = 3;
            // 
            // FriendDropDown
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(247, 142);
            Controls.Add(FriendNumberCombo);
            Controls.Add(OK);
            Controls.Add(NumberLabel);
            Name = "FriendDropDown";
            StartPosition = FormStartPosition.CenterParent;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label NumberLabel;
        private Button OK;
        private ComboBox FriendNumberCombo;
    }
}