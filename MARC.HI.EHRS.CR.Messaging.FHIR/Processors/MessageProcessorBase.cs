using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using MARC.Everest.Connectors;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.HI.EHRS.CR.Messaging.FHIR.Util;
using MARC.Everest.DataTypes;
using MARC.Everest.DataTypes.Interfaces;
using MARC.HI.EHRS.SVC.Messaging.FHIR.Handlers;
using MARC.HI.EHRS.SVC.Messaging.FHIR.DataTypes;
using MARC.HI.EHRS.SVC.Messaging.FHIR;
using MARC.HI.EHRS.SVC.Messaging.FHIR.Resources;
using MARC.HI.EHRS.SVC.Messaging.FHIR.Attributes;
using MARC.HI.EHRS.SVC.Core.Services;
using System.ComponentModel;
using MARC.HI.EHRS.SVC.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.Issues;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.Processors
{
    /// <summary>
    /// FHIR message processor base
    /// </summary>
    public abstract class MessageProcessorBase : IFhirMessageProcessor, IFhirResourceHandler
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
        /// Gets the name of the Oid for data persistence load operation
        /// </summary>
        public abstract String DataDomain { get; }

        /// <summary>
        /// Parse query
        /// </summary>
        [SearchParameterProfile(Name = "stateid", Type = "string", Description = "A unique identifier for the state of a query being continued")]
        [SearchParameterProfile(Name = "_count", Type = "integer", Description = "The number of results to return in one page")]
        [SearchParameterProfile(Name = "page", Type = "integer", Description = "The page number of results to return")]
        [SearchParameterProfile(Name = "confidence", Type = "integer", Description = "The confidence of the returned results (0..100)")]
        public virtual MARC.HI.EHRS.CR.Messaging.FHIR.Util.DataUtil.ClientRegistryFhirQuery ParseQuery(System.Collections.Specialized.NameValueCollection parameters, List<Everest.Connectors.IResultDetail> dtls)
        {

            MARC.HI.EHRS.CR.Messaging.FHIR.Util.DataUtil.ClientRegistryFhirQuery retVal = new Util.DataUtil.ClientRegistryFhirQuery();
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
                             retVal.MinimumDegreeMatch = Int32.Parse(parameters.GetValues(i)[0]) / 100.0f;
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
        public abstract System.ComponentModel.IComponent ProcessResource(ResourceBase resource, List<Everest.Connectors.IResultDetail> dtls);
        
        /// <summary>
        /// Process a component
        /// </summary>
        public abstract ResourceBase ProcessComponent(System.ComponentModel.IComponent component, List<Everest.Connectors.IResultDetail> dtls);


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
        /// Convert a code
        /// </summary>
        internal CodeableConcept ConvertCode(CodeValue codeValue)
        {
            ITerminologyService itermSvc = ApplicationContext.CurrentContext.GetService(typeof(ITerminologyService)) as ITerminologyService;
            if(itermSvc != null)
                codeValue = itermSvc.FillInDetails(codeValue);

            // Attempt to lookup the OID
            var oid = ApplicationContext.ConfigurationService.OidRegistrar.FindData(codeValue.CodeSystem);
            var retVal = new CodeableConcept() ;
            var coding = new Coding();

            // Code system
            if (oid == null)
                coding.System = new Uri(String.Format("urn:oid:{0}", codeValue.CodeSystem));
            else if (codeValue.CodeSystem == "urn:ietf:rfc:3986")
                coding.System = new Uri(codeValue.CodeSystem);
            else
                coding.System = new Uri(oid.Ref != null ? oid.Ref.ToString() : string.Format("urn:oid:{0}", codeValue.CodeSystem));
            
            // Display name
            coding.Display = codeValue.DisplayName;

            // Mnemonic
            coding.Code = new PrimitiveCode<string>(codeValue.Code);

            return retVal;
            
        }


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
                    retVal.Assigner = new Resource<Organization>()
                    {
                        Display = assgn
                    };
                else
                    retVal.Assigner = new Resource<Organization>()
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



        #region IFhirResourceHandler Members

        /// <summary>
        /// Create a resource
        /// </summary>
        public SVC.Messaging.FHIR.FhirOperationResult Create(SVC.Messaging.FHIR.Resources.ResourceBase target, SVC.Core.Services.DataPersistenceMode mode)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Delete a resource
        /// </summary>
        public SVC.Messaging.FHIR.FhirOperationResult Delete(string id, SVC.Core.Services.DataPersistenceMode mode)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Query data from the client registry
        /// </summary>
        public SVC.Messaging.FHIR.FhirQueryResult Query(System.Collections.Specialized.NameValueCollection parameters)
        {
            FhirQueryResult result = new FhirQueryResult();
            result.Details = new List<IResultDetail>();

            // Get query parameters
            var resourceProcessor = FhirMessageProcessorUtil.GetMessageProcessor(this.ResourceName);

            // Process incoming request
            var queryObject = resourceProcessor.ParseQuery(parameters, result.Details);
            result.Query = queryObject;

            // sanity check
            if (result.Query.ActualParameters.Count == 0)
            {
                result.Outcome = ResultCode.Rejected;
                result.Details.Add(new ValidationResultDetail(ResultDetailType.Error, ApplicationContext.LocalizationService.GetString("MSGE077"), null, null));
            }
            else if (result.Details.Exists(o => o.Type == ResultDetailType.Error))
            {
                result.Outcome = ResultCode.Error;
                result.Details.Add(new ResultDetail(ResultDetailType.Error, ApplicationContext.LocalizationService.GetString("MSGE00A"), null, null));
            }
            else if (queryObject.Filter == null || result.Outcome != ResultCode.Accepted)
                throw new InvalidOperationException("Could not process query parameters!");
            else if (result.Query.QueryId == Guid.Empty)
            {
                result.Query.QueryId = Guid.NewGuid();
                result = DataUtil.Query(queryObject, result.Details);
            }
            else
                ; // todo: 

            return result;
        }

        /// <summary>
        /// Read a patint resource
        /// </summary>
        public SVC.Messaging.FHIR.FhirOperationResult Read(string id, string versionId)
        {
            FhirOperationResult result = new FhirOperationResult();
            result.Details = new List<IResultDetail>();
            result.Results = new List<ResourceBase>();

            // Data persistence service
            IDataPersistenceService dataPersistence = ApplicationContext.CurrentContext.GetService(typeof(IDataPersistenceService)) as IDataPersistenceService;
            var container = dataPersistence.GetContainer(new VersionedDomainIdentifier()
            {
                Domain = this.DataDomain,
                Identifier = id,
                Version = String.IsNullOrEmpty(versionId) ? null : versionId
            }, String.IsNullOrEmpty(versionId));

            // Container was not found
            if (container == null)
                result.Outcome = ResultCode.NotAvailable;
            else
            { 

                var processor = FhirMessageProcessorUtil.GetComponentProcessor(container.GetType());

                // Was there a history? 
                if (versionId == null)
                    result.Results.Add(processor.ProcessComponent(container as IComponent, result.Details));
                else if (versionId == String.Empty) // Get all versions
                    while (container != null)
                    {
                        var hsrc = container as HealthServiceRecordContainer;
                        var resource = processor.ProcessComponent(container as IComponent, result.Details);

                        if(hsrc.IsMasked) // record is masked so add a detected issue
                            result.Issues.Add(new SVC.Core.Issues.DetectedIssue() {
                                MitigatedBy = ManagementType.OtherActionTaken,
                                Severity = IssueSeverityType.Moderate,
                                Text = String.Format("{0}/history/@{1} will not be returned as it has been masked", resource.Id, resource.VersionId),
                                Type = IssueType.DetectedIssue
                            });
                        else
                            result.Results.Add(resource);
                        container = hsrc.FindComponent(HealthServiceRecordSiteRoleType.ReplacementOf) as IContainer;
                    }
                else // Some version
                    while (container != null)
                    {
                        var hsrc = container as HealthServiceRecordContainer;
                        var resource = processor.ProcessComponent(container as IComponent, result.Details);
                        container = hsrc.FindComponent(HealthServiceRecordSiteRoleType.ReplacementOf) as IContainer;

                        if (resource.VersionId.ToString() != versionId) continue;

                        if (hsrc.IsMasked) // record is masked so add a detected issue
                            result.Issues.Add(new SVC.Core.Issues.DetectedIssue()
                            {
                                MitigatedBy = ManagementType.OtherActionTaken,
                                Severity = IssueSeverityType.Moderate,
                                Text = String.Format("{0}/history/@{1} will not be returned as it has been masked", resource.Id, resource.VersionId),
                                Type = IssueType.DetectedIssue
                            });
                        else
                            result.Results.Add(resource);
                    }
                result.Outcome = ResultCode.Accepted;
            }
            return result;
        }

        public SVC.Messaging.FHIR.FhirOperationResult Update(string id, SVC.Messaging.FHIR.Resources.ResourceBase target, SVC.Core.Services.DataPersistenceMode mode)
        {
            throw new NotImplementedException();
        }

        public SVC.Messaging.FHIR.FhirOperationResult Validate(string id, SVC.Messaging.FHIR.Resources.ResourceBase target)
        {
            throw new NotImplementedException();
        }

        #endregion

    }
}
