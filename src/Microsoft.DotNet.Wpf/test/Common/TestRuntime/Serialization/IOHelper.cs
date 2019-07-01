// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading;
using System.Xml;
using System.Text;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Input;

namespace Microsoft.Test.Serialization
{
    /// <summary>
    /// Helper routines for converting and saving various forms of text, xml, etc.
    /// </summary>
    public class IOHelper
    {
        /// <summary>
        /// Wrapper of Application.LoadComponent.
        /// </summary>
        /// <param name="resourceLocator">Location of component.</param>
        /// <returns>Tree root.</returns>
        [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Assert, Name = "FullTrust")]
        public static object LoadComponent(Uri resourceLocator)
        {
            return System.Windows.Application.LoadComponent(resourceLocator);
        }

        /// <summary>
        /// Converts a text string to a Stream.
        /// </summary>
        [CLSCompliant(false)]
        static public Stream ConvertTextToStream(string text)
        {
            System.Text.Encoding encoding = System.Text.Encoding.Unicode;
            byte[] encodedBytes = encoding.GetBytes(text);
            byte[] preamble = encoding.GetPreamble();
            byte[] identifiableContent;

            if (preamble.Length == 0)
            {
                identifiableContent = encodedBytes;
            }
            else
            {
                identifiableContent = new byte[preamble.Length + encodedBytes.Length];
                preamble.CopyTo(identifiableContent, 0);
                encodedBytes.CopyTo(identifiableContent, preamble.Length);
            }

            return new MemoryStream(identifiableContent);
        }

        /// <summary>
        /// Writes an XmlDocument to a stream.
        /// </summary>
        /// <param name="document">The XmlDocument to write.</param>
        /// <returns>A Stream of xml.</returns>
        [CLSCompliant(false)]
        static public Stream ConvertXmlDocumentToStream(XmlDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException("document");
            }

            // Save xml to stream.
            Stream stream = new MemoryStream();
            XmlTextWriter writer = new XmlTextWriter(stream, System.Text.Encoding.Unicode);
            writer.Formatting = Formatting.Indented;

            document.WriteTo(writer);
            writer.Flush();

            // Reposition stream at beginning.
            stream.Seek(0, SeekOrigin.Begin);

            return stream;
        }

        /// <summary>
        /// Writes a text string to a file.
        /// </summary>
        /// <param name="content">The string content to write.</param>
        /// <param name="fileName">The name of the file to create.</param>
        static public void SaveTextToFile(string content, string fileName)
        {
            FileStream fs = null;
            StreamWriter sw = null;

            try
            {
                fs = new FileStream(fileName, FileMode.Create);
                sw = new StreamWriter(fs);
                sw.Write(content);
            }
            finally
            {
                if (null != sw)
                    sw.Close();

                if (null != fs)
                    fs.Close();
            }
        }

        /// <summary>
        /// Writes a stream of text to a file.
        /// </summary>
        /// <param name="content">The string content to write.</param>
        /// <param name="fileName">The name of the file to create.</param>
        [CLSCompliant(false)]
        static public void SaveTextToFile(Stream content, string fileName)
        {
            if (content == null)
            {
                throw new ArgumentNullException("content");
            }

            //read out
            string xaml = null;
            StreamReader sr = new StreamReader(content);

            xaml = sr.ReadToEnd();

            // Reposition stream at beginning so the caller
            // may use the stream again.
            content.Seek(0, SeekOrigin.Begin);

            StreamWriter sw = new StreamWriter(fileName);

            try
            {
                sw.Write(xaml);
                sw.Flush();
            }
            finally
            {
                sw.Close();
            }
        }

        /// <summary>
        /// Opens a file and serializes the object to SOAP format.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <param name="fileName">The name of the file to create.</param>
        [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Assert, Name = "FullTrust")]
        static public void SaveObjectToFile(object obj, string fileName)
        {
            Stream stream = File.Open(fileName, FileMode.Create);

            try
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, obj);
            }
            finally
            {
                stream.Close();
            }
        }

        /// <summary>
        /// Opens a file and deserializes an object from SOAP format.
        /// </summary>
        /// <param name="fileName">The name of the file to open.</param>
        [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Assert, Name = "FullTrust")]
        static public object LoadObjectFromFile(string fileName)
        {
            Stream stream = File.Open(fileName, FileMode.Open, FileAccess.Read);
            object obj = null;

            try
            {
                BinaryFormatter formatter = new BinaryFormatter();
                obj = formatter.Deserialize(stream);
            }
            finally
            {
                stream.Close();
            }

            return obj;
        }

        /// <summary>
        /// Constructs a new Cursor object using the given file name.
        /// </summary>
        /// <param name="fileName">The name of the cursor file to open.</param>
        [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Assert, Name = "FullTrust")]
        static public Cursor LoadCursorObjectFromFile(string fileName)
        {
            Stream stream = File.Open(fileName, FileMode.Open, FileAccess.Read);
            Cursor cursor = null;

            try
            {
                cursor = new Cursor(stream);
            }
            finally
            {
                stream.Close();
            }

            return cursor;
        }

    }
}
