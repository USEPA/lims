namespace LIMSDesktop
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            comboBox1 = new System.Windows.Forms.ComboBox();
            label1 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            txtInput = new System.Windows.Forms.TextBox();
            btnSelectFile = new System.Windows.Forms.Button();
            lblID = new System.Windows.Forms.Label();
            lblName = new System.Windows.Forms.Label();
            lblDesc = new System.Windows.Forms.Label();
            lblFileType = new System.Windows.Forms.Label();
            templateDataGridView = new System.Windows.Forms.DataGridView();
            txtID = new System.Windows.Forms.TextBox();
            txtName = new System.Windows.Forms.TextBox();
            txtDesc = new System.Windows.Forms.TextBox();
            txtFileType = new System.Windows.Forms.TextBox();
            btnRun = new System.Windows.Forms.Button();
            label3 = new System.Windows.Forms.Label();
            txtPath = new System.Windows.Forms.TextBox();
            btnSave = new System.Windows.Forms.Button();
            lblMessage = new System.Windows.Forms.Label();
            txtMessage = new System.Windows.Forms.TextBox();
            btnClearMsg = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)templateDataGridView).BeginInit();
            SuspendLayout();
            // 
            // comboBox1
            // 
            comboBox1.FormattingEnabled = true;
            comboBox1.Location = new System.Drawing.Point(127, 14);
            comboBox1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new System.Drawing.Size(243, 23);
            comboBox1.TabIndex = 0;
            comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(48, 17);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(58, 15);
            label1.TabIndex = 1;
            label1.Text = "Processor";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(48, 42);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(56, 15);
            label2.TabIndex = 2;
            label2.Text = "Input File";
            // 
            // txtInput
            // 
            txtInput.Location = new System.Drawing.Point(127, 42);
            txtInput.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            txtInput.Name = "txtInput";
            txtInput.Size = new System.Drawing.Size(157, 23);
            txtInput.TabIndex = 3;
            // 
            // btnSelectFile
            // 
            btnSelectFile.Location = new System.Drawing.Point(290, 44);
            btnSelectFile.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            btnSelectFile.Name = "btnSelectFile";
            btnSelectFile.Size = new System.Drawing.Size(31, 19);
            btnSelectFile.TabIndex = 4;
            btnSelectFile.Text = "...";
            btnSelectFile.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            btnSelectFile.UseVisualStyleBackColor = true;
            btnSelectFile.Click += btnSelectFile_Click;
            // 
            // lblID
            // 
            lblID.AutoSize = true;
            lblID.Location = new System.Drawing.Point(442, 12);
            lblID.Name = "lblID";
            lblID.Size = new System.Drawing.Size(21, 15);
            lblID.TabIndex = 5;
            lblID.Text = "ID:";
            // 
            // lblName
            // 
            lblName.AutoSize = true;
            lblName.Location = new System.Drawing.Point(421, 33);
            lblName.Name = "lblName";
            lblName.Size = new System.Drawing.Size(42, 15);
            lblName.TabIndex = 5;
            lblName.Text = "Name:";
            // 
            // lblDesc
            // 
            lblDesc.AutoSize = true;
            lblDesc.Location = new System.Drawing.Point(389, 55);
            lblDesc.Name = "lblDesc";
            lblDesc.Size = new System.Drawing.Size(70, 15);
            lblDesc.TabIndex = 6;
            lblDesc.Text = "Description:";
            // 
            // lblFileType
            // 
            lblFileType.AutoSize = true;
            lblFileType.Location = new System.Drawing.Point(402, 77);
            lblFileType.Name = "lblFileType";
            lblFileType.Size = new System.Drawing.Size(55, 15);
            lblFileType.TabIndex = 7;
            lblFileType.Text = "File Type:";
            // 
            // templateDataGridView
            // 
            templateDataGridView.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            templateDataGridView.ColumnHeadersHeight = 29;
            templateDataGridView.Location = new System.Drawing.Point(18, 135);
            templateDataGridView.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            templateDataGridView.Name = "templateDataGridView";
            templateDataGridView.RowHeadersWidth = 51;
            templateDataGridView.Size = new System.Drawing.Size(875, 275);
            templateDataGridView.TabIndex = 8;
            templateDataGridView.DataBindingComplete += templateDataGridView_DataBindingComplete;
            // 
            // txtID
            // 
            txtID.Enabled = false;
            txtID.Location = new System.Drawing.Point(469, 10);
            txtID.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            txtID.Name = "txtID";
            txtID.Size = new System.Drawing.Size(210, 23);
            txtID.TabIndex = 9;
            // 
            // txtName
            // 
            txtName.Enabled = false;
            txtName.Location = new System.Drawing.Point(470, 32);
            txtName.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            txtName.Name = "txtName";
            txtName.Size = new System.Drawing.Size(210, 23);
            txtName.TabIndex = 10;
            // 
            // txtDesc
            // 
            txtDesc.Enabled = false;
            txtDesc.Location = new System.Drawing.Point(470, 54);
            txtDesc.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            txtDesc.Name = "txtDesc";
            txtDesc.Size = new System.Drawing.Size(423, 23);
            txtDesc.TabIndex = 11;
            // 
            // txtFileType
            // 
            txtFileType.Enabled = false;
            txtFileType.Location = new System.Drawing.Point(469, 76);
            txtFileType.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            txtFileType.Name = "txtFileType";
            txtFileType.Size = new System.Drawing.Size(210, 23);
            txtFileType.TabIndex = 12;
            // 
            // btnRun
            // 
            btnRun.Location = new System.Drawing.Point(127, 69);
            btnRun.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            btnRun.Name = "btnRun";
            btnRun.Size = new System.Drawing.Size(83, 22);
            btnRun.TabIndex = 13;
            btnRun.Text = "Run";
            btnRun.UseVisualStyleBackColor = true;
            btnRun.Click += btnRun_Click;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(423, 101);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(34, 15);
            label3.TabIndex = 14;
            label3.Text = "Path:";
            // 
            // txtPath
            // 
            txtPath.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            txtPath.Enabled = false;
            txtPath.Location = new System.Drawing.Point(470, 100);
            txtPath.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            txtPath.Name = "txtPath";
            txtPath.Size = new System.Drawing.Size(423, 23);
            txtPath.TabIndex = 15;
            // 
            // btnSave
            // 
            btnSave.Location = new System.Drawing.Point(127, 107);
            btnSave.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            btnSave.Name = "btnSave";
            btnSave.Size = new System.Drawing.Size(83, 22);
            btnSave.TabIndex = 16;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            // 
            // lblMessage
            // 
            lblMessage.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            lblMessage.AutoSize = true;
            lblMessage.Location = new System.Drawing.Point(17, 430);
            lblMessage.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            lblMessage.Name = "lblMessage";
            lblMessage.Size = new System.Drawing.Size(58, 15);
            lblMessage.TabIndex = 17;
            lblMessage.Text = "Messages";
            // 
            // txtMessage
            // 
            txtMessage.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            txtMessage.Location = new System.Drawing.Point(18, 448);
            txtMessage.Multiline = true;
            txtMessage.Name = "txtMessage";
            txtMessage.ReadOnly = true;
            txtMessage.Size = new System.Drawing.Size(876, 75);
            txtMessage.TabIndex = 18;
            txtMessage.WordWrap = false;
            // 
            // btnClearMsg
            // 
            btnClearMsg.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            btnClearMsg.Location = new System.Drawing.Point(389, 529);
            btnClearMsg.Name = "btnClearMsg";
            btnClearMsg.Size = new System.Drawing.Size(110, 23);
            btnClearMsg.TabIndex = 19;
            btnClearMsg.Text = "Clear Messages";
            btnClearMsg.UseVisualStyleBackColor = true;
            btnClearMsg.Click += btnClearMsg_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(906, 552);
            Controls.Add(btnClearMsg);
            Controls.Add(txtMessage);
            Controls.Add(lblMessage);
            Controls.Add(btnSave);
            Controls.Add(txtPath);
            Controls.Add(label3);
            Controls.Add(btnRun);
            Controls.Add(txtFileType);
            Controls.Add(txtDesc);
            Controls.Add(txtName);
            Controls.Add(txtID);
            Controls.Add(lblFileType);
            Controls.Add(lblDesc);
            Controls.Add(lblID);
            Controls.Add(btnSelectFile);
            Controls.Add(txtInput);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(comboBox1);
            Controls.Add(lblName);
            Controls.Add(templateDataGridView);
            Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            Name = "Form1";
            Text = "LIMS";
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)templateDataGridView).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtInput;
        private System.Windows.Forms.Button btnSelectFile;
        private System.Windows.Forms.Label lblID;
        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.Label lblDesc;
        private System.Windows.Forms.Label lblFileType;
        private System.Windows.Forms.DataGridView templateDataGridView;
        private System.Windows.Forms.TextBox txtID;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.TextBox txtDesc;
        private System.Windows.Forms.TextBox txtFileType;
        private System.Windows.Forms.Button btnRun;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtPath;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Label lblMessage;
        private System.Windows.Forms.TextBox txtMessage;
        private System.Windows.Forms.Button btnClearMsg;
    }
}