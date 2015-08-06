using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace MARC.HI.EHRS.CR.Core.ComponentModel
{
    /// <summary>
    /// Roles a person can play
    /// </summary>
    [XmlType("PersonRole", Namespace = "urn:marc-hi:svc:componentModel")]

    public enum PersonRole
    {
        /// <summary>
        /// The person is a patient
        /// </summary>
        PAT = 0x01,
        /// <summary>
        /// The person is related to a patient
        /// </summary>
        PRS = 0x02,
        /// <summary>
        /// The person is a tag (source copy)
        /// </summary>
        TAG = 0x04
    }
}
