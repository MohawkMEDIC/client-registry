using System;
using System.Collections.Generic;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using MARC.Everest.Connectors;
using MARC.Everest.DataTypes;
using MARC.Everest.RMIM.CA.R020402.Vocabulary;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.Everest.DataTypes.Interfaces;
using System.ComponentModel;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;
using MARC.HI.EHRS.SVC.Core.ComponentModel;
using MARC.Everest.DataTypes.Primitives;

namespace MARC.HI.EHRS.CR.Messaging.Everest
{
    /// <summary>
    /// Component utility for converting components to message parts
    /// </summary>
	public partial class DeComponentUtil : IUsesHostContext
	{

        // Invalid classifier
        private const string ERR_EVENT_CLASSIFIER = "Event cannot be translated to messaging format, invalid classifier code";

        /// <summary>
        /// Terminology service
        /// </summary>
        ITerminologyService m_terminologyService = null;


        /// <summary>
        /// Create service delivery location
        /// </summary>
        private MARC.Everest.RMIM.CA.R020402.COCT_MT011001CA.ServiceDeliveryLocation CreateServiceDeliveryLocation3(ServiceDeliveryLocation location, List<IResultDetail> dtls)
        {
            MARC.Everest.RMIM.CA.R020402.COCT_MT011001CA.ServiceDeliveryLocation retVal = new MARC.Everest.RMIM.CA.R020402.COCT_MT011001CA.ServiceDeliveryLocation();
            retVal.Code = CreateCV<ServiceDeliveryLocationRoleType>(location.LocationType, dtls);
            return retVal;
        }

        /// <summary>
        /// Create an annotation
        /// </summary>
        private MARC.Everest.RMIM.CA.R020402.COCT_MT120600CA.Annotation CreateAnnotation(Annotation annotation, List<IResultDetail> dtls)
        {
            var retVal = new MARC.Everest.RMIM.CA.R020402.COCT_MT120600CA.Annotation();
            HealthcareParticipant author = annotation.FindComponent(HealthServiceRecordSiteRoleType.AuthorOf) as HealthcareParticipant;

            // Populate text
            retVal.Text = annotation.Text;
            
            // Populate language
            retVal.Text.Language = annotation.Language;

            // Populate author
            if (author != null)
            {
                retVal.Author = new MARC.Everest.RMIM.CA.R020402.COCT_MT120600CA.Author(
                    annotation.Timestamp,
                    null);
                if (author.Classifier == HealthcareParticipant.HealthcareParticipantType.Organization)
                    retVal.Author.SetAssignedPerson(CreateAssignedEntityOrganization(author, dtls));
                else if (author.Classifier == HealthcareParticipant.HealthcareParticipantType.Person)
                    retVal.Author.SetAssignedPerson(CreateAssignedEntityPerson(author, dtls));
                else
                {
                    dtls.Add(new NotImplementedResultDetail(ResultDetailType.Error, "Can't create annotation author as it is neither a person nor an organization", null));
                    return null;
                }
            }
            return retVal;
        }
       
        /// <summary>
        /// Create author alternative
        /// </summary>
        private MARC.Everest.RMIM.CA.R020402.REPC_MT220003CA.Author3 CreateAuthor(IComponent aut, List<IResultDetail> dtls)
        {
            MARC.Everest.RMIM.CA.R020402.REPC_MT220003CA.Author3 retVal = new MARC.Everest.RMIM.CA.R020402.REPC_MT220003CA.Author3();

            // Determine the type of entity
            if (aut is HealthcareParticipant)
                switch ((aut as HealthcareParticipant).Classifier)
                {
                    case HealthcareParticipant.HealthcareParticipantType.Person:
                        retVal.SetActingPerson(CreateAssignedEntityPerson(aut as HealthcareParticipant, dtls));
                        return retVal;
                    case HealthcareParticipant.HealthcareParticipantType.Organization:
                        retVal.SetActingPerson(CreateAssignedEntityOrganization(aut as HealthcareParticipant, dtls));
                        return retVal;
                }
            else if (aut is PersonalRelationship)
                retVal.SetActingPerson(CreatePersonalRelationship(aut as PersonalRelationship, dtls)); // TODO: Personal Relationship

            return null;
        }

