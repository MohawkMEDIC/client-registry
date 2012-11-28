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
    public partial class ucWcfConfigurator : frmEditListener.ConnectorConfigurator
    {
        public ucWcfConfigurator()
        {
            InitializeComponent();
            this.Dock = DockStyle.Fill;
        }

        /// <summary>
        /// Bidning name
        /// </summary>
        string m_serviceName = String.Empty;

        /// <summary>
        /// Get the type that this configurator handles
        /// </summary>
        public override string HandlesType
        {
            get
            {
                return "MARC.Everest.Connectors.WCF.WcfServerConnector";
            }
        }

        /// <summary>
        /// Connection string
        /// </summary>
        public override string ConnectionString
        {
            get
            {
                return String.Format("servicename={0}", m_serviceName);
            }
            set
            {
                if (value == null)
                {
                    m_serviceName = Guid.NewGuid().ToString();
                    m_serviceName = String.Format("svc{0}", m_serviceName.Substring(0, m_serviceName.IndexOf("-")));
                    return;
                }

                var connStringParts = ConnectionStringParser.ParseConnectionString(value);
                List<String> serviceNames = null;
                if (connStringParts.TryGetValue("servicename", out serviceNames))
                    m_serviceName = serviceNames[0];
                if (String.IsNullOrEmpty(m_serviceName))
                {
                    m_serviceName = Guid.NewGuid().ToString();
                    m_serviceName = String.Format("svc{0}", m_serviceName.Substring(0, m_serviceName.IndexOf("-")));
                }

                // Get service info
                var wcfInfo = GetWcfEndpointInfo(m_serviceName);

                // Try to populate the form
                string t;
                if (wcfInfo.TryGetValue("uri", out t))
                    txtUri.Text = t;
                if (wcfInfo.TryGetValue("binding", out t))
                    cboBinding.Text = t;
                if (wcfInfo.TryGetValue("wsrm", out t))
                    chkReliable.Checked = t.ToLower() == "true";
                if (wcfInfo.TryGetValue("wssec", out t))
                    cboSecurity.Text = String.IsNullOrEmpty(t) ? "None" : t;
                else
                    cboSecurity.Text = "None";
                if (wcfInfo.TryGetValue("stack", out t))
                    chkIncludeExceptions.Checked = t.ToLower() == "true";
                if (wcfInfo.TryGetValue("meta", out t))
                    chkPublishMeta.Checked = t.ToLower() == "true";
                if (wcfInfo.TryGetValue("help", out t))
                    chkEnableHelp.Checked = t.ToLower() == "true";
            }
        }

        /// <summary>
        /// Get WCF Service Endpoint Connection Info
        /// </summary>
        private Dictionary<String, String> GetWcfEndpointInfo(string serviceName)
        {
            var data = ConfigHelper.GetRawConfigurationData(new string[] { 
                String.Format("//system.serviceModel/services/service[@name='{0}']/endpoint/@address", serviceName),
                String.Format("//system.serviceModel/services/service[@name='{0}']/endpoint/@binding", serviceName),
                String.Format("//system.serviceModel/services/service[@name='{0}']/@behaviorConfiguration", serviceName), 
                String.Format("//system.serviceModel/services/service[@name='{0}']/endpoint/@bindingConfiguration", serviceName)
            });
            Dictionary<String, String> retVal = new Dictionary<string, string>() {
                { "uri", data[0] },
                { "binding", data[1] },
                { "behaviorConfig", data[2] },
                { "bindingConfig", data[3] }
            };

            // Now get binding configuration
            data = ConfigHelper.GetRawConfigurationData(new string[] {
                String.Format("//system.serviceModel/bindings/{0}/binding[@name='{1}']/reliableSession/@enabled", data[1],data[3]), 
                String.Format("//system.serviceModel/bindings/{0}/binding[@name='{1}']/security/@mode", data[1], data[3]),
                String.Format("//system.serviceModel/behaviors/serviceBehaviors/behavior[@name='{0}']/serviceDebug/@includeExceptionDetailInFaults", data[2]),
                String.Format("//system.serviceModel/behaviors/serviceBehaviors/behavior[@name='{0}']/serviceMetadata/@httpGetEnabled", data[2]), 
                String.Format("//system.serviceModel/behaviors/serviceBehaviors/behavior[@name='{0}']/serviceDebug/@httpHelpPageEnabled", data[2])
            });
            retVal.Add("wsrm", data[0]);
            retVal.Add("wssec", data[1]);
            retVal.Add("stack", data[2]);
            retVal.Add("meta", data[3]);
            retVal.Add("help", data[4]);

            return retVal;
        }

        /// <summary>
        /// Commit changes
        /// </summary>
        public override void Commit()
        {
            // Update WCF Stuff
            ConfigHelper.UpdateWcfService(
                m_serviceName,
                cboBinding.Text,
                txtUri.Text,
                chkReliable.Checked,
                cboSecurity.Text,
                chkReliable.Checked,
                chkPublishMeta.Checked,
                chkEnableHelp.Checked
            );
        }
    }


}
