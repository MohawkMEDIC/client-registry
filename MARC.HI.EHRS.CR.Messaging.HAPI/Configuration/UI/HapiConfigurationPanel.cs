using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.Configuration;
using System.Xml.Serialization;
using System.IO;
using System.Reflection;

namespace MARC.HI.EHRS.CR.Messaging.HL7.Configuration.UI
{
    /// <summary>
    /// Represents the HAPI configuration panel
    /// </summary>
    public class HapiConfigurationPanel : IConfigurationPanel
    {

        // Panel
        private pnlHapiConfiguration m_panel = new pnlHapiConfiguration();
        // True if the panel needs a sync with configuration
        private bool m_needsSync = true;
        // Revision template
        private List<HandlerConfigTemplate> m_templates = new List<HandlerConfigTemplate>();
        // configuration
        private HL7ConfigurationSection m_configuration = new HL7ConfigurationSection();

        /// <summary>
        /// todo: default configuration
        /// </summary>
        public HapiConfigurationPanel()
        {
            String etplPath = Path.Combine(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Config"), "Everest");
            XmlSerializer xsz = new XmlSerializer(typeof(HandlerConfigTemplate));
            foreach (var etpFileName in Directory.GetFiles(etplPath))
            {
                try
                {
                    this.m_templates.Add(xsz.Deserialize(File.OpenRead(etpFileName)) as HandlerConfigTemplate);
                }
                catch { }
            }
        }

        #region IConfigurationPanel Members

        /// <summary>
        /// Configure the panel
        /// </summary>
        /// <param name="configurationDom"></param>
        public void Configure(System.Xml.XmlDocument configurationDom)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// True if the configuration is enabled
        /// </summary>
        public bool EnableConfiguration
        {
            get;
            set;
        }

        /// <summary>
        /// True if the option is configured
        /// </summary>
        public bool IsConfigured(System.Xml.XmlDocument configurationDom)
        {
            return false;
        }

        /// <summary>
        /// Ggets the name of the configuration
        /// </summary>
        public string Name
        {
            get { return "Messaging/HAPI Messages"; }
        }

        /// <summary>
        /// Gets the control panel
        /// </summary>
        public System.Windows.Forms.Control Panel
        {
            get { return this.m_panel; }
        }

        /// <summary>
        /// Uninstall the option
        /// </summary>
        public void UnConfigure(System.Xml.XmlDocument configurationDom)
        {
            return;
        }

        /// <summary>
        /// Validate the configuration options
        /// </summary>
        public bool Validate(System.Xml.XmlDocument configurationDom)
        {
            return false;
        }

        #endregion
    }
}
