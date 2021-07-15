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
using System.Printing;
using System.Security;
using System.ComponentModel.Design.Serialization;
using System.Windows.Xps.Packaging;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Markup;
using System.Windows.Shapes;

namespace System.Windows.Xps.Serialization
{
    /// <summary>
    /// 
    /// </summary>
    internal class DocumentPageSerializerAsync :
                   ReachSerializerAsync
    {
        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        public
        DocumentPageSerializerAsync(
            PackageSerializationManager manager
            )
            :
        base(manager)
        {

        }

        #endregion Constructors


        #region Public Methods

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

            base.SerializeObject(serializedObject);
        }

        public
        override
        void
        AsyncOperation(
            ReachSerializerContext context
            )
        {
            if(context == null)
            {

            }
           
            switch (context.Action) 
            {
                case SerializerAction.endPersistObjectData:
                {
                    EndPersistObjectData();
                    break;
                }
                
                case SerializerAction.endSerializeDocumentPage:
                {
                    ReachFixedPageSerializerContext thisContext = context as ReachFixedPageSerializerContext;
                    if(thisContext == null)
                    {

                    }
                    EndSerializeDocumentPage(thisContext);
                    break;
                }
    
                default:
                {
                    base.AsyncOperation(context);
                    break;
                }
            }
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

        #endregion Public Methods

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

            ReachSerializerContext context = new ReachFixedPageSerializerContext(this,
                                                                                 serializableObjectContext,
                                                                                 SerializerAction.endSerializeDocumentPage,
                                                                                 false,
                                                                                 treeWalker);

            ((IXpsSerializationManagerAsync)SerializationManager).OperationStack.Push(context);

            //
            // Serialize the data for the PrintTicket
            //
            if (printTicket != null)
            {
                PrintTicketSerializerAsync serializer = new PrintTicketSerializerAsync(SerializationManager);
                serializer.SerializeObject(printTicket);
            }
   
            SerializeChild(dp.Visual, serializableObjectContext);
            
        }

        private void SerializeChild(Visual child, SerializableObjectContext parentContext)
        {
            ReachSerializer serializer = SerializationManager.GetSerializer(child);

            if (serializer != null)
            {
                serializer.SerializeObject(child);
            }
        }

        private
        void
        EndSerializeDocumentPage(
            ReachSerializerContext context
            )
        {
            ReachFixedPageSerializerContext thisContext = context as ReachFixedPageSerializerContext;

            if(thisContext != null)
            {
                ((XpsSerializationManager)SerializationManager).PackagingPolicy.PreCommitCurrentPage();

                //copy hyperlinks into stream
                thisContext.TreeWalker.CommitHyperlinks();

                XmlWriter.WriteEndElement();
                XmlWriter = null;
                //
                // Free the image table in use for this page
                //
                ((XpsSerializationManagerAsync)SerializationManager).ResourcePolicy.CurrentPageImageTable = null;
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

        }

        private void WriteAttribute(XmlWriter writer, string name, object value)
        {
            writer.WriteAttributeString(name, TypeDescriptor.GetConverter(value).ConvertToInvariantString(value));
        }
    };
}
