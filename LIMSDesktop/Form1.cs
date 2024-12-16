using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Reflection;
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
            templateDataGridView.DataBindingComplete += templateDataGridView_DataBindingComplete;
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
            {
                row.HeaderCell.Value = (row.Index + 1).ToString();
            }
            templateDataGridView.AutoResizeRowHeadersWidth(DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders);
            //foreach (DataGridViewRow row in templateDataGridView.Rows)
            //    row.HeaderCell.Value = String.Format("{0}", row.Index + 1);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ProcessorManager procMgr = new ProcessorManager();
            string basePath = Assembly.GetExecutingAssembly().Location;
            string baseFolder = Path.GetDirectoryName(basePath);
            string procPaths = Path.Combine(baseFolder, "app_files\\processors");
            var lstProc = procMgr.GetProcessors(procPaths);
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
            DataTableResponseMessage dtRespMsg = null;
            try
            {
                templateDataGridView.DataSource = null;
                ClearMessage();
                UserMessage("Running");
                ProcessorDTO proc = comboBox1.Items[comboBox1.SelectedIndex] as ProcessorDTO;
                txtID.Text = proc.UniqueId;
                txtName.Text = proc.Name;
                txtDesc.Text = proc.Description;
                txtFileType.Text = proc.InstrumentFileType;

                ProcessorManager procMgr = new ProcessorManager();
                //string output = @"E:\lims\LIMSDesktop\bin\Debug\netcoreapp3.0\Processors\Output\file.csv";
                //string procPaths = @"E:\lims\lims_server\app_files\processors";
                dtRespMsg = procMgr.ExecuteProcessor(proc.Path, txtName.Text, txtInput.Text);
                GC.Collect();
                GC.WaitForPendingFinalizers();

                if (dtRespMsg == null)
                {
                    //dtRespMsg = new DataTableResponseMessage();
                    UserMessage(string.Format("Error processing file {0} with processor {1}", txtInput.Text, txtName.Text));
                    //dtRespMsg.ErrorMessage = string.Format("Error processing file {0} with processor {1}", txtInput.Text, txtName.Text);
                    return;

                }
                if (dtRespMsg.ErrorMessage != null)
                {
                    LogMessage(dtRespMsg.ErrorMessage);
                    //UserMessage(dtRespMsg.ErrorMessage);
                    return;
                }

                foreach (DataRow dr in dtRespMsg.TemplateData.Rows)
                {
                    string aliquot = dr["Aliquot"].ToString();
                    if (aliquot.Contains("@"))
                    {
                        string[] tokens = aliquot.Split("@");
                        dr["Aliquot"] = tokens[0].Trim(); ;

                        double dval = 0.0;
                        if (Double.TryParse(tokens[1].Trim(), out dval))
                            dr["Dilution Factor"] = dval;
                    }
                }

                if (dtRespMsg.TemplateData != null)
                {
                    templateDataGridView.DataSource = dtRespMsg.TemplateData;
                    ClearMessage();
                    UserMessage("Success");
                }
            }
            catch(Exception ex)
            {
                LogMessage(string.Format("Error executing processor {0} with input {1}", txtID.Text, txtInput.Text));
                LogMessage($"Error: {ex.Message}");
                if (dtRespMsg != null && dtRespMsg.LogMessage != null)
                    LogMessage(dtRespMsg.LogMessage);
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            //ClearMessage();
            string dtName = "";
            try
            {
                //SaveFileDialog sfd = new SaveFileDialog();
                FolderBrowserDialog fbd = new FolderBrowserDialog();
                fbd.Description = "Save output file location";
                fbd.ShowNewFolderButton = true;
                string fName = Path.GetFileNameWithoutExtension(txtInput.Text);

                //sfd.FileName = fName;
                //sfd.Filter = @"Excel Files (*.xlsx)|*.xlsx";
                DialogResult dr = fbd.ShowDialog();
                if (dr != DialogResult.OK)
                    return;

                ProcessorManager procMgr = new ProcessorManager();
                DataTable dt = templateDataGridView.DataSource as DataTable;
                if (dt != null)
                    dtName = dt.TableName;

                //string dir = Path.GetDirectoryName(sfd.FileName);

                //string outPath = Path.Combine(sfd.FileName, "output");
                DirectoryInfo di = new DirectoryInfo(fbd.SelectedPath);
                if (!di.Exists)
                    di.Create();

                //procMgr.WriteTemplateOutputFile(fbd.SelectedPath, dt);
                ProcessorManager.WriteTemplateOutputFile(fbd.SelectedPath, dt);
                UserMessage("Success");
            }
            catch(Exception ex)
            {
                LogMessage(string.Format("Error saving output {0}", dtName));
                LogMessage(ex.Message);               
            }
        }

        private void UserMessage(string message)
        {
            //string msg = string.Format("Message: {0}", message);
            //lblMessage.Text = msg;
            if (string.IsNullOrWhiteSpace(txtMessage.Text))
                txtMessage.Text = message;
            else            
                txtMessage.Text = txtMessage.Text + Environment.NewLine + message;            
        }

        private void ClearMessage()
        {
            txtMessage.Text = "";
        }

        private void LogMessage(string message)
        {
            string logPath = "";
            UserMessage(message);
            try
            {
                string loc = System.Reflection.Assembly.GetExecutingAssembly().Location;
                FileInfo fi = new FileInfo(loc);
                DirectoryInfo di = fi.Directory;
                logPath = Path.Combine(fi.Directory.FullName, "logs");
                if (!Directory.Exists(logPath))
                    Directory.CreateDirectory(logPath);

                logPath = Path.Combine(logPath, "lims_desktop.log");

                File.AppendAllText(logPath, message);
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error writing to log file. " + ex.Message);
                //UserMessage("Error writing to log file - " + ex.Message);
            }

            return;
        }

        private void btnClearMsg_Click(object sender, EventArgs e)
        {
            ClearMessage();
        }
    }
}
