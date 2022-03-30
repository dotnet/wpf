// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
    internal static partial class SR
    {
        public static string Get(string name)
        {
            return GetResourceString(name, null);
        }

        public static string Get(string name, params object[] args)
        {
            return Format(GetResourceString(name, null), args);
        }

        // Remove properties below when this project uses properties for resources.

        /// <summary>Cannot promote from '{0}' to '{1}' because the target map is too small.</summary>
        internal static string @FrugalList_TargetMapCannotHoldAllData => GetResourceString("FrugalList_TargetMapCannotHoldAllData");
        /// <summary>Cannot promote from Array.</summary>
        internal static string @FrugalList_CannotPromoteBeyondArray => GetResourceString("FrugalList_CannotPromoteBeyondArray");
        /// <summary>'{0}' type name does not have the expected format 'className, assembly'.</summary>
        internal static string @QualifiedNameHasWrongFormat => GetResourceString("QualifiedNameHasWrongFormat");
        /// <summary>Too many attributes are specified for '{0}'.</summary>
        internal static string @ParserAttributeArgsHigh => GetResourceString("ParserAttributeArgsHigh");
        /// <summary>'{0}' requires more attributes.</summary>
        internal static string @ParserAttributeArgsLow => GetResourceString("ParserAttributeArgsLow");
        /// <summary>Cannot load assembly '{0}' because a different version of that same assembly is loaded '{1}'.</summary>
        internal static string @ParserAssemblyLoadVersionMismatch => GetResourceString("ParserAssemblyLoadVersionMismatch");
        /// <summary>Choice is valid only in AlternateContent.</summary>
        internal static string @XCRChoiceOnlyInAC => GetResourceString("XCRChoiceOnlyInAC");
        /// <summary>Choice cannot follow a Fallback.</summary>
        internal static string @XCRChoiceAfterFallback => GetResourceString("XCRChoiceAfterFallback");
        /// <summary>Choice must contain Requires attribute.</summary>
        internal static string @XCRRequiresAttribNotFound => GetResourceString("XCRRequiresAttribNotFound");
        /// <summary>Requires attribute must contain a valid namespace prefix.</summary>
        internal static string @XCRInvalidRequiresAttribute => GetResourceString("XCRInvalidRequiresAttribute");
        /// <summary>Fallback is valid only in AlternateContent.</summary>
        internal static string @XCRFallbackOnlyInAC => GetResourceString("XCRFallbackOnlyInAC");
        /// <summary>AlternateContent must contain one or more Choice elements.</summary>
        internal static string @XCRChoiceNotFound => GetResourceString("XCRChoiceNotFound");
        /// <summary>AlternateContent must contain only one Fallback element.</summary>
        internal static string @XCRMultipleFallbackFound => GetResourceString("XCRMultipleFallbackFound");
        /// <summary>'{0}' attribute is not valid for '{1}' element.</summary>
        internal static string @XCRInvalidAttribInElement => GetResourceString("XCRInvalidAttribInElement");
        /// <summary>Unrecognized Compatibility element '{0}'.</summary>
        internal static string @XCRUnknownCompatElement => GetResourceString("XCRUnknownCompatElement");
        /// <summary>'{0}' element is not a valid child of AlternateContent. Only Choice and Fallback elements are valid children of an AlternateContent element.</summary>
        internal static string @XCRInvalidACChild => GetResourceString("XCRInvalidACChild");
        /// <summary>'{0}' format is not valid.</summary>
        internal static string @XCRInvalidFormat => GetResourceString("XCRInvalidFormat");
        /// <summary>'{0}' prefix is not defined.</summary>
        internal static string @XCRUndefinedPrefix => GetResourceString("XCRUndefinedPrefix");
        /// <summary>Unrecognized compatibility attribute '{0}'.</summary>
        internal static string @XCRUnknownCompatAttrib => GetResourceString("XCRUnknownCompatAttrib");
        /// <summary>'{0}' namespace cannot process content; it must be declared Ignorable first.</summary>
        internal static string @XCRNSProcessContentNotIgnorable => GetResourceString("XCRNSProcessContentNotIgnorable");
        /// <summary>Duplicate ProcessContent declaration for element '{1}' in namespace '{0}'.</summary>
        internal static string @XCRDuplicateProcessContent => GetResourceString("XCRDuplicateProcessContent");
        /// <summary>Cannot have both a specific and a wildcard ProcessContent declaration for namespace '{0}'.</summary>
        internal static string @XCRInvalidProcessContent => GetResourceString("XCRInvalidProcessContent");
        /// <summary>Duplicate wildcard ProcessContent declaration for namespace '{0}'.</summary>
        internal static string @XCRDuplicateWildcardProcessContent => GetResourceString("XCRDuplicateWildcardProcessContent");
        /// <summary>MustUnderstand condition failed on namespace '{0}'</summary>
        internal static string @XCRMustUnderstandFailed => GetResourceString("XCRMustUnderstandFailed");
        /// <summary>'{0}' namespace cannot preserve items; it must be declared Ignorable first.</summary>
        internal static string @XCRNSPreserveNotIgnorable => GetResourceString("XCRNSPreserveNotIgnorable");
        /// <summary>Duplicate Preserve declaration for element {1} in namespace '{0}'.</summary>
        internal static string @XCRDuplicatePreserve => GetResourceString("XCRDuplicatePreserve");
        /// <summary>Cannot have both a specific and a wildcard Preserve declaration for namespace '{0}'.</summary>
        internal static string @XCRInvalidPreserve => GetResourceString("XCRInvalidPreserve");
        /// <summary>Duplicate wildcard Preserve declaration for namespace '{0}'.</summary>
        internal static string @XCRDuplicateWildcardPreserve => GetResourceString("XCRDuplicateWildcardPreserve");
        /// <summary>'{0}' attribute value is not a valid XML name.</summary>
        internal static string @XCRInvalidXMLName => GetResourceString("XCRInvalidXMLName");
        /// <summary>There is a cycle of XML compatibility definitions, such that namespace '{0}' overrides itself. This could be due to inconsistent XmlnsCompatibilityAttributes in different assemblies. Please change the definitions to eliminate this cycle.</summary>
        internal static string @XCRCompatCycle => GetResourceString("XCRCompatCycle");
    }
}