        /// <summary>
        /// Create a service delivery location
        /// </summary>
        private MARC.Everest.RMIM.CA.R020402.COCT_MT240007CA.ServiceDeliveryLocation CreateServiceDeliveryLocation2(ServiceDeliveryLocation sdl, List<IResultDetail> dtls)
        {
            // Get the configuration service
            ISystemConfigurationService configService = Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;
            MARC.Everest.RMIM.CA.R020402.COCT_MT240007CA.ServiceDeliveryLocation retVal = new MARC.Everest.RMIM.CA.R020402.COCT_MT240007CA.ServiceDeliveryLocation();

            // Get the site where this component is contained in the contqainer
            HealthServiceRecordSite hsSite = sdl.Site as HealthServiceRecordSite;

            // Find the original identifier that matches the domain we want to process
            var exportId = hsSite.OriginalIdentifier.Find(o => o.Domain.Equals(configService.JurisdictionData.PlaceDomain) && !o.IsLicenseAuthority);
            if (exportId == null)
                exportId = hsSite.OriginalIdentifier[0]; // Use the first available

            retVal.Id = CreateII(exportId, dtls);
            retVal.Code = CreateCV<ServiceDeliveryLocationRoleType>(sdl.LocationType, dtls);
            retVal.Addr = CreateAD(sdl.Address, dtls);
            retVal.Location = new MARC.Everest.RMIM.CA.R020402.COCT_MT240012CA.Place(sdl.Name);
            retVal.SubjectOf.Add(new MARC.Everest.RMIM.CA.R020402.COCT_MT240003CA.Subject()
            {
                NullFlavor = MARC.Everest.DataTypes.NullFlavor.NoInformation
            });

            if (sdl.TelecomAddresses.Count > 0)
            {
                retVal.Telecom = new SET<TEL>();
                foreach (var tel in sdl.TelecomAddresses)
                    retVal.Telecom.Add(CreateTEL(tel, dtls));
            }
            
            return retVal;
        }

        /// <summary>
        /// Create an IVL from the TimeStampSet
        /// </summary>
        public IVL<TS> CreateIVL(TimestampSet timestampSet, List<IResultDetail> dtls)
        {
            if (timestampSet.Parts == null)
                return new IVL<TS>() { NullFlavor = MARC.Everest.DataTypes.NullFlavor.NoInformation };

            // Return value
            IVL<TS> retVal = new IVL<TS>();
            foreach (var part in timestampSet.Parts)
            {
                switch(part.PartType)
                {
                    case TimestampPart.TimestampPartType.HighBound:
                        retVal.High = CreateTS(part, dtls);
                        break;
                    case TimestampPart.TimestampPartType.LowBound:
                        retVal.Low = CreateTS(part, dtls);
                        break;
                    case TimestampPart.TimestampPartType.Standlone:
                        retVal.Value = CreateTS(part, dtls);
                        break;
                    case TimestampPart.TimestampPartType.Width:
                        retVal.Width = (decimal)part.Value.Subtract(DateTime.MinValue).TotalDays;
                        retVal.Width.Unit = "d";
                        break;
                }
            }

            if (retVal.Low != null && retVal.High != null && retVal.Low.Equals(retVal.High))
            {
                retVal.Value = retVal.Low;
                retVal.Low = null;
                retVal.High = null;
            }
            return retVal;
        }

        /// <summary>
        /// Create a timestamp 
        /// </summary>
        public TS CreateTS(TimestampPart part, List<IResultDetail> dtls)
        {
            DatePrecision prec = default(DatePrecision);
            foreach (var kv in ComponentUtil.m_precisionMap)
                if (kv.Value.Equals(part.Precision))
                    prec = kv.Key;
            return new TS(part.Value, prec);
        }

        /// <summary>
        /// Create a CD from the code value supplied
        /// </summary>
        public CD<T> CreateCD<T>(CodeValue codeValue, List<IResultDetail> dtls)
        {
            if (codeValue == null)
                return null; // return new CD<T>() { NullFlavor = MARC.Everest.DataTypes.NullFlavor.NoInformation };


            // Attempt to create the CV
            CD<T> retVal = new CD<T>((T)Util.Convert<T>(codeValue.Code));


            // Fill in details
            if (m_terminologyService != null && (retVal.Code.IsAlternateCodeSpecified ||
                typeof(T) == typeof(String))) 
                codeValue = m_terminologyService.FillInDetails(codeValue);

            if(!String.IsNullOrEmpty(codeValue.CodeSystem))
                retVal.CodeSystem = codeValue.CodeSystem;
            retVal.CodeSystemVersion = codeValue.CodeSystemVersion;
            if (codeValue.DisplayName != null)
                retVal.DisplayName = codeValue.DisplayName;
            else if (codeValue.CodeSystem != null)
                retVal.CodeSystemName = codeValue.CodeSystemName;

            if(codeValue.OriginalText != null)
                retVal.OriginalText = codeValue.OriginalText;
            
            // Qualifiers
            if (codeValue.Qualifies != null)
            {
                retVal.Qualifier = new LIST<CR<T>>();
                foreach (var kv in codeValue.Qualifies)
                    retVal.Qualifier.Add(new CR<T>()
                    {
                        Name = CreateCV<T>(kv.Key, dtls),
                        Value = CreateCD<T>(kv.Value, dtls)
                    });
            }


            return retVal;
        }

