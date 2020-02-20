using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Text;
using System.Windows.Forms;
using LimsServer.Entities;
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

        private void templateDataGridView_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            foreach (DataGridViewRow row in templateDataGridView.Rows)
                row.HeaderCell.Value = String.Format("{0}", row.Index + 1);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ProcessorManager procMgr = new ProcessorManager();
            string procPaths = @"E:\lims\lims_server\app_files\processors";
            var lstProc = procMgr.GetProcessors(procPaths);
            comboBox1.DataSource = lstProc;
            comboBox1.DisplayMember = "Name";
            //comboBox1.ValueMember = "Processor";
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            Processor proc = comboBox1.Items[comboBox1.SelectedIndex] as Processor;
            txtID.Text = proc.id;
            txtName.Text = proc.name;
            txtDesc.Text = proc.description;
            txtFileType.Text = proc.file_type;
            //txtPath.Text = proc.;

        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            ProcessorManager procMgr = new ProcessorManager();
            string output = @"E:\lims\LIMSDesktop\bin\Debug\netcoreapp3.0\Processors\Output\file.csv";
            DataTableResponseMessage dtRespMsg = procMgr.ExecuteProcessor(txtPath.Text, txtID.Text, txtInput.Text);

            templateDataGridView.DataSource = dtRespMsg.TemplateData;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            ProcessorManager procMgr = new ProcessorManager();
            DataTable dt = templateDataGridView.DataSource as DataTable;
            string outPath = Path.Combine(Path.GetDirectoryName(txtInput.Text), "output");
            procMgr.WriteTemplateOutputFile(outPath, dt);
        }
    }
}
