using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MARC.HI.EHRS.SVC.Core.Configuration.Update;

namespace MARC.HI.EHRS.CR.Persistence.Data.Configuration.UI
{
    public partial class frmDatabaseUpdateConfirmation : Form
    {
        public frmDatabaseUpdateConfirmation()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Set the updates
        /// </summary>
        public List<DbSchemaUpdate> Updates
        {
            set
            {
                lstUpdates.Items.Clear();
                foreach(var itm in value)
                {
                    lstUpdates.Items.Add(itm);
                }
                
            }
        }

        /// <summary>
        /// Install the updates
        /// </summary>
        private void btnInstall_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.Close();
        }
    }
}
