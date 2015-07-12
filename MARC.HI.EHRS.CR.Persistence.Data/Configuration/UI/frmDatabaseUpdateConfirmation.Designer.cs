namespace MARC.HI.EHRS.CR.Persistence.Data.Configuration.UI
{
    partial class frmDatabaseUpdateConfirmation
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmDatabaseUpdateConfirmation));
            this.label1 = new System.Windows.Forms.Label();
            this.lstUpdates = new System.Windows.Forms.ListBox();
            this.btnInstall = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(474, 86);
            this.label1.TabIndex = 0;
            this.label1.Text = resources.GetString("label1.Text");
            // 
            // lstUpdates
            // 
            this.lstUpdates.FormattingEnabled = true;
            this.lstUpdates.Location = new System.Drawing.Point(15, 98);
            this.lstUpdates.Name = "lstUpdates";
            this.lstUpdates.Size = new System.Drawing.Size(471, 95);
            this.lstUpdates.TabIndex = 1;
            // 
            // btnInstall
            // 
            this.btnInstall.Location = new System.Drawing.Point(411, 199);
            this.btnInstall.Name = "btnInstall";
            this.btnInstall.Size = new System.Drawing.Size(75, 23);
            this.btnInstall.TabIndex = 2;
            this.btnInstall.Text = "Apply";
            this.btnInstall.UseVisualStyleBackColor = true;
            this.btnInstall.Click += new System.EventHandler(this.btnInstall_Click);
            // 
            // frmDatabaseUpdateConfirmation
            // 
            this.AcceptButton = this.btnInstall;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(498, 228);
            this.ControlBox = false;
            this.Controls.Add(this.btnInstall);
            this.Controls.Add(this.lstUpdates);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmDatabaseUpdateConfirmation";
            this.Text = "Updates Available";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListBox lstUpdates;
        private System.Windows.Forms.Button btnInstall;
    }
}