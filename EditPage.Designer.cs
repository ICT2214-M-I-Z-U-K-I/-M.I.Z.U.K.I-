using Mizuki.Classes;

namespace Mizuki
{
    public partial class EditPage : Form
    {
        private TextBox textBox;
        private Button OKButton;
        private Button cancelButton;

        public string editedIP { get; private set; }
        public string editedName { get; private set; }


        public EditPage(string editedName,string editedIP)
        {
            InitializeComponent();


            NameText.Text = editedName;
            IPText.Text = editedIP;

        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            editedName = NameText.Text;
            editedIP = IPText.Text;

            DialogResult = DialogResult.OK;
            Close();
        }

        private Button button1;
        private Panel panel1;
    }

}