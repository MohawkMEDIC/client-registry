/**
 * Copyright 2012-2013 Mohawk College of Applied Arts and Technology
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you 
 * may not use this file except in compliance with the License. You may 
 * obtain a copy of the License at 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations under 
 * the License.
 * 
 * User: fyfej
 * Date: 17-10-2012
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Xml;
using System.Reflection;

namespace MARC.HI.EHRS.CR.Messaging.HL7.Configuration
{
    /// <summary>
    /// Configuration section handler
    /// </summary>
    public class ConfigurationSectionHandler : IConfigurationSectionHandler
    {

        #region IConfigurationSectionHandler Members

        /// <summary>
        /// Create the configuration section
        /// </summary>
        public object Create(object parent, object configContext, System.Xml.XmlNode section)
        {
            HL7ConfigurationSection config = new HL7ConfigurationSection();

            // Read the configuration
            var xConnectionDatas = section.SelectNodes("./*[local-name() = 'services']/*[local-name() = 'service']");
            foreach (XmlElement xConnection in xConnectionDatas)
            {
                if (xConnection.Attributes["address"] != null)
                {
                    ServiceDefinition sd = new ServiceDefinition();
                    if (xConnection.Attributes["name"] != null)
                        sd.Name = xConnection.Attributes["name"].Value;
                    sd.Address = new Uri(xConnection.Attributes["address"].Value);
                    
                    // Timeout
                    TimeSpan timeout = new TimeSpan(0,0,0,0,500);
                    if (xConnection.Attributes["timeout"] != null && !TimeSpan.TryParse(xConnection.Attributes["timeout"].Value, out timeout))
                        throw new ConfigurationErrorsException("Cannot parse the 'timeout' attribute");
                    sd.ReceiveTimeout = timeout;

                    // Attributes
                    var xAttributes = xConnection.SelectNodes("./*[local-name() = 'attribute']");
                    sd.Attributes = new List<KeyValuePair<string, string>>();
                    foreach (XmlElement xAttr in xAttributes)
                    {
                        if (xAttr.Attributes["name"] != null && xAttr.Attributes["value"] != null)
                            sd.Attributes.Add(new KeyValuePair<string, string>(
                                xAttr.Attributes["name"].Value,
                                xAttr.Attributes["value"].Value
                            ));
                    }

                    // Message handlers
                    var xHandlers = xConnection.SelectNodes("./*[local-name() = 'handler']");
                    foreach (XmlElement xHandler in xHandlers)
                    {
                        if (xHandler.Attributes["type"] == null)
                            throw new ConfigurationErrorsException("handler element must have a type attribute");
                        Type hType = Type.GetType(xHandler.Attributes["type"].Value);
                        if (hType == null)
                            throw new ConfigurationErrorsException(String.Format("Cannot find type described by '{0}'", xHandler.Attributes["type"].Value));
                        ConstructorInfo ci = hType.GetConstructor(Type.EmptyTypes);
                        if (ci == null)
                            throw new ConfigurationErrorsException(String.Format("Type '{0}' does not have a parameterless constructor", xHandler.Attributes["type"].Value));
                        IHL7MessageHandler msh = ci.Invoke(null) as IHL7MessageHandler;
                        if (msh == null)
                            throw new ConfigurationErrorsException(String.Format("Type '{0}' must implement '{1}'", hType.FullName, typeof(IHL7MessageHandler).FullName));

                        HandlerDefinition hd = new HandlerDefinition();
                        hd.Handler = msh;
                        // Message types
                        foreach(XmlElement typ in xHandler.ChildNodes)
                            if (typ.Name == "message" && typ.Attributes["name"] != null)
                            {
                                MessageDefinition md = new MessageDefinition()
                                {
                                    Name = typ.Attributes["name"].Value
                                };
                                if (typ.Attributes["isQuery"] != null)
                                    md.IsQuery = Convert.ToBoolean(typ.Attributes["isQuery"].Value);
                                hd.Types.Add(md);
                            }
                        sd.Handlers.Add(hd);
                    }

                    config.Services.Add(sd);
                }
                else
                    throw new ConfigurationErrorsException("PIX configuration listen element must have attribute 'url'");
            }

            return config;
        }

        #endregion
    }
}
