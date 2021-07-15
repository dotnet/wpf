// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++
                                                                              
                                                                   
    Abstract:
        This file contains the definition of a class that
        defines the common functionality required to serialize 
        a FixedPage
                                                                        
--*/
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

namespace System.Windows.Xps.Serialization
{
    /// <summary>
    /// Class defining common functionality required to
    /// serialize a FixedPage.
    /// </summary>
    internal class FixedPageSerializerAsync :
                   ReachSerializerAsync
    {
        #region Constructor

        /// <summary>
        /// Constructor for class FixedPageSerializer
        /// </summary>
        /// <param name="manager">
        /// The serialization manager, the services of which are
        /// used later in the serialization process of the type.
        /// </param>
        public
        FixedPageSerializerAsync(
            PackageSerializationManager manager
            ):
        base(manager)
        {
            
        }

        #endregion Constructor
        
        #region Public Methods
        
        public
        override
        void
        AsyncOperation(
            ReachSerializerContext context
            )
        {
            if(context != null)
            {
                switch (context.Action) 
                {
                    case SerializerAction.endPersistObjectData:
                    {
                        EndPersistObjectData();
                        break;
                    }

                    case SerializerAction.endSerializeReachFixedPage:
                    {
                        ReachFixedPageSerializerContext thisContext = context as ReachFixedPageSerializerContext;
                        if(thisContext == null)
                        {

                        }
                        EndSerializeReachFixedPage(thisContext);
                        break;
                    }

                    default:
                    {
                        base.AsyncOperation(context);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// The main method that is called to serialize a FixedPage.
        /// </summary>
        /// <param name="serializedObject">
        /// Instance of object to be serialized.
        /// </param>
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
            ((XpsSerializationManagerAsync)SerializationManager).ResourcePolicy.CurrentPageImageTable = new Dictionary<int, Uri>();
            //
            // Create the ColorContextTable required by the Type Converters
            // The Image table at this time is shared / currentPage
            //
            ((XpsSerializationManagerAsync)SerializationManager).ResourcePolicy.CurrentPageColorContextTable = new Dictionary<int, Uri>();

            base.SerializeObject(serializedObject);
        }

        #endregion Public Methods

        #region Internal Methods
        
        /// <summary>
        /// The main method that is called to serialize the FixedPage
        /// and that is usually called from within the serialization manager 
        /// when a node in the graph of objects is at a turn where it should 
        /// be serialized.
        /// </summary>
        /// <param name="serializedProperty">
        /// The context of the property being serialized at this time and
        /// it points internally to the object encapsulated by that node.
        /// </param>
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
            ((XpsSerializationManagerAsync)SerializationManager).ResourcePolicy.CurrentPageImageTable = new Dictionary<int, Uri>();
            //
            // Create the ColorContextTable required by the Type Converters
            // The Image table at this time is shared / currentPage
            //
            ((XpsSerializationManagerAsync)SerializationManager).ResourcePolicy.CurrentPageColorContextTable = new Dictionary<int, Uri>();

            base.SerializeObject(serializedProperty);
        }

        /// <summary>
        /// The method is called once the object data is discovered at that 
        /// point of the serialization process.
        /// </summary>
        /// <param name="serializableObjectContext">
        /// The context of the object to be serialized at this time.
        /// </param>
        internal
        override
        void
        PersistObjectData(
            SerializableObjectContext   serializableObjectContext
            )
        {
            if(serializableObjectContext == null)
            {
                throw new ArgumentNullException("serializableObjectContext");
            }

            FixedPage fixedPage = serializableObjectContext.TargetObject as FixedPage;

            if( SerializationManager is IXpsSerializationManager)
            {
                (SerializationManager as IXpsSerializationManager).RegisterPageStart();
            }

            ReachTreeWalker treeWalker = new ReachTreeWalker(this);
            treeWalker.SerializeLinksInFixedPage((FixedPage)serializableObjectContext.TargetObject);

            String xmlnsForType = SerializationManager.GetXmlNSForType(serializableObjectContext.TargetObject.GetType());

            if(xmlnsForType == null)
            {
                XmlWriter.WriteStartElement(serializableObjectContext.Name);
            }
            else
            {
                XmlWriter.WriteStartElement(serializableObjectContext.Name);
                
                XmlWriter.WriteAttributeString(XpsS0Markup.Xmlns, xmlnsForType);
                XmlWriter.WriteAttributeString(XpsS0Markup.XmlnsX, XpsS0Markup.XmlnsXSchema);

                XmlLanguage language = fixedPage.Language;
                if (language == null)
                {
                    //If the language property is null, assign the language to the default
                    language = XmlLanguage.GetLanguage(XpsS0Markup.XmlLangValue);
                }

                SerializationManager.Language = language;

                XmlWriter.WriteAttributeString(XpsS0Markup.XmlLang, language.ToString());
            }
            {
                Size fixedPageSize = new Size(fixedPage.Width, fixedPage.Height);
                ((IXpsSerializationManager)SerializationManager).FixedPageSize = fixedPageSize;
            
                //
                // Before we serialize any properties on the FixedPage, we need to 
                // serialize the FixedPage as a Visual
                //
                Visual fixedPageAsVisual = serializableObjectContext.TargetObject as Visual;

                bool   needEndVisual     = false;

                if(fixedPageAsVisual != null)
                {
                    needEndVisual = SerializePageAsVisual(fixedPageAsVisual);
                }

                if (serializableObjectContext.IsComplexValue)
                {
                    ReachSerializerContext context = new ReachFixedPageSerializerContext(this,
                                                                                         serializableObjectContext,
                                                                                         SerializerAction.endSerializeReachFixedPage,
                                                                                         needEndVisual,
                                                                                         treeWalker);

                    ((IXpsSerializationManagerAsync)SerializationManager).OperationStack.Push(context);

                    PrintTicket printTicket = ((IXpsSerializationManager)SerializationManager).FixedPagePrintTicket;
                    
                    if(printTicket != null)
                    {
                        PrintTicketSerializer serializer = new PrintTicketSerializer(SerializationManager);
                        serializer.SerializeObject(printTicket);
                        ((IXpsSerializationManager)SerializationManager).FixedPagePrintTicket = null;
                    }


                    SerializeObjectCore(serializableObjectContext);
                }
                else
                {
                    throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_WrongPropertyTypeForFixedPage));
                }

            }
        }

        /// <summary>
        /// This method is the one that writes out the attribute within
        /// the xml stream when serializing simple properties.
        /// </summary>
        /// <param name="serializablePropertyContext">
        /// The property that is to be serialized as an attribute at this time.
        /// </param>
        internal
        override
        void
        WriteSerializedAttribute(
            SerializablePropertyContext serializablePropertyContext
            )
        {
            if(serializablePropertyContext == null)
            {
                throw new ArgumentNullException("serializablePropertyContext");
            }

            String attributeValue = String.Empty;

            attributeValue = GetValueOfAttributeAsString(serializablePropertyContext);

            if ( (attributeValue != null) && 
                 (attributeValue.Length > 0) )
            {
                //
                // Emit name="value" attribute
                //
                XmlWriter.WriteAttributeString(serializablePropertyContext.Name, attributeValue);
            }
        }

        /// <summary>
        /// Converts the Value of the Attribute to a String by calling into 
        /// the appropriate type converters.
        /// </summary>
        /// <param name="serializablePropertyContext">
        /// The property that is to be serialized as an attribute at this time.
        /// </param>
        private
        String
        GetValueOfAttributeAsString(
            SerializablePropertyContext serializablePropertyContext
            )
        {
            if(serializablePropertyContext == null)
            {
                throw new ArgumentNullException("serializablePropertyContext");
            }

            String valueAsString                  = null;
            Object targetObjectContainingProperty = serializablePropertyContext.TargetObject;
            Object propertyValue                  = serializablePropertyContext.Value;

            if(propertyValue != null)
            {
                TypeConverter typeConverter = serializablePropertyContext.TypeConverter;

                valueAsString = typeConverter.ConvertToInvariantString(new XpsTokenContext(SerializationManager,
                                                                                             serializablePropertyContext),
                                                                       propertyValue);


                if (typeof(Type).IsInstanceOfType(propertyValue))
                {
                    int index = valueAsString.LastIndexOf('.');

                    if (index > 0)
                    {
                        valueAsString = valueAsString.Substring(index + 1);
                    }

                    valueAsString = XpsSerializationManager.TypeOfString + valueAsString + "}";
                }
            }
            else
            {
                valueAsString = XpsSerializationManager.NullString;
            }

            return valueAsString;
        }

        #endregion Internal Methods
    
        #region Public Properties
        
        /// <summary>
        /// Queries / Set the XmlWriter for a FixedPage.
        /// </summary>
        public
        override
        XmlWriter
        XmlWriter
        {
            get
            {
                if(base.XmlWriter == null)
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

        #endregion Public Properties
        
        #region Private Methods

        private
        bool
        SerializePageAsVisual(
            Visual fixedPageAsVisual
            )
        {
            bool needEndVisual = false;

            ReachVisualSerializerAsync serializer = new ReachVisualSerializerAsync(SerializationManager);

            if(serializer!=null)
            {
                needEndVisual = serializer.SerializeDisguisedVisual(fixedPageAsVisual);
            }
            else
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_NoSerializer));
            }

            return needEndVisual;
        }

