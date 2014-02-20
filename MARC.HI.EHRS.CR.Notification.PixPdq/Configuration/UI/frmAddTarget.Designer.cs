namespace MARC.HI.EHRS.CR.Notification.PixPdq.Configuration.UI
{
    partial class frmAddTarget
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
            this.components = new System.ComponentModel.Container();
            this.txtAddress = new System.Windows.Forms.TextBox();
            this.grpSSL = new System.Windows.Forms.GroupBox();
            this.btnChooseCert = new System.Windows.Forms.Button();
            this.txtCertificate = new System.Windows.Forms.TextBox();
            this.cbxStoreLocation = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.cbxStore = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.chkSendClient = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.txtName = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.cbxActor = new System.Windows.Forms.ComboBox();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.chkValidateIssuer = new System.Windows.Forms.CheckBox();
            this.label7 = new System.Windows.Forms.Label();
            this.txtOid = new System.Windows.Forms.TextBox();
            this.errMain = new System.Windows.Forms.ErrorProvider(this.components);
            this.cbxAuthority = new System.Windows.Forms.ComboBox();
            this.label8 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnChooseServerCert = new System.Windows.Forms.Button();
            this.txtServerCert = new System.Windows.Forms.TextBox();
            this.cbxServerStoreLocation = new System.Windows.Forms.ComboBox();
            this.label10 = new System.Windows.Forms.Label();
            this.cbxServerStore = new System.Windows.Forms.ComboBox();
            this.label11 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.grpSSL.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errMain)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtAddress
            // 
            this.txtAddress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtAddress.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.txtAddress.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.HistoryList;
            this.txtAddress.Location = new System.Drawing.Point(60, 32);
            this.txtAddress.Name = "txtAddress";
            this.txtAddress.Size = new System.Drawing.Size(283, 20);
            this.txtAddress.TabIndex = 1;
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
            this.grpSSL.Controls.Add(this.label3);
            this.grpSSL.Controls.Add(this.label2);
            this.grpSSL.Enabled = false;
            this.grpSSL.Location = new System.Drawing.Point(19, 161);
            this.grpSSL.Name = "grpSSL";
            this.grpSSL.Size = new System.Drawing.Size(324, 114);
            this.grpSSL.TabIndex = 9;
            this.grpSSL.TabStop = false;
            this.grpSSL.Text = "Client Credentials";
            // 
            // btnChooseCert
            // 
            this.btnChooseCert.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnChooseCert.Location = new System.Drawing.Point(285, 80);
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
            this.txtCertificate.Size = new System.Drawing.Size(182, 20);
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
            this.cbxStoreLocation.Size = new System.Drawing.Size(221, 21);
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
            this.cbxStore.Size = new System.Drawing.Size(221, 21);
            this.cbxStore.TabIndex = 7;
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
            // chkSendClient
            // 
            this.chkSendClient.AutoSize = true;
            this.chkSendClient.Location = new System.Drawing.Point(19, 138);
            this.chkSendClient.Name = "chkSendClient";
            this.chkSendClient.Size = new System.Drawing.Size(200, 17);
            this.chkSendClient.TabIndex = 5;
            this.chkSendClient.Text = "Service requires client authentication";
            this.chkSendClient.UseVisualStyleBackColor = true;
            this.chkSendClient.CheckedChanged += new System.EventHandler(this.chkSendClient_CheckedChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 35);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(48, 13);
            this.label1.TabIndex = 8;
            this.label1.Text = "Address:";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(16, 9);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(38, 13);
            this.label5.TabIndex = 10;
            this.label5.Text = "Name:";
            // 
            // txtName
            // 
            this.txtName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtName.Location = new System.Drawing.Point(60, 6);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(283, 20);
            this.txtName.TabIndex = 0;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(6, 114);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(50, 13);
            this.label6.TabIndex = 12;
            this.label6.Text = "Send As:";
            // 
            // cbxActor
            // 
            this.cbxActor.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbxActor.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxActor.FormattingEnabled = true;
            this.cbxActor.Location = new System.Drawing.Point(60, 111);
            this.cbxActor.Name = "cbxActor";
            this.cbxActor.Size = new System.Drawing.Size(283, 21);
            this.cbxActor.TabIndex = 4;
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(268, 429);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 17;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.Location = new System.Drawing.Point(187, 429);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 16;
            this.btnOk.Text = "Ok";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // chkValidateIssuer
            // 
            this.chkValidateIssuer.AutoSize = true;
            this.chkValidateIssuer.Location = new System.Drawing.Point(18, 281);
            this.chkValidateIssuer.Name = "chkValidateIssuer";
            this.chkValidateIssuer.Size = new System.Drawing.Size(259, 17);
            this.chkValidateIssuer.TabIndex = 10;
            this.chkValidateIssuer.Text = "Validate service\'s certificate against trusted issuer";
            this.chkValidateIssuer.UseVisualStyleBackColor = true;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(4, 61);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(50, 13);
            this.label7.TabIndex = 19;
            this.label7.Text = "Node ID:";
            // 
            // txtOid
            // 
            this.txtOid.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtOid.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.txtOid.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
            this.txtOid.Location = new System.Drawing.Point(60, 58);
            this.txtOid.Name = "txtOid";
            this.txtOid.Size = new System.Drawing.Size(283, 20);
            this.txtOid.TabIndex = 2;
            // 
            // errMain
            // 
            this.errMain.ContainerControl = this;
            // 
            // cbxAuthority
            // 
            this.cbxAuthority.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbxAuthority.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxAuthority.FormattingEnabled = true;
            this.cbxAuthority.Location = new System.Drawing.Point(60, 84);
            this.cbxAuthority.Name = "cbxAuthority";
            this.cbxAuthority.Size = new System.Drawing.Size(283, 21);
            this.cbxAuthority.TabIndex = 3;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(5, 87);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(51, 13);
            this.label8.TabIndex = 21;
            this.label8.Text = "Authority:";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.panel1);
            this.groupBox1.Location = new System.Drawing.Point(19, 304);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(324, 115);
            this.groupBox1.TabIndex = 22;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Trusted Issuer for Server";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btnChooseServerCert);
            this.panel1.Controls.Add(this.txtServerCert);
            this.panel1.Controls.Add(this.cbxServerStoreLocation);
            this.panel1.Controls.Add(this.label10);
            this.panel1.Controls.Add(this.cbxServerStore);
            this.panel1.Controls.Add(this.label11);
            this.panel1.Controls.Add(this.label12);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(3, 17);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(318, 95);
            this.panel1.TabIndex = 13;
            // 
            // btnChooseServerCert
            // 
            this.btnChooseServerCert.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnChooseServerCert.Location = new System.Drawing.Point(272, 66);
            this.btnChooseServerCert.Name = "btnChooseServerCert";
            this.btnChooseServerCert.Size = new System.Drawing.Size(33, 20);
            this.btnChooseServerCert.TabIndex = 16;
            this.btnChooseServerCert.Text = "...";
            this.btnChooseServerCert.UseVisualStyleBackColor = true;
            // 
            // txtServerCert
            // 
            this.txtServerCert.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtServerCert.Location = new System.Drawing.Point(98, 66);
            this.txtServerCert.Name = "txtServerCert";
            this.txtServerCert.ReadOnly = true;
            this.txtServerCert.Size = new System.Drawing.Size(168, 20);
            this.txtServerCert.TabIndex = 15;
            // 
            // cbxServerStoreLocation
            // 
            this.cbxServerStoreLocation.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbxServerStoreLocation.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxServerStoreLocation.FormattingEnabled = true;
            this.cbxServerStoreLocation.Location = new System.Drawing.Point(98, 12);
            this.cbxServerStoreLocation.Name = "cbxServerStoreLocation";
            this.cbxServerStoreLocation.Size = new System.Drawing.Size(207, 21);
            this.cbxServerStoreLocation.TabIndex = 13;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(13, 15);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(79, 13);
            this.label10.TabIndex = 12;
            this.label10.Text = "Store Location:";
            // 
            // cbxServerStore
            // 
            this.cbxServerStore.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbxServerStore.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxServerStore.FormattingEnabled = true;
            this.cbxServerStore.Location = new System.Drawing.Point(98, 39);
            this.cbxServerStore.Name = "cbxServerStore";
            this.cbxServerStore.Size = new System.Drawing.Size(207, 21);
            this.cbxServerStore.TabIndex = 14;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(35, 69);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(57, 13);
            this.label11.TabIndex = 11;
            this.label11.Text = "Certificate:";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(7, 42);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(85, 13);
            this.label12.TabIndex = 10;
            this.label12.Text = "Certificate Store:";
            // 
            // frmAddTarget
            // 
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(361, 458);
            this.ControlBox = false;
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.cbxAuthority);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.txtOid);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.chkValidateIssuer);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.cbxActor);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.txtName);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.txtAddress);
            this.Controls.Add(this.chkSendClient);
            this.Controls.Add(this.grpSSL);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "frmAddTarget";
            this.Text = "Add PIXv3 Notification Target";
            this.grpSSL.ResumeLayout(false);
            this.grpSSL.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errMain)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtAddress;
        private System.Windows.Forms.GroupBox grpSSL;
        private System.Windows.Forms.Button btnChooseCert;
        private System.Windows.Forms.TextBox txtCertificate;
        private System.Windows.Forms.ComboBox cbxStoreLocation;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox cbxStore;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox chkSendClient;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox cbxActor;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.CheckBox chkValidateIssuer;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox txtOid;
        private System.Windows.Forms.ErrorProvider errMain;
        private System.Windows.Forms.ComboBox cbxAuthority;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btnChooseServerCert;
        private System.Windows.Forms.TextBox txtServerCert;
        private System.Windows.Forms.ComboBox cbxServerStoreLocation;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.ComboBox cbxServerStore;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label12;
    }
}