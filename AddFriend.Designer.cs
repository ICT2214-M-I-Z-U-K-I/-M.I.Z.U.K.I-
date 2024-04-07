namespace Mizuki
{
    partial class AddFriend
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
            ConfirmButton = new Button();
            label1 = new Label();
            NameLabel = new Label();
            IPLabel = new Label();
            UUIDLabel = new Label();
            NameText = new TextBox();
            IPText = new TextBox();
            UUIDText = new TextBox();
            SuspendLayout();
            // 
            // ConfirmButton
            // 
            ConfirmButton.Location = new Point(580, 135);
            ConfirmButton.Name = "ConfirmButton";
            ConfirmButton.Size = new Size(91, 30);
            ConfirmButton.TabIndex = 0;
            ConfirmButton.Text = "OK";
            ConfirmButton.UseVisualStyleBackColor = true;
            ConfirmButton.Click += ConfirmButton_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 9F, FontStyle.Bold | FontStyle.Underline, GraphicsUnit.Point, 0);
            label1.Location = new Point(26, 9);
            label1.Name = "label1";
            label1.Size = new Size(67, 15);
            label1.TabIndex = 1;
            label1.Text = "Add Friend";
            // 
            // NameLabel
            // 
            NameLabel.AutoSize = true;
            NameLabel.Location = new Point(26, 59);
            NameLabel.Name = "NameLabel";
            NameLabel.Size = new Size(39, 15);
            NameLabel.TabIndex = 2;
            NameLabel.Text = "Name";
            // 
            // IPLabel
            // 
            IPLabel.AutoSize = true;
            IPLabel.Location = new Point(204, 59);
            IPLabel.Name = "IPLabel";
            IPLabel.Size = new Size(17, 15);
            IPLabel.TabIndex = 3;
            IPLabel.Text = "IP";
            // 
            // UUIDLabel
            // 
            UUIDLabel.AutoSize = true;
            UUIDLabel.Location = new Point(397, 59);
            UUIDLabel.Name = "UUIDLabel";
            UUIDLabel.Size = new Size(34, 15);
            UUIDLabel.TabIndex = 4;
            UUIDLabel.Text = "UUID";
            // 
            // NameText
            // 
            NameText.Location = new Point(26, 95);
            NameText.Name = "NameText";
            NameText.Size = new Size(132, 23);
            NameText.TabIndex = 5;
            // 
            // IPText
            // 
            IPText.Location = new Point(195, 95);
            IPText.Name = "IPText";
            IPText.Size = new Size(142, 23);
            IPText.TabIndex = 6;
            // 
            // UUIDText
            // 
            UUIDText.Location = new Point(397, 95);
            UUIDText.Name = "UUIDText";
            UUIDText.Size = new Size(201, 23);
            UUIDText.TabIndex = 7;
            // 
            // AddFriend
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(683, 170);
            Controls.Add(UUIDText);
            Controls.Add(IPText);
            Controls.Add(NameText);
            Controls.Add(UUIDLabel);
            Controls.Add(IPLabel);
            Controls.Add(NameLabel);
            Controls.Add(label1);
            Controls.Add(ConfirmButton);
            Name = "AddFriend";
            StartPosition = FormStartPosition.CenterParent;
            Text = "AddFriend";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button ConfirmButton;
        private Label label1;
        private Label NameLabel;
        private Label IPLabel;
        private Label UUIDLabel;
        private TextBox NameText;
        private TextBox IPText;
        private TextBox UUIDText;
    }
}