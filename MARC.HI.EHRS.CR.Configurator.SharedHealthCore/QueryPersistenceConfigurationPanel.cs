using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.Configuration;
using System.Windows.Forms;

namespace MARC.HI.EHRS.CR.Configurator.SharedHealthCore
{
    /// <summary>
    /// Query persistence configuration panel
    /// </summary>
    public class QueryPersistenceConfigurationPanel : IDataboundConfigurationPanel
    {
        #region IConfigurationPanel Members

        /// <summary>
        /// Message persistence
        /// </summary>
        public string Name
        {
            get { return "Query Persistence"; }
        }

        /// <summary>
        /// Enable configuration
        /// </summary>
        public bool EnableConfiguration { get; set; }

        /// <summary>
        /// Gets the configuration panel
        /// </summary>
        public System.Windows.Forms.Control Panel
        {
            get { return new Label() { Text = "No Configuration Yet Supported", TextAlign = System.Drawing.ContentAlignment.MiddleCenter, AutoSize = false }; }
        }

        /// <summary>
        /// Configure the option
        /// </summary>
        public void Configure(System.Xml.XmlDocument configurationDom)
        {
            //throw new NotImplementedException();
        }

        /// <summary>
        /// Unconfigure the option
        /// </summary>
        public void UnConfigure(System.Xml.XmlDocument configurationDom)
        {
            //throw new NotImplementedException();
        }

        /// <summary>
        /// Determine if the option is configured
        /// </summary>
        public bool IsConfigured(System.Xml.XmlDocument configurationDom)
        {
            return false;
        }

        /// <summary>
        /// Validate the configuration options
        /// </summary>
        public bool Validate(System.Xml.XmlDocument configurationDom)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IDataboundConfigurationPanel Members

        /// <summary>
        /// Gets or sets the connection string for the message persistence
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the database configurator
        /// </summary>
        public IDatabaseConfigurator DatabaseConfigurator { get; set; }

        #endregion
    }
}
