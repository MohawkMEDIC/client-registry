using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.Everest.Connectors;
using MARC.Everest.RMIM.CA.R020402.Interactions;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;
using MARC.HI.EHRS.SVC.Core.ComponentModel;
using MARC.Everest.RMIM.CA.R020402.Vocabulary;
using MARC.Everest.DataTypes;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using System.ComponentModel;

namespace MARC.HI.EHRS.CR.Messaging.Everest
{
    /// <summary>
    /// Component utility
    /// </summary>
    public partial class CaComponentUtil : ComponentUtil
    {
        
        /// <summary>
        /// Create client component
        /// </summary>
        public Client CreateClientComponent(MARC.Everest.RMIM.CA.R020402.COCT_MT050207CA.Patient patient, List<IResultDetail> dtls)
        {
            // Patient is null
            if (patient == null || patient.NullFlavor != null)
            {
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error,
                    this.m_localeService.GetString("MSGE028"), null));
                return null;
            }

            Client retVal = new Client();

            // Resolve id
            if (patient.Id == null || patient.Id.NullFlavor != null ||
                patient.Id.IsEmpty || patient.Id.Count(o => o.Use == IdentifierUse.Business) == 0)
            {
                dtls.Add(new ResultDetail(ResultDetailType.Error,
                    this.m_localeService.GetString("MSGE029"),
                    null, null));
                return null;
            }

            // Alternative identifiers
            retVal.AlternateIdentifiers.AddRange(CreateDomainIdentifierList(patient.Id, dtls));

            // Client Legal Name
            PN legalName = patient.PatientPerson.Name;
            if (legalName == null || legalName.IsNull)
                dtls.Add(new ResultDetail(ResultDetailType.Warning, this.m_localeService.GetString("MSGE02A"), null, null));
            else
                retVal.LegalName = CreateNameSet(legalName, dtls);

            // Client telecom
            if (patient.Telecom != null &&
                !patient.Telecom.IsNull)
                foreach (TEL tel in patient.Telecom)
                    retVal.TelecomAddresses.Add(new TelecommunicationsAddress()
                    {
                        Value = tel.Value,
                        Use = tel.Use == null ? null : Util.ToWireFormat(tel.Use)
                    });

            AD addr = patient.Addr;
            if (addr != null && !addr.IsNull)
                retVal.PerminantAddress = CreateAddressSet(addr, dtls);