        /// <summary>
        /// Create a service delivery location
        /// </summary>
        private MARC.Everest.RMIM.CA.R020402.COCT_MT240003CA.ServiceDeliveryLocation CreateServiceDeliveryLocation(ServiceDeliveryLocation sdl, List<IResultDetail> dtls)
        {
            // Get the configuration service
            ISystemConfigurationService configService = Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;
            MARC.Everest.RMIM.CA.R020402.COCT_MT240003CA.ServiceDeliveryLocation retVal = new MARC.Everest.RMIM.CA.R020402.COCT_MT240003CA.ServiceDeliveryLocation();

            // Get the site where this component is contained in the contqainer
            HealthServiceRecordSite hsSite = sdl.Site as HealthServiceRecordSite;

            // Find the original identifier that matches the domain we want to process
            var exportId = hsSite.OriginalIdentifier.Find(o => o.Domain.Equals(configService.JurisdictionData.PlaceDomain) && !o.IsLicenseAuthority);
            if (exportId == null)
                exportId = hsSite.OriginalIdentifier[0]; // Use the first available

            retVal.Id = CreateII(exportId, dtls);
            retVal.Code = CreateCV<ServiceDeliveryLocationRoleType>(sdl.LocationType, dtls);
            retVal.Addr = CreateAD(sdl.Address, dtls);
            retVal.Location = new MARC.Everest.RMIM.CA.R020402.COCT_MT240012CA.Place(sdl.Name);

            if (sdl.TelecomAddresses.Count > 0)
            {
                retVal.Telecom = new SET<TEL>();
                foreach (var tel in sdl.TelecomAddresses)
                    retVal.Telecom.Add(CreateTEL(tel, dtls));
            }
            else
                retVal.Telecom = new SET<TEL>() { NullFlavor = NullFlavor.NoInformation };

            return retVal;
        }

        /// <summary>
        /// Create an address from the specified address set
        /// </summary>
        public AD CreateAD(AddressSet addressSet, List<IResultDetail> dtls)
        {
            if (addressSet == null)
                return new AD() { NullFlavor = MARC.Everest.DataTypes.NullFlavor.NoInformation };

            AD retVal = new AD();
            foreach (var kv in ComponentUtil.m_addressUseMap)
                if (kv.Value.Equals(addressSet.Use))
                    retVal.Use = new SET<CS<PostalAddressUse>>(kv.Key, CS<PostalAddressUse>.Comparator);            
            foreach(var pt in addressSet.Parts)
                retVal.Part.Add(new ADXP(pt.AddressValue, (AddressPartType)Enum.Parse(typeof(AddressPart.AddressPartType), pt.PartType.ToString())));
            return retVal;
        }

        /// <summary>
        /// Create a responsible party 
        /// </summary>
        private MARC.Everest.RMIM.CA.R020402.REPC_MT230003CA.ResponsibleParty CreateResponsibleParty(HealthcareParticipant rsp, List<IResultDetail> dtls)
        {
            MARC.Everest.RMIM.CA.R020402.REPC_MT230003CA.ResponsibleParty retVal = new MARC.Everest.RMIM.CA.R020402.REPC_MT230003CA.ResponsibleParty();
            if (rsp.Classifier == HealthcareParticipant.HealthcareParticipantType.Organization)
                retVal.SetActingPerson(CreateAssignedEntityOrganization(rsp, dtls));
            else
                retVal.SetActingPerson(CreateAssignedEntityPerson(rsp, dtls));
            return retVal;
        }

