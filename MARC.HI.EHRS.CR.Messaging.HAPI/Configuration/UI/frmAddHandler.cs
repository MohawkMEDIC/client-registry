using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using MARC.HI.EHRS.CR.Messaging.HL7.TransportProtocol;
using System.Net;

namespace MARC.HI.EHRS.CR.Messaging.HL7.Configuration.UI
{
    public partial class frmAddHandler : Form
    {

        // The constructed and configured transport
        private ServiceDefinition m_serviceDefn;

        /// <summary>
        /// Add handlers
        /// </summary>
        public List<HandlerConfigTemplate> HandlerTemplates
        {
            set
            {
                foreach (var hdlr in value)
                    lstHandlers.Items.Add(hdlr);
            }
        }
        
        /// <summary>
        /// Gets or sets the transport option
        /// </summary>
        public ServiceDefinition ServiceDefinition 
        {
            get
            {
                var currentTx = (TransportOption)cbxTransport.SelectedItem;
                this.m_serviceDefn.Address = new Uri(String.Format("{0}://{1}:{2}", currentTx.TransportType.ProtocolName, GetIPAddress(), txtPort.Value));

                this.m_serviceDefn.Handlers.Clear();
                foreach (HandlerConfigTemplate defn in this.lstHandlers.CheckedItems)
                    this.m_serviceDefn.Handlers.Add(defn.HandlerConfiguration);

                this.m_serviceDefn.Name = txtName.Text;
                this.m_serviceDefn.ReceiveTimeout = TimeSpan.Parse(txtTimeout.Text);
                m_serviceDefn.Attributes = currentTx.TransportType.SerializeConfiguration();

                return this.m_serviceDefn;
            }
            set
            {
                this.m_serviceDefn = value;
                this.txtPort.Value = m_serviceDefn.Address.Port;
                this.txtName.Text = this.m_serviceDefn.Name;
                this.txtTimeout.Text = this.m_serviceDefn.ReceiveTimeout.ToString(@"hh\:mm\:ss");
                this.SetIpText(this.m_serviceDefn.Address.Host);
                // Get the type
                foreach(TransportOption itm in this.cbxTransport.Items)
                    if (itm.TransportType.ProtocolName == m_serviceDefn.Address.Scheme)
                    {
                        cbxTransport.SelectedItem = itm;
                        itm.TransportType.SetupConfiguration(this.m_serviceDefn);
                        pgSettings.SelectedObject = itm.TransportType.ConfigurationObject;
                    }

                // Set handlers
                this.lstHandlers.ClearSelected();
                for (int i = 0; i < lstHandlers.Items.Count; i++)
                {
                    var hdlr = this.lstHandlers.Items[i] as HandlerConfigTemplate;
                    if (this.m_serviceDefn.Handlers.Exists(o => o.HandlerType == hdlr.HandlerConfiguration.HandlerType))
                        this.lstHandlers.SetItemChecked(i, true);
                }
            }
        }

        /// <summary>
        /// Get IP Address
        /// </summary>
        private IPAddress GetIPAddress()
        {
            byte[] address = new byte[4];
            string[] part = txtIp.Text.Split('.');
            for (int i = 0; i < part.Length; i++)
                address[i] = Byte.Parse(part[i]);
            return new IPAddress(address);
        }

        /// <summary>
        /// Transport option
        /// </summary>
        private struct TransportOption
        {
            /// <summary>
            /// Gets the transport
            /// </summary>
            public ITransportProtocol TransportType { get; set; }

            /// <summary>
            /// String representation
            /// </summary>
            public override string ToString()
            {
                var descAtts = this.TransportType.GetType().GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (descAtts.Length > 0)
                    return (descAtts[0] as DescriptionAttribute).Description;
                return TransportType.GetType().Name;
            }
        }

        /// <summary>
        /// Scan transport options
        /// </summary>
        private void ScanTransportOptions()
        {
            foreach (Type t in Array.FindAll(this.GetType().Assembly.GetTypes(), s => s.GetInterface(typeof(ITransportProtocol).FullName) != null))
            {
                var descAtts = t.GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (descAtts.Length > 0)
                    cbxTransport.Items.Add(new TransportOption() { TransportType = Activator.CreateInstance(t) as ITransportProtocol });
            }
        }

        public frmAddHandler()
        {
            InitializeComponent();
            ScanTransportOptions();
        }


        /// <summary>
        /// Pad IP address text
        /// </summary>
        private void SetIpText(String ipAddress)
        {

            IPAddress ipAdd = null;

            StringBuilder fullIp = new StringBuilder();
            foreach (var strP in ipAddress.Split('.'))
            {
                byte b = 0;
                if (Byte.TryParse(strP, out b))
                    fullIp.AppendFormat("{0:000}.", b);
            }
            fullIp.Remove(fullIp.Length - 1, 1);
            txtIp.Text = fullIp.ToString();

        }

        /// <summary>
        /// IP address input helper
        /// </summary>
        private void txtIp_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Decimal)
            {
                // Get the last byte that was entered
                string lastByteText = txtIp.Text,
                    ipAddress = txtIp.Text;

                int byteNumber = txtIp.SelectionStart / 4;
                if (txtIp.SelectionStart % 4 != 0 && byteNumber != 3)
                {
                    
                    lastByteText = lastByteText.Substring(byteNumber * 4, 3).Trim();
                    if (lastByteText.Length != 3)
                    {
                        ipAddress = ipAddress.Remove(byteNumber * 4, 3);
                        ipAddress = ipAddress.Insert(byteNumber * 4, String.Format("{0}{1}", new String('0', 3 - lastByteText.Length), lastByteText));
                        txtIp.Text = ipAddress;
                        txtIp.Select((byteNumber + 1) * 4, 0);
                    }
                }
            }
        }

        private void txtIp_Validated(object sender, EventArgs e)
        {
            this.SetIpText(txtIp.Text);
        }

        private void cbxTransport_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Populate
            var config = (TransportOption)cbxTransport.SelectedItem;
            config.TransportType.SetupConfiguration(this.m_serviceDefn);
            pgSettings.SelectedObject = config.TransportType.ConfigurationObject;

        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            bool isValid = true;
            
            byte b;
            foreach(var str in txtIp.Text.Split('.'))
                isValid &= Byte.TryParse(str, out b);

            if(!isValid)
                errProvider.SetError(txtIp, "Invalid IP Address");
            if(String.IsNullOrEmpty(txtName.Text))
            {
                errProvider.SetError(txtName, "Must supply a name");
                isValid = false;
            }

            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Close();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.Close();

        }

    }
}
