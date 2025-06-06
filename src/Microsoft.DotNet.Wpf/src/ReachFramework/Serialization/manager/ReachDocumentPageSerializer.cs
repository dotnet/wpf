﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.ComponentModel;
using System.Xml;
using System.Printing;
using System.Windows.Xps.Packaging;
using System.Windows.Documents;
using System.Windows.Media;

namespace System.Windows.Xps.Serialization
{
    /// <summary>
    /// 
    /// </summary>
    internal class DocumentPageSerializer :
                   ReachSerializer
    {
        /// <summary>
        /// 
        /// </summary>
        public
        DocumentPageSerializer(
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
            // The Image table at this time is shared / currentPage
            //
            ((XpsSerializationManager)SerializationManager).ResourcePolicy.CurrentPageImageTable = new Dictionary<int, Uri>();
            //
            // Create the ColorContextTable required by the Type Converters
            // The Image table at this time is shared / currentPage
            //
            ((XpsSerializationManager)SerializationManager).ResourcePolicy.CurrentPageColorContextTable = new Dictionary<int, Uri>();

            base.SerializeObject(serializedObject);
        }

        /// <summary>
        ///
        /// </summary>
        internal
        override
        void
        SerializeObject(
            SerializablePropertyContext serializedProperty
            )
        {
            //
            // Create the ImageTable required by the Type Converters
            // The Image table at this time is shared / currentPage
            //
            ((XpsSerializationManager)SerializationManager).ResourcePolicy.CurrentPageImageTable = new Dictionary<int, Uri>();
            //
            // Create the ColorContextTable required by the Type Converters
            // The Image table at this time is shared / currentPage
            //
            ((XpsSerializationManager)SerializationManager).ResourcePolicy.CurrentPageColorContextTable = new Dictionary<int, Uri>();

            base.SerializeObject(serializedProperty);
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
            if (SerializationManager is IXpsSerializationManager)
            {
                (SerializationManager as IXpsSerializationManager).RegisterPageStart();
            }

            //
            // A DocumentPage is persisted as a FixedPage
            //
            DocumentPage dp = (DocumentPage)serializableObjectContext.TargetObject;

            ReachTreeWalker treeWalker = new ReachTreeWalker(this);
            treeWalker.SerializeLinksInDocumentPage(dp);
            
            XmlWriter.WriteStartElement(XpsS0Markup.FixedPage);

            String xmlnsForType = SerializationManager.GetXmlNSForType(typeof(FixedPage));
            if (xmlnsForType != null)
            {
                XmlWriter.WriteAttributeString(XpsS0Markup.Xmlns, xmlnsForType);
                XmlWriter.WriteAttributeString(XpsS0Markup.XmlnsX, XpsS0Markup.XmlnsXSchema);

                if (SerializationManager.Language != null)
                {
                    XmlWriter.WriteAttributeString(XpsS0Markup.XmlLang, SerializationManager.Language.ToString());
                }
                else
                {
                    XmlWriter.WriteAttributeString(XpsS0Markup.XmlLang, XpsS0Markup.XmlLangValue);
                }
            }

            XpsSerializationPrintTicketRequiredEventArgs e = 
            new XpsSerializationPrintTicketRequiredEventArgs(PrintTicketLevel.FixedPagePrintTicket,
                                                             0);

            ((IXpsSerializationManager)SerializationManager).OnXPSSerializationPrintTicketRequired(e);

            PrintTicket printTicket = null;
            if(e.Modified)
            {
                printTicket = e.PrintTicket;
            }
            
            Size size = Toolbox.ValidateDocumentSize(dp.Size, printTicket);

            ((IXpsSerializationManager)SerializationManager).FixedPageSize = size;

            //
            //write length and width elements
            //
            WriteAttribute(XmlWriter, XpsS0Markup.PageWidth, size.Width);
            WriteAttribute(XmlWriter, XpsS0Markup.PageHeight, size.Height);

            //
            // Serialize the data for the PrintTicket
            //
            if(printTicket != null)
            {
                PrintTicketSerializer serializer = new PrintTicketSerializer(SerializationManager);
                serializer.SerializeObject(printTicket);
            }

            SerializeChild(dp.Visual, serializableObjectContext);

            ((XpsSerializationManager)SerializationManager).PackagingPolicy.PreCommitCurrentPage();

            //copy hyperlinks into stream
            treeWalker.CommitHyperlinks();

            XmlWriter.WriteEndElement();
            XmlWriter = null;
            //
            // Free the image table in use for this page
            //
            ((XpsSerializationManager)SerializationManager).ResourcePolicy.CurrentPageImageTable = null;
            //
            // Free the colorContext table in use for this page
            //
            ((XpsSerializationManager)SerializationManager).ResourcePolicy.CurrentPageColorContextTable = null;

            ((IXpsSerializationManager)SerializationManager).VisualSerializationService.ReleaseVisualTreeFlattener();

            if( SerializationManager is IXpsSerializationManager)
            {
               (SerializationManager as IXpsSerializationManager).RegisterPageEnd();
            }
            //
            // Signal to any registered callers that the Page has been serialized
            //
            XpsSerializationProgressChangedEventArgs progressEvent = 
            new XpsSerializationProgressChangedEventArgs(XpsWritingProgressChangeLevel.FixedPageWritingProgress,
                                                         0,
                                                         0,
                                                         null);

            ((IXpsSerializationManager)SerializationManager).OnXPSSerializationProgressChanged(progressEvent);
        }

        private void SerializeChild(Visual child, SerializableObjectContext parentContext)
        {
            ReachSerializer serializer = SerializationManager.GetSerializer(child);

            serializer?.SerializeObject(child);
        }

        private void WriteAttribute(XmlWriter writer, string name, object value)
        {
            writer.WriteAttributeString(name, TypeDescriptor.GetConverter(value).ConvertToInvariantString(value));
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
                    base.XmlWriter = SerializationManager.AcquireXmlWriter(typeof(FixedPage));
                }

                return base.XmlWriter;
            }

            set
            {
                base.XmlWriter = null;
                SerializationManager.ReleaseXmlWriter(typeof(FixedPage));
            }
        }
    };
}