        /// <summary>
        /// Create primary information recipient
        /// </summary>
        private MARC.Everest.RMIM.CA.R020402.REPC_MT230003CA.InformationRecipient CreateInformationRecipient(System.ComponentModel.IComponent recptTo, List<IResultDetail> dtls)
        {
            MARC.Everest.RMIM.CA.R020402.REPC_MT230003CA.InformationRecipient retVal = new MARC.Everest.RMIM.CA.R020402.REPC_MT230003CA.InformationRecipient();

            // Determine the type of info recipient
            if (recptTo is HealthcareParticipant)
            {
                var hcRecptTo = recptTo as HealthcareParticipant;
                if (hcRecptTo.Classifier == HealthcareParticipant.HealthcareParticipantType.Person)
                    retVal.SetRecipients(CreateAssignedEntityPerson(recptTo as HealthcareParticipant, dtls));
                else
                    retVal.SetRecipients(CreateAssignedEntityOrganization(recptTo as HealthcareParticipant, dtls)); // organization
            }
            else if (recptTo is PersonalRelationship)
                retVal.SetRecipients(CreatePersonalRelationship(recptTo as PersonalRelationship, dtls));// TODO: Personal relation
            else if (recptTo is ServiceDeliveryLocation)
                retVal.SetRecipients(CreateServiceDeliveryLocation(recptTo as ServiceDeliveryLocation, dtls)); // SDL
            else
            {
                dtls.Add(new NotImplementedResultDetail(ResultDetailType.Warning, "This document contains an information recipient that the current service cannot interpret", null));
                return null;
            }
            return retVal;
        }


        /// <summary>
        /// Create an assigned entity
        /// </summary>
        private MARC.Everest.RMIM.CA.R020402.COCT_MT090108CA.AssignedEntity CreateAssignedEntityPerson(HealthcareParticipant healthcareParticipant, List<IResultDetail> dtls)
        {
            // Get the configuration service
            ISystemConfigurationService configService = Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            // GEt the site where this component is contained in the contqainer
            HealthServiceRecordSite hsSite = healthcareParticipant.Site as HealthServiceRecordSite;
            List<DomainIdentifier> idList = hsSite.OriginalIdentifier ?? healthcareParticipant.AlternateIdentifiers;

            // Create return value and find an appropriate identifier
            MARC.Everest.RMIM.CA.R020402.COCT_MT090108CA.AssignedEntity retVal = new MARC.Everest.RMIM.CA.R020402.COCT_MT090108CA.AssignedEntity();
            List<DomainIdentifier> exportId = idList.FindAll(o => !o.IsLicenseAuthority && o.Domain.Equals(configService.JurisdictionData.ProviderDomain));

            SET<II> autId = null;

            if (exportId.Count == 0)
                autId = CreateIISet(idList.FindAll(o => !o.IsLicenseAuthority), dtls);
            else
                autId = CreateIISet(exportId, dtls);
            
            retVal.Id = autId;
            retVal.Code = CreateCV<HealthCareProviderRoleType>(healthcareParticipant.Type, dtls);
            retVal.AssignedPerson = new MARC.Everest.RMIM.CA.R020402.COCT_MT090108CA.Person(
                    CreatePN(healthcareParticipant.LegalName, dtls), 
                    null
                );

            // Represented organization
            HealthcareParticipant repOrg = healthcareParticipant.FindComponent(HealthServiceRecordSiteRoleType.RepresentitiveOf) as HealthcareParticipant;
            if (repOrg != null)
            {
                // Get the id that we export for providers...
                DomainIdentifier expId = repOrg.AlternateIdentifiers.Find(o => o.Domain.Equals(configService.JurisdictionData.ProviderDomain));
                if (expId == null)
                    expId = repOrg.AlternateIdentifiers[0];
                retVal.RepresentedOrganization = new MARC.Everest.RMIM.CA.R020402.COCT_MT090108CA.Organization(
                    CreateII(expId, dtls),
                    repOrg.LegalName.Parts[0].Value
                    );
            }

            // As healthcare provider
            DomainIdentifier authorityId = idList.Find(o => o.IsLicenseAuthority);
            if (authorityId != null)
                retVal.AssignedPerson.AsHealthCareProvider = new MARC.Everest.RMIM.CA.R020402.COCT_MT090108CA.HealthCareProvider(
                    CreateII(authorityId, dtls)
                );
            
            return retVal;
        }

