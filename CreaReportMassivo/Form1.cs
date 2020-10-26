using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CreaReportMassivo
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
           
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text != "" && textBox2.Text != "")
            {
                Siav.APFlibrary.Flux apfLibrary = new Siav.APFlibrary.Flux();
                if (radioButton1.Checked == true)
                    apfLibrary.CreateReportMassiveProVal(textBox1.Text, textBox2.Text);
                else if (radioButton2.Checked == true)
                    apfLibrary.CreateReportMassiveIsc(textBox1.Text, textBox2.Text);
                else if (radioButton3.Checked == true)
                    apfLibrary.CreateReportMassiveCanc(textBox1.Text, textBox2.Text);
                else if (radioButton4.Checked == true)
                    apfLibrary.CreateReportMassiveIng(textBox1.Text, textBox2.Text);
            }
        }
    }
}
