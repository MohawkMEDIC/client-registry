using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Xml;

namespace MARC.HI.EHRS.CR.Notification.PixPdq.Configuration
{
    /// <summary>
    /// Configuration section handler for the PIXPDQv3 notification 
    /// </summary>
    /// <example>
    /// <targets>
    ///<add connectionString="servicename=xds" name="MARC-HI XDS Registry">
    ///<notify domain="1.3.6.1.4.1.33349.3.1.2.1.0">
    ///    <action type="Any" asRevise="false"/>
    ///</notify>
    ///<notify domain="1.2.840.114350.1.13.99998.8734">
    ///    <action type="Any" asRevise="false"/>
    ///</notify>
    ///</add>
    ///</targets>
    /// </example>
    public class ConfigurationSectionHandler : IConfigurationSectionHandler
    {
        #region IConfigurationSectionHandler Members

        /// <summary>
        /// Create the configuration section
        /// </summary>
        public object Create(object parent, object configContext, System.Xml.XmlNode section)
        {
            // throw new NotImplementedException();
            var targetsElement = section.SelectNodes("./*[local-name() = 'targets']/*[local-name() = 'add']");
            var retVal = new NotificationConfiguration();

            // Targets element not found
            if (targetsElement == null || targetsElement.Count == 0)
                return retVal;

            // Iterate through the <targets><add> elements and add them to the configuration
            foreach (XmlElement targ in targetsElement)
            {
                // VAlidate that the target has a domain and if not raise an error
                string connectionString = String.Empty;
                if(targ.Attributes["connectionString"] == null)
                    throw new ConfigurationErrorsException("Target must have a connectionString");
                else
                    connectionString = targ.Attributes["connectionString"].Value;

                // Default of target name is the domain, unless a new name is specified
                string targetName = connectionString;
                if (targ.Attributes["name"] != null)
                    targetName = targ.Attributes["name"].Value;

                // Now create the target
                TargetConfiguration targetConfig = new TargetConfiguration(targetName, connectionString);
                
                // Get the notification domains and add them to the configuration
                var notificationElements = targ.SelectNodes("./*[local-name() = 'notify']");
                foreach (XmlElement ne in notificationElements)
                {
                    // Attempt to parse the notification element configuration
                    string notificationDomain = string.Empty;
                    if (ne.Attributes["domain"] == null)
                        throw new ConfigurationErrorsException("Notification element must have a domain");
                    else
                        notificationDomain = ne.Attributes["domain"].Value;

                    NotificationDomainConfiguration notificationConfig = new NotificationDomainConfiguration(notificationDomain);

                    // Parse the actions
                    var actionsElements = ne.SelectNodes("./*[local-name() = 'action']");
                    foreach (XmlElement ae in actionsElements)
                    {

                        // Action types
                        ActionType value = ActionType.Create;
                        if(ae.Attributes["type"] == null)
                            throw new ConfigurationErrorsException("Action element must have a type");
                        else if (!Enum.TryParse(ae.Attributes["type"].Value, out value))
                            throw new ConfigurationErrorsException(String.Format("Invalid action type '{0}'", ae.Attributes["type"].Value));

                        // True as revise
                        bool asRevise = false;
                        if (ae.Attributes["asRevise"] != null && !Boolean.TryParse(ae.Attributes["asRevise"].Value, out asRevise))
                            throw new ConfigurationErrorsException("asRevise element must be a valid boolean");

                        notificationConfig.Actions.Add(new ActionConfiguration(value, asRevise));
                    }

                    targetConfig.NotificationDomain.Add(notificationConfig);
                }

                retVal.Targets.Add(targetConfig);
            }


            return retVal;
        }

        #endregion
    }
}
