namespace MARC.HI.EHRS.SVC.Config.Messaging.Forms
{
    partial class frmEditListener
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
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.lbllsnName = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.cboListener = new System.Windows.Forms.ComboBox();
            this.pnlConfig = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnOk.Location = new System.Drawing.Point(195, 279);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 8;
            this.btnOk.Text = "Ok";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(276, 279);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 9;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.pictureBox1.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.pictureBox1.Location = new System.Drawing.Point(14, 272);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(337, 1);
            this.pictureBox1.TabIndex = 7;
            this.pictureBox1.TabStop = false;
            // 
            // lbllsnName
            // 
            this.lbllsnName.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.lbllsnName.Dock = System.Windows.Forms.DockStyle.Top;
            this.lbllsnName.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbllsnName.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.lbllsnName.Location = new System.Drawing.Point(0, 0);
            this.lbllsnName.Name = "lbllsnName";
            this.lbllsnName.Size = new System.Drawing.Size(367, 20);
            this.lbllsnName.TabIndex = 10;
            this.lbllsnName.Text = "RV002020202";
            this.lbllsnName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 36);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(56, 13);
            this.label1.TabIndex = 11;
            this.label1.Text = "Connector";
            // 
            // cboListener
            // 
            this.cboListener.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboListener.FormattingEnabled = true;
            this.cboListener.Location = new System.Drawing.Point(89, 33);
            this.cboListener.Name = "cboListener";
            this.cboListener.Size = new System.Drawing.Size(260, 21);
            this.cboListener.TabIndex = 12;
            this.cboListener.SelectedIndexChanged += new System.EventHandler(this.cboListener_SelectedIndexChanged);
            // 
            // pnlConfig
            // 
            this.pnlConfig.Location = new System.Drawing.Point(-1, 62);
            this.pnlConfig.Name = "pnlConfig";
            this.pnlConfig.Size = new System.Drawing.Size(368, 208);
            this.pnlConfig.TabIndex = 13;
            // 
            // frmEditListener
            // 
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(367, 314);
            this.Controls.Add(this.pnlConfig);
            this.Controls.Add(this.cboListener);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lbllsnName);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.pictureBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmEditListener";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Edit Listener";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label lbllsnName;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cboListener;
        private System.Windows.Forms.Panel pnlConfig;
    }
}