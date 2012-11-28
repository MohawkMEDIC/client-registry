using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MARC.HI.EHRS.SVC.Config.Messaging.Forms.Configurators
{
    public partial class ucBasicConfigurator : frmEditListener.ConnectorConfigurator
    {
        public ucBasicConfigurator()
        {
            InitializeComponent();
            this.Dock = DockStyle.Fill;
        }

        /// <summary>
        /// Get or set connection string
        /// </summary>
        public override string ConnectionString
        {
            get
            {
                return txtConnectionString.Text;
            }
            set
            {
                txtConnectionString.Text = value;
            }
        }
    }
}
