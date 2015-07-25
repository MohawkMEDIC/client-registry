/**
 * Copyright 2012-2015 Mohawk College of Applied Arts and Technology
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you 
 * may not use this file except in compliance with the License. You may 
 * obtain a copy of the License at 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations under 
 * the License.
 * 
 * User: Justin
 * Date: 12-7-2015
 */

using System;
using System.Collections.Generic;
using MARC.HI.EHRS.CR.Core.ComponentModel;
using MARC.Everest.Connectors;
using MARC.Everest.DataTypes;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.SVC.Core.DataTypes;
using MARC.Everest.DataTypes.Interfaces;
using System.ComponentModel;
using MARC.HI.EHRS.SVC.Core.ComponentModel.Components;
using MARC.HI.EHRS.SVC.Core.ComponentModel;
using MARC.Everest.DataTypes.Primitives;

namespace MARC.HI.EHRS.CR.Notification.PixPdq
{
    /// <summary>
    /// Message utility class
    /// </summary>
    public partial class MessageUtility 
	{

        // Invalid classifier
        private const string ERR_EVENT_CLASSIFIER = "Event cannot be translated to messaging format, invalid classifier code";

        /// <summary>
        /// Terminology service
        /// </summary>
        protected ITerminologyService m_terminologyService = null;
        protected ILocalizationService m_localeService = null;
        protected ISystemConfigurationService m_configService = null;

        private readonly Dictionary<PostalAddressUse, AddressSet.AddressSetUse> m_addressUseMap = new Dictionary<PostalAddressUse, AddressSet.AddressSetUse>() 
            {
                { PostalAddressUse.BadAddress, AddressSet.AddressSetUse.BadAddress },
                { PostalAddressUse.Direct, AddressSet.AddressSetUse.Direct },
                { PostalAddressUse.HomeAddress, AddressSet.AddressSetUse.HomeAddress },
                { PostalAddressUse.PhysicalVisit, AddressSet.AddressSetUse.PhysicalVisit },
                { PostalAddressUse.PostalAddress, AddressSet.AddressSetUse.PostalAddress },
                { PostalAddressUse.PrimaryHome, AddressSet.AddressSetUse.PrimaryHome },
                { PostalAddressUse.Public, AddressSet.AddressSetUse.Public },
                { PostalAddressUse.TemporaryAddress, AddressSet.AddressSetUse.TemporaryAddress },
                { PostalAddressUse.VacationHome, AddressSet.AddressSetUse.VacationHome },
                { PostalAddressUse.WorkPlace, AddressSet.AddressSetUse.WorkPlace }
            };

         // Create the date precision maps
         private readonly Dictionary<DatePrecision, string> m_precisionMap = new Dictionary<DatePrecision, string>()
                { 
                       { DatePrecision.Day, "D" },
                       { DatePrecision.Full, "F" },
                       { DatePrecision.Hour, "H" },
                       { DatePrecision.Minute, "m" },
                       { DatePrecision.Month, "M" }, 
                       { DatePrecision.Second, "S" },
                       { DatePrecision.Year, "Y" }
                };
                 
            // Create the name use maps
           private readonly Dictionary<EntityNameUse, NameSet.NameSetUse> m_nameUseMap = new Dictionary<EntityNameUse, NameSet.NameSetUse>()
            {
                { EntityNameUse.Artist, NameSet.NameSetUse.Artist },
                { EntityNameUse.Assigned, NameSet.NameSetUse.Assigned },
                { EntityNameUse.Indigenous, NameSet.NameSetUse.Indigenous },
                { EntityNameUse.Legal, NameSet.NameSetUse.Legal },
                { EntityNameUse.License, NameSet.NameSetUse.License },
                { EntityNameUse.OfficialRecord, NameSet.NameSetUse.OfficialRecord },
                { EntityNameUse.Phonetic, NameSet.NameSetUse.Phonetic },
                { EntityNameUse.Pseudonym, NameSet.NameSetUse.Pseudonym },
                { EntityNameUse.Religious, NameSet.NameSetUse.Religious },
                { EntityNameUse.MaidenName, NameSet.NameSetUse.MaidenName },
                { EntityNameUse.Search, NameSet.NameSetUse.Search }
            };

            // Create name part type map
           private readonly Dictionary<EntityNamePartType?, NamePart.NamePartType> m_namePartTypeMap = new Dictionary<EntityNamePartType?, NamePart.NamePartType>()
            {
                { EntityNamePartType.Delimiter, NamePart.NamePartType.Delimeter },
                { EntityNamePartType.Family, NamePart.NamePartType.Family },
                { EntityNamePartType.Given, NamePart.NamePartType.Given } ,
                { EntityNamePartType.Prefix, NamePart.NamePartType.Prefix },
                { EntityNamePartType.Suffix, NamePart.NamePartType.Suffix }
            };

