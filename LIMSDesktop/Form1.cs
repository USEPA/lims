using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace LIMSDesktop
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void btnSelectFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Select input file";
            if (ofd.ShowDialog() == DialogResult.OK)
                textBox1.Text = ofd.FileName;

            DataTable dt = new DataTable();
            dt.Columns.Add("Hello", typeof(string));
            dt.Columns.Add("Goodbye", typeof(string));
            DataRow dr = dt.NewRow();
            dt.Rows.Add(dr);

            templateDataGridView.DataSource = dt;
            
        }
    }
}
