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
using MARC.HI.EHRS.SVC.Core.DataTypes;
using System.ComponentModel;
using System.Xml.Serialization;

namespace MARC.HI.EHRS.CR.Messaging.HL7.Configuration
{

    /// <summary>
    /// Handler definition
    /// </summary>
    [XmlRoot("handler")]
    public class HandlerDefinition
    {

        /// <summary>
        /// Handler defn ctor
        /// </summary>
        public HandlerDefinition()
        {
            this.Types = new List<MessageDefinition>();
        }

        /// <summary>
        /// Type name of the handler
        /// </summary>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [XmlAttribute("type")]
        public string HandlerType
        {
            get
            {
                return this.Handler.GetType().AssemblyQualifiedName;
            }
            set
            {
                this.Handler = Activator.CreateInstance(Type.GetType(value)) as IHL7MessageHandler;
            }
        }

        /// <summary>
        /// Gets or sets the handler
        /// </summary>
        [XmlIgnore]
        public IHL7MessageHandler Handler { get; set; }

        /// <summary>
        /// Message types that trigger this (MSH-9)
        /// </summary>
        [XmlElement("message")]
        public List<MessageDefinition> Types { get; set; }

        /// <summary>
        /// Get the string representation
        /// </summary>
        public override string ToString()
        {
            var descAtts = this.Handler.GetType().GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (descAtts.Length > 0)
                return (descAtts[0] as DescriptionAttribute).Description;
            return this.Handler.GetType().Name;
        }
    }

    /// <summary>
    /// Message definition
    /// </summary>
    public class MessageDefinition
    {

        /// <summary>
        /// Gets or sets the name
        /// </summary>
        [XmlAttribute("name")]
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets a value identifying whether this is a query
        /// </summary>
        [XmlAttribute("isQuery")]
        public bool IsQuery { get; set; }
    }

    /// <summary>
    /// Service definition
    /// </summary>
    public class ServiceDefinition
    {

        /// <summary>
        /// Service defn ctor
        /// </summary>
        public ServiceDefinition()
        {
            this.Handlers = new List<HandlerDefinition>();
            this.Attributes = new List<KeyValuePair<string, string>>();
        }

        /// <summary>
        /// Gets or sets the timeout
        /// </summary>
        public TimeSpan ReceiveTimeout { get; set; }

        /// <summary>
        /// Attributes
        /// </summary>
        public List<KeyValuePair<String, String>> Attributes { get; set; }

        /// <summary>
        /// Gets or sets the address of the service
        /// </summary>
        public Uri Address { get; set; }

        /// <summary>
        /// Gets or sets the name of the defintiion
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the handlers
        /// </summary>
        public List<HandlerDefinition> Handlers { get; set; }
    }

    /// <summary>
    /// Configuration section for the PIX handler
    /// </summary>
    public class HL7ConfigurationSection
    {

        /// <summary>
        /// PIX configuration section
        /// </summary>
        public HL7ConfigurationSection()
        {
            this.Services = new List<ServiceDefinition>();
        }

        /// <summary>
        /// The address to which to bind
        /// </summary>
        /// <remarks>A full Uri is required and must be tcp:// or mllp://</remarks>
        public List<ServiceDefinition> Services { get; private set; }


    }
}
