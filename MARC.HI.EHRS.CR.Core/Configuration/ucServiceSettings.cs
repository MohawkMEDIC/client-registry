using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ServiceTools;

namespace MARC.HI.EHRS.CR.Core.Configuration
{
    public partial class ucServiceSettings : UserControl
    {

        /// <summary>
        /// gets or sets the service name
        /// </summary>
        public ServiceBootFlag ServiceStart
        {
            get
            {
                return (ServiceBootFlag)(cbxStartMode.SelectedIndex + 2);
            }
            set
            {
                this.cbxStartMode.SelectedIndex = (int)(value - 2);
            }
        }

        /// <summary>
        /// Gets or sets the user account
        /// </summary>
        public string UserAccount
        {
            get
            {
                return rdoLocalService.Checked ? null : txtUserName.Text;
            }
            set
            {
                this.txtUserName.Text = value;
                if (value == null)
                    rdoLocalService.Checked = true;
            }
        }

        /// <summary>
        /// Gets or sets the password
        /// </summary>
        public string Password
        {
            get { return rdoLocalService.Checked ? null : txtPassword.Text; }
            set
            {
                this.txtPassword.Text = new String('f', value.Length);
                if (value == null)
                    rdoLocalService.Checked = true;
            }
        }

        /// <summary>
        /// Initializer
        /// </summary>
        public ucServiceSettings()
        {
            InitializeComponent();
        }

        private void rdoAccount_CheckedChanged(object sender, EventArgs e)
        {
            this.txtPassword.Enabled = this.txtUserName.Enabled = true;
        }

        private void rdoLocalService_CheckedChanged(object sender, EventArgs e)
        {
            this.txtPassword.Enabled = this.txtUserName.Enabled = false ;
        }
    }
}
