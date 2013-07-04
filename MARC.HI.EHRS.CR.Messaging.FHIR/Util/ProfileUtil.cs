using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Messaging.FHIR.Resources;
using MARC.HI.EHRS.SVC.Messaging.FHIR;
using MARC.HI.EHRS.SVC.Messaging.FHIR.Attributes;
using System.Reflection;
using System.ServiceModel.Web;
using System.Xml.Serialization;
using System.ComponentModel;
using MARC.HI.EHRS.SVC.Messaging.FHIR.DataTypes;
using System.Diagnostics;
using MARC.Everest.Exceptions;

namespace MARC.HI.EHRS.CR.Messaging.FHIR.Util
{
    /// <summary>
    /// Profile utility
    /// </summary>
    public static class ProfileUtil
    {

        /// <summary>
        /// Profiles that have been built
        /// </summary>
        private static Dictionary<String, Profile> s_builtProfiles = new Dictionary<string,Profile>();

        /// <summary>
        /// Synchronization lock
        /// </summary>
        private static Object s_syncLock = new object();

        /// <summary>
        /// Get a profile
        /// </summary>
        public static Profile GetProfile(string profileId)
        {
            Profile retVal = null;
            if (!s_builtProfiles.TryGetValue(profileId, out retVal))
            {
                BuildAllProfiles();
                s_builtProfiles.TryGetValue(profileId, out retVal);
            }
            return retVal;
        }

        /// <summary>
        /// Get all profiles 
        /// </summary>
        public static List<Profile> GetProfiles()
        {
            if(s_builtProfiles.Count == 0)
                BuildAllProfiles();
            List<Profile> retVal = new List<Profile>();
            foreach (var kv in s_builtProfiles)
                retVal.Add(kv.Value);
            return retVal;
        }

        /// <summary>
        /// Build all profiles from all assemblies in the app domain!
        /// </summary>
        private static void BuildAllProfiles()
        {
            Trace.TraceInformation("Starting profile compilation process...");           
            lock (s_syncLock)
                foreach(var asm in AppDomain.CurrentDomain.GetAssemblies())
                    foreach (var typ in asm.GetTypes())
                    {
                        ProfileAttribute profileAtt = typ.GetCustomAttribute<ProfileAttribute>();

                        if (profileAtt == null)
                            continue; // No profile not interested

                        // First, have we already started a profile with this name
                        Profile context = null;
                        if(!s_builtProfiles.TryGetValue(profileAtt.ProfileId, out context))
                        {
                            context = new Profile();
                            context.VersionId = asm.GetName().Version.ToString();
                            s_builtProfiles.Add(profileAtt.ProfileId, context);
                        }

                        ProcessProfileAttribute(profileAtt, context);

                        // Next we want to process any ResourceProfiles to set resource scope
                        ResourceProfileAttribute resourceAtt = typ.GetCustomAttribute<ResourceProfileAttribute>();
                        if (typeof(ResourceBase).IsAssignableFrom(typ))
                            ProcessResourceMembers(typ, context, resourceAtt);
                        else
                            ProcessMembers(typ, context, resourceAtt);
                    }
            Trace.TraceInformation("Profile compilation process complete : {0} profiles supported by this service", s_builtProfiles.Count);

        }

        /// <summary>
        /// Get property from path
        /// </summary>
        public static PropertyInfo GetPropertyFromPath(this Type me, String path)
        {
            String[] tokens = path == null ? new string[0] : path.Split('.');
            PropertyInfo retVal = null;
            Type context = me;
            foreach (var tkn in tokens)
            {
                retVal = context.GetProperty(tkn);
                if (retVal == null)
                    break;
                Type realType = retVal.PropertyType;
                while (realType.IsGenericType)
                    realType = realType.GetGenericArguments()[0];
                context = realType;
            }
            return retVal;
        }

