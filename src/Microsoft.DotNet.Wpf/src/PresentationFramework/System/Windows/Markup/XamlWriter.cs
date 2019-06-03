// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//   base Parser class that parses XML markup into an Avalon Element Tree
//

using System;
using System.Xml;
using System.IO;
using System.IO.Packaging;
using System.Windows;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Threading;

using MS.Utility;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using System.Text;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Windows.Markup.Primitives;

using MS.Internal.IO.Packaging;
using MS.Internal.PresentationFramework;

namespace System.Windows.Markup
{
    /// <summary>
    /// Parsing class used to create an Windows Presentation Platform Tree
    /// </summary>
    public static class XamlWriter
    {
#region Public Methods

        /// <summary>
        ///     Save gets the xml respresentation 
        ///     for the given object instance
        /// </summary>
        /// <param name="obj">
        ///     Object instance
        /// </param>
        /// <returns>
        ///     XAML string representing object instance
        /// </returns>
        /// <SecurityNote>
        ///     Critcal: We only allow Serialization in partial trust.  Although we would throw an exception later anyways,
        ///     we throw one here so we know where to expect the exception.  (BUG: 1466639)
        ///     Safe: Demands unmanaged code permissions
        /// </SecurityNote>
        /// <remarks>
        ///     This API requires unmanaged code permission 
        /// </remarks>
        [SecuritySafeCritical]
        public static string Save(object obj)
        {
            // Must be in full trust
            SecurityHelper.DemandUnmanagedCode();
            
            // Validate input arguments
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            // Create TextWriter
            StringBuilder sb = new StringBuilder();
            TextWriter writer = new StringWriter(sb, TypeConverterHelper.InvariantEnglishUS);

            try
            {
                Save(obj, writer);
            }
            finally
            {
                // Close writer
                writer.Close();
            }
            
            return sb.ToString();
        }

        /// <summary>
        ///     Save writes the xml respresentation 
        ///     for the given object instance using the given writer
        /// </summary>
        /// <param name="obj">
        ///     Object instance
        /// </param>
        /// <param name="writer">
        ///     Text Writer
        /// </param>
        /// <SecurityNote>
        ///     Critcal: We only allow Serialization in full trust.  Although we would throw an exception later anyways,
        ///     we throw one here so we know where to expect the exception.  (BUG: 1466639)
        ///     Safe: Demands unmanaged code permissions
        /// </SecurityNote>
        /// <remarks>
        ///     This API requires unmanaged code permission 
        /// </remarks>
        [SecuritySafeCritical]
        public static void Save(object obj, TextWriter writer)
        {
            // Must be in full trust
            SecurityHelper.DemandUnmanagedCode();
            
            // Validate input arguments
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            // Create XmlTextWriter
            XmlTextWriter xmlWriter = new XmlTextWriter(writer);

            MarkupWriter.SaveAsXml(xmlWriter, obj);
        }

        /// <summary>
        ///     Save writes the xml respresentation 
        ///     for the given object instance to the given stream
        /// </summary>
        /// <param name="obj">
        ///     Object instance
        /// </param>
        /// <param name="stream">
        ///     Stream
        /// </param>
        /// <SecurityNote>
        ///     Critcal: We only allow Serialization in full trust.  Although we would throw an exception later anyways,
        ///     we throw one here so we know where to expect the exception.  (BUG: 1466639)
        ///     Safe: Demands unmanaged code permissions
        /// </SecurityNote>
        /// <remarks>
        ///     This API requires unmanaged code permission 
        /// </remarks>
        [SecuritySafeCritical]
        public static void Save(object obj, Stream stream)
        {
            // Must be in full trust
            SecurityHelper.DemandUnmanagedCode();
            
            // Validate input arguments
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            // Create XmlTextWriter
            XmlTextWriter xmlWriter = new XmlTextWriter(stream, null);

            MarkupWriter.SaveAsXml(xmlWriter, obj);
        }

        /// <summary>
        ///     Save writes the xml respresentation 
        ///     for the given object instance using the given 
        ///     writer. In addition it also allows the designer 
        ///     to participate in this conversion.
        /// </summary>
        /// <param name="obj">
        ///     Object instance
        /// </param>
        /// <param name="xmlWriter">
        ///     XmlWriter
        /// </param>
        /// <SecurityNote>
        ///     Critcal: We only allow Serialization in full trust.  Although we would throw an exception later anyways,
        ///     we throw one here so we know where to expect the exception.  (BUG: 1466639)
        ///     Safe: Demands unmanaged code permissions
        /// </SecurityNote>
        /// <remarks>
        ///     This API requires unmanaged code permission 
        /// </remarks>
        [SecuritySafeCritical]
        public static void Save(object obj, XmlWriter xmlWriter)
        {
            // Must be in full trust
            SecurityHelper.DemandUnmanagedCode();
            
            // Validate input arguments
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }
            if (xmlWriter == null)
            {
                throw new ArgumentNullException("xmlWriter");
            }

            try
            {
                MarkupWriter.SaveAsXml(xmlWriter, obj);
            }
            finally
            {
                xmlWriter.Flush();
            }
        }

        /// <summary>
        ///     Save writes the xml respresentation 
        ///     for the given object instance using the 
        ///     given XmlTextWriter embedded in the manager.
        /// </summary>
        /// <param name="obj">
        ///     Object instance
        /// </param>
        /// <param name="manager">
        ///     Serialization Manager
        /// </param>
        /// <SecurityNote>
        ///     Critcal: We only allow Serialization in full trust.  Although we would throw an exception later anyways,
        ///     we throw one here so we know where to expect the exception.  (BUG: 1466639)
        ///     Safe: Demands unmanaged code permissions
        /// </SecurityNote>
        /// <remarks>
        ///     This API requires unmanaged code permission 
        /// </remarks>
        [SecuritySafeCritical]
        public static void Save(object obj, XamlDesignerSerializationManager manager)
        {
            // Must be in full trust
            SecurityHelper.DemandUnmanagedCode();
            
            // Validate input arguments
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }

            MarkupWriter.SaveAsXml(manager.XmlWriter, obj, manager);
        }

#endregion Public Methods
    }
}

