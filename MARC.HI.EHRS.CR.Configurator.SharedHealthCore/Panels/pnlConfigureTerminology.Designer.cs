namespace MARC.HI.EHRS.CR.Configurator.SharedHealthCore.Panels
{
    partial class pnlConfigureTerminology
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
            this.label1 = new System.Windows.Forms.Label();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.txtUserName = new System.Windows.Forms.TextBox();
            this.txtDatabaseAddress = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.cbxDatabase = new System.Windows.Forms.ComboBox();
            this.label8 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.cbxProviderType = new System.Windows.Forms.ComboBox();
            this.chkEnableDb = new System.Windows.Forms.CheckBox();
            this.label2 = new System.Windows.Forms.Label();
            this.chkEnableCTS = new System.Windows.Forms.CheckBox();
            this.txtCTSUrl = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.numCacheSize = new System.Windows.Forms.NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)(this.numCacheSize)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(408, 20);
            this.label1.TabIndex = 1;
            this.label1.Text = "Local Code Validation";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtPassword
            // 
            this.txtPassword.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtPassword.Enabled = false;
            this.txtPassword.Location = new System.Drawing.Point(107, 173);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.PasswordChar = '*';
            this.txtPassword.Size = new System.Drawing.Size(287, 20);
            this.txtPassword.TabIndex = 24;
            this.txtPassword.Validated += new System.EventHandler(this.connectionParameter_Validated);
            // 
            // txtUserName
            // 
            this.txtUserName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtUserName.Enabled = false;
            this.txtUserName.Location = new System.Drawing.Point(107, 147);
            this.txtUserName.Name = "txtUserName";
            this.txtUserName.Size = new System.Drawing.Size(287, 20);
            this.txtUserName.TabIndex = 23;
            this.txtUserName.Validated += new System.EventHandler(this.connectionParameter_Validated);
            // 
            // txtDatabaseAddress
            // 
            this.txtDatabaseAddress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDatabaseAddress.Enabled = false;
            this.txtDatabaseAddress.Location = new System.Drawing.Point(107, 121);
            this.txtDatabaseAddress.Name = "txtDatabaseAddress";
            this.txtDatabaseAddress.Size = new System.Drawing.Size(287, 20);
            this.txtDatabaseAddress.TabIndex = 21;
            this.txtDatabaseAddress.Validated += new System.EventHandler(this.connectionParameter_Validated);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(22, 124);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(79, 13);
            this.label9.TabIndex = 27;
            this.label9.Text = "Server Address";
            // 
            // cbxDatabase
            // 
            this.cbxDatabase.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbxDatabase.Enabled = false;
            this.cbxDatabase.FormattingEnabled = true;
            this.cbxDatabase.Location = new System.Drawing.Point(107, 199);
            this.cbxDatabase.Name = "cbxDatabase";
            this.cbxDatabase.Size = new System.Drawing.Size(287, 21);
            this.cbxDatabase.TabIndex = 26;
            this.cbxDatabase.DropDown += new System.EventHandler(this.cbxDatabase_DropDown);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(48, 176);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(53, 13);
            this.label8.TabIndex = 25;
            this.label8.Text = "Password";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(41, 150);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(60, 13);
            this.label7.TabIndex = 22;
            this.label7.Text = "User Name";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(17, 202);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(84, 13);
            this.label6.TabIndex = 20;
            this.label6.Text = "Database Name";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(3, 97);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(98, 13);
            this.label5.TabIndex = 18;
            this.label5.Text = "Database Software";
            // 
            // cbxProviderType
            // 
            this.cbxProviderType.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbxProviderType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxProviderType.Enabled = false;
            this.cbxProviderType.FormattingEnabled = true;
            this.cbxProviderType.Location = new System.Drawing.Point(107, 94);
            this.cbxProviderType.Name = "cbxProviderType";
            this.cbxProviderType.Size = new System.Drawing.Size(287, 21);
            this.cbxProviderType.TabIndex = 19;
            this.cbxProviderType.Validated += new System.EventHandler(this.connectionParameter_Validated);
            // 
            // chkEnableDb
            // 
            this.chkEnableDb.AutoSize = true;
            this.chkEnableDb.Location = new System.Drawing.Point(10, 71);
            this.chkEnableDb.Name = "chkEnableDb";
            this.chkEnableDb.Size = new System.Drawing.Size(159, 17);
            this.chkEnableDb.TabIndex = 28;
            this.chkEnableDb.Text = "Enable local code validation";
            this.chkEnableDb.UseVisualStyleBackColor = true;
            this.chkEnableDb.CheckedChanged += new System.EventHandler(this.chkEnableDb_CheckedChanged);
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.label2.Location = new System.Drawing.Point(0, 227);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(408, 21);
            this.label2.TabIndex = 29;
            this.label2.Text = "Remote Code Validation";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // chkEnableCTS
            // 
            this.chkEnableCTS.AutoSize = true;
            this.chkEnableCTS.Location = new System.Drawing.Point(10, 251);
            this.chkEnableCTS.Name = "chkEnableCTS";
            this.chkEnableCTS.Size = new System.Drawing.Size(259, 17);
            this.chkEnableCTS.TabIndex = 30;
            this.chkEnableCTS.Text = "Enable centralized code validation (HL7 CTS 1.2)";
            this.chkEnableCTS.UseVisualStyleBackColor = true;
            this.chkEnableCTS.CheckedChanged += new System.EventHandler(this.chkEnableCTS_CheckedChanged);
            // 
            // txtCTSUrl
            // 
            this.txtCTSUrl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtCTSUrl.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.txtCTSUrl.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.AllUrl;
            this.txtCTSUrl.Enabled = false;
            this.txtCTSUrl.Location = new System.Drawing.Point(107, 274);
            this.txtCTSUrl.Name = "txtCTSUrl";
            this.txtCTSUrl.Size = new System.Drawing.Size(287, 20);
            this.txtCTSUrl.TabIndex = 31;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(48, 277);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(59, 13);
            this.label3.TabIndex = 32;
            this.label3.Text = "MRT URL:";
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label4.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.label4.Location = new System.Drawing.Point(0, 47);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(408, 21);
            this.label4.TabIndex = 33;
            this.label4.Text = "Local Code Validation";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(17, 25);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(138, 13);
            this.label10.TabIndex = 34;
            this.label10.Text = "Maximum Memory Pool Size";
            // 
            // numCacheSize
            // 
            this.numCacheSize.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.numCacheSize.Increment = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numCacheSize.Location = new System.Drawing.Point(278, 23);
            this.numCacheSize.Maximum = new decimal(new int[] {
            50000,
            0,
            0,
            0});
            this.numCacheSize.Name = "numCacheSize";
            this.numCacheSize.Size = new System.Drawing.Size(116, 20);
            this.numCacheSize.TabIndex = 35;
            this.numCacheSize.ThousandsSeparator = true;
            this.numCacheSize.Value = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            // 
            // pnlConfigureTerminology
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.numCacheSize);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.txtCTSUrl);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.chkEnableCTS);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.chkEnableDb);
            this.Controls.Add(this.txtPassword);
            this.Controls.Add(this.txtUserName);
            this.Controls.Add(this.txtDatabaseAddress);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.cbxDatabase);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.cbxProviderType);
            this.Controls.Add(this.label1);
            this.Name = "pnlConfigureTerminology";
            this.Size = new System.Drawing.Size(408, 335);
            ((System.ComponentModel.ISupportInitialize)(this.numCacheSize)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.TextBox txtUserName;
        private System.Windows.Forms.TextBox txtDatabaseAddress;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.ComboBox cbxDatabase;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox cbxProviderType;
        private System.Windows.Forms.CheckBox chkEnableDb;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox chkEnableCTS;
        private System.Windows.Forms.TextBox txtCTSUrl;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.NumericUpDown numCacheSize;
    }
}
