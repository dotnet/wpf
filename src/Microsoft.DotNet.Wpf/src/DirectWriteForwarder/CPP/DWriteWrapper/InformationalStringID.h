// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef  __INFORMATIONALSTRINGID_H
#define  __INFORMATIONALSTRINGID_H
 
namespace MS { namespace Internal { namespace Text { namespace TextInterface
{
    /// <summary>
    /// The informational string enumeration identifies a string in a font.
    /// </summary>
    private enum class InformationalStringID
    {
        /// <summary>
        /// Unspecified name ID.
        /// </summary>
        None,

        /// <summary>
        /// Copyright notice provided by the font.
        /// </summary>
        CopyrightNotice,

        /// <summary>
        /// String containing a version number.
        /// </summary>
        VersionStrings,

        /// <summary>
        /// Trademark information provided by the font.
        /// </summary>
        Trademark,

        /// <summary>
        /// Name of the font manufacturer.
        /// </summary>
        Manufacturer,

        /// <summary>
        /// Name of the font designer.
        /// </summary>
        Designer,

        /// <summary>
        /// URL of font designer (with protocol, e.g., http://, ftp://).
        /// </summary>
        DesignerURL,

        /// <summary>
        /// Description of the font. Can contain revision information, usage recommendations, history, features, etc.
        /// </summary>
        Description,

        /// <summary>
        /// URL of font vendor (with protocol, e.g., http://, ftp://). If a unique serial number is embedded in the URL, it can be used to register the font.
        /// </summary>
        FontVendorURL,

        /// <summary>
        /// Description of how the font may be legally used, or different example scenarios for licensed use. This field should be written in plain language, not legalese.
        /// </summary>
        LicenseDescription,

        /// <summary>
        /// URL where additional licensing information can be found.
        /// </summary>
        LicenseInfoURL,

        /// <summary>
        /// GDI-compatible family name. Because GDI allows a maximum of four fonts per family, fonts in the same family may have different GDI-compatible family names
        /// (e.g., "Arial", "Arial Narrow", "Arial Black").
        /// </summary>
        WIN32FamilyNames,

        /// <summary>
        /// GDI-compatible subfamily name.
        /// </summary>
        Win32SubFamilyNames,

        /// <summary>
        /// Family name preferred by the designer. This enables font designers to group more than four fonts in a single family without losing compatibility with
        /// GDI. This name is typically only present if it differs from the GDI-compatible family name.
        /// </summary>
        PreferredFamilyNames,

        /// <summary>
        /// Subfamily name preferred by the designer. This name is typically only present if it differs from the GDI-compatible subfamily name. 
        /// </summary>
        PreferredSubFamilyNames,

        /// <summary>
        /// Sample text. This can be the font name or any other text that the designer thinks is the best example to display the font in.
        /// </summary>
        SampleText
    };

}}}}//MS::Internal::Text::TextInterface

#endif //__INFORMATIONALSTRINGID_H