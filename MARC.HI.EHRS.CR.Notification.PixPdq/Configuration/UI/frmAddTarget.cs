using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Security.Cryptography.X509Certificates;
using MARC.HI.EHRS.SVC.Core.DataTypes;

namespace MARC.HI.EHRS.CR.Notification.PixPdq.Configuration.UI
{
    public partial class frmAddTarget : Form
    {

        /// <summary>
        /// Target configuration
        /// </summary>
        TargetConfiguration m_targetConfiguration;

        // Oid registrar
        private OidRegistrar m_oids;

        /// <summary>
        /// Gets or sets the oid registry for the form
        /// </summary>
        public OidRegistrar OidRegistry {
            get
            {
                return this.m_oids;
            }
            set
            {
                this.m_oids = value;
                this.txtOid.AutoCompleteCustomSource.Clear();
                foreach (var oid in value)
                {
                    txtOid.AutoCompleteCustomSource.Add(oid.Oid);
                    
                    if(oid.Attributes.Exists(o=>o.Key == "AssigningAuthorityName") || oid.Name == "CR_CID")
                        cbxAuthority.Items.Add(oid);
                }
            }
        }

        /// <summary>
        /// Gets or sets the target configuration
        /// </summary>
        public MARC.HI.EHRS.CR.Notification.PixPdq.Configuration.UI.pnlNotification.TargetConfigurationInformation TargetConfiguration
        {
            get
            {
                var ndc = new NotificationDomainConfiguration((cbxAuthority.SelectedItem as OidRegistrar.OidData).Oid);
                ndc.Actions.Add(new ActionConfiguration(((TargetActorType)cbxActor.SelectedItem == TargetActorType.PAT_IDENTITY_X_REF_MGR) ? ActionType.Update : ActionType.Any));

                return new pnlNotification.TargetConfigurationInformation()
                {
                    Address = new Uri(this.txtAddress.Text),
                    ClientCertificate = txtCertificate.Tag as X509Certificate2,
                    ClientCertificateLocation = (StoreLocation)cbxStoreLocation.SelectedItem,
                    ClientCertificateStore = (StoreName)cbxStore.SelectedItem,
                    ValidateServerCert = chkValidateIssuer.Checked,
                    Configuration = new TargetConfiguration(txtName.Text, this.m_targetConfiguration.ConnectionString ?? String.Format("endpointname={0}", Guid.NewGuid().ToString().Substring(0, 6)), (TargetActorType)cbxActor.SelectedItem, txtOid.Text)
                    {
                        NotificationDomain = new List<NotificationDomainConfiguration>() { ndc }
                    }
                };
            }
            set
            {
                this.m_targetConfiguration = value.Configuration;
                txtOid.Text = this.m_targetConfiguration.DeviceIdentifier;
                txtName.Text = this.m_targetConfiguration.Name;
                txtAddress.Text = value.Address.ToString();
                cbxStore.SelectedItem = value.ClientCertificateStore;
                cbxStoreLocation.SelectedItem = value.ClientCertificateLocation;
                cbxActor.SelectedItem = this.m_targetConfiguration.ActAs;

                chkSendClient.Checked = value.ClientCertificate != null;
                txtCertificate.Tag = value.ClientCertificate;
                if(value.ClientCertificate != null) txtCertificate.Text = value.ClientCertificate.Thumbprint;
                cbxStore.SelectedItem = value.ClientCertificateStore;
                cbxStoreLocation.SelectedItem = value.ClientCertificateLocation;
                chkValidateIssuer.Checked = value.ValidateServerCert;
                if(value.Configuration.NotificationDomain != null &&
                    value.Configuration.NotificationDomain.Count > 0)
                    cbxAuthority.SelectedItem = this.OidRegistry.FindData(value.Configuration.NotificationDomain[0].Domain);
            }
        }

        public frmAddTarget()
        {
            InitializeComponent();
            this.InitializeStores();
        }

        private void chkSendClient_CheckedChanged(object sender, EventArgs e)
        {
            grpSSL.Enabled = chkSendClient.Checked;
            txtCertificate.Text = null;
            txtCertificate.Tag = null;
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
            foreach (var sv in Enum.GetValues(typeof(TargetActorType)))
                cbxActor.Items.Add(sv);
            cbxActor.SelectedIndex = cbxStoreLocation.SelectedIndex = cbxStore.SelectedIndex = 0;
        }

        private void btnChooseCert_Click(object sender, EventArgs e)
        {
            var cert = ConfigurationSectionHandler.ChooseCertificate((StoreName)cbxStore.SelectedItem, (StoreLocation)cbxStoreLocation.SelectedItem, true);
            txtCertificate.Text = cert.Thumbprint;
            txtCertificate.Tag = cert;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Close();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            Uri uri = null;
            bool noName = String.IsNullOrEmpty(txtName.Text),
                noOid = String.IsNullOrEmpty(txtOid.Text),
                noAddress = String.IsNullOrEmpty(txtAddress.Text) && Uri.TryCreate(txtAddress.Text, UriKind.Absolute, out uri),
                hasCert = (!chkSendClient.Checked) ^ (txtCertificate.Tag != null);
            if (noName)
                errMain.SetError(txtName, "Endpoint must have a name");
            if (noOid)
                errMain.SetError(txtOid, "Endpoint must have a valid OID");
            if (noAddress)
                errMain.SetError(txtAddress, "Address must be a valid URI");
            if (!hasCert)
                errMain.SetError(grpSSL, "When using client certificates, a client certificate must be selected");

            if (!noName && !noOid && !noAddress && hasCert)
            {
                this.DialogResult = System.Windows.Forms.DialogResult.OK;
                this.Close();
            }
        }

    }
}
