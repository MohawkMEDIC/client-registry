using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MARC.HI.EHRS.CR.Messaging.HL7.Configuration.UI
{
    public partial class pnlHapiConfiguration : UserControl
    {

        // configuration
        private HL7ConfigurationSection m_configuration;

        /// <summary>
        /// Gets or sets the handlers
        /// </summary>
        public List<HandlerConfigTemplate> Handlers { get; set; }

        /// <summary>
        /// Configuration options
        /// </summary>
        public HL7ConfigurationSection Configuration {
            get
            {
                return this.m_configuration;
            }
            set
            {
                this.m_configuration = value;
                this.lsvEp.Items.Clear();

                foreach (var svc in this.m_configuration.Services)
                {
                    var item = lsvEp.Items.Add(svc.Name, svc.Name, 0);
                    item.SubItems.Add(svc.Address.ToString());
                    item.Tag = svc;
                }
                lsvEp.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            }
        }

        public pnlHapiConfiguration()
        {
            InitializeComponent();
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            if (lsvEp.SelectedItems.Count == 0) return;
            var currentService = lsvEp.SelectedItems[0].Tag as ServiceDefinition;
            frmAddHandler addHandler = new frmAddHandler()
            {
                HandlerTemplates = this.Handlers,
                ServiceDefinition = currentService
            };
            if (addHandler.ShowDialog() == DialogResult.OK)
            {
                this.m_configuration.Services.Insert(this.m_configuration.Services.IndexOf(currentService), addHandler.ServiceDefinition);
                this.m_configuration.Services.Remove(currentService);
                this.Configuration = this.m_configuration;
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            frmAddHandler addHandler = new frmAddHandler()
            {
                ServiceDefinition = new ServiceDefinition()
                {
                    Address = new Uri("tcp://0.0.0.0:2100"),
                    Name = Guid.NewGuid().ToString().Substring(0, 6),
                    ReceiveTimeout = new TimeSpan(0, 0, 30)
                },
                HandlerTemplates = this.Handlers
            };
            if (addHandler.ShowDialog() == DialogResult.OK)
            {
                this.m_configuration.Services.Add(addHandler.ServiceDefinition);
                this.Configuration = this.m_configuration;
            }

        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (lsvEp.SelectedItems.Count == 0) return;

            if (MessageBox.Show(string.Format("Are you sure you want to remove the endpoint '{0}'?", lsvEp.SelectedItems[0].Text), "Confirm Removal", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                this.m_configuration.Services.Remove(lsvEp.SelectedItems[0].Tag as ServiceDefinition);
                this.Configuration = this.m_configuration;
            }

        }
    }
}
