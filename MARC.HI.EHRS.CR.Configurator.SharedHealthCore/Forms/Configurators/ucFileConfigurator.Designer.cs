namespace MARC.HI.EHRS.SVC.Config.Messaging.Forms.Configurators
{
    partial class ucFileConfigurator
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lbllsnName = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.chkKeepFiles = new System.Windows.Forms.CheckBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtFilter = new System.Windows.Forms.TextBox();
            this.txtDirectory = new System.Windows.Forms.TextBox();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.chkProcess = new System.Windows.Forms.CheckBox();
            this.fldBrowser = new System.Windows.Forms.FolderBrowserDialog();
            this.SuspendLayout();
            // 
            // lbllsnName
            // 
            this.lbllsnName.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.lbllsnName.Dock = System.Windows.Forms.DockStyle.Top;
            this.lbllsnName.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbllsnName.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.lbllsnName.Location = new System.Drawing.Point(0, 0);
            this.lbllsnName.Name = "lbllsnName";
            this.lbllsnName.Size = new System.Drawing.Size(329, 20);
            this.lbllsnName.TabIndex = 12;
            this.lbllsnName.Text = "File Listener Configuration";
            this.lbllsnName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 37);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(49, 13);
            this.label1.TabIndex = 13;
            this.label1.Text = "Directory";
            // 
            // chkKeepFiles
            // 
            this.chkKeepFiles.AutoSize = true;
            this.chkKeepFiles.Location = new System.Drawing.Point(16, 90);
            this.chkKeepFiles.Name = "chkKeepFiles";
            this.chkKeepFiles.Size = new System.Drawing.Size(150, 17);
            this.chkKeepFiles.TabIndex = 4;
            this.chkKeepFiles.Text = "Keep files after processing";
            this.chkKeepFiles.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(16, 67);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(29, 13);
            this.label2.TabIndex = 15;
            this.label2.Text = "Filter";
            // 
            // txtFilter
            // 
            this.txtFilter.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.txtFilter.Location = new System.Drawing.Point(96, 64);
            this.txtFilter.Name = "txtFilter";
            this.txtFilter.Size = new System.Drawing.Size(218, 20);
            this.txtFilter.TabIndex = 3;
            // 
            // txtDirectory
            // 
            this.txtDirectory.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDirectory.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.txtDirectory.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystem;
            this.txtDirectory.Location = new System.Drawing.Point(96, 34);
            this.txtDirectory.Name = "txtDirectory";
            this.txtDirectory.Size = new System.Drawing.Size(182, 20);
            this.txtDirectory.TabIndex = 1;
            // 
            // btnBrowse
            // 
            this.btnBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowse.Location = new System.Drawing.Point(284, 34);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(30, 20);
            this.btnBrowse.TabIndex = 2;
            this.btnBrowse.Text = "...";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // chkProcess
            // 
            this.chkProcess.AutoSize = true;
            this.chkProcess.Location = new System.Drawing.Point(16, 113);
            this.chkProcess.Name = "chkProcess";
            this.chkProcess.Size = new System.Drawing.Size(193, 17);
            this.chkProcess.TabIndex = 5;
            this.chkProcess.Text = "Process any existing files on startup";
            this.chkProcess.UseVisualStyleBackColor = true;
            // 
            // ucFileConfigurator
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.chkProcess);
            this.Controls.Add(this.btnBrowse);
            this.Controls.Add(this.txtDirectory);
            this.Controls.Add(this.txtFilter);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.chkKeepFiles);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lbllsnName);
            this.Name = "ucFileConfigurator";
            this.Size = new System.Drawing.Size(329, 135);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lbllsnName;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox chkKeepFiles;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtFilter;
        private System.Windows.Forms.TextBox txtDirectory;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.CheckBox chkProcess;
        private System.Windows.Forms.FolderBrowserDialog fldBrowser;
    }
}