        /// <summary>
        /// Create an IVL from the TimeStampSet
        /// </summary>
        public IVL<TS> CreateIVL(TimestampSet timestampSet)
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
                        retVal.High = CreateTS(part);
                        break;
                    case TimestampPart.TimestampPartType.LowBound:
                        retVal.Low = CreateTS(part);
                        break;
                    case TimestampPart.TimestampPartType.Standlone:
                        retVal.Value = CreateTS(part);
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
        public TS CreateTS(TimestampPart part)
        {
            if (part == null) return null;
            DatePrecision prec = default(DatePrecision);
            foreach (var kv in m_precisionMap)
                if (kv.Value.Equals(part.Precision))
                    prec = kv.Key;
            return new TS(part.Value, prec);
        }

        /// <summary>
        /// Create a CD from the code value supplied
        /// </summary>
        public CD<T> CreateCD<T>(CodeValue codeValue)
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
                        Name = CreateCV<T>(kv.Key),
                        Value = CreateCD<T>(kv.Value)
                    });
            }


            return retVal;
        }

       

        /// <summary>
        /// Create an address from the specified address set
        /// </summary>
        public AD CreateAD(AddressSet addressSet)
        {
            if (addressSet == null)
                return new AD() { NullFlavor = MARC.Everest.DataTypes.NullFlavor.NoInformation };

            AD retVal = new AD();
            retVal.Use = new SET<CS<PostalAddressUse>>();
            foreach (var kv in m_addressUseMap)
                if ((kv.Value & addressSet.Use) != 0)
                    retVal.Use.Add(kv.Key);
            if (retVal.Use.IsEmpty)
                retVal.Use = null;

            foreach(var pt in addressSet.Parts)

                retVal.Part.Add(new ADXP(pt.AddressValue, (AddressPartType)Enum.Parse(typeof(AddressPartType), pt.PartType.ToString())));
            return retVal;
        }

      
        /// <summary>
        /// Create an Everest TEL from the data model TEL
        /// </summary>
        public TEL CreateTEL(TelecommunicationsAddress tel)
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
        public II CreateII(DomainIdentifier id)
        {
            return new II() { Root = id.Domain, Extension = id.Identifier };
        }

        /// <summary>
        /// Create a set of II from a list of DomainIdentifier
        /// </summary>
        public SET<II> CreateIISet(List<DomainIdentifier> identifiers)
        {
            SET<II> retVal = new SET<II>(identifiers.Count, II.Comparator);
            foreach (var id in identifiers)
                retVal.Add(new II() { Root = id.Domain, Extension = id.Identifier });
            return retVal;
        }

        /// <summary>
        /// Create a person name from the specified name set
        /// </summary>
        public PN CreatePN(MARC.HI.EHRS.SVC.Core.DataTypes.NameSet nameSet)
        {
            EntityNameUse enUse = EntityNameUse.Legal;

            try
            {
                enUse = (EntityNameUse)Enum.Parse(typeof(EntityNameUse), nameSet.Use.ToString());
            }
            catch
            {
                throw;
            }

            PN retVal = new PN();
            if (enUse != EntityNameUse.Search)
                retVal.Use = new SET<CS<EntityNameUse>>(enUse);

            // Parts
            foreach (var part in nameSet.Parts)
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
                        break;
                }

            return retVal;
        }
      

        /// <summary>
        /// Create a person name from the specified name set
        /// </summary>
        public ON CreateON(MARC.HI.EHRS.SVC.Core.DataTypes.NameSet nameSet)
        {
            EntityNameUse enUse = EntityNameUse.Legal;
            if (nameSet == null) return null;

            try
            {
                enUse = (EntityNameUse)Enum.Parse(typeof(EntityNameUse), nameSet.Use.ToString());
            }
            catch
            {
                throw;
            }

            ON retVal = new ON();
            if (enUse != EntityNameUse.Search)
                retVal.Use = new SET<CS<EntityNameUse>>(enUse);

            // Parts
            foreach (var part in nameSet.Parts)
                retVal.Part.Add(new ENXP(part.Value));

            return retVal;

        }
        /// <summary>
        /// Create a CV from the specified domain
        /// </summary>
        public CV<T> CreateCV<T>(MARC.HI.EHRS.SVC.Core.DataTypes.CodeValue codeValue)
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
        protected IServiceProvider m_context;

        /// <summary>
        /// Gets or sets the context of under which this persister runs
        /// </summary>
        public IServiceProvider Context
        {
            get
            { return m_context; }
            set
            {
                m_context = value;
                this.m_terminologyService = Context.GetService(typeof(ITerminologyService)) as ITerminologyService;
                this.m_localeService = Context.GetService(typeof(ILocalizationService)) as ILocalizationService;
                this.m_configService = Context.GetService(typeof(ISystemConfigurationService)) as ISystemConfigurationService;
            }
        }

        #endregion


     
    }
}
