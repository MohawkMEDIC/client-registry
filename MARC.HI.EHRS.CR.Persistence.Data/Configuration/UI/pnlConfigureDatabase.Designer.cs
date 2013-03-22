namespace MARC.HI.EHRS.CR.Configurator.SharedHealthCore.Panels
{
    partial class pnlConfigureDatabase
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(pnlConfigureDatabase));
            this.chkUpdateIfExists = new System.Windows.Forms.CheckBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label11 = new System.Windows.Forms.Label();
            this.numMinCriteria = new System.Windows.Forms.NumericUpDown();
            this.label10 = new System.Windows.Forms.Label();
            this.lstMerge = new System.Windows.Forms.CheckedListBox();
            this.chkMerge = new System.Windows.Forms.CheckBox();
            this.pnlConnection = new System.Windows.Forms.Panel();
            this.label12 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.dlStrength = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.chkExact = new System.Windows.Forms.CheckBox();
            this.chkVariant = new System.Windows.Forms.CheckBox();
            this.chkSoundex = new System.Windows.Forms.CheckBox();
            this.label3 = new System.Windows.Forms.Label();
            this.chkAllowDuplicates = new System.Windows.Forms.CheckBox();
            this.dbSelector = new MARC.HI.EHRS.SVC.ConfigurationApplciation.DatabaseSelector();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numMinCriteria)).BeginInit();
            this.pnlConnection.SuspendLayout();
            this.SuspendLayout();
            // 
            // chkUpdateIfExists
            // 
            this.chkUpdateIfExists.AutoSize = true;
            this.chkUpdateIfExists.Location = new System.Drawing.Point(10, 359);
            this.chkUpdateIfExists.Name = "chkUpdateIfExists";
            this.chkUpdateIfExists.Size = new System.Drawing.Size(240, 17);
            this.chkUpdateIfExists.TabIndex = 2;
            this.chkUpdateIfExists.Text = "Registration of existing patient causes update";
            this.chkUpdateIfExists.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.label11);
            this.groupBox1.Controls.Add(this.numMinCriteria);
            this.groupBox1.Controls.Add(this.label10);
            this.groupBox1.Controls.Add(this.lstMerge);
            this.groupBox1.Location = new System.Drawing.Point(11, 415);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(334, 207);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Duplicate Detection";
            // 
            // label11
            // 
            this.label11.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(6, 181);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(173, 13);
            this.label11.TabIndex = 3;
            this.label11.Text = "Minimum matching criteria elements";
            // 
            // numMinCriteria
            // 
            this.numMinCriteria.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.numMinCriteria.Location = new System.Drawing.Point(265, 178);
            this.numMinCriteria.Name = "numMinCriteria";
            this.numMinCriteria.Size = new System.Drawing.Size(63, 20);
            this.numMinCriteria.TabIndex = 2;
            // 
            // label10
            // 
            this.label10.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label10.Location = new System.Drawing.Point(6, 25);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(322, 49);
            this.label10.TabIndex = 1;
            this.label10.Text = resources.GetString("label10.Text");
            // 
            // lstMerge
            // 
            this.lstMerge.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lstMerge.FormattingEnabled = true;
            this.lstMerge.Location = new System.Drawing.Point(6, 77);
            this.lstMerge.Name = "lstMerge";
            this.lstMerge.Size = new System.Drawing.Size(322, 79);
            this.lstMerge.TabIndex = 0;
            this.lstMerge.SelectedIndexChanged += new System.EventHandler(this.lstMerge_SelectedIndexChanged);
            // 
            // chkMerge
            // 
            this.chkMerge.AutoSize = true;
            this.chkMerge.Location = new System.Drawing.Point(10, 382);
            this.chkMerge.Name = "chkMerge";
            this.chkMerge.Size = new System.Drawing.Size(318, 17);
            this.chkMerge.TabIndex = 0;
            this.chkMerge.Text = "Enable auto-merging of patients when duplicates are detected";
            this.chkMerge.UseVisualStyleBackColor = true;
            // 
            // pnlConnection
            // 
            this.pnlConnection.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlConnection.Controls.Add(this.chkUpdateIfExists);
            this.pnlConnection.Controls.Add(this.groupBox1);
            this.pnlConnection.Controls.Add(this.label12);
            this.pnlConnection.Controls.Add(this.chkMerge);
            this.pnlConnection.Controls.Add(this.dlStrength);
            this.pnlConnection.Controls.Add(this.label2);
            this.pnlConnection.Controls.Add(this.label4);
            this.pnlConnection.Controls.Add(this.chkExact);
            this.pnlConnection.Controls.Add(this.chkVariant);
            this.pnlConnection.Controls.Add(this.chkSoundex);
            this.pnlConnection.Controls.Add(this.label3);
            this.pnlConnection.Controls.Add(this.chkAllowDuplicates);
            this.pnlConnection.Controls.Add(this.dbSelector);
            this.pnlConnection.Controls.Add(this.label1);
            this.pnlConnection.Location = new System.Drawing.Point(3, 3);
            this.pnlConnection.Name = "pnlConnection";
            this.pnlConnection.Size = new System.Drawing.Size(361, 652);
            this.pnlConnection.TabIndex = 47;
            // 
            // label12
            // 
            this.label12.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label12.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.label12.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label12.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.label12.Location = new System.Drawing.Point(0, 324);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(361, 21);
            this.label12.TabIndex = 47;
            this.label12.Text = "Merging";
            this.label12.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(361, 20);
            this.label1.TabIndex = 24;
            this.label1.Text = "Connection";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // dlStrength
            // 
            this.dlStrength.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dlStrength.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.dlStrength.FormattingEnabled = true;
            this.dlStrength.Items.AddRange(new object[] {
            "Exact",
            "Strong",
            "Moderate",
            "Weak"});
            this.dlStrength.Location = new System.Drawing.Point(149, 300);
            this.dlStrength.Name = "dlStrength";
            this.dlStrength.Size = new System.Drawing.Size(110, 21);
            this.dlStrength.TabIndex = 46;
            // 
            // label2
            // 
            this.label2.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.label2.Dock = System.Windows.Forms.DockStyle.Top;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.label2.Location = new System.Drawing.Point(0, 157);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(361, 21);
            this.label2.TabIndex = 25;
            this.label2.Text = "Validation";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(26, 303);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(117, 13);
            this.label4.TabIndex = 45;
            this.label4.Text = "Default Match Strength";
            // 
            // chkExact
            // 
            this.chkExact.AutoSize = true;
            this.chkExact.Location = new System.Drawing.Point(29, 277);
            this.chkExact.Name = "chkExact";
            this.chkExact.Size = new System.Drawing.Size(179, 17);
            this.chkExact.TabIndex = 44;
            this.chkExact.Text = "Enable exact matching algorithm";
            this.chkExact.UseVisualStyleBackColor = true;
            // 
            // chkVariant
            // 
            this.chkVariant.AutoSize = true;
            this.chkVariant.Location = new System.Drawing.Point(29, 254);
            this.chkVariant.Name = "chkVariant";
            this.chkVariant.Size = new System.Drawing.Size(253, 17);
            this.chkVariant.TabIndex = 43;
            this.chkVariant.Text = "Enable name variant matching (name synonyms)";
            this.chkVariant.UseVisualStyleBackColor = true;
            // 
            // chkSoundex
            // 
            this.chkSoundex.AutoSize = true;
            this.chkSoundex.Location = new System.Drawing.Point(29, 231);
            this.chkSoundex.Name = "chkSoundex";
            this.chkSoundex.Size = new System.Drawing.Size(193, 17);
            this.chkSoundex.TabIndex = 42;
            this.chkSoundex.Text = "Enable soundex matching algorithm";
            this.chkSoundex.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.label3.Location = new System.Drawing.Point(0, 201);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(361, 21);
            this.label3.TabIndex = 41;
            this.label3.Text = "Query Control";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // chkAllowDuplicates
            // 
            this.chkAllowDuplicates.AutoSize = true;
            this.chkAllowDuplicates.Location = new System.Drawing.Point(29, 181);
            this.chkAllowDuplicates.Name = "chkAllowDuplicates";
            this.chkAllowDuplicates.Size = new System.Drawing.Size(160, 17);
            this.chkAllowDuplicates.TabIndex = 40;
            this.chkAllowDuplicates.Text = "Permit duplicate registrations";
            this.chkAllowDuplicates.UseVisualStyleBackColor = true;
            // 
            // dbSelector
            // 
            this.dbSelector.DatabaseConfigurator = null;
            this.dbSelector.Dock = System.Windows.Forms.DockStyle.Top;
            this.dbSelector.Location = new System.Drawing.Point(0, 20);
            this.dbSelector.MinimumSize = new System.Drawing.Size(0, 137);
            this.dbSelector.Name = "dbSelector";
            this.dbSelector.Size = new System.Drawing.Size(361, 137);
            this.dbSelector.TabIndex = 20;
            // 
            // pnlConfigureDatabase
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.Controls.Add(this.pnlConnection);
            this.Name = "pnlConfigureDatabase";
            this.Size = new System.Drawing.Size(367, 280);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numMinCriteria)).EndInit();
            this.pnlConnection.ResumeLayout(false);
            this.pnlConnection.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.CheckBox chkUpdateIfExists;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.NumericUpDown numMinCriteria;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.CheckedListBox lstMerge;
        private System.Windows.Forms.CheckBox chkMerge;
        private System.Windows.Forms.Panel pnlConnection;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox dlStrength;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.CheckBox chkExact;
        private System.Windows.Forms.CheckBox chkVariant;
        private System.Windows.Forms.CheckBox chkSoundex;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox chkAllowDuplicates;
        private System.Windows.Forms.Label label12;
        private SVC.ConfigurationApplciation.DatabaseSelector dbSelector;


    }
}
