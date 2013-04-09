namespace MARC.HI.EHRS.CR.Messaging.Admin.Configuration.UI
{
    partial class pnlAdminIface
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(pnlAdminIface));
            this.pnlConfiguration = new System.Windows.Forms.Panel();
            this.txtAddress = new System.Windows.Forms.TextBox();
            this.grpSSL = new System.Windows.Forms.GroupBox();
            this.btnChooseCert = new System.Windows.Forms.Button();
            this.txtCertificate = new System.Windows.Forms.TextBox();
            this.cbxStoreLocation = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.cbxStore = new System.Windows.Forms.ComboBox();
            this.chkPKI = new System.Windows.Forms.CheckBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.chkMetaData = new System.Windows.Forms.CheckBox();
            this.chkDebug = new System.Windows.Forms.CheckBox();
            this.lblTitle = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.pnlConfiguration.SuspendLayout();
            this.grpSSL.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlConfiguration
            // 
            this.pnlConfiguration.Controls.Add(this.txtAddress);
            this.pnlConfiguration.Controls.Add(this.grpSSL);
            this.pnlConfiguration.Controls.Add(this.label1);
            this.pnlConfiguration.Controls.Add(this.chkMetaData);
            this.pnlConfiguration.Controls.Add(this.chkDebug);
            this.pnlConfiguration.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlConfiguration.Location = new System.Drawing.Point(0, 63);
            this.pnlConfiguration.Name = "pnlConfiguration";
            this.pnlConfiguration.Size = new System.Drawing.Size(329, 214);
            this.pnlConfiguration.TabIndex = 12;
            // 
            // txtAddress
            // 
            this.txtAddress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtAddress.Location = new System.Drawing.Point(60, 6);
            this.txtAddress.Name = "txtAddress";
            this.txtAddress.Size = new System.Drawing.Size(257, 20);
            this.txtAddress.TabIndex = 2;
            this.txtAddress.Text = "http://127.0.0.1:9999/admin";
            this.txtAddress.Validated += new System.EventHandler(this.txtAddress_Validated);
            // 
            // grpSSL
            // 
            this.grpSSL.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpSSL.Controls.Add(this.btnChooseCert);
            this.grpSSL.Controls.Add(this.txtCertificate);
            this.grpSSL.Controls.Add(this.cbxStoreLocation);
            this.grpSSL.Controls.Add(this.label4);
            this.grpSSL.Controls.Add(this.cbxStore);
            this.grpSSL.Controls.Add(this.chkPKI);
            this.grpSSL.Controls.Add(this.label3);
            this.grpSSL.Controls.Add(this.label2);
            this.grpSSL.Enabled = false;
            this.grpSSL.Location = new System.Drawing.Point(9, 78);
            this.grpSSL.Name = "grpSSL";
            this.grpSSL.Size = new System.Drawing.Size(308, 133);
            this.grpSSL.TabIndex = 6;
            this.grpSSL.TabStop = false;
            this.grpSSL.Text = "Security Configuration";
            // 
            // btnChooseCert
            // 
            this.btnChooseCert.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnChooseCert.Location = new System.Drawing.Point(269, 80);
            this.btnChooseCert.Name = "btnChooseCert";
            this.btnChooseCert.Size = new System.Drawing.Size(33, 20);
            this.btnChooseCert.TabIndex = 9;
            this.btnChooseCert.Text = "...";
            this.btnChooseCert.UseVisualStyleBackColor = true;
            this.btnChooseCert.Click += new System.EventHandler(this.btnChooseCert_Click);
            // 
            // txtCertificate
            // 
            this.txtCertificate.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtCertificate.Location = new System.Drawing.Point(97, 80);
            this.txtCertificate.Name = "txtCertificate";
            this.txtCertificate.ReadOnly = true;
            this.txtCertificate.Size = new System.Drawing.Size(166, 20);
            this.txtCertificate.TabIndex = 8;
            // 
            // cbxStoreLocation
            // 
            this.cbxStoreLocation.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbxStoreLocation.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxStoreLocation.FormattingEnabled = true;
            this.cbxStoreLocation.Location = new System.Drawing.Point(97, 26);
            this.cbxStoreLocation.Name = "cbxStoreLocation";
            this.cbxStoreLocation.Size = new System.Drawing.Size(205, 21);
            this.cbxStoreLocation.TabIndex = 6;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 29);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(79, 13);
            this.label4.TabIndex = 5;
            this.label4.Text = "Store Location:";
            // 
            // cbxStore
            // 
            this.cbxStore.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbxStore.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxStore.FormattingEnabled = true;
            this.cbxStore.Location = new System.Drawing.Point(97, 53);
            this.cbxStore.Name = "cbxStore";
            this.cbxStore.Size = new System.Drawing.Size(205, 21);
            this.cbxStore.TabIndex = 7;
            // 
            // chkPKI
            // 
            this.chkPKI.AutoSize = true;
            this.chkPKI.Location = new System.Drawing.Point(37, 110);
            this.chkPKI.Name = "chkPKI";
            this.chkPKI.Size = new System.Drawing.Size(193, 17);
            this.chkPKI.TabIndex = 10;
            this.chkPKI.Text = "Require clients to have a certificate";
            this.chkPKI.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(34, 83);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(57, 13);
            this.label3.TabIndex = 1;
            this.label3.Text = "Certificate:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 56);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(85, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "Certificate Store:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(48, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "Address:";
            // 
            // chkMetaData
            // 
            this.chkMetaData.AutoSize = true;
            this.chkMetaData.Location = new System.Drawing.Point(60, 55);
            this.chkMetaData.Name = "chkMetaData";
            this.chkMetaData.Size = new System.Drawing.Size(237, 17);
            this.chkMetaData.TabIndex = 5;
            this.chkMetaData.Text = "Enable meta-data publishing on this endpoint";
            this.chkMetaData.UseVisualStyleBackColor = true;
            // 
            // chkDebug
            // 
            this.chkDebug.AutoSize = true;
            this.chkDebug.Location = new System.Drawing.Point(60, 32);
            this.chkDebug.Name = "chkDebug";
            this.chkDebug.Size = new System.Drawing.Size(190, 17);
            this.chkDebug.TabIndex = 4;
            this.chkDebug.Text = "Enable debugging on this endpoint";
            this.chkDebug.UseVisualStyleBackColor = true;
            // 
            // lblTitle
            // 
            this.lblTitle.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTitle.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.lblTitle.Location = new System.Drawing.Point(0, 0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(329, 20);
            this.lblTitle.TabIndex = 11;
            this.lblTitle.Text = "Administration Interface Endpoint";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label5
            // 
            this.label5.Dock = System.Windows.Forms.DockStyle.Top;
            this.label5.Location = new System.Drawing.Point(0, 20);
            this.label5.Name = "label5";
            this.label5.Padding = new System.Windows.Forms.Padding(2);
            this.label5.Size = new System.Drawing.Size(329, 43);
            this.label5.TabIndex = 13;
            this.label5.Text = resources.GetString("label5.Text");
            // 
            // pnlAdminIface
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.pnlConfiguration);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.lblTitle);
            this.Name = "pnlAdminIface";
            this.Size = new System.Drawing.Size(329, 288);
            this.pnlConfiguration.ResumeLayout(false);
            this.pnlConfiguration.PerformLayout();
            this.grpSSL.ResumeLayout(false);
            this.grpSSL.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlConfiguration;
        private System.Windows.Forms.TextBox txtAddress;
        private System.Windows.Forms.GroupBox grpSSL;
        private System.Windows.Forms.Button btnChooseCert;
        private System.Windows.Forms.TextBox txtCertificate;
        private System.Windows.Forms.ComboBox cbxStoreLocation;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox cbxStore;
        private System.Windows.Forms.CheckBox chkPKI;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox chkMetaData;
        private System.Windows.Forms.CheckBox chkDebug;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label label5;
    }
}
