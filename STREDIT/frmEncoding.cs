using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace STREDIT
{
    public partial class frmEncoding : Form
    {
        public static int ChosenEncoding { get; private set; }
        public frmEncoding()
        {
            InitializeComponent();
        }

        private void frmEncoding_Load(object sender, EventArgs e)
        {
            comboBox1.SelectedIndex = 0;
        }

        private void frmEncoding_FormClosing(object sender, FormClosingEventArgs e)
        {
            switch (comboBox1.SelectedIndex)
            {
                case 0: ChosenEncoding = 949; break; // Korean
                case 1: ChosenEncoding = 28591; break; // European
                case 2: ChosenEncoding = 950; break; // Chinese Traditional (Big5)
                case 3: ChosenEncoding = 932; break; // Japanese
                case 4: ChosenEncoding = 936; break; // Chinese Simplified
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
