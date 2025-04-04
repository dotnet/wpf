// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Description:
//   base Parser class that parses XML markup into an Avalon Element Tree
//

using System.Xml;
using System.IO;
using System.Text;
using System.Windows.Markup.Primitives;

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
        public static string Save(object obj)
        {
            // Validate input arguments
            ArgumentNullException.ThrowIfNull(obj);

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
        public static void Save(object obj, TextWriter writer)
        {
            // Validate input arguments
            ArgumentNullException.ThrowIfNull(obj);
            ArgumentNullException.ThrowIfNull(writer);

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
        public static void Save(object obj, Stream stream)
        {
            // Validate input arguments
            ArgumentNullException.ThrowIfNull(obj);
            ArgumentNullException.ThrowIfNull(stream);

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
        public static void Save(object obj, XmlWriter xmlWriter)
        {
            // Validate input arguments
            ArgumentNullException.ThrowIfNull(obj);
            ArgumentNullException.ThrowIfNull(xmlWriter);

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
        public static void Save(object obj, XamlDesignerSerializationManager manager)
        {
            // Validate input arguments
            ArgumentNullException.ThrowIfNull(obj);
            ArgumentNullException.ThrowIfNull(manager);

            MarkupWriter.SaveAsXml(manager.XmlWriter, obj, manager);
        }

#endregion Public Methods
    }
}