        /// <summary>
        /// Create an author
        /// </summary>
        private MARC.Everest.RMIM.CA.R020402.REPC_MT230003CA.Author CreateAuthor(HealthcareParticipant aut, List<IResultDetail> dtls)
        {
            MARC.Everest.RMIM.CA.R020402.REPC_MT230003CA.Author retVal = new MARC.Everest.RMIM.CA.R020402.REPC_MT230003CA.Author();

            // Determine the type of entity
            switch (aut.Classifier)
            {
                case HealthcareParticipant.HealthcareParticipantType.Person:
                    retVal.ActingPerson = CreateAssignedEntityPerson(aut as HealthcareParticipant, dtls);
                    return retVal;
                case HealthcareParticipant.HealthcareParticipantType.Organization:
                    retVal.ActingPerson = CreateAssignedEntityOrganization(aut as HealthcareParticipant, dtls);
                    return retVal;
            }
            return null;

        }

        /// <summary>
        /// Create an assigned entity organization
        /// </summary>
        private MARC.Everest.RMIM.CA.R020402.COCT_MT090508CA.AssignedEntity CreateAssignedEntityOrganization(HealthcareParticipant healthcareParticipant, List<IResultDetail> dtls)
        {
            // Get config service
            ISystemConfigurationService configService = Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            // Create return value
            var retVal = new MARC.Everest.RMIM.CA.R020402.COCT_MT090508CA.AssignedEntity();
            retVal.RepresentedOrganization = new MARC.Everest.RMIM.CA.R020402.COCT_MT090508CA.Organization();

            // Find an appropriate identifier to export
            HealthServiceRecordSite hsSite = healthcareParticipant.Site as HealthServiceRecordSite;
            var exportId = hsSite.OriginalIdentifier.Find(o => o.Domain.Equals(configService.JurisdictionData.ProviderDomain) && !o.IsLicenseAuthority);
            II autId = null;

            if (exportId == null)
                autId = CreateII(hsSite.OriginalIdentifier.Find(o => !o.IsLicenseAuthority), dtls);
            else
                autId = CreateII(exportId, dtls);

            // Represented organization data
            retVal.RepresentedOrganization.Id = autId;
            retVal.RepresentedOrganization.Name = healthcareParticipant.LegalName.Parts[0].Value;

            if (healthcareParticipant.Type != null || healthcareParticipant.TelecomAddresses != null)
                retVal.RepresentedOrganization.AssignedOrganization = new MARC.Everest.RMIM.CA.R020402.COCT_MT090108CA.AssignedOrganization();

            // Assigned organization details
            if(healthcareParticipant.Type != null)
                retVal.RepresentedOrganization.AssignedOrganization.Code = CreateCV<String>(healthcareParticipant.Type, dtls);
            if (healthcareParticipant.TelecomAddresses != null)
            {
                retVal.RepresentedOrganization.AssignedOrganization.Telecom = new SET<TEL>();
                foreach (var itm in healthcareParticipant.TelecomAddresses)
                    retVal.RepresentedOrganization.AssignedOrganization.Telecom.Add(CreateTEL(itm, dtls));
            }
            return retVal;
        }

        /// <summary>
        /// Create an Everest TEL from the data model TEL
        /// </summary>
        public TEL CreateTEL(TelecommunicationsAddress tel, List<IResultDetail> dtls)
        {
            var retVal = new TEL()
            {
                Value = tel.Value
            };
            if (tel.Use != null)
                retVal.Use = new SET<CS<TelecommunicationAddressUse>>(
                    (CS<TelecommunicationAddressUse>)Util.FromWireFormat(tel.Use, typeof(CS<TelecommunicationAddressUse>)),
                    CS<TelecommunicationAddressUse>.Comparator);
            return retVal;

        }

        /// <summary>
        /// Create an II from a domain identifier
        /// </summary>
        /// <param name="authorityId"></param>
        /// <param name="dtls"></param>
        /// <returns></returns>
        public II CreateII(DomainIdentifier id, List<IResultDetail> dtls)
        {
            return new II() { Root = id.Domain, Extension = id.Identifier };
        }

        /// <summary>
        /// Create a set of II from a list of DomainIdentifier
        /// </summary>
        public SET<II> CreateIISet(List<DomainIdentifier> identifiers, List<IResultDetail> dtls)
        {
            SET<II> retVal = new SET<II>(identifiers.Count, II.Comparator);
            foreach (var id in identifiers)
                retVal.Add(new II() { Root = id.Domain, Extension = id.Identifier });
            return retVal;
        }

