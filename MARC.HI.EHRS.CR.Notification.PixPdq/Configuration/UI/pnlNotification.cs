using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using MARC.Everest.Connectors;
using System.Security.Cryptography.X509Certificates;
using MARC.HI.EHRS.SVC.Core.DataTypes;

namespace MARC.HI.EHRS.CR.Notification.PixPdq.Configuration.UI
{
    public partial class pnlNotification : UserControl
    {

        /// <summary>
        /// OidRegistrar
        /// </summary>
        public OidRegistrar OidRegistrar { get; set; }

        // Targets configured
        private List<TargetConfigurationInformation> m_targets = new List<TargetConfigurationInformation>();

        /// <summary>
        /// Gets the targets configured
        /// </summary>
        public List<TargetConfigurationInformation> Targets { get { return this.m_targets; } }

        /// <summary>
        /// Target configuration (union of wcf and target config)
        /// </summary>
        public struct TargetConfigurationInformation
        {
            /// <summary>
            /// Configuration Information
            /// </summary>
            public TargetConfiguration Configuration { get; set; }

            /// <summary>
            /// Address information
            /// </summary>
            public Uri Address { get; set; }

            /// <summary>
            /// Client certificate
            /// </summary>
            public StoreName ClientCertificateStore { get; set; }

            /// <summary>
            /// Client certificate store
            /// </summary>
            public StoreLocation ClientCertificateLocation { get; set; }

            /// <summary>
            /// Gets or sets the client certificate 
            /// </summary>
            public X509Certificate2 ClientCertificate { get; set; }

            /// <summary>
            /// Client certificate
            /// </summary>
            public StoreName ServerCertificateStore { get; set; }

            /// <summary>
            /// Client certificate store
            /// </summary>
            public StoreLocation ServerCertificateLocation { get; set; }

            /// <summary>
            /// Gets or sets the client certificate 
            /// </summary>
            public X509Certificate2 ServerCertificate { get; set; }

            /// <summary>
            /// True if server cert should be validated
            /// </summary>
            public bool ValidateServerCert { get; set; }
        }

