namespace MARC.HI.EHRS.SVC.Config.Messaging.Forms.Configurators
{
    partial class ucBasicConfigurator
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
            this.txtConnectionString = new System.Windows.Forms.TextBox();
            this.lbllsnName = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 31);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(91, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Connection String";
            // 
            // txtConnectionString
            // 
            this.txtConnectionString.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.txtConnectionString.Location = new System.Drawing.Point(98, 28);
            this.txtConnectionString.Name = "txtConnectionString";
            this.txtConnectionString.Size = new System.Drawing.Size(179, 20);
            this.txtConnectionString.TabIndex = 1;
            // 
            // lbllsnName
            // 
            this.lbllsnName.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.lbllsnName.Dock = System.Windows.Forms.DockStyle.Top;
            this.lbllsnName.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbllsnName.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.lbllsnName.Location = new System.Drawing.Point(0, 0);
            this.lbllsnName.Name = "lbllsnName";
            this.lbllsnName.Size = new System.Drawing.Size(280, 20);
            this.lbllsnName.TabIndex = 11;
            this.lbllsnName.Text = "Basic Configuration";
            this.lbllsnName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // ucBasicConfigurator
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lbllsnName);
            this.Controls.Add(this.txtConnectionString);
            this.Controls.Add(this.label1);
            this.Name = "ucBasicConfigurator";
            this.Size = new System.Drawing.Size(280, 64);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtConnectionString;
        private System.Windows.Forms.Label lbllsnName;
    }
}