        /// <summary>
        /// Create a person name from the specified name set
        /// </summary>
        public PN CreatePN(MARC.HI.EHRS.SVC.Core.DataTypes.NameSet nameSet, List<IResultDetail> dtls)
        {
            EntityNameUse enUse = EntityNameUse.Legal;
            // TODO: Map EntityNameUse from object model

            PN retVal = new PN();
            retVal.Use = new SET<CS<EntityNameUse>>(enUse);
            
            // Parts
            foreach(var part in nameSet.Parts)
                switch (part.Type)
                {
                    case NamePart.NamePartType.Family:
                        retVal.Part.Add(new ENXP(part.Value, EntityNamePartType.Family));
                        break;
                    case NamePart.NamePartType.Delimeter:
                        retVal.Part.Add(new ENXP(part.Value, EntityNamePartType.Delimiter));
                        break;
                    case NamePart.NamePartType.Given:
                        retVal.Part.Add(new ENXP(part.Value, EntityNamePartType.Given));
                        break;
                    case NamePart.NamePartType.Prefix:
                        retVal.Part.Add(new ENXP(part.Value, EntityNamePartType.Prefix));
                        break;
                    case NamePart.NamePartType.Suffix:
                        retVal.Part.Add(new ENXP(part.Value, EntityNamePartType.Suffix));
                        break;
                    default:
                        dtls.Add(new PersistenceResultDetail(ResultDetailType.Warning, String.Format("Can't represent name part type '{0}' in HL7v3", part.Type), null));
                        break;
                }

            return retVal;
        }

        /// <summary>
        /// Create a CV from the specified domain
        /// </summary>
        public CV<T> CreateCV<T>(MARC.HI.EHRS.SVC.Core.DataTypes.CodeValue codeValue, List<IResultDetail> dtls)
        {

            if (codeValue == null)
                return new CV<T>() { NullFlavor = MARC.Everest.DataTypes.NullFlavor.NoInformation };

            // Attempt to create the CV
            CV<T> retVal = new CV<T>();
            retVal.Code = CodeValue<T>.Parse(codeValue.Code);

            // Fill in details
            if (m_terminologyService != null && (retVal.Code.IsAlternateCodeSpecified ||
                typeof(T) == typeof(String)))
                codeValue = m_terminologyService.FillInDetails(codeValue);

            retVal.CodeSystemVersion = codeValue.CodeSystemVersion;
            if (!String.IsNullOrEmpty(codeValue.CodeSystem))
                retVal.CodeSystem = codeValue.CodeSystem;
            if(codeValue.DisplayName != null)
                retVal.DisplayName = codeValue.DisplayName;
            else if(codeValue.CodeSystem != null)
                retVal.CodeSystemName = codeValue.CodeSystemName;

            if (codeValue.OriginalText != null)
                retVal.OriginalText = codeValue.OriginalText;

            return retVal;

        }
        
        /// <summary>
        /// Create an instance identifier set
        /// </summary>
        /// <param name="versionedDomainIdentifier"></param>
        /// <returns></returns>
        public SET<II> CreateIISet(MARC.HI.EHRS.SVC.Core.DataTypes.VersionedDomainIdentifier id)
        {
            SET<II> retVal = new SET<II>(2, II.Comparator);
            retVal.Add(new II(id.Domain, id.Identifier) { Scope = IdentifierScope.BusinessIdentifier });
            retVal.Add(new II(id.Domain, id.Version) { Scope = IdentifierScope.VersionIdentifier});
            return retVal;

        }



        #region IUsesHostContext Members

        // Host context
        private MARC.HI.EHRS.SVC.Core.HostContext m_context;

        /// <summary>
        /// Gets or sets the context of under which this persister runs
        /// </summary>
        public MARC.HI.EHRS.SVC.Core.HostContext Context
        {
            get
            { return m_context; }
            set
            {
                m_context = value;
                this.m_terminologyService = Context.GetService(typeof(ITerminologyService)) as ITerminologyService;
            }
        }

        #endregion


