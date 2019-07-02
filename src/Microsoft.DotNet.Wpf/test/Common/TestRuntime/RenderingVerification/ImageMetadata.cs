// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
namespace Microsoft.Test.RenderingVerification
{
    using System;
    using System.Xml;
    using System.Text;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    //using System.Runtime.Serialization.Formatters.Soap;
    using System.Reflection;

    /// <summary>
    /// Extract/add the OsInfo from/to an image
    /// </summary>
    [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Assert, Name = "FullTrust")]
    internal static class ImageMetadata
    {
        private const int EXIF_IMAGEINFO = 0x10B;
        private const int EXIF_XP_KEYWORD = 0x9C9E;
        private const int EXIF_XP_DESCRIPTION = 0x9C9C;
        private const int EXIF_VISTA_INFO = 0x2BC;
        private const string KEYWORD = "_KEYWORD_";
        private const string KEYWORD_TAG = "<rdf:li>" + KEYWORD + "</rdf:li>";
        private const string KEYWORD_TAGS = "_KEYWORD_TAGS_";
        private const string DESCRIPTIONS = "_DESCRIPTIONS_";

        private const string XMP_STRING = "<?xpacket begin='﻿' id='W5M0MpCehiHzreSzNTczkc9d'?>\r\n<xmp:xmpmeta xmlns:xmp=\"adobe:ns:meta/\"><rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\"><rdf:Description rdf:about=\"uuid:faf5bdd5-ba3d-11da-ad31-d33d75182f1b\" xmlns:exif=\"http://ns.adobe.com/exif/1.0/\"><exif:UserComment><rdf:Alt xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\"><rdf:li xml:lang=\"x-default\">" +
            DESCRIPTIONS +
            "</rdf:li></rdf:Alt>\r\n\t\t\t</exif:UserComment></rdf:Description><rdf:Description rdf:about=\"uuid:faf5bdd5-ba3d-11da-ad31-d33d75182f1b\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\"><dc:subject><rdf:Bag xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\">" +
            KEYWORD_TAGS +
            "</rdf:Bag>\r\n\t\t\t</dc:subject></rdf:Description><rdf:Description rdf:about=\"uuid:faf5bdd5-ba3d-11da-ad31-d33d75182f1b\" xmlns:MicrosoftPhoto=\"http://ns.microsoft.com/photo/1.0\"><MicrosoftPhoto:LastKeywordXMP><rdf:Bag xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\">" +
            KEYWORD_TAGS +
            "</rdf:Bag>\r\n\t\t\t</MicrosoftPhoto:LastKeywordXMP></rdf:Description></rdf:RDF></xmp:xmpmeta>\r\n<?xpacket end='w'?>                                                                                                    \n                                                                                                    \n                                                                                                    \n                                                                                                    \n                                                                                                    \n                                                                                                    \n                                                                                                    \n                                                                                                    \n                                                                                                    \n                                                                                                    \n                                                                                                    \n                                                                                                    \n                                                                                                    \n                                                                                                    \n                                                                                                    \n                                                                                                    \n                                                                                                    \n                                                                                                    \n                                                                                                    \n                                                                                                    \n                                                                                          ";

        private static XmlDocument xmlDoc = new XmlDocument();

        /// <summary>
        /// Retrieve the OSinfo stored in the image
        /// </summary>
        /// <param name="image">The image to parse</param>
        /// <returns>returne a MasterMetadata object containing the description and criteria</returns>
        public static MasterMetadata MetadataFromImage(Image image)
        {
            MasterMetadata retVal = new MasterMetadata();

            // Check if the image contains criteria & description
            List<int> idList = new List<int>();
            idList.AddRange(image.PropertyIdList);
            if (idList.Contains(EXIF_IMAGEINFO))
            {
                // Deserialize Description and Criteria from the image metadata (saved as xmlNode)

                PropertyItem pi = image.GetPropertyItem(EXIF_IMAGEINFO);

                // Remove trailing '\0' since it messes up deserialization
                string serializedInfo = System.Text.UTF8Encoding.UTF8.GetString(pi.Value);
                System.IO.MemoryStream memoryStream = new System.IO.MemoryStream(System.Text.UTF8Encoding.UTF8.GetBytes(serializedInfo.Trim('\0')));
                XmlReaderSettings readerSettings = new XmlReaderSettings();
                memoryStream.Position = 0;
                XmlReader reader = XmlReader.Create(memoryStream, readerSettings);
                while (reader.Read())
                {
                    string dim = string.Empty;
                    string val = string.Empty;
                    string index = string.Empty;

                    if (reader.Name == "Dim")
                    {
                        dim = reader["name"];
                        val = reader["value"];
                        retVal.Description[MasterMetadata.GetDimension(dim)] = val;
                    }
                    if (reader.Name == "Index")
                    {
                        index = reader["name"];
                        retVal._criteria.Add(MasterMetadata.GetDimension(index));
                    }
                }
            }

            return retVal;
        }
        /// <summary>
        /// Set the metadata OSinfo into an image
        /// </summary>
        /// <param name="metadata"></param>
        /// <param name="image"></param>
        public static void SetMetadataToImage(MasterMetadata metadata, Image image)
        {
            // Serialize Description and Criteria into an Xml node and put into image metadata
            using (System.IO.MemoryStream metadataStream = new System.IO.MemoryStream())
            {
                XmlWriterSettings writerSettings = new XmlWriterSettings();
                writerSettings.Encoding = System.Text.ASCIIEncoding.UTF8;
                using (XmlWriter writer = XmlWriter.Create(metadataStream, writerSettings))
                {
                    writer.WriteStartElement("MasterMetadata");
                    foreach (KeyValuePair<IMasterDimension, string> keyValue in metadata.Description)
                    {
                        writer.WriteStartElement("Description");
                        writer.WriteStartElement("Dim");
                        writer.WriteAttributeString("name", keyValue.Key.GetType().Name);
                        writer.WriteAttributeString("value", keyValue.Value);
                        writer.WriteEndElement(); // </Dim>
                        writer.WriteEndElement(); // </Description>
                    }
                    foreach (KeyValuePair<IMasterDimension, string> keyValue in metadata.Criteria)
                    {
                        writer.WriteStartElement("Criteria");
                        writer.WriteStartElement("Index");
                        writer.WriteAttributeString("name", keyValue.Key.GetType().Name);
                        writer.WriteEndElement(); // </Index>
                        writer.WriteEndElement(); // </Criteria>
                    }
                    writer.WriteEndElement(); // </MasterMetadata>;
                }

                // Workaround Win7 Bug 687567: GDI+ : 1.1 expects NULL terminated string when 
                //  the PropertyItem type is PropertyTagTypeASCII in SetPropertyItem, while 1.0 always append NULL terminator
                UTF8Encoding utf8Encoding = new UTF8Encoding();
                byte[] nullTerminator = utf8Encoding.GetBytes(new char[] { '\0' });
                metadataStream.WriteByte(nullTerminator[0]);

                metadataStream.Position = 0;

                // Build PropertyItem and save serialized info into it.
                PropertyItem pi = CreatePropertyItem();
                pi.Id = EXIF_IMAGEINFO;
                pi.Value = metadataStream.GetBuffer();
                pi.Len = pi.Value.Length;
                pi.Type = 2;
                image.SetPropertyItem(pi);
            }

            // Encode XP Keyword & description into image
            image.SetPropertyItem(BuildUserFriendlyTag(EXIF_XP_KEYWORD, metadata.Criteria));
            image.SetPropertyItem(BuildUserFriendlyTag(EXIF_XP_DESCRIPTION, metadata.Description));

            // Add Vista Comments and Tag (Description and Keyword counterpart to XP) 
            image.SetPropertyItem(AddVistaInfo(metadata));


        }

        private static PropertyItem CreatePropertyItem()
        {
            return (PropertyItem)typeof(PropertyItem).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { }, null).Invoke(new object[] { });
        }
        private static PropertyItem BuildUserFriendlyTag(int id, Dictionary<IMasterDimension, string> dimensionValueMap)
        {
            PropertyItem retVal = CreatePropertyItem();
            retVal.Id = id;

            string textCriteria = string.Empty;
            foreach (KeyValuePair<IMasterDimension, string> keyPair in dimensionValueMap)
            {
                textCriteria += keyPair.Key.GetType().Name + "=" + keyPair.Value + " / ";
            }

            byte[] buffer = System.Text.UTF8Encoding.UTF8.GetBytes(textCriteria);

            retVal.Value = buffer;
            retVal.Len = buffer.Length;
            retVal.Type = 2;

            return retVal;
        }
        private static PropertyItem AddVistaInfo(MasterMetadata metadata)
        {
            PropertyItem retVal = CreatePropertyItem();
            retVal.Id = EXIF_VISTA_INFO;
            retVal.Type = 1;

            List<string> keywords = new List<string>();
            if (metadata.Criteria != null)
            {
                foreach (KeyValuePair<IMasterDimension, string> criteriaKeyPair in metadata.Criteria)
                {
                    string keywordTag = KEYWORD_TAG.Replace(KEYWORD, criteriaKeyPair.Key.GetType().Name + "=" + criteriaKeyPair.Value);
                    keywords.Add(keywordTag);
                }
            }

            string descriptions = string.Empty;
            if (metadata.Description != null)
            {
                foreach (KeyValuePair<IMasterDimension, string> descKeyPair in metadata.Description)
                {
                    descriptions += descKeyPair.Key.GetType().Name + "=" + descKeyPair.Value + " / ";
                }
            }

            string xmpString = XMP_STRING;
            xmpString = xmpString.Replace(KEYWORD_TAGS, string.Join("", keywords.ToArray()));
            xmpString = xmpString.Replace(DESCRIPTIONS, descriptions);
            retVal.Value = System.Text.UTF8Encoding.UTF8.GetBytes(xmpString);
            retVal.Len = retVal.Value.Length;
            return retVal;
        }

    }
}
