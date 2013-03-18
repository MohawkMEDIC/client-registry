using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.Configuration;

namespace MARC.HI.EHRS.CR.Configurator
{
    public class ClientRegistryAboutPanel : IAlwaysDeployedConfigurationPanel
    {
        #region IConfigurationPanel Members

        private ucAboutClientRegistry m_panel = new ucAboutClientRegistry();
        /// <summary>
        /// Configure the item
        /// </summary>
        public void Configure(System.Xml.XmlDocument configurationDom)
        {
        }

        /// <summary>
        /// Enable configuration
        /// </summary>
        public bool EnableConfiguration
        {
            get;set;
        }

        /// <summary>
        /// Is configured
        /// </summary>
        public bool IsConfigured(System.Xml.XmlDocument configurationDom)
        {
            return true;
        }

        /// <summary>
        /// Get the name
        /// </summary>
        public string Name
        {
            get { return "Client Registry"; }
        }

        /// <summary>
        /// Get the panel
        /// </summary>
        public System.Windows.Forms.Control Panel
        {
            get { return m_panel; }
        }

        /// <summary>
        /// Unconfigure
        /// </summary>
        public void UnConfigure(System.Xml.XmlDocument configurationDom)
        {
        }

        /// <summary>
        /// Validate
        /// </summary>
        public bool Validate(System.Xml.XmlDocument configurationDom)
        {
            return true;
        }

        #endregion
    }
}
