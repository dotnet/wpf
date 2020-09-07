// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      Set of internal string constants used to produce/consume XML that
//      adheres to the annotation schema.
//

using System;

namespace MS.Internal.Annotations
{
    /// <summary>
    ///     Internal class groups together constants used to produce/consume
    ///     XML that adheres to annotations schema.
    /// </summary>
    internal struct AnnotationXmlConstants
    {
        // AnnotationFramework schema namespaces
        internal struct Namespaces
        {
            /// <summary>
            ///     Namespace Uri for Annotations core schema.
            /// </summary>
            public const string CoreSchemaNamespace = "http://schemas.microsoft.com/windows/annotations/2003/11/core";

            /// <summary>
            ///     Namespace Uri for Annotations base schema.
            /// </summary>
            public const string BaseSchemaNamespace = "http://schemas.microsoft.com/windows/annotations/2003/11/base";
        }

        //prefixes, used for xml generation
        internal struct Prefixes
        {
            /// <summary>
            ///     Constant for "xml" prefix 
            /// </summary>
            internal const string XmlPrefix = "xml";

            /// <summary>
            ///     Constant for "xmlns" prefix 
            /// </summary>
            internal const string XmlnsPrefix = "xmlns";

            /// <summary>
            ///     Constant for standard core prefix 
            /// </summary>
            internal const string CoreSchemaPrefix = "anc";

            /// <summary>
            ///     Constant for standard base prefix 
            /// </summary>
            internal const string BaseSchemaPrefix = "anb";
        }

        // Names of Xml elements
        internal struct Elements
        {
            internal const string Annotation = "Annotation";
            internal const string Resource = "Resource";
            internal const string ContentLocator = "ContentLocator";
            internal const string ContentLocatorGroup = "ContentLocatorGroup";
            internal const string AuthorCollection = "Authors";
            internal const string AnchorCollection = "Anchors";
            internal const string CargoCollection = "Cargos";
            internal const string Item = "Item";  // Individual name/value pair within a ContentLocatorPart

            internal const string StringAuthor = "StringAuthor";
        }

        // Names of Xml attributes
        internal struct Attributes
        {
            internal const string Id = "Id";
            internal const string CreationTime = "CreationTime";
            internal const string LastModificationTime = "LastModificationTime";
            internal const string TypeName = "Type";
            internal const string ResourceName = "Name";
            internal const string ItemName = "Name";
            internal const string ItemValue = "Value";
        }
    }
}