        /// <summary>
        /// Create a FHIR friendly traversal name
        /// </summary>
        public static String CreateXmlTraversalName(this Type me, String path)
        {
            String[] tokens = path == null ? new string[0] : path.Split('.');
            XmlTypeAttribute root = me.GetCustomAttribute<XmlTypeAttribute>();
            if (root == null) return null;
            StringBuilder retVal = new StringBuilder(root.TypeName);
            
            // Now traverse
            Type context = me;
            foreach (var tkn in tokens)
            {
                var pi = context.GetProperty(tkn);
                if (pi == null)
                    break;
                var xmlElement = pi.GetCustomAttribute<XmlElementAttribute>();
                if(xmlElement == null)
                    break;

                retVal.AppendFormat(".{0}", xmlElement.ElementName);
                
                Type realType = pi.PropertyType;
                while (realType.IsGenericType)
                    realType = realType.GetGenericArguments()[0];
                context = realType;
            }
            return retVal.ToString();
            
        }

        /// <summary>
        /// Find or create an element definition
        /// </summary>
        public static Element FindOrCreateElement(this Structure me, ElementProfileAttribute att)
        {
            return null;
        }

        /// <summary>
        /// Find a structure definition, barring that create one
        /// </summary>
        public static Structure FindOrCreateStructureDefinition(this Profile me, Type resourceType)
        {
            Structure retVal = me.Structure.Find(o => o.ResouceType == resourceType);
            if (retVal == null)
            {
                retVal = new Structure()
                {
                    ResouceType = resourceType
                };
                me.Structure.Add(retVal);

                // Construct the structure's default profile
                ProcessResourceMembers(resourceType, me, resourceType.GetCustomAttribute<ResourceProfileAttribute>());
            }
            return retVal;
        }

        /// <summary>
        /// Process resource members
        /// </summary>
        private static void ProcessResourceMembers(Type resourceType, Profile context, ResourceProfileAttribute resourceAtt)
        {
            // Get the resource name
            Structure structureContext = context.FindOrCreateStructureDefinition(resourceType);

            // Now process the resource members
            structureContext.Name = resourceAtt.Name == null ? null : resourceAtt.Name;
            // Add the root element
            XmlRootAttribute rootAtt = resourceType.GetCustomAttribute<XmlRootAttribute>();
            if (rootAtt != null && structureContext.Elements.Find(o => o.Path == rootAtt.ElementName) == null)
                structureContext.Elements.Add(new Element(resourceType));

            // Now add property elements
            var typeStack = new Stack<Type>();
            typeStack.Push(resourceType);
            ProcessResourceProperties(typeStack, structureContext, String.Empty);

        }

        /// <summary>
        /// Process all properties in a resource
        /// </summary>
        private static void ProcessResourceProperties(Stack<Type> stack, Structure context, string path)
        {

            Type resourceType = stack.Last(),
                scanType = stack.Peek();

            foreach (var propInfo in scanType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                // Get traversal name and load appropriate element
                string traversalName = String.Empty;
                string propertyPath = path;
                if (!String.IsNullOrEmpty(propertyPath))
                    propertyPath += "." + propInfo.Name;
                else
                    propertyPath = propInfo.Name;

                traversalName = resourceType.CreateXmlTraversalName(propertyPath);

                // Is there a definition?
                Element definition = context.Elements.Find(o => o.Path == traversalName);
                if (definition == null)
                {
                    definition = new Element()
                    {
                        Path = traversalName,
                        Definition = new ElementDefinition(propInfo)
                    };
                    context.Elements.Add(definition);
                }
                else // override
                {
                    ElementProfileAttribute attribute = propInfo.GetCustomAttribute<ElementProfileAttribute>();
                    if (attribute != null)
                    {
                        definition.Definition.Binding = attribute.Binding != null ? (FhirString)attribute.Binding.GetValueSetDefinition().ToString() : definition.Definition.Binding;
                        definition.Definition.FormalDefinition = attribute.FormalDefinition ?? definition.Definition.FormalDefinition;
                        definition.Definition.MaxOccurs = attribute.MaxOccurs == -1 ? "*" : attribute.MaxOccurs.ToString();
                        definition.Definition.MinOccurs = attribute.MinOccurs;
                        definition.Definition.MustSupport = attribute.MustSupport;
                        definition.Definition.MustUnderstand = attribute.MustUnderstand;
                        definition.Definition.ShortDefinition = attribute.ShortDescription ?? definition.Definition.ShortDefinition;
                        definition.Definition.Comments = attribute.Comment;
                    }
                }

                // Navigate down?
                Type realType = propInfo.PropertyType;
                while (realType.IsGenericType)
                    realType = realType.GetGenericArguments()[0];

                if (!realType.Namespace.EndsWith("DataTypes") && !typeof(ResourceBase).IsAssignableFrom(realType) && !stack.Contains(realType))
                {
                        stack.Push(realType);
                        ProcessResourceProperties(stack, context, propertyPath);
                        stack.Pop();
                }

            }

        }

