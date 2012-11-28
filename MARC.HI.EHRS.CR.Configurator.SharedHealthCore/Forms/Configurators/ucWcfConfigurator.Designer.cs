namespace MARC.HI.EHRS.SVC.Config.Messaging.Forms.Configurators
{
    partial class ucWcfConfigurator
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
            this.label2 = new System.Windows.Forms.Label();
            this.cboBinding = new System.Windows.Forms.ComboBox();
            this.txtUri = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.cboSecurity = new System.Windows.Forms.ComboBox();
            this.chkReliable = new System.Windows.Forms.CheckBox();
            this.chkIncludeExceptions = new System.Windows.Forms.CheckBox();
            this.chkPublishMeta = new System.Windows.Forms.CheckBox();
            this.chkEnableHelp = new System.Windows.Forms.CheckBox();
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
            this.lbllsnName.Size = new System.Drawing.Size(373, 20);
            this.lbllsnName.TabIndex = 13;
            this.lbllsnName.Text = "WCF Server Configuration";
            this.lbllsnName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(15, 32);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(60, 13);
            this.label1.TabIndex = 14;
            this.label1.Text = "Listen URI:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(15, 58);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(42, 13);
            this.label2.TabIndex = 15;
            this.label2.Text = "Binding";
            // 
            // cboBinding
            // 
            this.cboBinding.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.cboBinding.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboBinding.FormattingEnabled = true;
            this.cboBinding.Items.AddRange(new object[] {
            "basicHttpBinding",
            "wsHttpBinding",
            "ws2007HttpBinding",
            "wsFederationHttpBinding",
            "netTcpBinding",
            "netMsmqBinding",
            "webHttpBinding"});
            this.cboBinding.Location = new System.Drawing.Point(99, 55);
            this.cboBinding.Name = "cboBinding";
            this.cboBinding.Size = new System.Drawing.Size(258, 21);
            this.cboBinding.TabIndex = 1;
            // 
            // txtUri
            // 
            this.txtUri.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.txtUri.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.txtUri.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.HistoryList;
            this.txtUri.Location = new System.Drawing.Point(99, 29);
            this.txtUri.Name = "txtUri";
            this.txtUri.Size = new System.Drawing.Size(258, 20);
            this.txtUri.TabIndex = 0;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(15, 85);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(45, 13);
            this.label3.TabIndex = 18;
            this.label3.Text = "Security";
            // 
            // cboSecurity
            // 
            this.cboSecurity.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.cboSecurity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboSecurity.FormattingEnabled = true;
            this.cboSecurity.Items.AddRange(new object[] {
            "None",
            "Message",
            "Transport",
            "TransportWithMessageCredential",
            "TransportCredentialOnly"});
            this.cboSecurity.Location = new System.Drawing.Point(99, 82);
            this.cboSecurity.Name = "cboSecurity";
            this.cboSecurity.Size = new System.Drawing.Size(258, 21);
            this.cboSecurity.TabIndex = 2;
            // 
            // chkReliable
            // 
            this.chkReliable.AutoSize = true;
            this.chkReliable.Location = new System.Drawing.Point(18, 119);
            this.chkReliable.Name = "chkReliable";
            this.chkReliable.Size = new System.Drawing.Size(175, 17);
            this.chkReliable.TabIndex = 3;
            this.chkReliable.Text = "Enable WS-Reliable Messaging";
            this.chkReliable.UseVisualStyleBackColor = true;
            // 
            // chkIncludeExceptions
            // 
            this.chkIncludeExceptions.AutoSize = true;
            this.chkIncludeExceptions.Location = new System.Drawing.Point(18, 142);
            this.chkIncludeExceptions.Name = "chkIncludeExceptions";
            this.chkIncludeExceptions.Size = new System.Drawing.Size(191, 17);
            this.chkIncludeExceptions.TabIndex = 4;
            this.chkIncludeExceptions.Text = "Include stack trace in SOAP Faults";
            this.chkIncludeExceptions.UseVisualStyleBackColor = true;
            // 
            // chkPublishMeta
            // 
            this.chkPublishMeta.AutoSize = true;
            this.chkPublishMeta.Location = new System.Drawing.Point(18, 165);
            this.chkPublishMeta.Name = "chkPublishMeta";
            this.chkPublishMeta.Size = new System.Drawing.Size(132, 17);
            this.chkPublishMeta.TabIndex = 5;
            this.chkPublishMeta.Text = "Publish service WSDL";
            this.chkPublishMeta.UseVisualStyleBackColor = true;
            // 
            // chkEnableHelp
            // 
            this.chkEnableHelp.AutoSize = true;
            this.chkEnableHelp.Location = new System.Drawing.Point(18, 188);
            this.chkEnableHelp.Name = "chkEnableHelp";
            this.chkEnableHelp.Size = new System.Drawing.Size(199, 17);
            this.chkEnableHelp.TabIndex = 6;
            this.chkEnableHelp.Text = "Enable service help / welcome page";
            this.chkEnableHelp.UseVisualStyleBackColor = true;
            // 
            // ucWcfConfigurator
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.chkEnableHelp);
            this.Controls.Add(this.chkPublishMeta);
            this.Controls.Add(this.chkReliable);
            this.Controls.Add(this.chkIncludeExceptions);
            this.Controls.Add(this.cboSecurity);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtUri);
            this.Controls.Add(this.cboBinding);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lbllsnName);
            this.Name = "ucWcfConfigurator";
            this.Size = new System.Drawing.Size(373, 214);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lbllsnName;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cboBinding;
        private System.Windows.Forms.TextBox txtUri;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox cboSecurity;
        private System.Windows.Forms.CheckBox chkReliable;
        private System.Windows.Forms.CheckBox chkIncludeExceptions;
        private System.Windows.Forms.CheckBox chkPublishMeta;
        private System.Windows.Forms.CheckBox chkEnableHelp;
    }
}
