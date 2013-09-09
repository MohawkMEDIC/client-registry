using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.Everest.Connectors;

namespace MARC.HI.EHRS.CR.Messaging.FHIR
{
    /// <summary>
    /// Unsupported FHIR data type property
    /// </summary>
    public class UnsupportedFhirDatatypePropertyResultDetail : UnsupportedDatatypePropertyResultDetail
    {

        /// <summary>
        /// Creates a new instance of the unsupported FHIR property result detail
        /// </summary>
        public UnsupportedFhirDatatypePropertyResultDetail(ResultDetailType type, String propertyName, String datatypeName) : base(type, propertyName, datatypeName, null)
        {

        }
    }
}
