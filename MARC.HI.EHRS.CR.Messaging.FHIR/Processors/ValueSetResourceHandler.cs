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
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Messaging.FHIR.Resources;
using MARC.HI.EHRS.SVC.Messaging.FHIR.Handlers;
using MARC.Everest.Connectors;
using MARC.Everest.Attributes;
using System.Xml.Serialization;
using System.ServiceModel.Web;
using System.ComponentModel;
using System.IO;
using MARC.HI.EHRS.SVC.Messaging.FHIR;
using MARC.HI.EHRS.SVC.Messaging.FHIR.Attributes;
using MARC.HI.EHRS.SVC.Messaging.FHIR.DataTypes;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.Processors
{

   
    /// <summary>
    /// FHIR Message processor for a value set
    /// </summary>
    [Profile(ProfileId = "pdq-support", Name = "PDQ Supporting resources profile")]
    [ResourceProfile(Resource = typeof(ValueSet), Name = "Client registry value-set profile")]
    public class ValueSetResourceHandler : IFhirResourceHandler
    {

        #region IFhirResourceHandler Members

        /// <summary>
        /// Create a value set .. Method not allowed
        /// </summary>
        public SVC.Messaging.FHIR.FhirOperationResult Create(ResourceBase target, SVC.Core.Services.DataPersistenceMode mode)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Delete a value set
        /// </summary>
        public SVC.Messaging.FHIR.FhirOperationResult Delete(string id, SVC.Core.Services.DataPersistenceMode mode)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Query for a value set
        /// </summary>
        public SVC.Messaging.FHIR.FhirQueryResult Query(System.Collections.Specialized.NameValueCollection parameters)
        {
            // Get all value sets referenced in the profile

            return null;
        }

        /// <summary>
        /// Read a value set
        /// </summary>
        public SVC.Messaging.FHIR.FhirOperationResult Read(string id, string versionId)
        {
            // Determine where to fetch the code system from
            List<Type> codeSystemType = new List<Type>();
            if (id != null && id.StartsWith("v3-")) // Everest
            {
                id = id.Substring(3);
                foreach (var asm in new System.Reflection.Assembly[] { typeof(MARC.Everest.DataTypes.II).Assembly, typeof(MARC.Everest.RMIM.UV.NE2008.Vocabulary.AdministrativeGender).Assembly, typeof(MARC.Everest.RMIM.CA.R020402.Vocabulary.AdministrativeGender).Assembly })
                {
                   if (!String.IsNullOrEmpty(versionId))
                    {
                        var version = asm.GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>();
                        if (version != null && version.InformationalVersion != versionId)
                            continue;
                    } 
                    
                    var tcodesys = Array.Find(asm.GetTypes(), t => t.IsEnum && t.GetCustomAttribute<StructureAttribute>() != null && t.GetCustomAttribute<StructureAttribute>().Name == id);

                    if (tcodesys != null)
                        codeSystemType.Add(tcodesys);
                }
            }
            else
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (!String.IsNullOrEmpty(versionId) && asm.GetName().Version.ToString() != versionId)
                        continue;

                    var tcodesys = Array.Find(asm.GetTypes(), t => t.Name.Equals(id));
                    if (tcodesys != null) 
                        codeSystemType.Add(tcodesys);
                }
            }

            // Code system type
            if (codeSystemType == null || codeSystemType.Count == 0)
                return new FhirOperationResult()
                {
                    Outcome = ResultCode.TypeNotAvailable,
                };
            else
            {
                var retVal = new FhirOperationResult()
                {
                    Outcome = ResultCode.Accepted,
                    Results = new List<ResourceBase>()
                };
                foreach (var cs in codeSystemType)
                    retVal.Results.Add(this.CreateValueSetFromEnum(cs));
                return retVal;
            }
            
            
        }

        /// <summary>
        /// Gets the resource name
        /// </summary>
        public string ResourceName
        {
            get { return "ValueSet"; }
        }

        /// <summary>
        /// Update
        /// </summary>
        public SVC.Messaging.FHIR.FhirOperationResult Update(string id, ResourceBase target, SVC.Core.Services.DataPersistenceMode mode)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Validate
        /// </summary>
        public SVC.Messaging.FHIR.FhirOperationResult Validate(string id, ResourceBase target)
        {
            throw new NotSupportedException();
        }

        #endregion
        
        /// <summary>
        /// Create a value set from an enumeration
        /// </summary>
        public ValueSet CreateValueSetFromEnum(Type enumType)
        {
            // Does this have Everest attributes?
            if (enumType.GetCustomAttribute<StructureAttribute>() != null)
                return CreateValueFromEverestEnum(enumType);
            else
            {
                ValueSet retVal = new ValueSet();
                var baseUri = WebOperationContext.Current.IncomingRequest.UriTemplateMatch.BaseUri.ToString();

                // XmlType?
                XmlTypeAttribute xTypeAtt = enumType.GetCustomAttribute<XmlTypeAttribute>();
                DescriptionAttribute descriptionAtt = enumType.GetCustomAttribute<DescriptionAttribute>();

                retVal.Name = enumType.Name;
                retVal.Identifier = String.Format("{0}/ValueSet/@{1}", baseUri, enumType.Name);
                retVal.Version = enumType.Assembly.GetName().Version.ToString();
                retVal.Id = retVal.Identifier;
                retVal.VersionId = retVal.Version;

                // Description
                if (descriptionAtt != null)
                    retVal.Description = descriptionAtt.Description;

                // Use the company attribute
                var companyAtt = enumType.Assembly.GetCustomAttribute<System.Reflection.AssemblyCompanyAttribute>();
                if (companyAtt != null)
                    retVal.Publisher = companyAtt.Company;

                // Date of the assembly file
                if (!String.IsNullOrEmpty(enumType.Assembly.Location) && File.Exists(enumType.Assembly.Location))
                    retVal.Date = new SVC.Messaging.FHIR.DataTypes.DateOnly() { DateValue = new FileInfo(enumType.Assembly.Location).LastWriteTime };
                retVal.Timestamp = retVal.Date.DateValue.Value;
                retVal.Status = new SVC.Messaging.FHIR.DataTypes.PrimitiveCode<string>("testing");

                // Definition
                retVal.Define = new ValueSetDefinition();
                if (xTypeAtt != null)
                    retVal.Define.System = new Uri(String.Format("{0}#{1}", xTypeAtt.Namespace, xTypeAtt.TypeName));
                else
                    retVal.Define.System = new Uri(String.Format("{0}/ValueSet/@{1}", baseUri, enumType.Name));

                // Now populate
                foreach (var value in enumType.GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public))
                {
                    var definition = new ConceptDefinition();
                    definition.Code = new SVC.Messaging.FHIR.DataTypes.PrimitiveCode<string>(MARC.Everest.Connectors.Util.ToWireFormat(value.GetValue(null)));
                    definition.Abstract = false;

                    descriptionAtt = value.GetCustomAttribute<DescriptionAttribute>();
                    if (descriptionAtt != null)
                        definition.Display = descriptionAtt.Description;

                    retVal.Define.Concept.Add(definition);
                }

                return retVal;
            }
        }

        /// <summary>
        /// Create a value from an Everest enum
        /// </summary>
        /// TODO: Optimize this
        private ValueSet CreateValueFromEverestEnum(Type enumType)
        {
            StructureAttribute structAtt = enumType.GetCustomAttribute<StructureAttribute>();
            var baseUri = WebOperationContext.Current.IncomingRequest.UriTemplateMatch.BaseUri.ToString();

            ValueSet retVal = new ValueSet();
            retVal.Name = structAtt.Name;
            retVal.Identifier = structAtt.CodeSystem;
            retVal.Id = retVal.Identifier;
            // Use the company attribute
            var companyAtt = enumType.Assembly.GetCustomAttribute<System.Reflection.AssemblyCompanyAttribute>();
            if (companyAtt != null)
                retVal.Publisher = companyAtt.Company;
            var versionAtt = enumType.Assembly.GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>();
            if (versionAtt != null)
                retVal.Version = versionAtt.InformationalVersion;
            retVal.VersionId = retVal.Version;

            // Date of the assembly file
            if (!String.IsNullOrEmpty(enumType.Assembly.Location) && File.Exists(enumType.Assembly.Location))
                retVal.Date = new SVC.Messaging.FHIR.DataTypes.DateOnly() { DateValue = new FileInfo(enumType.Assembly.Location).LastWriteTime };
            retVal.Timestamp = retVal.Date.DateValue.Value;
            retVal.Status = new SVC.Messaging.FHIR.DataTypes.PrimitiveCode<string>("published");

            // Compose the codes if it has codes from a known code system
            var enumFields = enumType.GetFields();
            bool hasRegisteredCodes = Array.Exists(enumFields, (f) =>
            {
                var enumAtt = f.GetCustomAttribute<EnumerationAttribute>();

                if (enumAtt != null)
                    return ApplicationContext.ConfigurationService.OidRegistrar.FindData(enumAtt.SupplierDomain) != null;
                else
                    return false;
            }), hasDifferentSuppliers = Array.Exists(enumFields, (f) => {
                var enumAtt = f.GetCustomAttribute<EnumerationAttribute>();

                if (enumAtt != null)
                    return Array.Exists(enumFields, (fi) =>
                    {
                        var ienumAtt = fi.GetCustomAttribute<EnumerationAttribute>();

                        if (ienumAtt != null)
                            return ienumAtt.SupplierDomain != enumAtt.SupplierDomain;
                        return false;
                    });
                else
                    return false;
            });

            // Compose or define 
            var sysOid = ApplicationContext.ConfigurationService.OidRegistrar.FindData(retVal.Identifier) ;
            if (sysOid != null)
            {
                retVal.Compose = new ComposeDefinition();
                retVal.Compose.Import.Add(new SVC.Messaging.FHIR.DataTypes.FhirUri(sysOid.Ref));
            }
            else if (hasRegisteredCodes || hasDifferentSuppliers)
            {
                retVal.Compose = new ComposeDefinition();
                // Group like items
                Array.Sort(enumFields, (a, b) =>
                {
                    EnumerationAttribute aAtt = a.GetCustomAttribute<EnumerationAttribute>(),
                        bAtt = b.GetCustomAttribute<EnumerationAttribute>();


                    if ((aAtt == null) ^ (bAtt == null))
                        return aAtt == null ? -1 : 1;
                    else if (aAtt == bAtt)
                        return 0;
                    return aAtt.SupplierDomain.CompareTo(bAtt.SupplierDomain);

                });
                // Build the concept sets
                ConceptSet currentSet = null;
                foreach (var itm in enumFields)
                {
                    EnumerationAttribute enumValue = itm.GetCustomAttribute<EnumerationAttribute>();

                    if (enumValue == null) continue;

                    // Extract code system
                    var oidData = ApplicationContext.ConfigurationService.OidRegistrar.FindData(enumValue.SupplierDomain);
                    Uri codeSystem = oidData == null ? new Uri(String.Format("urn:oid:{0}", enumValue.SupplierDomain)) : oidData.Ref;

                    // add current set and construct
                    if (currentSet == null || !currentSet.System.Value.Equals(codeSystem))
                    {
                        currentSet = new ConceptSet() { System = codeSystem };
                        retVal.Compose.Include.Add(currentSet);
                    }

                    // Now add mnemonic
                    currentSet.Code.Add(new SVC.Messaging.FHIR.DataTypes.PrimitiveCode<string>(enumValue.Value));

                }

            }
            else
            {
                // Create a definition for a valueset
                retVal.Define = new ValueSetDefinition();
                retVal.Define.System = new Uri(String.Format("{0}/ValueSet/@v3-{1}", baseUri, structAtt.Name));
                foreach (var itm in enumFields)
                {
                    EnumerationAttribute enumValue = itm.GetCustomAttribute<EnumerationAttribute>();
                    
                    if (enumValue == null) continue;
                    DescriptionAttribute description = itm.GetCustomAttribute<DescriptionAttribute>();

                    retVal.Define.Concept.Add(new ConceptDefinition()
                    {
                        Code = new SVC.Messaging.FHIR.DataTypes.PrimitiveCode<string>(enumValue.Value),
                        Abstract = false,
                        Display = description == null ? itm.Name : description.Description
                    });

                }
            }
            
            return retVal;
        }

      
    }

}
