using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RenWeb
{
    public partial class Welcomer : Form
    {
        public Welcomer()
        {
            InitializeComponent();
        }

        private void Welcomer_Load(object sender, EventArgs e)
        {
            progressBar1.Style = ProgressBarStyle.Marquee;
            progressBar1.MarqueeAnimationSpeed = 10;
            progressBar1.Value = 0;
        }

        public void UpdateLabel(string Text)
        {
            label2.Text = Text;
        }

        public void Done()
        {
            UpdateLabel("Done! Now you can see your website in your IP/Host with Port!");
            button1.Visible = true;
            progressBar1.Visible = false;
            Refresh();
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            this.Close();
            this.Dispose();
        }
    }
}
