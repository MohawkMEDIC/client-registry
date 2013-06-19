using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.CR.Messaging.FHIR.Resources;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.CR.Messaging.FHIR.DataTypes;
using MARC.Everest.Connectors;
using MARC.HI.EHRS.SVC.Core.DataTypes;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.Processors
{
    /// <summary>
    /// Message processor for patients
    /// </summary>
    public class PatientMessageProcessor : MessageProcessorBase, IFhirMessageProcessor
    {
        #region IFhirMessageProcessor Members

        /// <summary>
        /// The name of the resource
        /// </summary>
        public override string ResourceName
        {
            get { return "Patient"; }
        }

        /// <summary>
        /// Gets the type of resource
        /// </summary>
        public override Type ResourceType
        {
            get { return typeof(Patient); }
        }

        /// <summary>
        /// Component type that matches this
        /// </summary>
        public override Type ComponentType
        {
            get { return typeof(Person); }
        }

        /// <summary>
        /// Parse parameters
        /// </summary>
        public override Util.DataUtil.FhirQuery ParseQuery(System.Collections.Specialized.NameValueCollection parameters, List<IResultDetail> dtls)
        {
            ITerminologyService termSvc = ApplicationContext.CurrentContext.GetService(typeof(ITerminologyService)) as ITerminologyService;

            Util.DataUtil.FhirQuery retVal = base.ParseQuery(parameters, dtls);

            var queryFilter = new Person();
            //MARC.HI.EHRS.SVC.Core.DataTypes.AddressSet addressFilter = null;
            MARC.HI.EHRS.SVC.Core.DataTypes.NameSet nameFilter = null;

            for(int i = 0; i < parameters.Count; i++)
                try
                {
                    switch (parameters.GetKey(i))
                    {
                        case "_id":
                            queryFilter.Id = Decimal.Parse(parameters.GetValues(i)[0]);
                            retVal.ActualParameters.Add("_id", queryFilter.Id.ToString());
                            break;
                        case "active":
                            queryFilter.Status = Boolean.Parse(parameters.GetValues(i)[0]) ? StatusType.Active | StatusType.Completed : StatusType.Obsolete | StatusType.Nullified | StatusType.Cancelled | StatusType.Aborted;
                            retVal.ActualParameters.Add("active", (queryFilter.Status == (StatusType.Active | StatusType.Completed)).ToString());
                            break;
                        //case "address.use":
                        //case "address.line":
                        //case "address.city":
                        //case "address.state":
                        //case "address.zip":
                        //case "address.country":
                        //    {
                        //        if (addressFilter == null)
                        //            addressFilter = new SVC.Core.DataTypes.AddressSet();
                        //        string property = parameters.GetKey(i).Substring(parameters.GetKey(i).IndexOf(".") + 1);
                        //        string value = parameters.GetValues(i)[0];

                        //        if(value.Contains(",") || parameters.GetValues(i).Length > 1)
                        //            dtls.Add(new InsufficientRepetitionsResultDetail(ResultDetailType.Warning, "Cannot perform AND or OR on address fields", "address." + property));

                        //        if (property == "use")
                        //        {
                        //            addressFilter.Use = HackishCodeMapping.Lookup(HackishCodeMapping.ADDRESS_USE, value);
                        //            retVal.ActualParameters.Add("address.use", HackishCodeMapping.ReverseLookup(HackishCodeMapping.ADDRESS_USE, addressFilter.Use));
                        //        }
                        //        else
                        //            addressFilter.Parts.Add(new SVC.Core.DataTypes.AddressPart()
                        //            {
                        //                AddressValue = value,
                        //                PartType = HackishCodeMapping.Lookup(HackishCodeMapping.ADDRESS_PART, value)
                        //            });
                        //            retVal.ActualParameters.Add(String.Format("address.{0}", HackishCodeMapping.ReverseLookup(HackishCodeMapping.ADDRESS_PART, addressFilter.Parts.Last().PartType)), value);

                        //        break;
                        //    }
                        case "address": // Address is really messy ... 
                            {
                                queryFilter.Addresses = new List<SVC.Core.DataTypes.AddressSet>();
                                // OR is only supported for this
                                if (parameters.GetValues(i).Length > 1)
                                    dtls.Add(new InsufficientRepetitionsResultDetail(ResultDetailType.Warning, "Cannot perform AND on address", null));

                                // Now values
                                foreach (var adpn in parameters.GetValues(i)[0].Split(','))
                                {
                                    foreach (var kv in HackishCodeMapping.ADDRESS_PART)
                                        queryFilter.Addresses.Add(new SVC.Core.DataTypes.AddressSet()
                                        {
                                            Use = SVC.Core.DataTypes.AddressSet.AddressSetUse.Search,
                                            Parts = new List<SVC.Core.DataTypes.AddressPart>() {
                                                new MARC.HI.EHRS.SVC.Core.DataTypes.AddressPart() {
                                                    PartType = kv.Value,
                                                    AddressValue = adpn
                                                }
                                            }
                                        });
                                    retVal.ActualParameters.Add("address", adpn);
                                }
                                break;
                            }
                        case "birthdate":
                            {
                                string value = parameters.GetValues(i)[0];
                                if (value.Contains(","))
                                    value = value.Substring(0, value.IndexOf(","));
                                else if (parameters.GetValues(i).Length > 1)
                                    dtls.Add(new InsufficientRepetitionsResultDetail(ResultDetailType.Warning, "Cannot perform AND on birthdate", null));

                                var dValue = new DateOnly() { Value = value };
                                queryFilter.BirthTime = new SVC.Core.DataTypes.TimestampPart()
                                {
                                    Value = dValue.DateValue.Value,
                                    Precision = HackishCodeMapping.ReverseLookup(HackishCodeMapping.DATE_PRECISION, dValue.Precision)
                                };
                                retVal.ActualParameters.Add("birthdate", dValue.XmlValue);
                                break;
                            }
                        case "family":
                            {
                                if (nameFilter == null)
                                    nameFilter = new SVC.Core.DataTypes.NameSet() { Use = SVC.Core.DataTypes.NameSet.NameSetUse.Search };

                                foreach (var nm in parameters.GetValues(i))
                                {
                                    if (nm.Contains(",")) // Cannot do an OR on Name
                                        dtls.Add(new InsufficientRepetitionsResultDetail(ResultDetailType.Warning, "Cannot perform OR on family", null));

                                    nameFilter.Parts.Add(new SVC.Core.DataTypes.NamePart()
                                    {
                                        Type = SVC.Core.DataTypes.NamePart.NamePartType.Family,
                                        Value = parameters.GetValues(i)[0]
                                    });
                                    retVal.ActualParameters.Add("family", nm);
                                }
                                break;
                            }
                        case "given":
                            {

                                if (nameFilter == null)
                                    nameFilter = new SVC.Core.DataTypes.NameSet() { Use = SVC.Core.DataTypes.NameSet.NameSetUse.Search };
                                foreach (var nm in parameters.GetValues(i))
                                {
                                    if (nm.Contains(",")) // Cannot do an OR on Name
                                        dtls.Add(new InsufficientRepetitionsResultDetail(ResultDetailType.Warning, "Cannot perform OR on given", null));
                                    nameFilter.Parts.Add(new SVC.Core.DataTypes.NamePart()
                                    {
                                        Type = SVC.Core.DataTypes.NamePart.NamePartType.Given,
                                        Value = nm
                                    });
                                    retVal.ActualParameters.Add("given", nm);
                                }
                                break;
                            }
                        case "gender":
                            {
                                string value = parameters.GetValues(i)[0].ToUpper();
                                if (value.Contains(",") || parameters.GetValues(i).Length > 1)
                                    dtls.Add(new InsufficientRepetitionsResultDetail(ResultDetailType.Warning, "Cannot perform OR or AND on gender", null));

                                var gCode = FhirMessageProcessorUtil.CodeFromToken(value);
                                if (gCode.Code == "UNK") // Null Flavor
                                    retVal.ActualParameters.Add("gender", String.Format("http://hl7.org/fhir/v3/NullFlavor!UNK"));
                                else if (!new List<String>() { "M", "F", "UN" }.Contains(gCode.Code))
                                    dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, String.Format("Cannot find code {0} in administrative gender", gCode.Code), null));

                                else
                                {
                                    queryFilter.GenderCode = gCode.Code;
                                    retVal.ActualParameters.Add("gender", String.Format("http://hl7.org/fhir/v3/AdministrativeGender!{0}", gCode.Code));
                                }
                                break;
                            }
                        case "identifier":
                            {
                                if (parameters.GetValues(i).Length > 1)
                                    dtls.Add(new InsufficientRepetitionsResultDetail(ResultDetailType.Warning, "Cannot perform AND on identifiers", null));

                                if (queryFilter.AlternateIdentifiers == null)
                                    queryFilter.AlternateIdentifiers = new List<SVC.Core.DataTypes.DomainIdentifier>();
                                foreach (var val in parameters.GetValues(i)[0].Split(','))
                                {
                                    queryFilter.AlternateIdentifiers.Add(FhirMessageProcessorUtil.IdentifierFromToken(val));
                                    retVal.ActualParameters.Add("identifier", val);
                                }
                                break;
                            }
                        case "provider.identifier": // maps to the target domains ? 
                            {

                                foreach (var val in parameters.GetValues(i)[0].Split(','))
                                {
                                    var did = new DomainIdentifier() { Domain = FhirMessageProcessorUtil.IdentifierFromToken(val).Domain };
                                    if (String.IsNullOrEmpty(did.Domain))
                                    {
                                        dtls.Add(new NotSupportedChoiceResultDetail(ResultDetailType.Warning, "Provider organization identifier unknown", null));
                                        continue;
                                    }

                                    queryFilter.AlternateIdentifiers.Add(did);
                                    retVal.ActualParameters.Add("provider.identifier", String.Format("{0}!", FhirMessageProcessorUtil.TranslateCrDomain(did.Domain)));
                                }
                                break;

                            }
                        default:
                            dtls.Add(new NotSupportedChoiceResultDetail(ResultDetailType.Warning, String.Format("{0} is not a supported query parameter", parameters.GetKey(i)), null));
                            break;
                    }
                }
                catch (Exception e)
                {
                    dtls.Add(new ResultDetail(ResultDetailType.Error, string.Format("Unable to process parameter {0} due to error {1}", parameters.Get(i), e.Message), e));
                }

            // Add a name filter?
            if(nameFilter != null)
                queryFilter.Names = new List<SVC.Core.DataTypes.NameSet>() { nameFilter };

            retVal.Filter = queryFilter;

            return retVal;
        }

        /// <summary>
        /// Process resource
        /// </summary>
        public override System.ComponentModel.IComponent ProcessResource(Resources.ResourceBase resource, List<IResultDetail> dtls)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Process components
        /// </summary>
        /// TODO: make this more robust
        public override Resources.ResourceBase ProcessComponent(System.ComponentModel.IComponent component, List<IResultDetail> dtls)
        {
            // Setup references
            Patient retVal = new Patient();
            Person person = component as Person;

            retVal.Id = person.Id;
            retVal.VersionId = person.VersionId;
            retVal.Active = new FhirBoolean((person.Status & (StatusType.Active | StatusType.Completed)) != null);

            // Deceased time
            if (person.DeceasedTime != null)
            {
                retVal.DeceasedDate = new Date(person.DeceasedTime.Value);
                retVal.DeceasedDate.Precision = HackishCodeMapping.Lookup(HackishCodeMapping.DATE_PRECISION, person.DeceasedTime.Precision);
            }

            // Identifiers
            foreach(var itm in person.AlternateIdentifiers)
                retVal.Identifier.Add(new Identifier() {
                    System = new FhirUri(new Uri(FhirMessageProcessorUtil.TranslateCrDomain(itm.Domain))),
                    Key = new FhirString(itm.Identifier)
                });

            // Birth order
            if (person.BirthOrder.HasValue)
                retVal.MultipleBirth = new FhirInt(person.BirthOrder.Value);

            return retVal;
        }

        #endregion
    }
}