            return retVal;
        }

        /// <summary>
        /// Create a healthcare participant component
        /// </summary>
        protected HealthcareParticipant CreateParticipantComponent(MARC.Everest.RMIM.CA.R020402.COCT_MT090508CA.AssignedEntity assignedEntity, List<IResultDetail> dtls)
        {
            HealthcareParticipant retVal = new HealthcareParticipant();
            retVal.Classifier = HealthcareParticipant.HealthcareParticipantType.Organization;

            // Check for id
            if (assignedEntity.RepresentedOrganization == null || assignedEntity.RepresentedOrganization.NullFlavor != null
             || assignedEntity.RepresentedOrganization.Id == null || assignedEntity.RepresentedOrganization.Id.IsNull)
            {
                dtls.Add(new ResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE02B"), null, null));
                return null;
            }

            // Add identifier
            retVal.AlternateIdentifiers.Add(new DomainIdentifier()
            {
                Domain = assignedEntity.RepresentedOrganization.Id.Root,
                Identifier = assignedEntity.RepresentedOrganization.Id.Extension
            });

            // Legal name
            if (assignedEntity.RepresentedOrganization.Name != null &&
                !assignedEntity.RepresentedOrganization.Name.IsNull)
            {
                retVal.LegalName = new NameSet();
                retVal.LegalName.Use = NameSet.NameSetUse.Legal;
                retVal.LegalName.Parts.Add(new NamePart()
                {
                    Value = assignedEntity.RepresentedOrganization.Name,
                    Type = NamePart.NamePartType.Given
                });
            }

            // Organization type
            if (assignedEntity.RepresentedOrganization.AssignedOrganization != null &&
                assignedEntity.RepresentedOrganization.AssignedOrganization.NullFlavor == null)
            {
                if (assignedEntity.RepresentedOrganization.AssignedOrganization.Code == null || assignedEntity.RepresentedOrganization.AssignedOrganization.Code.IsNull)
                    dtls.Add(new ResultDetail(ResultDetailType.Warning, this.m_localeService.GetString("MSGW00F"), null, null));
                else
                    retVal.Type = CreateCodeValue<String>(assignedEntity.RepresentedOrganization.AssignedOrganization.Code, dtls);

                // Telecommunications
                if (assignedEntity.RepresentedOrganization.AssignedOrganization.Telecom != null &&
                    !assignedEntity.RepresentedOrganization.AssignedOrganization.Telecom.IsEmpty)
                    foreach (var tel in assignedEntity.RepresentedOrganization.AssignedOrganization.Telecom)
                        retVal.TelecomAddresses.Add(new TelecommunicationsAddress()
                        {
                            Use = tel.Use == null ? null : Util.ToWireFormat(tel.Use),
                            Value = tel.Value
                        });
            }

            return retVal;
        }

        /// <summary>
        /// Create a personal relationship component
        /// </summary>
        protected PersonalRelationship CreatePersonalRelationshipComponent(MARC.Everest.RMIM.CA.R020402.COCT_MT910108CA.PersonalRelationship personalRelationship, List<IResultDetail> dtls)
        {
            PersonalRelationship retVal = new PersonalRelationship();

            // Identifier
            if (personalRelationship.Id == null || personalRelationship.Id.IsNull)
            {
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE02C"), null));
                return null;
            }
            retVal.AlternateIdentifiers.Add(new DomainIdentifier()
            {
                Domain = personalRelationship.Id.Root,
                Identifier = personalRelationship.Id.Extension
            });

            // Name
            if (personalRelationship.RelationshipHolder == null || personalRelationship.RelationshipHolder.NullFlavor != null)
            {
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE02D"), null));
                return null;
            }
            retVal.LegalName = CreateNameSet(personalRelationship.RelationshipHolder.Name, dtls);

            // Type
            if (personalRelationship.Code == null || personalRelationship.Code.IsNull ||
                personalRelationship.Code.Code.IsAlternateCodeSpecified)
            {
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE02E"), null));
                return null;
            }
            retVal.RelationshipKind = Util.ToWireFormat(personalRelationship.Code);

            // Telecom addresses
            if (personalRelationship.RelationshipHolder.Telecom != null && !personalRelationship.RelationshipHolder.Telecom.IsEmpty)
            {
                foreach (var telecom in personalRelationship.RelationshipHolder.Telecom)
                    retVal.TelecomAddresses.Add(new TelecommunicationsAddress()
                    {
                        Use = telecom.Use == null ? null : Util.ToWireFormat(telecom.Use),
                        Value = telecom.Value
                    });
            }

            return retVal;

        }


        /// <summary>
        /// Create a healthcare participant component
        /// </summary>
        protected HealthcareParticipant CreateParticipantComponent(MARC.Everest.RMIM.CA.R020402.COCT_MT090108CA.AssignedEntity assignedEntity, List<IResultDetail> dtls)
        {
            HealthcareParticipant retVal = new HealthcareParticipant();
            retVal.Classifier = HealthcareParticipant.HealthcareParticipantType.Person;

            // Check for an id
            if (assignedEntity.Id == null || assignedEntity.Id.IsNull)
            {
                dtls.Add(new ResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE02F"), null, null));
                return null;
            }

            // Identifiers
            retVal.AlternateIdentifiers.AddRange(CreateDomainIdentifierList(assignedEntity.Id, dtls));

            // Name
            if (assignedEntity.AssignedPerson == null || assignedEntity.AssignedPerson.NullFlavor != null)
            {
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE030"), null));
                return null;
            }

            if (assignedEntity.AssignedPerson.Name != null && !assignedEntity.AssignedPerson.Name.IsNull)
                retVal.LegalName = CreateNameSet(assignedEntity.AssignedPerson.Name, dtls);

            // Type
            if (assignedEntity.Code == null || assignedEntity.Code.IsNull)
            {
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE031"), null));
                return null;
            }
            retVal.Type = CreateCodeValue<HealthCareProviderRoleType>(assignedEntity.Code, dtls);

            // Telecom addresses
            if (assignedEntity.Telecom != null && !assignedEntity.Telecom.IsEmpty)
            {
                foreach (var telecom in assignedEntity.Telecom)
                    retVal.TelecomAddresses.Add(new TelecommunicationsAddress()
                    {
                        Use = telecom.Use == null ? null : Util.ToWireFormat(telecom.Use),
                        Value = telecom.Value
                    });
            }

            // License number
            if (assignedEntity.AssignedPerson.AsHealthCareProvider != null &&
                assignedEntity.AssignedPerson.AsHealthCareProvider.Id != null &&
                !assignedEntity.AssignedPerson.AsHealthCareProvider.Id.IsNull)
                retVal.AlternateIdentifiers.Add(new DomainIdentifier()
                {
                    Domain = assignedEntity.AssignedPerson.AsHealthCareProvider.Id.Root,
                    Identifier = assignedEntity.AssignedPerson.AsHealthCareProvider.Id.Extension,
                    IsLicenseAuthority = true
                });

            // Represented organization
            if (assignedEntity.RepresentedOrganization != null && assignedEntity.RepresentedOrganization.NullFlavor == null)
            {
                HealthcareParticipant ptcpt = CreateParticipantComponent(assignedEntity.RepresentedOrganization, dtls);
                if (ptcpt != null)
                    retVal.Add(ptcpt, "RPO", HealthServiceRecordSiteRoleType.RepresentitiveOf, ptcpt.AlternateIdentifiers);
                else
                    dtls.Add(new ResultDetail(ResultDetailType.Warning, this.m_localeService.GetString("MSGW010"), null, null));
            }

            return retVal;
        }

        private HealthcareParticipant CreateParticipantComponent(MARC.Everest.RMIM.CA.R020402.COCT_MT090108CA.Organization organization, List<IResultDetail> dtls)
        {
            HealthcareParticipant retVal = new HealthcareParticipant();
            retVal.Classifier = HealthcareParticipant.HealthcareParticipantType.Organization;

            // Organization identifier
            if (organization.Id == null || organization.Id.IsNull)
            {
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE032"), null));
                return null;
            }
            retVal.AlternateIdentifiers.Add(new DomainIdentifier()
            {
                Domain = organization.Id.Root,
                Identifier = organization.Id.Extension
            });

            // Organization name
            if (organization.Name != null)
            {
                retVal.LegalName = new NameSet()
                {
                    Use = NameSet.NameSetUse.Legal
                };
                retVal.LegalName.Parts.Add(new NamePart()
                {
                    Type = NamePart.NamePartType.Given,
                    Value = organization.Name
                });
            };

            // Organization type
            if (organization.AssignedOrganization != null && organization.AssignedOrganization.NullFlavor == null)
            {
                if (organization.AssignedOrganization.Code == null || organization.AssignedOrganization.Code.IsNull)
                    dtls.Add(new ResultDetail(ResultDetailType.Warning, this.m_localeService.GetString("MSGW011"), null, null));
                else
                    retVal.Type = CreateCodeValue<String>(organization.AssignedOrganization.Code, dtls);

                // Telecommunications
                if (organization.AssignedOrganization.Telecom != null && !organization.AssignedOrganization.Telecom.IsEmpty)
                    foreach (var tel in organization.AssignedOrganization.Telecom)
                        retVal.TelecomAddresses.Add(new TelecommunicationsAddress()
                        {
                            Use = tel.Use == null ? null : Util.ToWireFormat(tel.Use),
                            Value = tel.Value
                        });
            }

            return retVal;
        }

        /// <summary>
        /// Create a healthcare participant component
        /// </summary>
        private HealthcareParticipant CreateParticipantComponent(MARC.Everest.RMIM.CA.R020402.COCT_MT090102CA.AssignedEntity assignedEntity, List<IResultDetail> dtls)
        {
            // Create healthcare participant
            HealthcareParticipant retVal = new HealthcareParticipant();
            retVal.Classifier = HealthcareParticipant.HealthcareParticipantType.Person;

            // Check for an id
            if (assignedEntity.Id == null || assignedEntity.Id.IsNull)
            {
                dtls.Add(new ResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE02F"), null, null));
                return null;
            }

            // Identifiers
            retVal.AlternateIdentifiers.AddRange(CreateDomainIdentifierList(assignedEntity.Id, dtls));

            // Name
            if (assignedEntity.AssignedPerson != null && assignedEntity.AssignedPerson.NullFlavor == null
                && assignedEntity.AssignedPerson.Name != null && !assignedEntity.AssignedPerson.Name.IsNull)
                retVal.LegalName = CreateNameSet(assignedEntity.AssignedPerson.Name, dtls);

            // License number
            if (assignedEntity.AssignedPerson.AsHealthCareProvider != null &&
                assignedEntity.AssignedPerson.AsHealthCareProvider.Id != null &&
                !assignedEntity.AssignedPerson.AsHealthCareProvider.Id.IsNull)
                retVal.AlternateIdentifiers.Add(new DomainIdentifier()
                {
                    Domain = assignedEntity.AssignedPerson.AsHealthCareProvider.Id.Root,
                    Identifier = assignedEntity.AssignedPerson.AsHealthCareProvider.Id.Extension,
                    IsLicenseAuthority = true
                });

            // Represented organization
            if (assignedEntity.RepresentedOrganization != null && assignedEntity.RepresentedOrganization.NullFlavor == null)
            {
                HealthcareParticipant ptcpt = CreateParticipantComponent(assignedEntity.RepresentedOrganization, dtls);
                if (ptcpt != null)
                    retVal.Add(ptcpt, "RPO", HealthServiceRecordSiteRoleType.RepresentitiveOf, ptcpt.AlternateIdentifiers);
                else
                    dtls.Add(new ResultDetail(ResultDetailType.Warning, this.m_localeService.GetString("MSGW010"), null, null));
            }

            return retVal;
        }

        /// <summary>
        /// Create location component
        /// </summary>
        private ServiceDeliveryLocation CreateLocationComponent(MARC.Everest.RMIM.CA.R020402.COCT_MT240003CA.ServiceDeliveryLocation serviceDeliveryLocation, List<IResultDetail> dtls)
        {
            ServiceDeliveryLocation retVal = new ServiceDeliveryLocation();

            // Check for identifier
            if (serviceDeliveryLocation.Id == null || serviceDeliveryLocation.Id.IsNull)
            {
                dtls.Add(new ResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE033"), null, null));
                return null;
            }
            retVal.AlternateIdentifiers.Add(
                new DomainIdentifier()
                {
                    Domain = serviceDeliveryLocation.Id.Root,
                    Identifier = serviceDeliveryLocation.Id.Extension
                });

            // Check for name
            if (serviceDeliveryLocation.Location != null && serviceDeliveryLocation.Location.NullFlavor == null)
                retVal.Name = serviceDeliveryLocation.Location.Name;

            // Telecom
            if (serviceDeliveryLocation.Telecom != null)
                foreach (var tel in serviceDeliveryLocation.Telecom)
                    retVal.TelecomAddresses.Add(new TelecommunicationsAddress()
                    {
                        Value = tel.Value,
                        Use = tel.Use != null ? Util.ToWireFormat(tel.Use) : null
                    });

            // Address
            if (serviceDeliveryLocation.Addr != null)
                retVal.Address = CreateAddressSet(serviceDeliveryLocation.Addr, dtls);

            // Location type
            if (serviceDeliveryLocation.Code == null || serviceDeliveryLocation.Code.IsNull)
            {
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE034"), null));
                return null;
            }
            retVal.LocationType = CreateCodeValue(serviceDeliveryLocation.Code, dtls);

            return retVal;
        }

        /// <summary>
        /// Create a participant component from an organization
        /// </summary>
        private HealthcareParticipant CreateParticipantComponent(MARC.Everest.RMIM.CA.R020402.COCT_MT090102CA.Organization organization, List<IResultDetail> dtls)
        {
            HealthcareParticipant retVal = new HealthcareParticipant();
            retVal.Classifier = HealthcareParticipant.HealthcareParticipantType.Organization;

            // Organization identifier
            if (organization.Id == null || organization.Id.IsNull)
            {
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE032"), null));
                return null;
            }
            retVal.AlternateIdentifiers.Add(new DomainIdentifier()
            {
                Domain = organization.Id.Root,
                Identifier = organization.Id.Extension
            });

            // Organization name
            if (organization.Name != null)
            {
                retVal.LegalName = new NameSet()
                {
                    Use = NameSet.NameSetUse.Legal
                };
                retVal.LegalName.Parts.Add(new NamePart()
                {
                    Type = NamePart.NamePartType.Given,
                    Value = organization.Name
                });
            };

            return retVal;
        }

        /// <summary>
        /// Create a healthcare participant
        /// </summary>
        /// <param name="p"></param>
        /// <param name="dtls"></param>
        /// <returns></returns>
        private HealthcareParticipant CreateParticipantComponent(MARC.Everest.RMIM.CA.R020402.COCT_MT090502CA.AssignedEntity assignedEntity, List<IResultDetail> dtls)
        {
            // Create the healthcare participant
            HealthcareParticipant retVal = new HealthcareParticipant();
            retVal.Classifier = HealthcareParticipant.HealthcareParticipantType.Organization;

            // Check for id
            if (assignedEntity.RepresentedOrganization == null || assignedEntity.RepresentedOrganization.NullFlavor != null
             || assignedEntity.RepresentedOrganization.Id == null || assignedEntity.RepresentedOrganization.Id.IsNull)
            {
                dtls.Add(new ResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE02B"), null, null));
                return null;
            }

            // Add identifier
            retVal.AlternateIdentifiers.Add(new DomainIdentifier()
            {
                Domain = assignedEntity.RepresentedOrganization.Id.Root,
                Identifier = assignedEntity.RepresentedOrganization.Id.Extension
            });

            // Legal name
            if (assignedEntity.RepresentedOrganization.Name != null &&
                !assignedEntity.RepresentedOrganization.Name.IsNull)
            {
                retVal.LegalName = new NameSet();
                retVal.LegalName.Use = NameSet.NameSetUse.Legal;
                retVal.LegalName.Parts.Add(new NamePart()
                {
                    Value = assignedEntity.RepresentedOrganization.Name,
                    Type = NamePart.NamePartType.Given
                });
            }

            return retVal;
        }

        /// <summary>
        /// Create location component
        /// </summary>
        private ServiceDeliveryLocation CreateLocationComponent(MARC.Everest.RMIM.CA.R020402.COCT_MT240012CA.ServiceDeliveryLocation serviceDeliveryLocation, List<IResultDetail> dtls)
        {
            ServiceDeliveryLocation retVal = new ServiceDeliveryLocation();

            // Check for identifier
            if (serviceDeliveryLocation.Id == null || serviceDeliveryLocation.Id.IsNull)
            {
                dtls.Add(new ResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE033"), null, null));
                return null;
            }
            retVal.AlternateIdentifiers.Add(
                new DomainIdentifier()
                {
                    Domain = serviceDeliveryLocation.Id.Root,
                    Identifier = serviceDeliveryLocation.Id.Extension
                });

            // Check for name
            if (serviceDeliveryLocation.Location != null && serviceDeliveryLocation.Location.NullFlavor == null)
                retVal.Name = serviceDeliveryLocation.Location.Name;

            return retVal;
        }

        /// <summary>
        /// Create a location component
        /// </summary>
        private ServiceDeliveryLocation CreateLocationComponent(MARC.Everest.RMIM.CA.R020402.COCT_MT240007CA.ServiceDeliveryLocation serviceDeliveryLocation, List<IResultDetail> dtls)
        {
            ServiceDeliveryLocation retVal = new ServiceDeliveryLocation();

            // Check for identifier
            if (serviceDeliveryLocation.Id == null || serviceDeliveryLocation.Id.IsNull)
                dtls.Add(new ResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE033"), null, null));
            else
                retVal.AlternateIdentifiers.Add(
                    new DomainIdentifier()
                    {
                        Domain = serviceDeliveryLocation.Id.Root,
                        Identifier = serviceDeliveryLocation.Id.Extension
                    });

            // SDL type
            if (serviceDeliveryLocation.Code == null || serviceDeliveryLocation.Code.IsNull)
                dtls.Add(new ResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE034"), null, null));
            else
                retVal.LocationType = CreateCodeValue(serviceDeliveryLocation.Code, dtls);

            // Telecom
            if (serviceDeliveryLocation.Telecom != null)
                foreach (var tel in serviceDeliveryLocation.Telecom)
                    retVal.TelecomAddresses.Add(new TelecommunicationsAddress()
                    {
                        Value = tel.Value,
                        Use = tel.Use != null ? Util.ToWireFormat(tel.Use) : null
                    });

            // Address
            if (serviceDeliveryLocation.Addr != null)
                retVal.Address = CreateAddressSet(serviceDeliveryLocation.Addr, dtls);

            // Check for name
            if (serviceDeliveryLocation.Location != null && serviceDeliveryLocation.Location.NullFlavor == null)
                retVal.Name = serviceDeliveryLocation.Location.Name;

            return retVal;
        }

        /// <summary>
        /// Creates the component of events
        /// </summary>
        /// <param name="components"></param>
        /// <param name="dtls"></param>
        /// <returns></returns>
        private List<IComponent> CreateComponentOfRefs(List<MARC.Everest.RMIM.CA.R020402.REPC_MT410001CA.Component3> components, List<IResultDetail> dtls)
        {
            List<IComponent> retVal = new List<IComponent>(components.Count);
            foreach (var cmpOf in components)
            {
                if (cmpOf == null ||
                    cmpOf.NullFlavor != null ||
                    cmpOf.PatientCareProvisionEvent == null ||
                    cmpOf.PatientCareProvisionEvent.NullFlavor != null)
                    continue;

                if (cmpOf.PatientCareProvisionEvent.Id == null ||
                    cmpOf.PatientCareProvisionEvent.Id.IsNull)
                    dtls.Add(new RequiredElementMissingResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE036"), null));
                else
                    retVal.Add(new HealthServiceRecordComponentRef()
                    {
                        AlternateIdentifier = CreateDomainIdentifier(cmpOf.PatientCareProvisionEvent.Id, dtls)
                    });
            }
            return retVal;
        }


        /// <summary>
        /// Create the component of references for document based transactions
        /// </summary>
        private IEnumerable<IComponent> CreateComponentOfRefs(List<MARC.Everest.RMIM.CA.R020402.REPC_MT230003CA.Component6> components, List<IResultDetail> dtls)
        {
            
            List<IComponent> retVal = new List<IComponent>(components.Count);
            foreach (var cmpOf in components)
            {
                if (cmpOf == null ||
                    cmpOf.NullFlavor != null ||
                    cmpOf.PatientCareProvisionEvent == null ||
                    cmpOf.PatientCareProvisionEvent.NullFlavor != null)
                    continue;

                if (cmpOf.PatientCareProvisionEvent.Id == null ||
                    cmpOf.PatientCareProvisionEvent.Id.IsNull)
                    dtls.Add(new RequiredElementMissingResultDetail(ResultDetailType.Error, this.m_localeService.GetString("MSGE036"), null));
                else
                    retVal.Add(new HealthServiceRecordComponentRef()
                    {
                        AlternateIdentifier = CreateDomainIdentifier(cmpOf.PatientCareProvisionEvent.Id, dtls)
                    });
            }
            return retVal;
        }

        /// <summary>
        /// Create client component
        /// </summary>
        public Client CreateClientComponent(MARC.Everest.RMIM.CA.R020402.COCT_MT050202CA.Patient patient, List<IResultDetail> dtls)
        {
            // Patient is null
            if (patient == null || patient.NullFlavor != null ||
                patient.PatientPerson == null || patient.PatientPerson.NullFlavor != null ||
                patient.PatientPerson.AdministrativeGenderCode == null || patient.PatientPerson.AdministrativeGenderCode.IsNull ||
                patient.PatientPerson.BirthTime == null || patient.PatientPerson.BirthTime.IsNull)
            {
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error,
                    this.m_localeService.GetString("MSGE028"), null));
                return null;
            }

            Client retVal = new Client();
            // Resolve id
            if (patient.Id == null || patient.Id.NullFlavor != null ||
                patient.Id.IsEmpty)
            {
                dtls.Add(new ResultDetail(ResultDetailType.Error,
                    this.m_localeService.GetString("MSGE029"),
                    null, null));
                return null;
            }

            // Alternative identifiers
            retVal.AlternateIdentifiers.AddRange(CreateDomainIdentifierList(patient.Id, dtls));

            // Client Legal Name
            PN legalName = patient.PatientPerson.Name;
            if (legalName == null || legalName.IsNull)
                dtls.Add(new ResultDetail(ResultDetailType.Warning, this.m_localeService.GetString("MSGE02A"), null, null));
            else
                retVal.LegalName = CreateNameSet(legalName, dtls);

            // Client birth time
            retVal.BirthTime = new TimestampPart()
            {
                PartType = TimestampPart.TimestampPartType.Standlone,
                Precision = m_precisionMap[patient.PatientPerson.BirthTime.DateValuePrecision.Value],
                Value = patient.PatientPerson.BirthTime.DateValue
            };

            // Client gender
            retVal.GenderCode = Util.ToWireFormat(patient.PatientPerson.AdministrativeGenderCode);

            return retVal;
        }
       
        /// <summary>
        /// Convert status code
        /// </summary>
        private StatusType ConvertStatusCode(MARC.Everest.DataTypes.CS<RoleStatus> status, List<IResultDetail> dtls)
        {

            if(status.Code.IsAlternateCodeSpecified)
            {
                dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, m_localeService.GetString("MSGE010"), null, null));
                return StatusType.Unknown;
            }

            // Status
            switch ((RoleStatus)status)
            {
                case RoleStatus.Active:
                    return StatusType.Active;
                case RoleStatus.Cancelled:
                    return StatusType.Cancelled;
                case RoleStatus.Nullified:
                    return StatusType.Nullified;
                case RoleStatus.Pending:
                    return StatusType.New;
                case RoleStatus.Suspended:
                    return StatusType.Aborted;
                case RoleStatus.Terminated:
                    return StatusType.Obsolete;
                case RoleStatus.Normal:
                    dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, m_localeService.GetString("MSGE011"), null, null));
                    return StatusType.Unknown;
                default:
                    dtls.Add(new VocabularyIssueResultDetail(ResultDetailType.Error, m_localeService.GetString("MSGE010"), null, null));
                    return StatusType.Unknown;
            }
        }

        /// <summary>
        /// Create Repository Device
        /// </summary>
        private RepositoryDevice CreateRepositoryDevice(MARC.Everest.RMIM.CA.R020402.COCT_MT090310CA.AssignedDevice assignedDevice, List<IResultDetail> dtls)
        {

            RepositoryDevice retVal = new RepositoryDevice();

            // Identifier for the device
            if (assignedDevice.Id == null || assignedDevice.Id.IsNull)
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, m_localeService.GetString("MSGE00D"), null));
            else
                retVal.AlternateIdentifier = CreateDomainIdentifier(assignedDevice.Id, dtls);

            // Repository jurisdiction
            if (assignedDevice.RepresentedRepositoryJurisdiction == null || assignedDevice.RepresentedRepositoryJurisdiction.NullFlavor != null ||
                assignedDevice.RepresentedRepositoryJurisdiction.Name == null || assignedDevice.RepresentedRepositoryJurisdiction.Name.IsNull)
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, m_localeService.GetString("MSGE00E"), null));
            else
                retVal.Jurisdiction = assignedDevice.RepresentedRepositoryJurisdiction.Name;

            // Assigned repository
            if (assignedDevice.AssignedRepository == null || assignedDevice.AssignedRepository.NullFlavor != null ||
                assignedDevice.AssignedRepository.Name == null || assignedDevice.AssignedRepository.Name.IsNull)
                dtls.Add(new MandatoryElementMissingResultDetail(ResultDetailType.Error, m_localeService.GetString("MSGE00F"), null));
            else
                retVal.Name = assignedDevice.AssignedRepository.Name;

            return retVal;
        }


    }
}
