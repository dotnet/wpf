// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Xml;
using System.IO;
using System.Security;
using System.ComponentModel.Design.Serialization;
using System.Windows.Xps.Packaging;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Markup;

namespace System.Windows.Xps.Serialization
{
    /// <summary>
    /// 
    /// </summary>
    internal class DocumentPaginatorSerializer :
                   ReachSerializer
    {
        /// <summary>
        /// 
        /// </summary>
        public
        DocumentPaginatorSerializer(
            PackageSerializationManager manager
            )
            :
        base(manager)
        {

        }

        public
        override
        void
        SerializeObject(
            Object serializedObject
            )
        {
            //
            // Create the ImageTable required by the Type Converters
            // The Image table at this time is shared / document
            //
            ((XpsSerializationManager)SerializationManager).ResourcePolicy.ImageCrcTable = new Dictionary<UInt32, Uri>();

            ((XpsSerializationManager)SerializationManager).ResourcePolicy.ImageUriHashTable = new Dictionary<int,Uri>();
            //
            // Create the ColorContextTable required by the Type Converters
            // The ColorContext table at this time is shared / document
            //
            ((XpsSerializationManager)SerializationManager).ResourcePolicy.ColorContextTable = new Dictionary<int, Uri>();
            SerializableObjectContext serializableObjectContext = new SerializableObjectContext(serializedObject, null);
            PersistObjectData(serializableObjectContext);
        }


        /// <summary>
        ///
        /// </summary>
        internal
        override
        void
        PersistObjectData(
            SerializableObjectContext serializableObjectContext
            )
        {
            String xmlnsForType = SerializationManager.GetXmlNSForType(typeof(FixedDocument));
            String nameForType = XpsS0Markup.FixedDocument;

            if( SerializationManager is XpsSerializationManager)
            {
               (SerializationManager as XpsSerializationManager).RegisterDocumentStart();
            }

            if (xmlnsForType == null)
            {
                XmlWriter.WriteStartElement(nameForType);
            }
            else
            {
                XmlWriter.WriteStartElement(nameForType,
                                            xmlnsForType);
            }

            {
                XpsSerializationPrintTicketRequiredEventArgs e = 
                new XpsSerializationPrintTicketRequiredEventArgs(PrintTicketLevel.FixedDocumentPrintTicket,
                                                                 0);

                ((IXpsSerializationManager)SerializationManager).OnXPSSerializationPrintTicketRequired(e);

                //
                // Serialize the data for the PrintTicket
                //
                if(e.Modified)
                {
                    if(e.PrintTicket != null)
                    {
                        PrintTicketSerializer serializer = new PrintTicketSerializer(SerializationManager);
                        serializer.SerializeObject(e.PrintTicket);
                    }
                }

                DocumentPaginator paginator = (DocumentPaginator)serializableObjectContext.TargetObject;

                XmlLanguage language = null;

                DependencyObject dependencyObject = paginator.Source as DependencyObject;
                if (dependencyObject != null)
                {
                    language = (XmlLanguage)dependencyObject.GetValue(FrameworkContentElement.LanguageProperty);
                }

                if (language == null)
                {
                    //If the language property is null, assign the language to the default
                    language = XmlLanguage.GetLanguage(XpsS0Markup.XmlLangValue);
                }

                SerializationManager.Language = language;

                for (int i = 0; !paginator.IsPageCountValid || (i < paginator.PageCount); i++)
                {
                    DocumentPage page = Toolbox.GetPage(paginator, i);

                    ReachSerializer serializer = SerializationManager.GetSerializer(page);

                    if (serializer != null)
                    {
                        serializer.SerializeObject(page);
                    }
                }
            }

            XmlWriter.WriteEndElement();
            XmlWriter = null;
            //
            // Clear off the table from the resource policy
            //
             ((XpsSerializationManager)SerializationManager).ResourcePolicy.ImageCrcTable = null;

            ((XpsSerializationManager)SerializationManager).ResourcePolicy.ImageUriHashTable = null;
            //
            // Clear off the table from the resource policy
            //
            ((XpsSerializationManager)SerializationManager).ResourcePolicy.ColorContextTable = null;
            //
            // Signal to any registered callers that the Document has been serialized
            //
            XpsSerializationProgressChangedEventArgs progressEvent = 
            new XpsSerializationProgressChangedEventArgs(XpsWritingProgressChangeLevel.FixedDocumentWritingProgress,
                                                         0,
                                                         0,
                                                         null);

            if( SerializationManager is XpsSerializationManager)
            {
               (SerializationManager as XpsSerializationManager).RegisterDocumentEnd();
            }
            ((IXpsSerializationManager)SerializationManager).OnXPSSerializationProgressChanged(progressEvent);
        }

        /// <summary>
        /// 
        /// </summary>
        public
        override
        XmlWriter
        XmlWriter
        {
            get
            {
                if (base.XmlWriter == null)
                {
                    base.XmlWriter = SerializationManager.AcquireXmlWriter(typeof(FixedDocument));
                }

                return base.XmlWriter;
            }

            set
            {
                base.XmlWriter = null;
                SerializationManager.ReleaseXmlWriter(typeof(FixedDocument));
            }
        }
    };
}


