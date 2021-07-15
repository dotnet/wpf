// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections;
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
    internal abstract class NGCSerializerAsync :
                            ReachSerializer
    {
        #region Constructor

        /// <summary>
        /// Constructor for class ReachSerializer
        /// </summary>
        /// <param name="manager">
        /// The serializtion manager, the services of which are
        /// used later for the serialization process of the type.
        /// </param>
        public
        NGCSerializerAsync(
            PackageSerializationManager   manager
            )
        {
            if(manager == null)
            {
                throw new ArgumentNullException("manager");
            }

            _serializationManager = manager as NgcSerializationManagerAsync;

            if(_serializationManager == null)
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerializationAsync_NoNgcType));
            }
        }

        #endregion Constructor

        #region Public Methods


        public
        virtual
        void
        AsyncOperation(
            NGCSerializerContext context
            )
        {
            if(context == null)
            {

            }

            switch (context.Action)
            {
                case SerializerAction.endSerializeObject:
                {
                    EndSerializeObject(context.ObjectContext);
                    break;
                }

                case SerializerAction.serializeNextProperty:
                {
                    SerializeNextProperty(context.ObjectContext);
                    break;
                }
            }
        }

        /// <summary>
        /// The main method that is called to serialize the object of
        /// that given type.
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
            BeginSerializeObject(serializedObject);
        }

        #endregion Public Methods


        #region Internal Methods

        /// <summary>
        /// The main method that is called to serialize the object of
        /// that given type and that is usually called from within the
        /// serialization manager when a node in the graph of objects is
        /// at a turn where it should be serialized.
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
            BeginSerializeObject(serializedProperty);
        }

        internal
        virtual
        void
        BeginSerializeObject(
            SerializablePropertyContext serializedProperty
            )
        {
            if(serializedProperty == null)
            {
                throw new ArgumentNullException("serializedProperty");
            }

            if(SerializationManager == null)
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_MustHaveSerializationManager));
            }

            //
            // At this stage discover the graph of properties of the object that
            // need to be serialized
            //
            SerializableObjectContext serializableObjectContext = DiscoverObjectData(serializedProperty.Value,
                                                                                     serializedProperty);

            if(serializableObjectContext!=null)
            {
                //
                // Push the object at hand on the context stack
                //
                SerializationManager.GraphContextStack.Push(serializableObjectContext);

                NGCSerializerContext context = new NGCSerializerContext(this,
                                                                        serializableObjectContext,
                                                                        SerializerAction.endSerializeObject);

                _serializationManager.OperationStack.Push(context);
                //
                // At this stage we should start streaming the markup representing the
                // object graph to the corresponding destination
                //
                PersistObjectData(serializableObjectContext);

            }
        }

        /// <summary>
        ///
        /// </summary>
        internal
        virtual
        void
        BeginSerializeObject(
            Object serializedObject
            )
        {
            if(serializedObject == null)
            {
                throw new ArgumentNullException("serializedObject");
            }
            if(SerializationManager == null)
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_MustHaveSerializationManager));
            }
            //
            // At this stage discover the graph of properties of the object that
            // need to be serialized
            //
            SerializableObjectContext serializableObjectContext = DiscoverObjectData(serializedObject,
                                                                                     null);

            if(serializableObjectContext!=null)
            {
                //
                // Push the object at hand on the context stack
                //
                SerializationManager.GraphContextStack.Push(serializableObjectContext);

                NGCSerializerContext context = new NGCSerializerContext(this,
                                                                        serializableObjectContext,
                                                                        SerializerAction.endSerializeObject);

                _serializationManager.OperationStack.Push(context);
                //
                // At this stage we should start streaming the markup representing the
                // object graph to the corresponding destination
                //
                PersistObjectData(serializableObjectContext);
                }
        }

        internal
        virtual
        void
        EndSerializeObject(
            SerializableObjectContext   serializableObjectContext
            )
        {
            //
            // Pop the object from the context stack
            //
            SerializationManager.GraphContextStack.Pop();

            //
            // Recycle the used SerializableObjectContext
            //
            SerializableObjectContext.RecycleContext(serializableObjectContext);
        }


        /*/// <summary>
        /// The method is called once the object data is discovered at that
        /// point of the serialization process.
        /// </summary>
        /// <param name="serializableObjectContext">
        /// The context of the object to be serialized at this time.
        /// </param>
        internal
        abstract
        void
        PersistObjectData(
            SerializableObjectContext   serializableObjectContext
            );*/


        /// <summary>
        ///
        /// </summary>
        internal
        virtual
        void
        EndPersistObjectData(
            )
        {
            //
            // Do nothing in the base class
            //
        }


        /// <summary>
        ///     Serialize the properties within the object
        ///     context into METRO
        /// </summary>
        /// <remarks>
        ///     Method follows these steps
        ///     1. Serializes the instance as string content
        ///         if is not meant to be a complex value. Else ...
        ///     2. Serialize Properties as attributes
        ///     3. Serialize Complex Properties as separate parts
        ///         through calling separate serializers
        ///     Also this is the virtual to override custom attributes or
        ///     contents need to be serialized
        /// </remarks>
        /// <param name="serializableObjectContext">
        /// The context of the object to be serialized at this time.
        /// </param>
        internal
        override
        void
        SerializeObjectCore(
            SerializableObjectContext   serializableObjectContext
            )
        {
            if (serializableObjectContext == null)
            {
                throw new ArgumentNullException("serializableObjectContext");
            }

            if (!serializableObjectContext.IsReadOnlyValue &&
                serializableObjectContext.IsComplexValue)
            {
                SerializeProperties(serializableObjectContext);
            }
        }

        /// <summary>
        /// This method is the one that writes out the attribute within
        /// the xml stream when serializing simple properites.
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
        }

        #endregion Internal Methods

        #region Private Methods

        /// <summary>
        /// This method is the one that parses down the object at hand
        /// to discover all the properties that are expected to be serialized
        /// at that object level.
        /// the xml stream when serializing simple properties.
        /// </summary>
        /// <param name="serializedObject">
        /// The instance of the object being serialized.
        /// </param>
        /// <param name="serializedProperty">
        /// The instance of property on the parent object from which this
        /// object stemmed. This could be null if this is the node object
        /// or the object has no parent.
        /// </param>
        private
        SerializableObjectContext
        DiscoverObjectData(
            Object                      serializedObject,
            SerializablePropertyContext serializedProperty
            )
        {
            //
            // Trying to figure out the parent of this node, which is at this stage
            // the same node previously pushed on the stack or in other words it is
            // the node that is currently on the top of the stack
            //
            SerializableObjectContext
            serializableObjectParentContext = (SerializableObjectContext)SerializationManager.
                                              GraphContextStack[typeof(SerializableObjectContext)];
            //
            // Create the context for the current object
            //
            SerializableObjectContext serializableObjectContext =
            SerializableObjectContext.CreateContext(SerializationManager,
                                                    serializedObject,
                                                    serializableObjectParentContext,
                                                    serializedProperty);

            //
            // Set the root object to be serialized at the level of the SerializationManager
            //
            if(SerializationManager.RootSerializableObjectContext == null)
            {
                SerializationManager.RootSerializableObjectContext = serializableObjectContext;
            }

            return serializableObjectContext;
        }

        /// <summary>
        /// Trigger all properties serialization
        /// </summary>
        private
        void
        SerializeProperties(
            SerializableObjectContext   serializableObjectContext
            )
        {
            if (serializableObjectContext == null)
            {
                throw new ArgumentNullException("serializableObjectContext");
            }

            SerializablePropertyCollection propertyCollection = serializableObjectContext.PropertiesCollection;

            if(propertyCollection!=null)
            {
                //for(propertyCollection.Reset();
                //    propertyCollection.MoveNext();)
                //{
                //    SerializablePropertyContext serializablePropertyContext =
                //    (SerializablePropertyContext)propertyCollection.Current;
                //
                //    if(serializablePropertyContext!=null)
                //    {
                //        SerializeProperty(serializablePropertyContext);
                //    }
                //}
                propertyCollection.Reset();
                NGCSerializerContext context = new NGCSerializerContext(this,
                                                                        serializableObjectContext,
                                                                        SerializerAction.serializeNextProperty);

                _serializationManager.OperationStack.Push(context);
            }
        }

        private
        void
        SerializeNextProperty(
            SerializableObjectContext   serializableObjectContext
            )
        {
            SerializablePropertyCollection propertyCollection = serializableObjectContext.PropertiesCollection;

            if(propertyCollection.MoveNext())
            {
                SerializablePropertyContext serializablePropertyContext =
                (SerializablePropertyContext)propertyCollection.Current;

                if(serializablePropertyContext!=null)
                {

                    NGCSerializerContext context = new NGCSerializerContext(this,
                                                                            serializableObjectContext,
                                                                            SerializerAction.serializeNextProperty);

                    _serializationManager.OperationStack.Push(context);
                    SerializeProperty(serializablePropertyContext);
                }

            }
        }

        /// <summary>
        /// Trigger serializing one property at a time.
        /// </summary>
        private
        void
        SerializeProperty(
            SerializablePropertyContext serializablePropertyContext
            )
        {
            if(serializablePropertyContext == null)
            {
                throw new ArgumentNullException("serializablePropertyContext");
            }

            if(!serializablePropertyContext.IsComplex)
            {
                //
                // Non-Complex Properties are serialized as attributes
                //
                WriteSerializedAttribute(serializablePropertyContext);
            }
            else
            {
                //
                // Complex properties could be treated in different ways
                // based on their type. Examples of that are:
                //
                //
                //
                ReachSerializer serializer = SerializationManager.GetSerializer(serializablePropertyContext.Value);

                // If there is no serializer for this type, we won't serialize this property
                if(serializer!=null)
                {
                    serializer.SerializeObject(serializablePropertyContext);
                }
            }
        }

        #endregion Private Methods

        #region Public Properties

        /// <summary>
        /// Query the SerializationManager used by this serializer.
        /// </summary>
        public
        override
        PackageSerializationManager
        SerializationManager
        {
            get
            {
                return _serializationManager;
            }
        }

        protected
        NgcSerializationManagerAsync
        NgcSerializationManager
        {
            get
            {
                return _serializationManager;
            }
        }

        #endregion Public Properties

        #region Private Data members

        private
        NgcSerializationManagerAsync   _serializationManager;

        #endregion Private Data members
    };

    /// <summary>
    ///
    /// </summary>
    internal class NgcFixedDocumentSerializerAsync :
                   NGCSerializerAsync
    {
        /// <summary>
        ///
        /// </summary>
        public
        NgcFixedDocumentSerializerAsync(
            PackageSerializationManager   manager
            ):
        base(manager)
        {
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }
        }

        public
        override
        void
        AsyncOperation(
            NGCSerializerContext context
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
        internal
        override
        void
        BeginSerializeObject(
            Object serializedObject
            )
        {
            FixedDocument fd =  serializedObject as FixedDocument;
            if(!NgcSerializationManager.IsPrintTicketEventHandlerEnabled)
            {
                //NgcSerializationManager.FdPrintTicket = fd.PrintTicket as PrintTicket;
            }

            NgcSerializationManager.StartDocument(fd,true);

            //
            // Create the context for the current object
            //

            NGCSerializerContext context = new NGCSerializerContext(this,
                                                                    null,
                                                                    SerializerAction.endPersistObjectData);

            NgcSerializationManager.OperationStack.Push(context);

            ReachSerializer serializer = NgcSerializationManager.GetSerializer(fd.Pages);
            serializer.SerializeObject(fd.Pages);
        }
       
        /// <summary>
        ///
        /// </summary>
        internal
        override
        void
        PersistObjectData(
            SerializableObjectContext   serializableObjectContext
            )
        {
            if (serializableObjectContext == null)
            {
                throw new ArgumentNullException("serializableObjectContext");
            }

            BeginSerializeObject(serializableObjectContext.TargetObject);

        }

        internal
        override
        void
        EndPersistObjectData(
            )
        {
            NgcSerializationManager.EndDocument();
        }
    };

    /// <summary>
    ///
    /// </summary>
    internal class NgcDocumentPaginatorSerializerAsync :
                   NGCSerializerAsync
    {
        /// <summary>
        ///
        /// </summary>
        public
        NgcDocumentPaginatorSerializerAsync(
            PackageSerializationManager manager
            ) :
        base(manager)
        {
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }
        }

        public
        override
        void
        AsyncOperation(
            NGCSerializerContext context
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

        /// <summary>
        ///
        /// </summary>
        internal
        override
        void
        BeginSerializeObject(
            Object serializedObject
            )
        {
            DocumentPaginator paginator = (DocumentPaginator)serializedObject;

            NgcSerializationManager.StartDocument(paginator,true);

            NGCSerializerContext context = new NGCSerializerContext(this,
                                                                    null,
                                                                    SerializerAction.endPersistObjectData);

            NgcSerializationManager.OperationStack.Push(context);



            if (paginator != null)
            {
                NGCDocumentPaginatorSerializerContext
                paginatorContext = new NGCDocumentPaginatorSerializerContext(this,
                                                                             paginator,
                                                                             0,
                                                                             SerializerAction.serializeNextDocumentPage);

                NgcSerializationManager.OperationStack.Push(paginatorContext);
            }
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
            if (serializableObjectContext == null)
            {
                throw new ArgumentNullException("serializableObjectContext");
            }

            BeginSerializeObject(serializableObjectContext.TargetObject);
        }

        internal
        override
        void
        EndPersistObjectData(
            )
        {
            NgcSerializationManager.EndDocument();
        }

        private
        void
        SerializeNextDocumentPage(
            NGCSerializerContext  context
            )
        {

            NGCDocumentPaginatorSerializerContext paginatorContext = context as NGCDocumentPaginatorSerializerContext;

            if(paginatorContext != null)
            {
                DocumentPaginator  paginator = paginatorContext.Paginator;
                int                index     = paginatorContext.Index;

                if(!paginator.IsPageCountValid||
                   (index < paginator.PageCount))
                {
                    index++;


                    NGCDocumentPaginatorSerializerContext
                    nextContext = new NGCDocumentPaginatorSerializerContext(this,
                                                                            paginator,
                                                                            index,
                                                                            SerializerAction.serializeNextDocumentPage);


                    NgcSerializationManager.OperationStack.Push(nextContext);

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


    /// <summary>
    ///
    /// </summary>
    internal class NgcFixedPageSerializerAsync :
                   NGCSerializerAsync
    {
        /// <summary>
        ///
        /// </summary>
        public
        NgcFixedPageSerializerAsync(
            PackageSerializationManager   manager
            ):
        base(manager)
        {
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }
        }

        public
        override
        void
        AsyncOperation(
            NGCSerializerContext context
            )
        {
            if(context == null)
            {

            }

            switch (context.Action)
            {
                case SerializerAction.endPersistObjectData:
                {
                    NGCPageSerializerContext ngcPageSerializerContext = context as NGCPageSerializerContext;
                    EndPersistObjectData(ngcPageSerializerContext.IsManualStartDoc);
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
        internal
        override
        void
        BeginSerializeObject(
            Object serializedObject
            )
        {
            FixedPage fp = serializedObject as FixedPage;

            bool bManualStartDoc = NgcSerializationManager.StartPage();

            SerializableObjectContext serializableObjectContext =
            SerializableObjectContext.CreateContext(SerializationManager,
                                                    serializedObject,
                                                    null,
                                                    null);
             NGCPageSerializerContext context = new NGCPageSerializerContext(this,
                                                                            serializableObjectContext,
                                                                            SerializerAction.endPersistObjectData,
                                                                            bManualStartDoc);

            NgcSerializationManager.OperationStack.Push(context);

            Visual visual = (Visual)serializableObjectContext.TargetObject as Visual;

            Size pageSize = new Size(fp.Width, fp.Height);
            NgcSerializationManager.PageSize = pageSize;
            
            if (visual != null)
            {
                NgcSerializationManager.WalkVisual(visual);
            }
        }
        
        /// <summary>
        ///
        /// </summary>
        internal
        override
        void
        PersistObjectData(
            SerializableObjectContext   serializableObjectContext
            )
        {
            if (serializableObjectContext == null)
            {
                throw new ArgumentNullException("serializableObjectContext");
            }

            BeginSerializeObject(serializableObjectContext.TargetObject);
        }

        internal
        void
        EndPersistObjectData(
            bool isManualStartDoc
            )
        {
            NgcSerializationManager.EndPage();

            if (isManualStartDoc)
            {
                NgcSerializationManager.EndDocument();
            }
        }
    };

    /// <summary>
    ///
    /// </summary>
    internal class NgcDocumentPageSerializerAsync :
                   NGCSerializerAsync
    {
        /// <summary>
        ///
        /// </summary>
        public
        NgcDocumentPageSerializerAsync(
            PackageSerializationManager   manager
            ):
        base(manager)
        {
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }
        }

        public
        override
        void
        AsyncOperation(
            NGCSerializerContext context
            )
        {
            if(context == null)
            {

            }

            switch (context.Action)
            {
                case SerializerAction.endPersistObjectData:
                {
                    NGCPageSerializerContext ngcPageSerializerContext = context as NGCPageSerializerContext;
                    EndPersistObjectData(ngcPageSerializerContext.IsManualStartDoc);
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
        internal
        override
        void
        BeginSerializeObject(
            Object serializedObject
            )
        {
            DocumentPage dp = serializedObject as DocumentPage;
            if (dp != null)
            {
                Visual pageRootVisual = dp.Visual;

                bool bManualStartDoc = NgcSerializationManager.StartPage();

                SerializableObjectContext serializableObjectContext =
                SerializableObjectContext.CreateContext(SerializationManager,
                                                        serializedObject,
                                                        null,
                                                        null);
                 NGCPageSerializerContext context = new NGCPageSerializerContext(this,
                                                                                serializableObjectContext,
                                                                                SerializerAction.endPersistObjectData,
                                                                                bManualStartDoc);

                NgcSerializationManager.OperationStack.Push(context);

                ReachSerializer serializer = SerializationManager.GetSerializer(pageRootVisual);

                serializer.SerializeObject(pageRootVisual);
            }
        }
            
        /// <summary>
        ///
        /// </summary>
        internal
        override
        void
        PersistObjectData(
            SerializableObjectContext   serializableObjectContext
            )
        {
            if (serializableObjectContext == null)
            {
                throw new ArgumentNullException("serializableObjectContext");
            }

            //
            // A DocumentPage is persisted as a FixedPage
            //
            BeginSerializeObject( serializableObjectContext.TargetObject );
        }

        internal
        void
        EndPersistObjectData(
            bool isManualStartDoc
            )
        {
            NgcSerializationManager.EndPage();

            if (isManualStartDoc)
            {
                NgcSerializationManager.EndDocument();
            }
        }
    };

    /// <summary>
    ///
    /// </summary>
    internal class NgcReachVisualSerializerAsync :
                   NGCSerializerAsync
    {
        /// <summary>
        ///
        /// </summary>
        public
        NgcReachVisualSerializerAsync(
            PackageSerializationManager   manager
            ):
        base(manager)
        {
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }
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
            // Do nothing here.
            // We do not support serializing visuals that come in as
            // properties out of context of a FixedPage or a DocumentPage
            //
        }

        /// <summary>
        ///
        /// </summary>
        internal
        override
        void
        BeginSerializeObject(
            Object serializedObject
            )
        {
            Visual visual = serializedObject as Visual;

            if (visual != null)
            {
                NgcSerializationManager.WalkVisual(visual);
            }
        }
        /// <summary>
        ///
        /// </summary>
        internal
        override
        void
        PersistObjectData(
            SerializableObjectContext   serializableObjectContext
            )
        {
            if (serializableObjectContext == null)
            {
                throw new ArgumentNullException("serializableObjectContext");
            }

            Visual visual = (Visual)serializableObjectContext.TargetObject as Visual;

            if (visual != null)
            {
                NgcSerializationManager.WalkVisual(visual);
            }
        }
    };


    /// <summary>
    ///
    /// </summary>
    internal class NgcDocumentSequenceSerializerAsync :
                   NGCSerializerAsync
    {
        /// <summary>
        ///
        /// </summary>
        public
        NgcDocumentSequenceSerializerAsync(
            PackageSerializationManager manager
            )
            :
        base(manager)
        {

        }

        public
        override
        void
        AsyncOperation(
            NGCSerializerContext context
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

                    XpsSerializationProgressChangedEventArgs e = 
                    new XpsSerializationProgressChangedEventArgs(XpsWritingProgressChangeLevel.FixedDocumentSequenceWritingProgress,
                                                                 0,
                                                                 0,
                                                                 null);
                    NgcSerializationManager.OnNGCSerializationProgressChanged(e);            

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
        internal
        override
        void
        BeginSerializeObject(
            Object serializedObject
            )
        {
            FixedDocumentSequence fds = serializedObject as FixedDocumentSequence;

            if(!NgcSerializationManager.IsPrintTicketEventHandlerEnabled)
            {
                //NgcSerializationManager.FdsPrintTicket = fds.PrintTicket as PrintTicket;
            }
            else
            {
                XpsSerializationPrintTicketRequiredEventArgs e =
                new XpsSerializationPrintTicketRequiredEventArgs(PrintTicketLevel.FixedDocumentSequencePrintTicket,
                                                                 0);
                NgcSerializationManager.OnNGCSerializationPrintTicketRequired(e);
            }

            NgcSerializationManager.StartDocument(fds,false);

            NGCSerializerContext context = new NGCSerializerContext(this,
                                                                    null,
                                                                    SerializerAction.endPersistObjectData);

            NgcSerializationManager.OperationStack.Push(context);


            ReachSerializer serializer = NgcSerializationManager.GetSerializer(fds.References);
            serializer.SerializeObject(fds.References);
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
            if (serializableObjectContext == null)
            {
                throw new ArgumentNullException("serializableObjectContext");
            }
            BeginSerializeObject( serializableObjectContext.TargetObject );
        }

        internal
        override
        void
        EndPersistObjectData(
            )
        {
            NgcSerializationManager.EndDocument();
        }
     };

    /// <summary>
    ///
    /// </summary>
    internal class NgcDocumentReferenceCollectionSerializerAsync :
                   NGCSerializerAsync
    {
        /// <summary>
        ///
        /// </summary>
        public
        NgcDocumentReferenceCollectionSerializerAsync(
            PackageSerializationManager manager
            ):
        base(manager)
        {

        }

        public
        override
        void
        AsyncOperation(
            NGCSerializerContext context
            )
        {
            if(context == null)
            {

            }

            switch (context.Action)
            {
                case SerializerAction.serializeNextDocumentReference:
                {
                    NgcDocumentReferenceCollectionSerializerContext thisContext =
                    context as NgcDocumentReferenceCollectionSerializerContext;

                    if(thisContext != null)
                    {
                        SerializeNextDocumentReference(thisContext.Enumerator,
                                                       thisContext.ObjectContext);
                    }

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
        internal
        override
        void
        BeginSerializeObject(
            Object serializedObject
            )
        {
            IEnumerator enumerator =
            ((System.Collections.Generic.IEnumerable<DocumentReference>)serializedObject).
            GetEnumerator();

            enumerator.Reset();

            SerializableObjectContext serializableObjectContext =
            SerializableObjectContext.CreateContext(SerializationManager,
                                                    serializedObject,
                                                    null,
                                                    null);

            NgcDocumentReferenceCollectionSerializerContext
            context = new NgcDocumentReferenceCollectionSerializerContext(this,
                                                                          serializableObjectContext,
                                                                          enumerator,
                                                                          SerializerAction.serializeNextDocumentReference);

            NgcSerializationManager.OperationStack.Push(context);
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
            if (serializableObjectContext == null)
            {
                throw new ArgumentNullException("serializableObjectContext");
            }
            //
            // Serialize the PageContent Items contained within the collection
            //
            BeginSerializeObject(serializableObjectContext.TargetObject);
        }


        private
        void
        SerializeNextDocumentReference(
            IEnumerator                 enumerator,
            SerializableObjectContext   serializableObjectContext
            )
        {
            if(enumerator.MoveNext())
            {

                NgcDocumentReferenceCollectionSerializerContext
                context = new NgcDocumentReferenceCollectionSerializerContext(this,
                                                                              serializableObjectContext,
                                                                              enumerator,
                                                                              SerializerAction.serializeNextDocumentReference);


                NgcSerializationManager.OperationStack.Push(context);

                object documentReference = enumerator.Current;

                SerializeDocumentReference(documentReference);
            }
        }


        /// <summary>
        ///     Called to serialize a single DocumentReference
        /// </summary>
        private
        void
        SerializeDocumentReference(
            object documentReference
            )
        {
            IDocumentPaginatorSource idp = ((DocumentReference)documentReference).GetDocument(false);
            
            if (idp != null)
            {
                FixedDocument fixedDoc = idp as FixedDocument;

                if (fixedDoc != null)
                {
                    ReachSerializer serializer = NgcSerializationManager.GetSerializer(fixedDoc);
                    if (serializer != null)
                    {
                        serializer.SerializeObject(fixedDoc);
                    }
                }
                else
                {
                    ReachSerializer serializer = NgcSerializationManager.GetSerializer(idp.DocumentPaginator);
                    if (serializer != null)
                    {
                        serializer.SerializeObject(idp);
                    }
                }
            }
        }
    };

    internal class NGCSerializerContext
    {

        public
        NGCSerializerContext(
            NGCSerializerAsync          serializer,
            SerializerAction            action
            )
        {
            this._action        = action;
            this._serializer    = serializer;
            this._objectContext = null;
        }

        public
        NGCSerializerContext(
            NGCSerializerAsync          serializer,
            SerializableObjectContext   objectContext,
            SerializerAction            action
            )
        {
            this._action        = action;
            this._serializer    = serializer;
            this._objectContext = objectContext;
        }

        public
        virtual
        SerializerAction
        Action
        {
            get
            {
                return _action;
            }
        }

        public
        virtual
        NGCSerializerAsync
        Serializer
        {
            get
            {
                return _serializer;
            }
        }

        public
        virtual
        SerializableObjectContext
        ObjectContext
        {
            get
            {
                return _objectContext;
            }
        }

        private
        SerializerAction            _action;

        private
        NGCSerializerAsync          _serializer;

        private
        SerializableObjectContext   _objectContext;
    };

    internal class NgcDocumentReferenceCollectionSerializerContext :
                   NGCSerializerContext
    {
        public
        NgcDocumentReferenceCollectionSerializerContext(
            NGCSerializerAsync          serializer,
            SerializableObjectContext   objectContext,
            IEnumerator                 enumerator,
            SerializerAction            action
            ):
            base(serializer,objectContext,action)
        {
            this._enumerator = enumerator;
        }


        public
        IEnumerator
        Enumerator
        {
            get
            {
                return _enumerator;
            }
        }

        private
        IEnumerator     _enumerator;
    };

    internal class NGCPageSerializerContext:
                   NGCSerializerContext
    {
        public
        NGCPageSerializerContext(
            NGCSerializerAsync          serializer,
            SerializableObjectContext   objectContext,
            SerializerAction            action,
            bool                        isManualStartDoc
            ):
        base(serializer,objectContext,action)
        {
            _isManualStartDoc = isManualStartDoc;
        }

        public
        bool
        IsManualStartDoc
        {
            get
            {
                return _isManualStartDoc;
            }
        }

        private
        bool    _isManualStartDoc;
    };


    internal class NGCDocumentPaginatorSerializerContext :
                   NGCSerializerContext
    {
        public
        NGCDocumentPaginatorSerializerContext(
            NGCSerializerAsync          serializer,
            DocumentPaginator           paginator,
            int                         index,
            SerializerAction            action
            ):
            base(serializer,action)
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


