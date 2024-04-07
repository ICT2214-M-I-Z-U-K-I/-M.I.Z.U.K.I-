using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Mizuki
{
    public partial class EditPage : Form
    {
        public EditPage()
        {
            InitializeComponent();
        }

        private Label label1;

        private void InitializeComponent()
        {
            NameText = new TextBox();
            HeaderLabel = new Label();
            IPText = new TextBox();
            NameLabel = new Label();
            IPLabel = new Label();
            OKButton = new Button();
            panel1 = new Panel();
            SuspendLayout();
            // 
            // NameText
            // 
            NameText.Location = new Point(12, 84);
            NameText.Multiline = true;
            NameText.Name = "NameText";
            NameText.Size = new Size(135, 33);
            NameText.TabIndex = 0;
            // 
            // HeaderLabel
            // 
            HeaderLabel.AutoSize = true;
            HeaderLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold | FontStyle.Underline, GraphicsUnit.Point, 0);
            HeaderLabel.Location = new Point(10, 20);
            HeaderLabel.Name = "HeaderLabel";
            HeaderLabel.Size = new Size(66, 15);
            HeaderLabel.TabIndex = 2;
            HeaderLabel.Text = "Edit Friend";
            // 
            // IPText
            // 
            IPText.Location = new Point(199, 84);
            IPText.Multiline = true;
            IPText.Name = "IPText";
            IPText.Size = new Size(135, 33);
            IPText.TabIndex = 3;
            // 
            // NameLabel
            // 
            NameLabel.AutoSize = true;
            NameLabel.Location = new Point(12, 66);
            NameLabel.Name = "NameLabel";
            NameLabel.Size = new Size(39, 15);
            NameLabel.TabIndex = 4;
            NameLabel.Text = "Name";
            // 
            // IPLabel
            // 
            IPLabel.AutoSize = true;
            IPLabel.Location = new Point(201, 66);
            IPLabel.Name = "IPLabel";
            IPLabel.Size = new Size(17, 15);
            IPLabel.TabIndex = 5;
            IPLabel.Text = "IP";
            // 
            // OKButton
            // 
            OKButton.Location = new Point(259, 155);
            OKButton.Name = "OKButton";
            OKButton.Size = new Size(75, 23);
            OKButton.TabIndex = 6;
            OKButton.Text = "Ok";
            OKButton.UseVisualStyleBackColor = true;
            OKButton.Click += OKButton_Click;
            // 
            // panel1
            // 
            panel1.Location = new Point(10, 44);
            panel1.Name = "panel1";
            panel1.Size = new Size(331, 138);
            panel1.TabIndex = 7;
            // 
            // EditPage
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(346, 194);
            Controls.Add(OKButton);
            Controls.Add(IPLabel);
            Controls.Add(HeaderLabel);
            Controls.Add(IPText);
            Controls.Add(NameText);
            Controls.Add(NameLabel);
            Controls.Add(panel1);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "EditPage";
            StartPosition = FormStartPosition.CenterParent;
            Text = "EditableMessageBox";
            ResumeLayout(false);
            PerformLayout();
        }

        private System.Windows.Forms.TextBox NameText;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Label HeaderLabel;
        private TextBox IPText;
        private Label IPLabel;
        private Label NameLabel;


    }
}

