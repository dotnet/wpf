// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//  This is a class for representing a PackageRelationshipCollection. This is an internal
//  class for manipulating relationships associated with a part 
//
// Details:
//   This class handles serialization to/from relationship parts, creation of those parts
//   and offers methods to create, delete and enumerate relationships. This code was
//   moved from the PackageRelationshipCollection class.
//
//
//
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;                           // for XmlReader/Writer
using System.IO.Packaging;
using System.Windows;                       // For Exception strings - SR
using System.IO;
using System.Diagnostics;
using System.Windows.Markup;                // For XMLCompatibilityReader

using MS.Internal;                          // For Invariant.
using MS.Internal.WindowsBase;

using MS.Internal.IO.Packaging.Extensions;
using PackageRelationship = MS.Internal.IO.Packaging.Extensions.PackageRelationship;

namespace MS.Internal.IO.Packaging
{
    /// <summary>
    /// Collection of all the relationships corresponding to a given source PackagePart
    /// </summary>
    internal static class InternalRelationshipCollection
    {
               /// <summary>
        /// Write one Relationship element for each member of relationships.
        /// This method is used by XmlDigitalSignatureProcessor code as well
        /// </summary>
        internal static void WriteRelationshipsAsXml(XmlWriter writer, IEnumerable<System.IO.Packaging.PackageRelationship> relationships, bool alwaysWriteTargetModeAttribute, bool inStreamingProduction)
        {
            foreach (System.IO.Packaging.PackageRelationship relationship in relationships)
            {
                writer.WriteStartElement(RelationshipTagName);

                // Write RelationshipType attribute.
                writer.WriteAttributeString(TypeAttributeName, relationship.RelationshipType);

                // Write Target attribute.
                // We would like to persist the uri as passed in by the user and so we use the
                // OriginalString property. This makes the persisting behavior consistent
                // for relative and absolute Uris. 
                // Since we accpeted the Uri as a string, we are at the minimum guaranteed that
                // the string can be converted to a valid Uri. 
                // Also, we are just using it here to persist the information and we are not
                // resolving or fetching a resource based on this Uri.
                writer.WriteAttributeString(TargetAttributeName, relationship.TargetUri.OriginalString);

                // TargetMode is optional attribute in the markup and its default value is TargetMode="Internal" 
                if (alwaysWriteTargetModeAttribute || relationship.TargetMode == TargetMode.External)
                    writer.WriteAttributeString(TargetModeAttributeName, relationship.TargetMode.ToString());

                // Write Id attribute.
                writer.WriteAttributeString(IdAttributeName, relationship.Id);

                writer.WriteEndElement();
            }
        }


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------


        private static readonly string RelationshipTagName      = "Relationship";
        private static readonly string TargetAttributeName      = "Target";
        private static readonly string TypeAttributeName        = "Type";
        private static readonly string IdAttributeName          = "Id";
        private static readonly string TargetModeAttributeName  = "TargetMode";
    }
}

