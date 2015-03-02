using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using MARC.HI.EHRS.CR.Core.ComponentModel;

namespace MARC.HI.EHRS.CR.Messaging.Admin
{
    [XmlRoot("registrationEventCollection", Namespace = "urn:marc-hi:svc:componentModel")]
    [XmlType("RegistrationEventCollection", Namespace = "urn:marc-hi:svc:componentModel")]
    public class RegistrationEventCollection 
    {

        /// <summary>
        /// Creates a new instance of the registration event collection
        /// </summary>
        public RegistrationEventCollection()
        {
            this.Event = new List<RegistrationEvent>();
        }

        /// <summary>
        /// Gets or sets count
        /// </summary>
        [XmlAttribute("count")]
        public int Count { get; set; }

        /// <summary>
        /// Registration event collection
        /// </summary>
        [XmlElement("registration")]
        public List<RegistrationEvent> Event { get; set; }

    }
}
