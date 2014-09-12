using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;
using MARC.HI.EHRS.SVC.Messaging.FHIR.Resources;
using MARC.HI.EHRS.SVC.Messaging.FHIR;
using MARC.HI.EHRS.SVC.Messaging.FHIR.Attributes;
using MARC.HI.EHRS.CR.Messaging.FHIR.Util;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.Processors
{
    /// <summary>
    /// Organization message processor
    /// </summary>
    [Profile(ProfileId = "pdqm")]
    [ResourceProfile(Resource = typeof(Organization), Name = "Client registry organization profile")]
    public class OrganizationMessageProcessor : MessageProcessorBase
    {
        /// <summary>
        /// Gets or sets the resource name
        /// </summary>
        public override string ResourceName
        {
            get { return "Organization"; }
        }

        /// <summary>
        /// Gets the resource type
        /// </summary>
        public override Type ResourceType
        {
            get { return typeof(Organization); }
        }

        /// <summary>
        /// Gets the component type
        /// </summary>
        public override Type ComponentType
        {
            get { return typeof(HealthcareParticipant); }
        }

        /// <summary>
        /// Data domain
        /// </summary>
        public override string DataDomain
        {
            get { return ApplicationContext.ConfigurationService.OidRegistrar.GetOid("CR_PID").Oid; }
        }

        /// <summary>
        /// Process a resource
        /// </summary>
        public override System.ComponentModel.IComponent ProcessResource(SVC.Messaging.FHIR.Resources.ResourceBase resource, List<Everest.Connectors.IResultDetail> dtls)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Process a query
        /// </summary>
        //[SearchParameterProfile(Name = "_id", Type = "token", Description = "Client registry assigned id for the organization (one repetition only)")]
        //[SearchParameterProfile(Name = "active", Type = "token", Description = "Whether the organization record is active (one repetition only)")]
        //[SearchParameterProfile(Name = "name", Type = "string", Description = "A portion of the organization's name (only supports OR)")]
        //[SearchParameterProfile(Name = "type", Type = "token", Description = "A code for the type of organization (only supports OR)")]
        public override Util.DataUtil.ClientRegistryFhirQuery ParseQuery(System.Collections.Specialized.NameValueCollection parameters, List<Everest.Connectors.IResultDetail> dtls)
        {
            // Parse a query 
            throw new NotImplementedException();
            return base.ParseQuery(parameters, dtls);
        }

        /// <summary>
        /// Process a component
        /// </summary>
        [ElementProfile(MaxOccurs = 0, Property = "PartOf", Comment = "The client registry does not store detailed organization information")]
        [ElementProfile(MaxOccurs = 1, Property = "Address", Comment = "This client registry can only store limiated address information for organizations")]
        [ElementProfile(Property = "Text", Comment = "Will be auto-generated, any provided will be stored and represented as an extension \"originalText\"")]
        public override SVC.Messaging.FHIR.Resources.ResourceBase ProcessComponent(System.ComponentModel.IComponent component, List<Everest.Connectors.IResultDetail> dtls)
        {
            // Create a component
            HealthcareParticipant ptcpt = component as HealthcareParticipant;
            if (ptcpt.Classifier != HealthcareParticipant.HealthcareParticipantType.Organization)
                ; // Not an organization pass off
            
            // Organization
            Organization retVal = new Organization();
            retVal.Id = ptcpt.Id.ToString();
            retVal.VersionId = ptcpt.Id.ToString();
            
            // Other identifiers
            foreach (var id in ptcpt.AlternateIdentifiers)
                retVal.Extension.Add(ExtensionUtil.CreateIdentificationExtension(id));

            if(ptcpt.Type != null)
                retVal.Type = base.ConvertCode(ptcpt.Type);

            retVal.Name = ptcpt.LegalName.Parts[0].Value;
            retVal.Active = true;

            // Address
            if(ptcpt.PrimaryAddress != null)
                retVal.Address = base.ConvertAddressSet(ptcpt.PrimaryAddress);

            // Telecoms
            if(ptcpt.TelecomAddresses != null)
                foreach (var tel in ptcpt.TelecomAddresses)
                    retVal.Telecom.AddRange(base.ConvertTelecom(tel));

            var contacts = ptcpt.FindAllComponents(SVC.Core.ComponentModel.HealthServiceRecordSiteRoleType.RepresentitiveOf);
            foreach (HealthcareParticipant contact in contacts)
            {
                ContactEntity ce = new ContactEntity();
                
                // Link
                var processor = FhirMessageProcessorUtil.GetComponentProcessor(contact.GetType());
                var processResult = processor.ProcessComponent(contact, dtls);

                if (processResult is Practictioner)
                {
                    var prac = processResult as Practictioner;
                    ce.Name = prac.Name[0];
                    ce.Address = prac.Address[0];
                    ce.Gender = prac.Gender;
                    ce.Telecom = prac.Telecom;
                }

                if(ce.Name != null)
                    ce.Name = base.ConvertNameSet(contact.LegalName);
                if (contact.TelecomAddresses != null)
                    foreach (var t in contact.TelecomAddresses)
                        ce.Telecom.AddRange(base.ConvertTelecom(t));
                if (contact.PrimaryAddress != null)
                    ce.Address = base.ConvertAddressSet(contact.PrimaryAddress)[0];

                retVal.ContactEntity.Add(ce);
            }

            return retVal;

        }
    }
}
