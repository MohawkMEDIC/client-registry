using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.Configuration;
using System.Windows.Forms;

namespace MARC.HI.EHRS.SHR.Configurator.SharedHealthCore
{
    public class MessagingConfigurationPanel : IConfigurationPanel
    {
        #region IConfigurationPanel Members

        /// <summary>
        /// Configure the option
        /// </summary>
        /// <param name="configurationDom"></param>
        public void Configure(System.Xml.XmlDocument configurationDom)
        {
            //throw new NotImplementedException();
        }

        /// <summary>
        /// True whether configuration is enabled
        /// </summary>
        public bool EnableConfiguration { get; set; }
        
        /// <summary>
        /// True if the option is configured
        /// </summary>
        public bool IsConfigured(System.Xml.XmlDocument configurationDom)
        {
            return false;
        }

        /// <summary>
        /// Gets the name of the configuration
        /// </summary>
        public string Name
        {
            get { return "Messaging"; }
        }

        /// <summary>
        /// Gets the configuration panel
        /// </summary>
        public System.Windows.Forms.Control Panel
        {
            get { return new Label() { Text = "No Configuration Yet Supported", TextAlign = System.Drawing.ContentAlignment.MiddleCenter, AutoSize = false }; }
        }

        /// <summary>
        /// Un-configure the messaging options
        /// </summary>
        /// <param name="configurationDom"></param>
        public void UnConfigure(System.Xml.XmlDocument configurationDom)
        {
            //throw new NotImplementedException();
        }

        /// <summary>
        /// Validate the messaging
        /// </summary>
        public bool Validate(System.Xml.XmlDocument configurationDom)
        {
            //throw new NotImplementedException();
            return true;
        }

        #endregion
    }
}
