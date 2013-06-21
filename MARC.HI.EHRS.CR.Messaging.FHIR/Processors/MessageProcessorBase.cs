using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using MARC.Everest.Connectors;
using MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.HI.EHRS.CR.Messaging.FHIR.Util;
using MARC.Everest.DataTypes;
using MARC.Everest.DataTypes.Interfaces;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.Processors
{
    /// <summary>
    /// FHIR message processor base
    /// </summary>
    public abstract class MessageProcessorBase : IFhirMessageProcessor
    {

        #region IFhirMessageProcessor Members

        /// <summary>
        /// Name of resource
        /// </summary>
        public abstract string ResourceName { get; }

        /// <summary>
        /// Type of resource
        /// </summary>
        public abstract Type ResourceType { get; }

        /// <summary>
        /// Type of component
        /// </summary>
        public abstract Type ComponentType { get; }

        /// <summary>
        /// Parse query
        /// </summary>
        public virtual Util.DataUtil.FhirQuery ParseQuery(System.Collections.Specialized.NameValueCollection parameters, List<Everest.Connectors.IResultDetail> dtls)
        {

            MARC.HI.EHRS.CR.Messaging.FHIR.Util.DataUtil.FhirQuery retVal = new Util.DataUtil.FhirQuery();
            retVal.ActualParameters = new System.Collections.Specialized.NameValueCollection();

             for(int i = 0; i < parameters.Count; i++)
                 try
                 {
                     switch (parameters.GetKey(i))
                     {
                         case "stateid":
                             retVal.QueryId = Guid.Parse(parameters.GetValues(i)[0]);
                             retVal.ActualParameters.Add("stateid", retVal.QueryId.ToString());
                             break;
                         case "_count":
                             retVal.Quantity = Int32.Parse(parameters.GetValues(i)[0]);
                             retVal.ActualParameters.Add("_count", retVal.Quantity.ToString());
                             break;
                         case "page":
                             retVal.Start = retVal.Quantity * Int32.Parse(parameters.GetValues(i)[0]);
                             retVal.ActualParameters.Add("page", (retVal.Start / retVal.Quantity).ToString());
                             break;
                         case "confidence":
                             retVal.MinimumDegreeMatch = (float)Decimal.Parse(parameters.GetValues(i)[0]);
                             retVal.ActualParameters.Add("confidence", retVal.MinimumDegreeMatch.ToString());
                             break;
                     }
                 }
                 catch (Exception e)
                 {
                     Trace.TraceError(e.ToString());
                     dtls.Add(new ResultDetail(ResultDetailType.Error, e.Message, e));
                 }
             return retVal;
        }

        /// <summary>
        /// Process a resource
        /// </summary>
        public abstract System.ComponentModel.IComponent ProcessResource(Resources.ResourceBase resource, List<Everest.Connectors.IResultDetail> dtls);
        
        /// <summary>
        /// Process a component
        /// </summary>
        public abstract Resources.ResourceBase ProcessComponent(System.ComponentModel.IComponent component, List<Everest.Connectors.IResultDetail> dtls);


        /// <summary>
        /// Process a name set
        /// </summary>
        protected HumanName ConvertNameSet(NameSet name)
        {
            HumanName retVal = new HumanName();
            retVal.Use = new PrimitiveCode<string>(HackishCodeMapping.ReverseLookup(HackishCodeMapping.NAME_USE, name.Use));

            foreach (var pt in name.Parts)
            {
                switch (pt.Type)
                {
                    case NamePart.NamePartType.Family:
                        retVal.Family.Add(pt.Value);
                        break;
                    case NamePart.NamePartType.Given:
                        retVal.Given.Add(pt.Value);
                        break;
                    case NamePart.NamePartType.Prefix:
                        retVal.Prefix.Add(pt.Value);
                        break;
                    case NamePart.NamePartType.Suffix:
                        retVal.Suffix.Add(pt.Value);
                        break;
                    case NamePart.NamePartType.None:
                        retVal.Text = pt.Value;
                        break;
                }
            }
            return retVal;
        }

        #endregion

        /// <summary>
        /// Convert domain identifier
        /// </summary>
        internal Identifier ConvertDomainIdentifier(DomainIdentifier itm)
        {
            // Attempt to lookup the OID
            var oid = ApplicationContext.ConfigurationService.OidRegistrar.FindData(itm.Domain);
            var retVal = new Identifier() { Key = itm.Identifier };

            if (oid == null)
                retVal.System = new Uri(String.Format("urn:oid:{0}", itm.Domain));
            else if (itm.Domain == "urn:ietf:rfc:3986")
                retVal.System = new Uri(itm.Domain);
            else
                retVal.System = new Uri(oid.Ref != null ? oid.Ref.ToString() : string.Format("urn:oid:{0}", itm.Domain));

            // Assigning auth?
            if (oid != null)
            {
                var assgn = oid.Attributes.Find(o => o.Key == "CustodialOrgName").Value;
                if (!String.IsNullOrEmpty(assgn))
                    retVal.Assigner = new Resources.Resource<Resources.Organization>()
                    {
                        Display = assgn
                    };
                else
                    retVal.Assigner = new Resources.Resource<Resources.Organization>()
                    {
                        Display = oid.Description
                    };
                assgn = oid.Attributes.Find(o => o.Key == "AssigningAuthorityName").Value;
                if (!String.IsNullOrEmpty(assgn))
                    retVal.Label = assgn;
            }

            return retVal;
        }

        /// <summary>
        /// Convert address set
        /// </summary>
        internal List<Address> ConvertAddressSet(AddressSet addr)
        {
            List<Address> retVal = new List<Address>();
            
            foreach (var use in Enum.GetValues(typeof(AddressSet.AddressSetUse)))
            {
                if (((int)use == 0 && addr.Use == 0) ^ ((int)use != 0 && addr.Use.HasFlag((AddressSet.AddressSetUse)use)))
                {
                    Address adEntry = new Address();
                    // An address can have multiple uses
                    adEntry.Use = new PrimitiveCode<string>(HackishCodeMapping.ReverseLookup(HackishCodeMapping.ADDRESS_USE, (AddressSet.AddressSetUse)use));
                    if (adEntry.Use == null || adEntry.Use.Value == null)
                    {
                        adEntry.Use = new PrimitiveCode<string>();
                        adEntry.Use.Extension.Add(ExtensionUtil.CreateADUseExtension((AddressSet.AddressSetUse)use));
                    }

                    foreach (var pt in addr.Parts)
                    {

                        switch (pt.PartType)
                        {
                            case AddressPart.AddressPartType.AddressLine:
                            case AddressPart.AddressPartType.StreetAddressLine:
                                adEntry.Line.Add(pt.AddressValue);
                                break;
                            case AddressPart.AddressPartType.City:
                                adEntry.City = pt.AddressValue;
                                break;
                            case AddressPart.AddressPartType.Country:
                                adEntry.Country = pt.AddressValue;
                                break;
                            case AddressPart.AddressPartType.PostalCode:
                                adEntry.Zip = pt.AddressValue;
                                break;
                            case AddressPart.AddressPartType.State:
                                adEntry.State = pt.AddressValue;
                                break;
                            default: // Can't find a place to put it and don't want to lose data ... so stuff it into an extension
                                adEntry.Extension.Add(ExtensionUtil.CreateADExtension(pt));
                                break;
                        }
                    }
                    retVal.Add(adEntry);
                }
            }
            return retVal;
        }

        /// <summary>
        /// Convert telecom
        /// </summary>
        internal List<Telecom> ConvertTelecom(TelecommunicationsAddress tel)
        {

            var retVal = new List<Telecom>();
            var use = MARC.Everest.Connectors.Util.Convert<SET<CS<TelecommunicationAddressUse>>>(tel.Use);
            foreach (var instance in use)
            {
                // Add telecom
                Telecom telInstance = new Telecom();
                // Convert use adding additional data if needed
                telInstance.Use = new PrimitiveCode<string>(HackishCodeMapping.ReverseLookup(HackishCodeMapping.TELECOM_USE, instance.Code));
                if (telInstance.Use == null || telInstance.Use.Value == null)
                {
                    telInstance.Use = new PrimitiveCode<string>();
                    telInstance.Use.Extension.Add(ExtensionUtil.CreateTELUseExtension((TelecommunicationAddressUse)instance.Code));
                }

                // Set values, etc
                try
                {
                    telInstance.Value = tel.Value;
                    switch (new Uri(telInstance.Value).Scheme)
                    {
                        case "mailto":
                            telInstance.System = new PrimitiveCode<string>("email");
                            break;
                        case "fax":
                            telInstance.System = new PrimitiveCode<string>("fax");
                            break;
                        case "tel":
                            telInstance.System = new PrimitiveCode<string>("phone");
                            break;
                        default:
                            telInstance.System = new PrimitiveCode<string>("url");
                            break;
                    }
                }
                catch { }
                retVal.Add(telInstance);
            }
            return retVal;
        }
    }
}