        /// <summary>
        /// Create a reason structure
        /// </summary>
        private MARC.Everest.RMIM.CA.R020402.REPC_MT230003CA.Reason CreateReason(Reason reason, List<IResultDetail> dtls)
        {
            MARC.Everest.RMIM.CA.R020402.REPC_MT230003CA.Reason retVal = new MARC.Everest.RMIM.CA.R020402.REPC_MT230003CA.Reason();

            if (reason.Status != StatusType.Unknown)
                retVal.SetIndications(new MARC.Everest.RMIM.CA.R020402.COCT_MT120402CA.OtherIndication()
                {
                    Code = CreateCV<ActNonObservationIndicationCode>(reason.ReasonType, dtls),
                    Text = reason.Text
                });
            else
                retVal.SetIndications(new MARC.Everest.RMIM.CA.R020402.COCT_MT120402CA.ObservationProblem(
                    CreateCV<ProblemType>(reason.ReasonType, dtls), 
                    CreateCD<String>(reason.Value, dtls)
                ));
            return retVal;
        }

        /// <summary>
        /// Creates a complex performer
        /// </summary>
        private MARC.Everest.RMIM.CA.R020402.REPC_MT410001CA.Performer CreatePerformerComplex(IComponent performer, List<IResultDetail> dtls)
        {
            MARC.Everest.RMIM.CA.R020402.REPC_MT410001CA.Performer retVal = new MARC.Everest.RMIM.CA.R020402.REPC_MT410001CA.Performer();

            HealthcareParticipant performerPtcpt = performer as HealthcareParticipant;
            // Set the performer
            if (performerPtcpt != null && performerPtcpt.Classifier == HealthcareParticipant.HealthcareParticipantType.Person)
                retVal.SetActingPerson(CreateAssignedEntityPerson(performerPtcpt, dtls));
            else if (performerPtcpt != null && performerPtcpt.Classifier == HealthcareParticipant.HealthcareParticipantType.Organization)
                retVal.SetActingPerson(CreateAssignedEntityOrganization(performerPtcpt, dtls));
            else if (performerPtcpt == null)
                retVal.SetActingPerson(CreatePersonalRelationship(performer as PersonalRelationship, dtls));

            return retVal;
        }

        /// <summary>
        /// Create personal relationshi[
        /// </summary>
        private MARC.Everest.RMIM.CA.R020402.COCT_MT910108CA.PersonalRelationship CreatePersonalRelationship(PersonalRelationship personalRelationship, List<IResultDetail> dtls)
        {
            // Get the configuration service
            ISystemConfigurationService configService = Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;

            // Find the original identifier that matches the domain we want to process
            HealthServiceRecordSite hsSite = personalRelationship.Site as HealthServiceRecordSite;

            List<DomainIdentifier> searchFields = hsSite.OriginalIdentifier;

            if (searchFields == null)
                searchFields = personalRelationship.AlternateIdentifiers;
            
            var exportId = searchFields.Find(o => o.Domain.Equals(configService.JurisdictionData.PlaceDomain) && !o.IsLicenseAuthority);
            if (exportId == null)
                exportId = searchFields[0]; // Use the first available

            // Personal relationship
            MARC.Everest.RMIM.CA.R020402.COCT_MT910108CA.PersonalRelationship retVal = new MARC.Everest.RMIM.CA.R020402.COCT_MT910108CA.PersonalRelationship(
                CreateII(exportId, dtls),
                Util.FromWireFormat(personalRelationship.RelationshipKind, typeof(CV<x_SimplePersonalRelationship>)) as CV<x_SimplePersonalRelationship>, 
                new MARC.Everest.RMIM.CA.R020402.COCT_MT910108CA.Person(
                    CreatePN(personalRelationship.LegalName, dtls)
                )
            );

            // Address
            if (personalRelationship.PerminantAddress != null)
                retVal.RelationshipHolder.Addr = CreateAD(personalRelationship.PerminantAddress, dtls);
            // Telecom
            if (personalRelationship.TelecomAddresses != null)
            {
                retVal.RelationshipHolder.Telecom = new SET<TEL>(personalRelationship.TelecomAddresses.Count);
                foreach (var tel in personalRelationship.TelecomAddresses)
                    retVal.RelationshipHolder.Telecom.Add(CreateTEL(tel, dtls));
            }

            // Return
            return retVal;
        }




        internal MARC.Everest.RMIM.CA.R020402.PRPA_MT101102CA.Person CreatePerson(IComponent verifiedPerson, List<IResultDetail> dtls)
        {
            throw new NotImplementedException();
        }

        internal MARC.Everest.RMIM.CA.R020402.PRPA_MT101102CA.IdentifiedEntity CreateIdentifiedEntity(RegistrationEvent verified, List<IResultDetail> dtls)
        {
            throw new NotImplementedException();
        }
    }
}