        /// <summary>
        /// Gets or sets the targets
        /// </summary>
        public void SetTargets(List<TargetConfiguration> targets, XmlElement wcfRoot)
        {
            foreach (var target in targets)
            {
                // Process connection string
                var connectionString = ConnectionStringParser.ParseConnectionString(target.ConnectionString);
                List<String> epName = null;
                XmlNode endpointNode = null;
                if (connectionString.TryGetValue("endpointname", out epName))
                {
                    // Load the ep
                    endpointNode = wcfRoot.SelectSingleNode(String.Format("./*[local-name() = 'client']/*[local-name() = 'endpoint'][@name = '{0}']", epName[0])) as XmlElement;
                    if (endpointNode == null || endpointNode.Attributes["address"] == null)
                        continue;
                }
                
                // Configuration information
                var configurationInformation = new TargetConfigurationInformation()
                {
                    Configuration = target
                };
                
                // Get address
                if(endpointNode != null)
                    configurationInformation.Address = new Uri(endpointNode.Attributes["address"].Value);
                else
                    configurationInformation.Address = new Uri(target.ConnectionString);

                // Get the binding configuration
                if (endpointNode != null && endpointNode.Attributes["bindingConfiguration"] != null)
                {
                    var bindingNode = wcfRoot.SelectSingleNode(String.Format("./*[local-name() = 'bindings']/*[local-name() = 'wsHttpBinding']/*[local-name() = 'binding'][@name = '{0}']", endpointNode.Attributes["bindingConfiguration"].Value)) as XmlElement;
                    if (bindingNode != null)
                    {
                        // Validate we can read this
                        var securityNode = bindingNode.SelectSingleNode("./*[local-name() = 'security']/@mode");
                        if (securityNode != null && securityNode.Value != "Transport" && securityNode.Value != "None")
                        {
                            MessageBox.Show(String.Format("Security mode of {0} cannot be configured with this tool", securityNode.Value));
                            continue;
                        }
                    }
                }

                // Get the behavior
                if (endpointNode != null && endpointNode.Attributes["behaviorConfiguration"] != null)
                {
                    var behaviorNode = wcfRoot.SelectSingleNode(String.Format("./*[local-name() = 'behaviors']/*[local-name() = 'endpointBehaviors']/*[local-name() = 'behavior'][@name = '{0}']", endpointNode.Attributes["behaviorConfiguration"].Value)) as XmlElement;

                    // Get the client credentials used?
                    if (behaviorNode != null)
                    {
                        // are there client credentials?
                        var clientCredentialsNode = behaviorNode.SelectSingleNode("./*[local-name() = 'clientCredentials']/*[local-name() = 'clientCertificate']") as XmlElement;
                        configurationInformation.ValidateServerCert = behaviorNode.SelectSingleNode("./*[local-name() = 'clientCredentials']/*[local-name() = 'serviceCertificate']/*[local-name() = 'authentication'][@certificateValidationMode = 'Custom']") != null;

                        if (clientCredentialsNode != null) // there are client credentials, so lets find them
                        {
                            XmlAttribute storeLocationAtt = clientCredentialsNode.Attributes["storeLocation"],
                                storeNameAtt = clientCredentialsNode.Attributes["storeName"],
                                findTypeAtt = clientCredentialsNode.Attributes["x509FindType"],
                                findValueAtt = clientCredentialsNode.Attributes["findValue"];

                            if(findTypeAtt == null || findValueAtt == null) continue; // can't find if nothing to find...

                            configurationInformation.ClientCertificateLocation = (StoreLocation)Enum.Parse(typeof(StoreLocation), storeLocationAtt == null ? "LocalMachine" : storeLocationAtt.Value);
                            configurationInformation.ClientCertificateStore = (StoreName)Enum.Parse(typeof(StoreName), storeNameAtt == null ? "My" : storeNameAtt.Value);
                            configurationInformation.ClientCertificate = ConfigurationSectionHandler.FindCertificate(configurationInformation.ClientCertificateStore, configurationInformation.ClientCertificateLocation, (X509FindType)Enum.Parse(typeof(X509FindType), findTypeAtt.Value), findValueAtt.Value);
                        }
                    }
                }

                this.m_targets.Add(configurationInformation);
            }
            RefreshListView();
        }


       
        /// <summary>
        /// Refresh the list view
        /// </summary>
        private void RefreshListView()
        {
            lsvEp.Items.Clear();
            foreach (var target in this.m_targets)
            {
                // Create the item
                var item = this.lsvEp.Items.Add(target.Configuration.Name, target.Configuration.Name, 0);
                item.Tag = target;
                item.SubItems.Add(target.Address.ToString());
                item.SubItems.Add(target.Configuration.Notifier.GetType().Name.ToString());
            }
            lsvEp.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);

        }
       

        public pnlNotification()
        {
            InitializeComponent();
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            if (lsvEp.SelectedItems.Count == 0) return;
            var currentService = (TargetConfigurationInformation)lsvEp.SelectedItems[0].Tag;
            frmAddTarget addHandler = new frmAddTarget()
            {
                OidRegistry = this.OidRegistrar,
                TargetConfiguration = currentService
            };
            if (addHandler.ShowDialog() == DialogResult.OK)
            {
                this.m_targets.Insert(this.m_targets.IndexOf(currentService), addHandler.TargetConfiguration);
                this.m_targets.Remove(currentService);
                this.RefreshListView();
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            string name = Guid.NewGuid().ToString().Substring(0, 6);
            frmAddTarget addHandler = new frmAddTarget()
            {
                TargetConfiguration = new TargetConfigurationInformation()
                {
                    Address = new Uri("http://pix/"),
                    Configuration = new TargetConfiguration(name, String.Format("endpointName={0}", name), "PAT_ID_X_REF_MGR_HL7v3", "1.2.3.4.5.6"),

                },
                OidRegistry = this.OidRegistrar
            };
            if (addHandler.ShowDialog() == DialogResult.OK)
            {
                this.m_targets.Add(addHandler.TargetConfiguration);
                this.RefreshListView();
            }

        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (lsvEp.SelectedItems.Count == 0) return;

            if (MessageBox.Show(string.Format("Are you sure you want to remove the endpoint '{0}'?", lsvEp.SelectedItems[0].Text), "Confirm Removal", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                this.m_targets.Remove((TargetConfigurationInformation)lsvEp.SelectedItems[0].Tag);
                this.RefreshListView();
            }

        }


       

    }
}