        private
        void
        EndSerializeReachFixedPage(
            ReachFixedPageSerializerContext context
            )
        {
            ReachFixedPageSerializerContext thisContext = context as ReachFixedPageSerializerContext;

            if(thisContext != null)
            {
                if(thisContext.EndVisual)
                {
                    XmlWriter           pageWriter  = ((XpsSerializationManagerAsync)SerializationManager).
                                                      PackagingPolicy.AcquireXmlWriterForPage();

                    XmlWriter           resWriter   = ((XpsSerializationManagerAsync)SerializationManager).
                                                      PackagingPolicy.AcquireXmlWriterForResourceDictionary();


                    Size fixedPageSize = ((IXpsSerializationManager)SerializationManager).FixedPageSize;
                    VisualTreeFlattener flattener  = ((IXpsSerializationManager)SerializationManager).
                                                      VisualSerializationService.AcquireVisualTreeFlattener(resWriter,
                                                                                                            pageWriter,
                                                                                                            fixedPageSize);

                    flattener.EndVisual();
                }


                ((XpsSerializationManagerAsync)SerializationManager).PackagingPolicy.PreCommitCurrentPage();

                //
                // Copy hyperlinks into stream
                //
                thisContext.TreeWalker.CommitHyperlinks();

                XmlWriter.WriteEndElement();
                //
                //Release used resources 
                //
                XmlWriter = null;
                //
                // Free the image table in use for this page
                //
                ((XpsSerializationManagerAsync)SerializationManager).ResourcePolicy.CurrentPageImageTable = null;
                //
                // Free the colorContext table in use for this page
                //
                ((XpsSerializationManagerAsync)SerializationManager).ResourcePolicy.CurrentPageColorContextTable = null;

                //
                // Signal to any registered callers that the Page has been serialized
                //
                XpsSerializationProgressChangedEventArgs progressEvent = 
                new XpsSerializationProgressChangedEventArgs(XpsWritingProgressChangeLevel.FixedPageWritingProgress,
                                                             0,
                                                             0,
                                                             null);

                ((IXpsSerializationManager)SerializationManager).OnXPSSerializationProgressChanged(progressEvent);
                ((IXpsSerializationManager)SerializationManager).VisualSerializationService.ReleaseVisualTreeFlattener();
                if (SerializationManager is IXpsSerializationManager)
                {
                    (SerializationManager as IXpsSerializationManager).RegisterPageEnd();
                }
            }

        }


        #endregion Private Methods
    };

    internal class ReachFixedPageSerializerContext :
                   ReachSerializerContext
    {
        public
        ReachFixedPageSerializerContext(
            ReachSerializerAsync        serializer,
            SerializableObjectContext   objectContext,
            SerializerAction            action,
            bool                        endVisual,
            ReachTreeWalker             treeWalker
            ):
            base(serializer,objectContext,action)
        {
            this._treeWalker = treeWalker;
            this._endVisual  = endVisual;
        }


        public
        ReachTreeWalker
        TreeWalker
        {
            get
            {
                return _treeWalker;
            }
        }

        public
        bool
        EndVisual
        {
            get
            {
                return _endVisual;
            }
        }

        private
        bool                _endVisual;

        private
        ReachTreeWalker     _treeWalker;
    };
}
