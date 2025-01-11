// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: This class provides a XamlXmlReader implementation that implements an allow-list of legal
// types when calling into the Read method, meant to prevent instantiation of unexpected types.
//

using Microsoft.Win32;
using System.Xaml;
using System.Xml;

namespace System.Windows.Markup
{
    /// <summary>
    /// Provides a XamlXmlReader implementation that that implements an allow-list of legal types.
    /// </summary>
    internal class RestrictiveXamlXmlReader : System.Xaml.XamlXmlReader
    {
        private const string AllowedTypesForRestrictiveXamlContexts = @"SOFTWARE\Microsoft\.NETFramework\Windows Presentation Foundation\XPSAllowedTypes";
        private static readonly HashSet<string> AllXamlNamespaces = new HashSet<string>(XamlLanguage.XamlNamespaces);
        private static readonly Type DependencyObjectType = typeof(System.Windows.DependencyObject);
        private static readonly HashSet<string> SafeTypesFromRegistry = ReadAllowedTypesForRestrictedXamlContexts();

        private static HashSet<string> ReadAllowedTypesForRestrictedXamlContexts()
        {
            HashSet<string> allowedTypesFromRegistry = new HashSet<string>();
            try
            {
                // n.b. Registry64 uses the 32-bit registry in 32-bit operating systems.
                // The registry key should have this format and is consistent across netfx & netcore:
                //
                // [HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\.NETFramework\Windows Presentation Foundation\XPSAllowedTypes]
                // "SomeValue1"="Contoso.Controls.MyControl"
                // "SomeValue2"="Fabrikam.Controls.MyOtherControl"
                // ...
                //
                // The value names aren't important. The value data should match Type.FullName (including namespace but not assembly).
                // If any value data is exactly "*", this serves as a global opt-out and allows everything through the system.
                using (RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                {
                    if (hklm != null)
                    {
                        using (RegistryKey xpsDangerKey = hklm.OpenSubKey(AllowedTypesForRestrictiveXamlContexts, false))
                        {
                            if (xpsDangerKey != null)
                            {
                                foreach (string typeName in xpsDangerKey.GetValueNames())
                                {
                                    object value = xpsDangerKey.GetValue(typeName);
                                    if (value != null)
                                    {
                                        allowedTypesFromRegistry.Add(value.ToString());
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // do nothing
            }
            return allowedTypesFromRegistry;
        }

        /// <summary>
        /// Builds the restricted set based on RestrictedTypes that have already been loaded.
        /// </summary>
        public RestrictiveXamlXmlReader(XmlReader xmlReader, XamlSchemaContext schemaContext, XamlXmlReaderSettings settings) : base(xmlReader, schemaContext, settings)
        {
        }

        /// <summary>
        /// Builds the restricted set based on RestrictedTypes that have already been loaded but adds the list of Types passed in in safeTypes to the instance of _safeTypesSet
        /// </summary>
        internal RestrictiveXamlXmlReader(XmlReader xmlReader, XamlSchemaContext schemaContext, XamlXmlReaderSettings settings, List<Type> safeTypes) : base(xmlReader, schemaContext, settings)
        {
            if (safeTypes != null)
            {
                foreach (Type safeType in safeTypes)
                {
                    _safeTypesSet.Add(safeType);
                }
            }
        }
        /// <summary>

        /// Calls the base Read method to extract a node from the Xaml parser, if it's found to be a StartObject node for a type we want to restrict we skip that node.
        /// </summary>
        /// <returns>
        /// Returns the next available Xaml node skipping over dangerous types.
        /// </returns>
        public override bool Read()
        {
            bool result;
            int skippingDepth = 0;

            while (result = base.Read())
            {
                if (skippingDepth <= 0)
                {
                    if ((NodeType == System.Xaml.XamlNodeType.StartObject && !IsAllowedType(Type.UnderlyingType)) ||
                        (NodeType == System.Xaml.XamlNodeType.StartMember && Member is XamlDirective directive && !IsAllowedDirective(directive)))
                    {
                        skippingDepth = 1;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    switch (NodeType)
                    {
                        case System.Xaml.XamlNodeType.StartObject:
                        case System.Xaml.XamlNodeType.StartMember:
                        case System.Xaml.XamlNodeType.GetObject:
                            skippingDepth += 1;
                            break;

                        case System.Xaml.XamlNodeType.EndObject:
                        case System.Xaml.XamlNodeType.EndMember:
                            skippingDepth -= 1;
                            break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Determines whether an incoming directive is allowed.
        /// </summary>
        private bool IsAllowedDirective(XamlDirective directive)
        {
            // If the global opt-out switch is enabled, all directives are allowed.
            if (SafeTypesFromRegistry.Contains("*"))
            {
                return true;
            }

            // If this isn't a XAML directive, allow it through.
            // This allows XML directives and other non-XAML directives through.
            // This largely follows the logic at XamlMember.Equals, but we trigger for *any*
            // overlapping namespace rather than requiring the namespace sets to match exactly.
            bool isXamlDirective = false;
            foreach (string xmlns in directive.GetXamlNamespaces())
            {
                if (AllXamlNamespaces.Contains(xmlns))
                {
                    isXamlDirective = true;
                    break;
                }
            }

            if (!isXamlDirective)
            {
                return true;
            }

            // The following is an exhaustive list of all allowed XAML directives.
            if (directive.Name == XamlLanguage.Items.Name ||
                directive.Name == XamlLanguage.Key.Name ||
                directive.Name == XamlLanguage.Name.Name ||
                Member == XamlLanguage.PositionalParameters)
            {
                return true;
            }

            // This is a XAML directive but isn't in the allow-list; forbid it.
            return false;
        }

        /// <summary>
        /// Determines whether an incoming type is present in the allow list.
        /// </summary>
        private bool IsAllowedType(Type type)
        {
            // If the global opt-out switch is enabled, or if this type has been explicitly
            // allow-listed (or is null, meaning this is a proxy which will be checked elsewhere),
            // then it can come through.
            if (type is null || SafeTypesFromRegistry.Contains("*") || _safeTypesSet.Contains(type) || SafeTypesFromRegistry.Contains(type.FullName))
            {
                return true;
            }

            // We also have an implicit allow list which consists of:
            // - primitives (int, etc.); and
            // - any DependencyObject-derived type which exists in the System.Windows.* namespace.

            bool isValidNamespace = type.Namespace != null && (type.Namespace.Equals("System.Windows", StringComparison.Ordinal) || type.Namespace.StartsWith("System.Windows.", StringComparison.Ordinal));
            bool isValidSubClass = type.IsSubclassOf(DependencyObjectType);
            bool isValidPrimitive = type.IsPrimitive;

            if (isValidPrimitive || (isValidNamespace && isValidSubClass))
            {
                // Add it to the explicit allow list to make future lookups on this instance faster.
                _safeTypesSet.Add(type);
                return true;
            }

            // Otherwise, it didn't exist on any of our allow lists.
            return false;
        }

        /// <summary>
        /// Per instance set of allow-listed types, may grow at runtime to encompass implicit allow list.
        /// </summary>
        private HashSet<Type> _safeTypesSet = new HashSet<Type>() { 
            typeof(System.Windows.ResourceDictionary),
            typeof(System.Windows.StaticResourceExtension),
            typeof(System.Windows.Documents.DocumentStructures.FigureStructure),
            typeof(System.Windows.Documents.DocumentStructures.ListItemStructure),
            typeof(System.Windows.Documents.DocumentStructures.ListStructure),
            typeof(System.Windows.Documents.DocumentStructures.NamedElement),
            typeof(System.Windows.Documents.DocumentStructures.ParagraphStructure),
            typeof(System.Windows.Documents.DocumentStructures.SectionStructure),
            typeof(System.Windows.Documents.DocumentStructures.StoryBreak),
            typeof(System.Windows.Documents.DocumentStructures.StoryFragment),
            typeof(System.Windows.Documents.DocumentStructures.StoryFragments),
            typeof(System.Windows.Documents.DocumentStructures.TableCellStructure),
            typeof(System.Windows.Documents.DocumentStructures.TableRowGroupStructure),
            typeof(System.Windows.Documents.DocumentStructures.TableRowStructure),
            typeof(System.Windows.Documents.DocumentStructures.TableStructure),
            typeof(System.Windows.Documents.LinkTarget)          
            };  
    }
}
