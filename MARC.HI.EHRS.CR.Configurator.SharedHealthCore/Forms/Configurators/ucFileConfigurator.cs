using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MARC.Everest.Connectors;

namespace MARC.HI.EHRS.SVC.Config.Messaging.Forms.Configurators
{
    public partial class ucFileConfigurator : MARC.HI.EHRS.SVC.Config.Messaging.Forms.frmEditListener.ConnectorConfigurator
    {
        public ucFileConfigurator()
        {
            InitializeComponent();
            this.Dock = DockStyle.Fill;
        }

        /// <summary>
        /// File Listen connector
        /// </summary>
        public override String HandlesType
        {
            get
            {
                return "MARC.Everest.Connectors.File.FileListenConnector";
            }
        }

        /// <summary>
        /// Connection string
        /// </summary>
        public override string ConnectionString
        {
            get
            {
                StringBuilder connStr = new StringBuilder();

                if (!string.IsNullOrEmpty(txtDirectory.Text))
                    connStr.AppendFormat("directory={0};", txtDirectory.Text);
                if (!string.IsNullOrEmpty(txtFilter.Text))
                    connStr.AppendFormat("pattern={0};", txtFilter.Text);
                if (chkKeepFiles.Checked)
                    connStr.Append("keepfiles=true;");
                if (chkProcess.Checked)
                    connStr.Append("processexisting=true;");
                connStr.Remove(connStr.Length - 1, 1);
                return connStr.ToString();
            }
            set
            {
                if (value == null)
                    return;

                var connDetails = ConnectionStringParser.ParseConnectionString(value);
                List<String> dir = null, filter = null, keepFiles = null, processExist = null;

                if (connDetails.TryGetValue("directory", out dir))
                    txtDirectory.Text = dir[0];
                if(connDetails.TryGetValue("pattern", out filter))
                    txtFilter.Text = filter[0];
                if(connDetails.TryGetValue("keepfiles", out keepFiles))
                    chkKeepFiles.Checked = keepFiles[0].Equals("true");
                if (connDetails.TryGetValue("processexisting", out processExist))
                    chkProcess.Checked = processExist[0].Equals("true");


            }
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            if (fldBrowser.ShowDialog() == DialogResult.OK)
                txtDirectory.Text = fldBrowser.SelectedPath;
        }
    }




}
