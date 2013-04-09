using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Security.Cryptography.X509Certificates;
using System.Xml;

namespace MARC.HI.EHRS.CR.Messaging.Admin.Configuration.UI
{
    public partial class pnlAdminIface : UserControl
    {
        public pnlAdminIface()
        {
            InitializeComponent();
            InitializeStores();
        }

        /// <summary>
        /// Gets or sets the service debug
        /// </summary>
        public bool ServiceDebugEnabled
        {
            get
            {
                return chkDebug.Checked;
            }
            set
            {
                chkDebug.Checked = value;
            }
        }

        /// <summary>
        /// Gets or sets the meta-data enable
        /// </summary>
        public bool ServiceMetaDataEnabled
        {
            get
            {
                return chkMetaData.Checked;
            }
            set
            {
                chkMetaData.Checked = value;
            }
        }

        /// <summary>
        /// Gets or sets the requirement of client certificates
        /// </summary>
        public bool RequireClientCerts
        {
            get
            {
                return chkPKI.Checked;
            }
            set
            {
                chkPKI.Checked = value;
            }
        }

        /// <summary>
        /// Gets or sets the endpoint address
        /// </summary>
        public string Address
        {
            get
            {
                return txtAddress.Text;
            }
            set
            {
                txtAddress.Text = value;
                RescanScheme();
            }
        }

        /// <summary>
        /// Gets or sets the store name to locate certificates
        /// </summary>
        public StoreName StoreName
        {
            get
            {
                return (StoreName)cbxStore.SelectedItem;
            }
            set
            {
                cbxStore.SelectedItem = value;
            }
        }

        /// <summary>
        /// Gets or sets the store name to locate certificates
        /// </summary>
        public StoreLocation StoreLocation
        {
            get
            {
                return (StoreLocation)cbxStoreLocation.SelectedItem;
            }
            set
            {
                cbxStoreLocation.SelectedItem = value;
            }
        }

        /// <summary>
        /// Gets or sets the certificate
        /// </summary>
        public X509Certificate2 Certificate
        {
            get
            {
                return (X509Certificate2)txtCertificate.Tag;
            }
            set
            {
                txtCertificate.Tag = value;
                txtCertificate.Text = value.GetSerialNumberString();
            }
        }

        /// <summary>
        /// Enable security based on URI scheme
        /// </summary>
        private void RescanScheme()
        {
            try
            {
                Uri myAddr = new Uri(txtAddress.Text);
                grpSSL.Enabled = myAddr.Scheme == "https";

            }
            catch { }
        }

        /// <summary>
        /// Initialize certificate stores
        /// </summary>
        private void InitializeStores()
        {
            foreach (var sv in Enum.GetValues(typeof(StoreLocation)))
                cbxStoreLocation.Items.Add(sv);
            foreach (var sv in Enum.GetValues(typeof(StoreName)))
                cbxStore.Items.Add(sv);
            cbxStoreLocation.SelectedIndex = 0;
            cbxStore.SelectedIndex = 0;
        }

        /// <summary>
        /// Address is validated
        /// </summary>
        private void txtAddress_Validated(object sender, EventArgs e)
        {
            RescanScheme();
        }

        private void btnChooseCert_Click(object sender, EventArgs e)
        {
            X509Store store = new X509Store(this.StoreName, this.StoreLocation);
            try
            {
                store.Open(OpenFlags.ReadOnly);
                var certs = store.Certificates.Find(X509FindType.FindByApplicationPolicy, "1.3.6.1.5.5.7.3.1", true);
                var selected = X509Certificate2UI.SelectFromCollection(certs, "Select Certificate", "Select a server certificate for this endpoint", X509SelectionFlag.SingleSelection);

                if (selected.Count > 0)
                    this.Certificate = selected[0];
            }
            finally
            {
                store.Close();
            }
        }

        // Configuration
        private ClientRegistryInterfaceConfiguration m_configuration = new ClientRegistryInterfaceConfiguration("adminsvc");

