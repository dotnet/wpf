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
    internal class DocumentPaginatorSerializerAsync :
                   ReachSerializerAsync
    {
        
        /// <summary>
        /// 
        /// </summary>
        public
        DocumentPaginatorSerializerAsync(
            PackageSerializationManager manager
            )
            :
        base(manager)
        {

        }

        #region Public Methods
        
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

                case SerializerAction.serializeNextDocumentPage:
                {
                    SerializeNextDocumentPage(context);
                    break;
                }
                
                default:
                {
                    base.AsyncOperation(context);
                    break;
                }
            }
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
            SerializableObjectContext serializableObjectContext = new SerializableObjectContext(serializedObject, null);
            PersistObjectData(serializableObjectContext);
 
        }

        #endregion Public Methods



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
            if( SerializationManager is XpsSerializationManager)
            {
               (SerializationManager as XpsSerializationManager).RegisterDocumentStart();
            }
            String xmlnsForType = SerializationManager.GetXmlNSForType(typeof(FixedDocument));
            String nameForType = XpsS0Markup.FixedDocument;

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

                ReachSerializerContext context = new ReachSerializerContext(this,
                                                                            SerializerAction.endPersistObjectData);

                ((IXpsSerializationManagerAsync)SerializationManager).OperationStack.Push(context);

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
                        PrintTicketSerializerAsync serializer = new PrintTicketSerializerAsync(SerializationManager);
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

                int index = 0;

                DocumentPaginatorSerializerContext 
                collectionContext = new DocumentPaginatorSerializerContext(this,
                                                                           serializableObjectContext,
                                                                           paginator,
                                                                           index,
                                                                           SerializerAction.serializeNextDocumentPage);

                ((IXpsSerializationManagerAsync)SerializationManager).OperationStack.Push(collectionContext);
            }
        }


        internal
        override
        void
        EndPersistObjectData(
            )
        {
            XmlWriter.WriteEndElement();
            XmlWriter = null;
            //
            // Clear off the table from the packaging policy
            //
            ((XpsSerializationManager)SerializationManager).ResourcePolicy.ImageCrcTable = null;

            ((XpsSerializationManager)SerializationManager).ResourcePolicy.ImageUriHashTable = null;

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

        private
        void
        SerializeNextDocumentPage(
            ReachSerializerContext  context
            )
        {

            DocumentPaginatorSerializerContext paginatorContext = context as DocumentPaginatorSerializerContext;

            if(paginatorContext != null)
            {
                DocumentPaginator  paginator = paginatorContext.Paginator;
                int                index     = paginatorContext.Index;

                if(!paginator.IsPageCountValid ||
                   (index < paginator.PageCount))
                {
                    index++;


                    DocumentPaginatorSerializerContext 
                    collectionContext = new DocumentPaginatorSerializerContext(this,
                                                                               paginatorContext.ObjectContext,
                                                                               paginator,
                                                                               index,
                                                                               SerializerAction.serializeNextDocumentPage);
                    ((IXpsSerializationManagerAsync)SerializationManager).OperationStack.Push(collectionContext);

                    DocumentPage page = Toolbox.GetPage(paginator, index-1);
                    
                    ReachSerializer serializer = SerializationManager.GetSerializer(page);
                    if (serializer != null)
                    {
                        serializer.SerializeObject(page);
                    }
                }
            }
            else
            {

            }
        }
    };

    internal class DocumentPaginatorSerializerContext :
                   ReachSerializerContext
    {
        public
        DocumentPaginatorSerializerContext(
            ReachSerializerAsync        serializer,
            SerializableObjectContext   objectContext,
            DocumentPaginator           paginator,
            int                         index,
            SerializerAction            action
            ):
            base(serializer,objectContext,action)
        {
            this._paginator = paginator;
            this._index     = index;
        }


        public
        DocumentPaginator
        Paginator
        {
            get
            {
                return _paginator;
            }
        }


        public
        int
        Index
        {
            get
            {
                return _index;
            }
        }


        private
        DocumentPaginator   _paginator;

        private
        int                 _index;
    };

}


