using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace тест
{
    public partial class Form2 : Form
    {
        Form1 form1;
        ProgressBar progressBar;
        Progress<int> progress;
        public Form2(Form1 parent)
        {
            InitializeComponent();
            form1 = parent;
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            progressBar = form1.Controls.Find("progressBar1", true).FirstOrDefault() as ProgressBar;
            progress = new Progress<int>(value => { progressBar.Value = value; });
        }

        private async void trackBar1_Scroll(object sender, EventArgs e)
        {
            PictureBox pictureBox = form1.Controls.Find("pictureBox1", true).FirstOrDefault() as PictureBox;
            int value = trackBar1.Value;
            if (pictureBox != null)
                pictureBox.Image = await Task.Run(() => form1.FastContrasting(progress, value));
        }
    }
}
