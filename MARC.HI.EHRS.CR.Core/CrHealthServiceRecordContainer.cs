using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.ComponentModel;
using System.Xml.Serialization;
using System.ComponentModel;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;

namespace MARC.HI.EHRS.CR.Core
{
    /// <summary>
    /// Represents a registration event
    /// </summary>
    [Serializable]
    [XmlType("CrHealthServiceRecordContainer")]
    public class CrHealthServiceRecordContainer : HealthServiceRecordContainer
    {
        /// <summary>
        /// Component entries for the Xml Serializer
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Browsable(false)]
        [XmlElement("client", typeof(Person))]
        [XmlElement("healthcareParticipant", typeof(HealthcareParticipant))]
        [XmlElement("changeSummary", typeof(ChangeSummary))]
        [XmlElement("healthServiceRecordComponentRef", typeof(HealthServiceRecordComponentRef))]
        [XmlElement("mask", typeof(MaskingIndicator))]
        [XmlElement("personalRelationship", typeof(PersonalRelationship))]
        [XmlElement("queryRestriction", typeof(QueryRestriction))]
        [XmlElement("serviceDeliveryLocation", typeof(ServiceDeliveryLocation))]
        [XmlElement("extension", typeof(ExtendedAttribute))]
        public override List<HealthServiceRecordComponent> XmlComponents
        {
            get
            {
                return base.XmlComponents;
            }
            set
            {
                base.XmlComponents = value;
            }
        }

    }
}
