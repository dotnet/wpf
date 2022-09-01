// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Resources;

namespace System.Windows
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

        /// <summary>Line {0} Position {1}</summary>
        internal static string @ParserLineAndOffset => GetResourceString("ParserLineAndOffset");
        /// <summary>LC1001: Localization comment target property is not valid in string '{0}'.</summary>
        internal static string @InvalidLocCommentTarget => GetResourceString("InvalidLocCommentTarget");
        /// <summary>LC1002: Localization comment value is not valid for target property '{0}' in string '{1}'.</summary>
        internal static string @InvalidLocCommentValue => GetResourceString("InvalidLocCommentValue");
        /// <summary>LC1003: Localization comment has no value set for target property: '{0}'.</summary>
        internal static string @UnmatchedLocComment => GetResourceString("UnmatchedLocComment");
        /// <summary>LC1004: Localizability attribute setting '{0}' is not valid.</summary>
        internal static string @InvalidLocalizabilityValue => GetResourceString("InvalidLocalizabilityValue");
        /// <summary>MC3004: There are not enough attributes specified for '{0}'.</summary>
        internal static string @ParserAttributeArgsLow => GetResourceString("ParserAttributeArgsLow");
        /// <summary>MC3006: Mapper.SetAssemblyPath cannot accept an empty assemblyName.</summary>
        internal static string @ParserBadAssemblyName => GetResourceString("ParserBadAssemblyName");
        /// <summary>MC3007: Mapper.SetAssemblyPath cannot accept an empty assemblyPath.</summary>
        internal static string @ParserBadAssemblyPath => GetResourceString("ParserBadAssemblyPath");
        /// <summary>MC3010: '{0}' Name property value is not valid. Name must start with a letter or an underscore and can contain only letters, digits, and underscores.</summary>
        internal static string @ParserBadName => GetResourceString("ParserBadName");
        /// <summary>MC3011: Cannot find the static member '{0}' on the type '{1}'.</summary>
        internal static string @ParserInvalidStaticMember => GetResourceString("ParserInvalidStaticMember");
        /// <summary>MC3012: A key for a dictionary cannot be of type '{0}'. Only String, TypeExtension, and StaticExtension are supported.</summary>
        internal static string @ParserBadKey => GetResourceString("ParserBadKey");
        /// <summary>MC3015: The attached property '{0}' is not defined on '{1}' or one of its base classes.</summary>
        internal static string @ParserAttachedPropInheritError => GetResourceString("ParserAttachedPropInheritError");
        /// <summary>MC3016: Two new namespaces cannot be compatible with the same old namespace using an XmlnsCompatibility attribute.�'{0}' namespace is already marked compatible with '{1}'.</summary>
        internal static string @ParserCompatDuplicate => GetResourceString("ParserCompatDuplicate");
        /// <summary>MC3018: Cannot modify data in a sealed XmlnsDictionary.</summary>
        internal static string @ParserDictionarySealed => GetResourceString("ParserDictionarySealed");
        /// <summary>MC3022: All objects added to an IDictionary must have a Key attribute or some other type of key associated with them.</summary>
        internal static string @ParserNoDictionaryKey => GetResourceString("ParserNoDictionaryKey");
        /// <summary>MC3029: '{0}' member is not valid because it does not have a qualifying type name.</summary>
        internal static string @ParserBadMemberReference => GetResourceString("ParserBadMemberReference");
        /// <summary>MC3031: Keys and values in XmlnsDictionary must be strings.</summary>
        internal static string @ParserKeysAreStrings => GetResourceString("ParserKeysAreStrings");
        /// <summary>MC3032: '{1}' cannot be used as a value for '{0}'. Numbers are not valid enumeration values.</summary>
        internal static string @ParserNoDigitEnums => GetResourceString("ParserNoDigitEnums");
        /// <summary>MC3033: The property '{0}' has already been set on this markup extension and can only be set once.</summary>
        internal static string @ParserDuplicateMarkupExtensionProperty => GetResourceString("ParserDuplicateMarkupExtensionProperty");
        /// <summary>MC3038: MarkupExtension expressions must end with a '}'.</summary>
        internal static string @ParserMarkupExtensionNoEndCurlie => GetResourceString("ParserMarkupExtensionNoEndCurlie");
        /// <summary>MC3040: Format is not valid for MarkupExtension that specifies constructor arguments in '{0}'.</summary>
        internal static string @ParserMarkupExtensionBadConstructorParam => GetResourceString("ParserMarkupExtensionBadConstructorParam");
        /// <summary>MC3041: Markup extensions require a single '=' between name and value, and a single ',' between constructor parameters and name/value pairs. The arguments '{0}' are not valid.</summary>
        internal static string @ParserMarkupExtensionBadDelimiter => GetResourceString("ParserMarkupExtensionBadDelimiter");
        /// <summary>MC3042: Name/value pairs in MarkupExtensions must have the format 'Name = Value' and each pair is separated by a comma. '{0}' does not follow this format.</summary>
        internal static string @ParserMarkupExtensionNoNameValue => GetResourceString("ParserMarkupExtensionNoNameValue");
        /// <summary>MC3043: Names and Values in a MarkupExtension cannot contain quotes. The MarkupExtension arguments '{0}' are not valid.</summary>
        internal static string @ParserMarkupExtensionNoQuotesInName => GetResourceString("ParserMarkupExtensionNoQuotesInName");
        /// <summary>MC3044: The text '{1}' is not allowed after the closing '{0}' of a MarkupExtension expression.</summary>
        internal static string @ParserMarkupExtensionTrailingGarbage => GetResourceString("ParserMarkupExtensionTrailingGarbage");
        /// <summary>MC3045: Unknown property '{0}' for type '{1}' encountered while parsing a Markup Extension.</summary>
        internal static string @ParserMarkupExtensionUnknownAttr => GetResourceString("ParserMarkupExtensionUnknownAttr");
        /// <summary>MC3047: Internal parser error - Cannot use multiple writable BAML records at the same time.</summary>
        internal static string @ParserMultiBamls => GetResourceString("ParserMultiBamls");
        /// <summary>MC3048: '{0}' value is not a valid MarkupExtension expression. Cannot resolve '{1}' in namespace '{2}'. '{1}' must be a subclass of MarkupExtension.</summary>
        internal static string @ParserNotMarkupExtension => GetResourceString("ParserNotMarkupExtension");
        /// <summary>MC3050: Cannot find the type '{0}'. Note that type names are case sensitive.</summary>
        internal static string @ParserNoType => GetResourceString("ParserNoType");
        /// <summary>MC3061: The Element type '{0}' does not have an associated TypeConverter to parse the string '{1}'.</summary>
        internal static string @ParserDefaultConverterElement => GetResourceString("ParserDefaultConverterElement");
        /// <summary>MC3064: Only public or internal classes can be used within markup. '{0}' type is not public or internal.</summary>
        internal static string @ParserPublicType => GetResourceString("ParserPublicType");
        /// <summary>MC3066: The type reference cannot find a public type named '{0}'.</summary>
        internal static string @ParserResourceKeyType => GetResourceString("ParserResourceKeyType");
        /// <summary>MC3070: A single XAML file cannot reference more than 4,096 different assemblies.</summary>
        internal static string @ParserTooManyAssemblies => GetResourceString("ParserTooManyAssemblies");
        /// <summary>MC3071: '{0}' is an undeclared namespace.</summary>
        internal static string @ParserUndeclaredNS => GetResourceString("ParserUndeclaredNS");
        /// <summary>MC3079: MarkupExtensions are not allowed for Uid or Name property values, so '{0}' is not valid.</summary>
        internal static string @ParserBadUidOrNameME => GetResourceString("ParserBadUidOrNameME");
        /// <summary>MC3080: The {0} '{1}' cannot be set because it does not have an accessible {2} accessor.</summary>
        internal static string @ParserCantSetAttribute => GetResourceString("ParserCantSetAttribute");
        /// <summary>MC3083: Cannot access the delegate type '{0}' for the '{1}' event. '{0}' has incorrect access level or its assembly does not allow access.</summary>
        internal static string @ParserEventDelegateTypeNotAccessible => GetResourceString("ParserEventDelegateTypeNotAccessible");
        /// <summary>MC3091: '{0}' is not valid. Markup extensions require only spaces between the markup extension name and the first parameter. Cannot have comma or equals sign before the first parameter.</summary>
        internal static string @ParserMarkupExtensionDelimiterBeforeFirstAttribute => GetResourceString("ParserMarkupExtensionDelimiterBeforeFirstAttribute");
        /// <summary>MC3092: XmlLangProperty attribute must specify a property name.</summary>
        internal static string @ParserXmlLangPropertyValueInvalid => GetResourceString("ParserXmlLangPropertyValueInvalid");
        /// <summary>MC3094: Cannot convert string value '{0}' to type '{1}'.</summary>
        internal static string @ParserBadString => GetResourceString("ParserBadString");
        /// <summary>MC3100: '{0}' XML namespace prefix does not map to a namespace URI, so cannot resolve property '{1}'.</summary>
        internal static string @ParserPrefixNSProperty => GetResourceString("ParserPrefixNSProperty");
        /// <summary>MC4401: Serializer does not support Convert operations.</summary>
        internal static string @InvalidDeSerialize => GetResourceString("InvalidDeSerialize");
        /// <summary>MC4402: Serializer does not support custom BAML serialization operations.</summary>
        internal static string @InvalidCustomSerialize => GetResourceString("InvalidCustomSerialize");
        /// <summary>MC8001: Encountered a closing BracketCharacter '{0}' at Line Number '{1}' and Line Position '{2}' without a corresponding opening BracketCharacter.</summary>
        internal static string @ParserMarkupExtensionInvalidClosingBracketCharacers => GetResourceString("ParserMarkupExtensionInvalidClosingBracketCharacers");
        /// <summary>MC8002: BracketCharacter '{0}' at Line Number '{1}' and Line Position '{2}' does not have a corresponding opening/closing BracketCharacter.</summary>
        internal static string @ParserMarkupExtensionMalformedBracketCharacers => GetResourceString("ParserMarkupExtensionMalformedBracketCharacers");
    }
}