        /// <summary>
        /// Add a revision panel
        /// </summary>
        public void SetConfiguration(XmlElement wcfConfig, ClientRegistryInterfaceConfiguration configuration)
        {
            this.m_configuration = configuration;
            string behaviorConfigurationName = String.Empty,
                endpointAddress = string.Empty,
                baseAddress = string.Empty,
                bindingName = string.Empty,
                bindingConfigurationName = string.Empty;

            string serviceName = configuration.WcfServiceName;

            // Have everything, now load from XML
            XmlElement serviceElement = wcfConfig.SelectSingleNode(String.Format("./*[local-name() = 'services']/*[local-name() = 'service'][@name = '{0}']", serviceName)) as XmlElement;
            if (serviceElement == null)
                return;
            if (serviceElement.Attributes["behaviorConfiguration"] != null)
                behaviorConfigurationName = serviceElement.Attributes["behaviorConfiguration"].Value;

            XmlElement endpointElement = serviceElement.SelectSingleNode("./*[local-name() = 'endpoint']") as XmlElement,
                hostElement = serviceElement.SelectSingleNode("./*[local-name() = 'host']/*[local-name() = 'baseAddresses']/*[local-name() = 'add']") as XmlElement;
            if (endpointElement == null)
                return; // invalid WCF config with no ep element

            // Base address
            if (hostElement != null && hostElement.Attributes["baseAddress"] != null)
                baseAddress = hostElement.Attributes["baseAddress"].Value;
            // EP element
            if (endpointElement != null)
            {
                if (endpointElement.Attributes["address"] != null)
                    endpointAddress = endpointElement.Attributes["address"].Value;
                if (endpointElement.Attributes["binding"] != null)
                    bindingName = endpointElement.Attributes["binding"].Value;
                if (endpointElement.Attributes["bindingConfiguration"] != null)
                    bindingConfigurationName = endpointElement.Attributes["bindingConfiguration"].Value;
            }

            this.Address = endpointAddress;

            // Behavior
            XmlElement behaviorElement = wcfConfig.SelectSingleNode(String.Format("./*[local-name() = 'behaviors']/*[local-name() = 'serviceBehaviors']/*[local-name() = 'behavior'][@name = '{0}']", behaviorConfigurationName)) as XmlElement;
            if (behaviorElement != null)
            {
                // Service debug?
                string addrScheme = "http";
                try
                {
                    Uri tUri = new Uri(endpointAddress);
                    addrScheme = tUri.Scheme;
                }
                catch { }

                XmlNode serviceDebug = behaviorElement.SelectSingleNode("./*[local-name() = 'serviceDebug']/@includeExceptionDetailInFaults"),
                    serviceMetaData = behaviorElement.SelectSingleNode(String.Format("./*[local-name() = 'serviceMetadata']/@{0}GetEnabled", addrScheme.ToLower()));
                XmlElement credentials = behaviorElement.SelectSingleNode("./*[local-name() = 'serviceCredentials']/*[local-name() = 'serviceCertificate']") as XmlElement;

                if (serviceDebug != null)
                    this.ServiceDebugEnabled = Boolean.Parse(serviceDebug.Value);
                if (serviceMetaData != null)
                    this.ServiceMetaDataEnabled = Boolean.Parse(serviceMetaData.Value);
                if (credentials != null)
                {
                    if (credentials.Attributes["storeName"] != null)
                        this.StoreName = (StoreName)Enum.Parse(typeof(StoreName), credentials.Attributes["storeName"].Value);
                    if (credentials.Attributes["storeLocation"] != null)
                        this.StoreLocation = (StoreLocation)Enum.Parse(typeof(StoreLocation), credentials.Attributes["storeLocation"].Value);
                    if (credentials.Attributes["findValue"] != null && credentials.Attributes["x509FindType"] != null)
                    {
                        X509FindType findType = (X509FindType)Enum.Parse(typeof(X509FindType), credentials.Attributes["x509FindType"].Value);
                        X509Store store = new X509Store(this.StoreName, this.StoreLocation);
                        try
                        {
                            store.Open(OpenFlags.ReadOnly);
                            var certs = store.Certificates.Find(findType, credentials.Attributes["findValue"].Value, false);
                            if (certs.Count == 1)
                                this.Certificate = certs[0];
                            else
                                MessageBox.Show("Could not locate the specified certificate for endpoint");
                        }
                        finally
                        {
                            store.Close();
                        }
                    }
                }

            }

            // Binding
            XmlElement bindingElement = wcfConfig.SelectSingleNode(String.Format("./*[local-name() = 'bindings']/*[local-name() = '{0}']/*[local-name() = 'binding'][@name = '{1}']", bindingName, bindingConfigurationName)) as XmlElement;
            if (bindingElement != null)
            {
                // Client credentials (ignore the rest)
                XmlElement credentials = bindingElement.SelectSingleNode("./*[local-name() = 'security']/*[local-name() = 'transport']") as XmlElement;
                this.RequireClientCerts = credentials != null && credentials.Attributes["clientCredentialType"] != null && credentials.Attributes["clientCredentialType"].Value == "Certificate";
            }


        }
        
    }
}
