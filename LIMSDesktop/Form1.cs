using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using PluginBase;

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
                txtInput.Text = ofd.FileName;            
            
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ProcessorManager procMgr = new ProcessorManager();
            var lstProc = procMgr.GetProcessors(@"Processors/");
            comboBox1.DataSource = lstProc;
            comboBox1.DisplayMember = "Name";
            //comboBox1.ValueMember = "Processor";
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            ProcessorDTO proc = comboBox1.Items[comboBox1.SelectedIndex] as ProcessorDTO;
            txtID.Text = proc.UniqueId;
            txtName.Text = proc.Name;
            txtDesc.Text = proc.Description;
            txtFileType.Text = proc.InstrumentFileType;
            txtPath.Text = proc.Path;

        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            ProcessorManager procMgr = new ProcessorManager();
            string output = @"E:\lims\LIMSDesktop\bin\Debug\netcoreapp3.0\Processors\Output\file.csv";
            DataTableResponseMessage dtRespMsg = procMgr.ExecuteProcessor(txtPath.Text, txtID.Text, txtInput.Text, output);

            templateDataGridView.DataSource = dtRespMsg.TemplateData;
        }
    }
}
