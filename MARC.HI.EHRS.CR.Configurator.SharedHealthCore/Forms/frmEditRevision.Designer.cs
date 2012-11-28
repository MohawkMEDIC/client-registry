namespace MARC.HI.EHRS.SVC.Config.Messaging.Forms
{
    partial class frmEditRevision
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
            this.lblRevName = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.chkValidate = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.txtRevName = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.cboFormatter = new System.Windows.Forms.ComboBox();
            this.cboDTFormatter = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.cboRevision = new System.Windows.Forms.ComboBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // lblRevName
            // 
            this.lblRevName.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.lblRevName.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblRevName.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblRevName.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.lblRevName.Location = new System.Drawing.Point(0, 0);
            this.lblRevName.Name = "lblRevName";
            this.lblRevName.Size = new System.Drawing.Size(355, 20);
            this.lblRevName.TabIndex = 0;
            this.lblRevName.Text = "RV002020202";
            this.lblRevName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pictureBox1
            // 
            this.pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.pictureBox1.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.pictureBox1.Location = new System.Drawing.Point(7, 172);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(337, 1);
            this.pictureBox1.TabIndex = 1;
            this.pictureBox1.TabStop = false;
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(269, 179);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 6;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnOk.Location = new System.Drawing.Point(188, 179);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 5;
            this.btnOk.Text = "Ok";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // chkValidate
            // 
            this.chkValidate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.chkValidate.AutoSize = true;
            this.chkValidate.Location = new System.Drawing.Point(13, 149);
            this.chkValidate.Name = "chkValidate";
            this.chkValidate.Size = new System.Drawing.Size(224, 17);
            this.chkValidate.TabIndex = 4;
            this.chkValidate.Text = "Validate inbound and outbound messages";
            this.chkValidate.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 37);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(35, 13);
            this.label1.TabIndex = 5;
            this.label1.Text = "Name";
            // 
            // txtRevName
            // 
            this.txtRevName.Location = new System.Drawing.Point(115, 34);
            this.txtRevName.Name = "txtRevName";
            this.txtRevName.Size = new System.Drawing.Size(228, 20);
            this.txtRevName.TabIndex = 0;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 64);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(88, 13);
            this.label2.TabIndex = 7;
            this.label2.Text = "Primary Formatter";
            // 
            // cboFormatter
            // 
            this.cboFormatter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboFormatter.FormattingEnabled = true;
            this.cboFormatter.Location = new System.Drawing.Point(115, 61);
            this.cboFormatter.Name = "cboFormatter";
            this.cboFormatter.Size = new System.Drawing.Size(229, 21);
            this.cboFormatter.TabIndex = 1;
            // 
            // cboDTFormatter
            // 
            this.cboDTFormatter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboDTFormatter.FormattingEnabled = true;
            this.cboDTFormatter.Location = new System.Drawing.Point(114, 88);
            this.cboDTFormatter.Name = "cboDTFormatter";
            this.cboDTFormatter.Size = new System.Drawing.Size(229, 21);
            this.cboDTFormatter.TabIndex = 2;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(11, 91);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(97, 13);
            this.label3.TabIndex = 9;
            this.label3.Text = "Datatype Formatter";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 119);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(48, 13);
            this.label4.TabIndex = 11;
            this.label4.Text = "Revision";
            // 
            // cboRevision
            // 
            this.cboRevision.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboRevision.FormattingEnabled = true;
            this.cboRevision.Location = new System.Drawing.Point(115, 116);
            this.cboRevision.Name = "cboRevision";
            this.cboRevision.Size = new System.Drawing.Size(228, 21);
            this.cboRevision.TabIndex = 3;
            // 
            // frmEditRevision
            // 
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(355, 208);
            this.Controls.Add(this.cboRevision);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.cboDTFormatter);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.cboFormatter);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtRevName);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.chkValidate);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.lblRevName);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmEditRevision";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Edit Revision";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblRevName;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.CheckBox chkValidate;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtRevName;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cboFormatter;
        private System.Windows.Forms.ComboBox cboDTFormatter;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox cboRevision;
    }
}