        /// <summary>
        /// Process all members in the scanned type
        /// </summary>
        private static void ProcessMembers(Type hostType, Profile context, ResourceProfileAttribute resourceAtt)
        {

            if (typeof(ResourceBase).IsAssignableFrom(hostType))
                throw new ArgumentException("Use ProcessResourceMembers to process source resource members");
            
            Structure structureContext = null;
            // Do we have a resource attribute, if so it becomes the master context
            if (resourceAtt != null)
            {
                structureContext = context.FindOrCreateStructureDefinition(resourceAtt.Resource);
                structureContext.Name = resourceAtt.Name ?? structureContext.Name;
            }

            // Scan the members
            foreach (var member in hostType.GetMembers())
            {
                
                // Does this member have any profiled elements?
                ElementProfileAttribute[] elements = member.GetCustomAttributes<ElementProfileAttribute>();
                if (elements != null)
                    foreach (var attribute in elements)
                    {
                        // First get the type that we're scoped to
                        Type typeContext = null;
                        if (attribute.HostType != null)
                            typeContext = attribute.HostType;
                        else if (resourceAtt != null)
                            typeContext = resourceAtt.Resource;
                        else
                            throw new InvalidOperationException("Could not compile profile. Missing type binding");
                        structureContext = context.FindOrCreateStructureDefinition(typeContext);

                        // No Path? Use this path 
                        if(String.IsNullOrEmpty(attribute.Property))
                            attribute.Property = member.Name;

                        // Get the property ... Traverse down the dotted names
                        var definitionPath = typeContext.CreateXmlTraversalName(attribute.Property);
                        var definition = structureContext.Elements.Find(o => o.Path == definitionPath);
                        if (definition == null)
                            Trace.TraceWarning("Could not find resource path {0}, not applying profile attribute", definitionPath);
                        else
                        {
                            definition.Definition.Binding = attribute.Binding != null ? (FhirString)attribute.Binding.GetValueSetDefinition().ToString() : definition.Definition.Binding;
                            definition.Definition.FormalDefinition = attribute.FormalDefinition ?? definition.Definition.FormalDefinition;
                            definition.Definition.MaxOccurs = attribute.MaxOccurs == -1 ? "*" : attribute.MaxOccurs.ToString();
                            definition.Definition.MinOccurs = attribute.MinOccurs;
                            definition.Definition.MustSupport = attribute.MustSupport;
                            definition.Definition.MustUnderstand = attribute.MustUnderstand;
                            definition.Definition.ShortDefinition = attribute.ShortDescription ?? definition.Definition.ShortDefinition;
                            definition.Definition.Comments = attribute.Comment ?? definition.Definition.Comments;
                        }


                    }
                 // Element profiles

                // Search parameter modifiers
                SearchParameterProfileAttribute[] searchParams = member.GetCustomAttributes<SearchParameterProfileAttribute>();
                if (searchParams != null)
                {
                    // Check resource attribute
                    if (resourceAtt == null)
                        throw new InvalidOperationException("Cannot create search parameter with null context");
                    structureContext = context.FindOrCreateStructureDefinition(resourceAtt.Resource);

                    // Iterate through the search param attributes
                    foreach (var sp in searchParams)
                    {
                        // Is this an update?
                        var definition = structureContext.SearchParams.Find(o => o.Name == sp.Name);
                        if (definition == null)
                        {
                            definition = new SearchParam()
                            {
                                Name = sp.Name,
                                Documentation = sp.Description,
                                Type = new PrimitiveCode<string>(sp.Type)
                            };
                            structureContext.SearchParams.Add(definition);
                        }
                        else
                        {
                            definition.Name = sp.Name;
                            definition.Documentation = sp.Description;
                            definition.Type = new PrimitiveCode<string>(sp.Type);
                        };
                    }
                } // search parameters

                // Extensions!!!
                ExtensionDefinitionAttribute[] extensions = member.GetCustomAttributes<ExtensionDefinitionAttribute>();
                if (extensions != null)
                {
                    foreach (var ext in extensions)
                    {
                        Type typeContext = null;
                        if (ext.HostType != null)
                            typeContext = ext.HostType;
                        else
                            throw new InvalidOperationException("Could not compile profile. Missing type binding");
               
                        // Get the property ... Traverse down the dotted names
                        var definitionPath = typeContext.CreateXmlTraversalName(ext.Property);
                        var definition = context.ExtensionDefinition.Find(o => o.Code == ext.Name);
                        var extensionTarget = typeContext.GetPropertyFromPath(ext.Property);

                        if (definition != null) // add the context
                            definition.Context.Add(definitionPath);
                        else
                        {
                            definition = new ExtensionDefinition()
                            {
                                Code = new PrimitiveCode<string>(ext.Name),
                                Definition = new ElementDefinition()
                                {
                                    Binding = ext.Binding == null ? null : ext.Binding.GetValueSetDefinition().ToString(),
                                    FormalDefinition = ext.FormalDefinition,
                                    ShortDefinition = ext.ShortDescription,
                                    MustSupport = ext.MustSupport,
                                    MustUnderstand = ext.MustUnderstand,
                                    Type = new List<TypeRef>() { TypeRef.MakeTypeRef(new Type[] { ext.ValueType }) }
                                }
                            };
                            // Add the type and contexts
                            definition.Context.Add(definitionPath);

                            if (ext.Property == null)
                                definition.ContextType = new PrimitiveCode<string>(typeof(ResourceBase).IsAssignableFrom(typeContext) ? "resource" : "datatype");
                            else if (extensionTarget == null)
                                definition.ContextType = new PrimitiveCode<string>("extension");
                            else
                                definition.ContextType = new PrimitiveCode<string>("datatype");

                            context.ExtensionDefinition.Add(definition);

                        }


                    }
                }

            }
        }

        /// <summary>
        /// Process the properties in the specified attribute
        /// </summary>
        private static void ProcessProfileAttribute(ProfileAttribute profileAttribute, Profile context)
        {
            context.Identifier = profileAttribute.ProfileId ?? context.Identifier;
            context.Name = profileAttribute.Name ?? context.Name;
            context.Id = context.Identifier;
            context.Description = "Automatically generated by the MARC-HI Service Core Framework"; // TODO: Put this in strings file
            context.Publisher = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyCompanyAttribute>().Company;
            
            // Import URL
            if (profileAttribute.Import != null)
            {
                Uri importUrl = null;
                if (!Uri.TryCreate(profileAttribute.Import, UriKind.Absolute, out importUrl))
                    importUrl = new Uri(String.Format("{0}/Profile/@{1}", WebOperationContext.Current.IncomingRequest.UriTemplateMatch.BaseUri, profileAttribute.Import));
                if (!context.Import.Exists(o => o.Uri.Value.Equals(importUrl)))
                    context.Import.Add(new ProfileImport() { Uri = importUrl });
            }

        }


    }
}
