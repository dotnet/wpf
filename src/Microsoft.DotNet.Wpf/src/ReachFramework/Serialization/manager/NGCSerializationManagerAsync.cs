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
using System.Printing;
using System.Windows.Threading;
using MS.Internal;
using Microsoft.Internal.AlphaFlattener;


//
// Ngc = Next Generation Converter. It means to convert the avalon element tree
//  to the downlevel GDI primitives.
//
namespace System.Windows.Xps.Serialization
{
    /// <summary>
    /// NgcSerializationManager is a class to help print avalon content (element tree) to traditional printer
    /// </summary>
    // [CLSCompliant(false)]
    internal sealed class NgcSerializationManagerAsync :
                          PackageSerializationManager
    {
        #region Constructor

        /// <summary>
        /// This constructor take PrintQueue parameter
        /// </summary>
        /// <exception cref="ArgumentNullException">queue is NULL.</exception>
        public
        NgcSerializationManagerAsync(
            PrintQueue   queue,
            bool         isBatchMode
            ):
        base()
        {
            if (queue == null)
            {
                throw new ArgumentNullException("queue");
            }
            _printQueue                      = queue;
            _operationStack                  = new Stack();
            _isBatchMode                     = isBatchMode;
            _batchOperationQueue             = new Queue();
            _dispatcher                      = Dispatcher.CurrentDispatcher;
            _serializationOperationCanceled  = false;
            _isSimulating                    = false;
            _isBatchWorkItemInProgress       = false;
            _printTicketManager              = new NgcPrintTicketManager(_printQueue);
        }

        #endregion Construtor

        #region PackageSerializationManager override

        /// <summary>
        /// The function will serializer the avalon content to the printer spool file.
        /// SaveAsXaml is not a propriate name. Maybe it should be "Print"
        /// </summary>
        /// <exception cref="ArgumentNullException">serializedObject is NULL.</exception>
        /// <exception cref="XpsSerializationException">serializedObject is not a supported type.</exception>
        public
        override
        void
        SaveAsXaml(
            Object  serializedObject
            )
        {
            if (serializedObject == null)
            {
                throw new ArgumentNullException("serializedObject");
            }

            if(!IsSerializedObjectTypeSupported(serializedObject))
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_NotSupported));
            }

            if(_isBatchMode && !_isSimulating)
            {
                XpsSerializationPrintTicketRequiredEventArgs printTicketEvent =
                new XpsSerializationPrintTicketRequiredEventArgs(PrintTicketLevel.FixedDocumentSequencePrintTicket,
                                                                 0);
                OnNGCSerializationPrintTicketRequired(printTicketEvent);

                StartDocument(null,false);
                _isSimulating = true;
            }

            if(!_isBatchMode &&
               IsDocumentSequencePrintTicketRequired(serializedObject))
            {
                XpsSerializationPrintTicketRequiredEventArgs printTicketEvent =
                new XpsSerializationPrintTicketRequiredEventArgs(PrintTicketLevel.FixedDocumentSequencePrintTicket,
                                                                 0);
                OnNGCSerializationPrintTicketRequired(printTicketEvent);
            }

            if(_isBatchMode)
            {
                //
                // Add the Visual received in to the queue
                //
                BatchOperationWorkItem  batchOperationWorkItem = new BatchOperationWorkItem(BatchOperationType.batchWrite,
                                                                                            serializedObject);
                _batchOperationQueue.Enqueue(batchOperationWorkItem);
                PostSerializationTask(new DispatcherOperationCallback(InvokeSaveAsXamlBatchWorkItem));
            }
            else
            {
                ReachSerializer reachSerializer = GetSerializer(serializedObject);

                if(reachSerializer != null)
                {
                    //
                    // Prepare the context that is going to be pushed on the stack
                    //
                    SerializationManagerOperationContextStack
                    contextStack = new SerializationManagerOperationContextStack(reachSerializer,
                                                                                 serializedObject);
                    //
                    // At this stage, start calling another method which would peak at the stack
                    //
                    _operationStack.Push(contextStack);

                    PostSerializationTask(new DispatcherOperationCallback(InvokeSaveAsXamlWorkItem));
                }
                else
                {
                    throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_NoSerializer));
                }
            }
        }

        internal
        Object
        InvokeSaveAsXamlWorkItem(
            Object arg
            )
        {
            //
            // PreSharp uses message numbers that the C# compiler doesn't know about.
            // Disable the C# complaints, per the PreSharp documentation.
            #pragma warning disable 1634, 1691
            //
            // PreSharp complains about catching NullReference (and other) exceptions.
            // This is an async model and we need to catch all exception ourselves and then
            // send them to the completion delegate
            #pragma warning disable 56500
            try
            {
                if(!_serializationOperationCanceled)
                {
                    if(_operationStack.Count > 0)
                    {
                        Object objectOnStack = _operationStack.Pop();

                        if(objectOnStack.GetType() ==
                           typeof(System.Windows.Xps.Serialization.SerializationManagerOperationContextStack))
                        {
                           SerializationManagerOperationContextStack context =
                                                                     (SerializationManagerOperationContextStack)objectOnStack;

                            context.ReachSerializer.SerializeObject(context.SerializedObject);
                        }
                        else if(typeof(System.Windows.Xps.Serialization.NGCSerializerContext).IsAssignableFrom(objectOnStack.GetType()))
                        {
                            NGCSerializerContext context = (NGCSerializerContext)objectOnStack;
                            context.Serializer.AsyncOperation(context);
                        }
                        PostSerializationTask(new DispatcherOperationCallback(InvokeSaveAsXamlWorkItem));
                    }
                    else
                    {
                        NGCSerializationCompletionMethod();
                    }
                }
            }
            catch(Exception e)
            {
                EndDocument(true);
                if(CriticalExceptions.IsCriticalException(e))
                {
                    throw;
                }

                //
                // Indicate that an error happened
                //
                bool canceled = false;

                XpsSerializationCompletedEventArgs args = new XpsSerializationCompletedEventArgs(canceled,
                                                                                                 null,
                                                                                                 e);

                _serializationOperationCanceled = true;

                PostSerializationTask(new DispatcherOperationCallback(OnNGCSerializationCompleted), args);

                return null;
            }
            #pragma warning restore 56500
            #pragma warning restore 1634, 1691

            return null;
        }


        /// <remarks>
        /// The logic in the method IsAsyncWorkPending *MUST* mirror the logic in InvokeSaveAsXamlBatchWorkItem
        /// IsAsyncWorkPending must return true when the manager is in a state that
        /// causes InvokeSaveAsXamlBatchWorkItem to process pending items.
        /// <remarks>
        internal
        Object
        InvokeSaveAsXamlBatchWorkItem(
            Object arg
            )
        {
            //
            // PreSharp uses message numbers that the C# compiler doesn't know about.
            // Disable the C# complaints, per the PreSharp documentation.
            #pragma warning disable 1634, 1691
            //
            // PreSharp complains about catching NullReference (and other) exceptions.
            // This is an async model and we need to catch all exception ourselves and then
            // send them to the completion delegate
            #pragma warning disable 56500
            try
            {
                // This logic must be mirrored in IsAsyncWorkPending see remarks.

                if (!_serializationOperationCanceled)
                {
                    if(!_isBatchWorkItemInProgress && _batchOperationQueue.Count > 0)
                    {
                        BatchOperationWorkItem batchOperationWorkItem = (BatchOperationWorkItem)_batchOperationQueue.Dequeue();

                        if(batchOperationWorkItem.OperationType == BatchOperationType.batchWrite)
                        {
                            StartPage();

                            ReachSerializer reachSerializer = GetSerializer(batchOperationWorkItem.SerializedObject);

                            if(reachSerializer != null)
                            {
                                //
                                // Prepare the context that is going to be pushed on the stack
                                //
                                SerializationManagerOperationContextStack
                                contextStack = new SerializationManagerOperationContextStack(reachSerializer,
                                                                                             batchOperationWorkItem.SerializedObject);
                                //
                                // At this stage, start calling another method which would peak at the stack
                                //
                                _operationStack.Push(contextStack);

                                PostSerializationTask(new DispatcherOperationCallback(InvokeSaveAsXamlBatchWorkItem));
                            }
                            else
                            {
                                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_NoSerializer));
                            }
                            _isBatchWorkItemInProgress = true;
                        }
                        else if(batchOperationWorkItem.OperationType == BatchOperationType.batchCommit)
                        {
                            if (_isSimulating)
                            {
                                EndDocument();
                            }
                            NGCSerializationCompletionMethod();
                        }
                    }
                    else
                    {
                        if(_operationStack.Count > 0)
                        {
                            Object objectOnStack = _operationStack.Pop();

                            if(objectOnStack.GetType() ==
                               typeof(System.Windows.Xps.Serialization.SerializationManagerOperationContextStack))
                            {
                               SerializationManagerOperationContextStack context =
                                                                         (SerializationManagerOperationContextStack)objectOnStack;

                                context.ReachSerializer.SerializeObject(context.SerializedObject);
                            }
                            else if(typeof(System.Windows.Xps.Serialization.NGCSerializerContext).IsAssignableFrom(objectOnStack.GetType()))
                            {
                                NGCSerializerContext context = (NGCSerializerContext)objectOnStack;
                                context.Serializer.AsyncOperation(context);
                            }
                            PostSerializationTask(new DispatcherOperationCallback(InvokeSaveAsXamlBatchWorkItem));
                        }
                        else
                        {
                            EndPage();
                            _isBatchWorkItemInProgress = false;
                        }
                    }
                }
            }
            catch(Exception e)
            {
                //
                //  Force document count back to 1
                //  This causes EndDocument to effectivly abort dispite not properly calling an End
                //  document for each Start Document
                //
                // 07/24/06 brianad
                //
                EndDocument(true);
                if (CriticalExceptions.IsCriticalException(e))
                {
                    throw;
                }

                //
                // Indicate that an error happened
                //
                bool canceled = false;

                XpsSerializationCompletedEventArgs args = new XpsSerializationCompletedEventArgs(canceled,
                                                                                                 null,
                                                                                                 e);

                _serializationOperationCanceled = true;

                PostSerializationTask(new DispatcherOperationCallback(OnNGCSerializationCompleted), args);

                return null;
            }
            #pragma warning restore 56500
            #pragma warning restore 1634, 1691

            return null;
        }

        /// <summary>
        ///
        /// </summary>
        public
        void
        CancelAsync(
            )
        {
            bool canceled = true;

            XpsSerializationCompletedEventArgs e = new XpsSerializationCompletedEventArgs(canceled,
                                                                                          null,
                                                                                          null);

            _serializationOperationCanceled = true;

            if(_startDocCnt != 0)
            {
                _device.AbortDocument();
                _startDocCnt = 0;
            }

            PostSerializationTask(new DispatcherOperationCallback(OnNGCSerializationCompleted), e);
        }

        /// <summary>
        ///
        /// </summary>
        public
        void
        Commit(
            )
        {
            if(_isBatchMode && _isSimulating)
            {
                // Wait for pending items to complete synchronously
                // otherwise the caller may dispose the underlying resource
                // before our async operations can commit remaining data
                WaitForPendingAsyncItems();

                // Post a final commit item
                // It's important to first drain all prior items before posting this item
                // otherwise a pending item may post a new item causing undesirable interleaving
                BatchOperationWorkItem batchOperationWorkItem = new BatchOperationWorkItem(BatchOperationType.batchCommit,
                                                                                            null);
                _batchOperationQueue.Enqueue(batchOperationWorkItem);
                PostSerializationTask(new DispatcherOperationCallback(InvokeSaveAsXamlBatchWorkItem));

                // Wait for pending items to complete synchronously
                WaitForPendingAsyncItems();
            }
        }

        /// <summary>
        ///
        /// </summary>
        public
        event
        XpsSerializationCompletedEventHandler               XpsSerializationCompleted;

        /// <summary>
        ///
        /// </summary>
        public
        event
        XpsSerializationPrintTicketRequiredEventHandler     XpsSerializationPrintTicketRequired;

        /// <summary>
        ///
        /// </summary>
        public
        event
        XpsSerializationProgressChangedEventHandler         XpsSerializationProgressChanged;

        /// <summary>
        /// This function GetXmlNSForType and the other four XML writer functiosn (like AcquireXmlWriter)
        /// and the two stream function (like ReleaseResourceStream) should be removed from
        /// the base PackageSerializationManager class.
        /// </summary>
        internal
        override
        String
        GetXmlNSForType(
            Type    objectType
            )
        {
            return null;
        }

        /// <summary>
        ///
        /// </summary>
        internal
        override
        ReachSerializer
        GetSerializer(
            Object serializedObject
            )
        {
            ReachSerializer reachSerializer = null;

            if((reachSerializer = base.GetSerializer(serializedObject)) == null)
            {
                reachSerializer = this.SerializersCacheManager.GetSerializer(serializedObject);
            }

            return reachSerializer;
        }

        /// <summary>
        ///
        /// </summary>
        internal
        override
        Type
        GetSerializerType(
            Type objectType
            )
        {
            Type serializerType = null;


            if (typeof(System.Windows.Documents.FixedDocumentSequence).IsAssignableFrom(objectType))
            {
                serializerType = typeof(NgcDocumentSequenceSerializerAsync);
            }
            else if (typeof(System.Windows.Documents.DocumentReferenceCollection).IsAssignableFrom(objectType))
            {
                serializerType = typeof(NgcDocumentReferenceCollectionSerializerAsync);
            }
            else if (typeof(System.Windows.Documents.FixedDocument).IsAssignableFrom(objectType))
            {
                serializerType = typeof(NgcFixedDocumentSerializerAsync);
            }
            else if(typeof(System.Windows.Documents.PageContentCollection).IsAssignableFrom(objectType))
            {
                serializerType = typeof(NgcPageContentCollectionSerializerAsync);
            }
            else if(typeof(System.Windows.Documents.PageContent).IsAssignableFrom(objectType))
            {
                serializerType = typeof(NgcPageContentSerializerAsync);
            }
            else if(typeof(System.Windows.Controls.UIElementCollection).IsAssignableFrom(objectType))
            {
                serializerType = typeof(NgcUIElementCollectionSerializerAsync);
            }
            else if(typeof(System.Windows.Documents.FixedPage).IsAssignableFrom(objectType))
            {
                serializerType = typeof(NgcFixedPageSerializerAsync);
            }
            else if (typeof(System.Windows.Documents.DocumentPaginator).IsAssignableFrom(objectType))
            {
                serializerType = typeof(NgcDocumentPaginatorSerializerAsync);
            }
            else if (typeof(System.Windows.Documents.DocumentPage).IsAssignableFrom(objectType))
            {
                serializerType = typeof(NgcDocumentPageSerializerAsync);
            }
            else if (typeof(System.Windows.Media.Visual).IsAssignableFrom(objectType))
            {
                serializerType = typeof(NgcReachVisualSerializerAsync);
            }

            if(serializerType == null)
            {
                serializerType = base.GetSerializerType(objectType);
            }

            return serializerType;
        }

        internal
        override
        XmlWriter
        AcquireXmlWriter(
            Type    writerType
            )
        {
            return null;
        }

        internal
        override
        void
        ReleaseXmlWriter(
            Type    writerType
            )
        {
            return;
        }

        internal
        override
        XpsResourceStream
        AcquireResourceStream(
            Type    resourceType
            )
        {
            return null;
        }

        internal
        override
        XpsResourceStream
        AcquireResourceStream(
            Type    resourceType,
            String  resourceID
            )
        {
            return null;
        }

        internal
        override
        void
        ReleaseResourceStream(
            Type    resourceType
            )
        {
            return;
        }

        internal
        override
        void
        ReleaseResourceStream(
            Type    resourceType,
            String  resourceID
            )
        {
            return;
        }

        internal
        override
        void
        AddRelationshipToCurrentPage(
            Uri targetUri,
            string relationshipName
            )
        {
            return;
        }


        internal
        override
        BasePackagingPolicy
        PackagingPolicy
        {
            get
            {
                return null;
            }
        }

        internal
        override
        XpsResourcePolicy
        ResourcePolicy
        {
            get
            {
                return null;
            }
        }
        #endregion PackageSerializationManager override

        #region Internal Properties

        /// <summary>
        ///
        /// </summary>
        internal
        PrintQueue
        PrintQueue
        {
            get
            {
                return _printQueue;
            }
        }


        internal
        String
        JobName
        {
            set
            {
                if (_jobName == null)
                {
                    _jobName = value;
                }
            }

            get
            {
                return _jobName;
            }
        }

        internal
        Size
        PageSize
        {
            set
            {
                _pageSize = value;
            }

            get
            {
                return _pageSize;
            }
        }

        #endregion Internal Properties

        #region Internal Methods

        /// <summary>
        ///
        /// </summary>
        ///
        internal
        void
        StartDocument(
            Object o,
            bool   documentPrintTicketRequired
            )
        {
            if(documentPrintTicketRequired)
            {
                XpsSerializationPrintTicketRequiredEventArgs e =
                new XpsSerializationPrintTicketRequiredEventArgs(PrintTicketLevel.FixedDocumentPrintTicket,
                                                                 0);
                OnNGCSerializationPrintTicketRequired(e);
            }

            if( _startDocCnt == 0 )
            {

                JobName = PrintQueue.CurrentJobSettings.Description;

                if(JobName == null)
                {
                    JobName = NgcSerializerUtil.InferJobName(o);
                }

                _device  = new MetroToGdiConverter(PrintQueue);

                _device.StartDocument(_jobName, _printTicketManager.ConsumeActivePrintTicket(true));
            }
            _startDocCnt++;
        }

        /// <summary>
        ///
        /// </summary>
        ///
        internal
        void
        EndDocument()
        {
            EndDocument(false);
        }

        /// <summary>
        ///
        /// </summary>
        ///
        internal
        void
        EndDocument(bool abort)
        {
            if( _startDocCnt == 1 || abort )
            {
                _device.EndDocument(abort);

                //
                // Inform the listener that the doucment has been printed
                //
                XpsSerializationProgressChangedEventArgs e =
                new XpsSerializationProgressChangedEventArgs(XpsWritingProgressChangeLevel.FixedDocumentWritingProgress,
                                                             0,
                                                             0,
                                                             null);
                OnNGCSerializationProgressChanged(e);
            }
            _startDocCnt--;
        }

        /// <summary>
        ///
        /// </summary>
        ///
        internal
        bool
        StartPage()
        {
            bool    bManulStartDoc = false;

            if (_startDocCnt == 0)
            {
                StartDocument(null,true);
                bManulStartDoc = true;
            }

            if (_isPrintTicketMerged == false)
            {
                XpsSerializationPrintTicketRequiredEventArgs e =
                new XpsSerializationPrintTicketRequiredEventArgs(PrintTicketLevel.FixedPagePrintTicket,
                                                                 0);
                OnNGCSerializationPrintTicketRequired(e);
            }

            _device.StartPage(_printTicketManager.ConsumeActivePrintTicket(true));
            _isStartPage = true;

            return bManulStartDoc;
        }

        /// <summary>
        ///
        /// </summary>
        ///
        internal
        void
        EndPage()
        {
            _device.FlushPage();
            _isStartPage = false;
            _isPrintTicketMerged = false;

            //
            // Inform the listener that the page has been printed
            //
            XpsSerializationProgressChangedEventArgs e =
            new XpsSerializationProgressChangedEventArgs(XpsWritingProgressChangeLevel.FixedPageWritingProgress,
                                                         0,
                                                         0,
                                                         null);
            OnNGCSerializationProgressChanged(e);
        }

        internal
        void
        OnNGCSerializationPrintTicketRequired(
            object operationState
            )
        {
            XpsSerializationPrintTicketRequiredEventArgs e = operationState as XpsSerializationPrintTicketRequiredEventArgs;

            if(XpsSerializationPrintTicketRequired != null)
            {
                e.Modified = true;

                if (e.PrintTicketLevel == PrintTicketLevel.FixedPagePrintTicket)
                {
                    _isPrintTicketMerged = true;
                }

                XpsSerializationPrintTicketRequired(this,e);

                _printTicketManager.ConstructPrintTicketTree(e);
            }
        }

        internal
        void
        OnNGCSerializationProgressChanged(
            object operationState
            )
        {
            XpsSerializationProgressChangedEventArgs e = operationState as XpsSerializationProgressChangedEventArgs;

            if(XpsSerializationProgressChanged != null)
            {
                XpsSerializationProgressChanged(this,e);
            }
        }

        /// <summary>
        ///
        /// </summary>
        ///
        internal
        void
        WalkVisual(
            Visual visual
            )
        {
            bool    bManulStartDoc = false;
            bool    bManulStartpage = false;

            if (_startDocCnt == 0)
            {
                StartDocument(visual,true);
                bManulStartDoc = true;
            }
            if (!_isStartPage)
            {
                StartPage();
                bManulStartpage = true;
            }

            //
            // Call VisualTreeFlattener to flatten the visual on IMetroDrawingContext
            //
            VisualTreeFlattener.Walk(visual, _device, PageSize, new TreeWalkProgress());

            if (bManulStartpage)
            {
                EndPage();
            }
            if (bManulStartDoc)
            {
                EndDocument();
            }
        }

        internal
        PrintTicket
        GetActivePrintTicket()
        {
            return _printTicketManager.ActivePrintTicket;
        }

        internal
        bool
        IsPrintTicketEventHandlerEnabled
        {
            get
            {
                if(XpsSerializationPrintTicketRequired!=null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        internal
        Stack
        OperationStack
        {
            get
            {
                return _operationStack;
            }
        }

        private
        void
        PostSerializationTask(
            DispatcherOperationCallback taskItem
            )
        {
            _dispatcher.BeginInvoke(DispatcherPriority.Background,
                                    taskItem,
                                    null);
        }

        private
        void
        PostSerializationTask(
            DispatcherOperationCallback taskItem,
            object arg
            )
        {
            _dispatcher.BeginInvoke(DispatcherPriority.Background,
                                    taskItem,
                                    arg);
        }



        private
        bool
        IsSerializedObjectTypeSupported(
            Object  serializedObject
            )
        {
            bool isSupported = false;

            Type serializedObjectType = serializedObject.GetType();

            if(_isBatchMode)
            {
                if((typeof(System.Windows.Media.Visual).IsAssignableFrom(serializedObjectType)) &&
                    (serializedObjectType != typeof(System.Windows.Documents.FixedPage)))
                {
                    isSupported = true;
                }
            }
            else
            {
                if ( (serializedObjectType == typeof(System.Windows.Documents.FixedDocumentSequence)) ||
                     (serializedObjectType == typeof(System.Windows.Documents.FixedDocument))    ||
                     (serializedObjectType == typeof(System.Windows.Documents.FixedPage))        ||
                     (typeof(System.Windows.Documents.DocumentPaginator).IsAssignableFrom(serializedObjectType)) ||
                     (typeof(System.Windows.Media.Visual).IsAssignableFrom(serializedObjectType)) )
                {
                    isSupported = true;
                }
            }
            return isSupported;
        }

        private
        void
        NGCSerializationCompletionMethod(
            )
        {
            bool canceled = false;

            XpsSerializationCompletedEventArgs e = new XpsSerializationCompletedEventArgs(canceled,
                                                                                          null,
                                                                                          null);

            PostSerializationTask(new DispatcherOperationCallback(OnNGCSerializationCompleted), e);
        }

        private
        object
        OnNGCSerializationCompleted(
            object operationState
            )
        {
            XpsSerializationCompletedEventArgs e = operationState as XpsSerializationCompletedEventArgs;

            if (XpsSerializationCompleted != null)
            {
                XpsSerializationCompleted(this, e);
            }
            return null;
        }

        private
        bool
        IsDocumentSequencePrintTicketRequired(
            Object  serializedObject
            )
        {
            bool isRequired = false;

            Type serializedObjectType = serializedObject.GetType();

            if ((serializedObjectType == typeof(System.Windows.Documents.FixedDocument))    ||
                (serializedObjectType == typeof(System.Windows.Documents.FixedPage))        ||
                (typeof(System.Windows.Documents.DocumentPaginator).IsAssignableFrom(serializedObjectType)) ||
                (typeof(System.Windows.Media.Visual).IsAssignableFrom(serializedObjectType)) )
            {
                isRequired = true;
            }

            return isRequired;
        }

        private
        void
        WaitForPendingAsyncItems(
            )
        {
            do
            {
                _dispatcher.Invoke(DispatcherPriority.Background, (DispatcherOperationCallback)DoNothingCallback, null);

            }
            while (IsAsyncWorkPending());
        }

        /// <remarks>
        /// The logic in the method IsAsyncWorkPending *MUST* mirror the logic in InvokeSaveAsXamlBatchWorkItem
        /// IsAsyncWorkPending must return true when the manager is in a state that
        /// causes InvokeSaveAsXamlBatchWorkItem to process pending items.
        /// <remarks>
        private
        bool
        IsAsyncWorkPending(
            )
        {
            // This logic must mirror InvokeSaveAsXamlBatchWorkItem see remarks.

            if(!_serializationOperationCanceled)
            {
                if(!_isBatchWorkItemInProgress && _batchOperationQueue.Count > 0)
                {
                    // InvokeSaveAsXamlBatchWorkItem is expected to process an item from _batchOperationQueue
                    return true;
                }
                else {
                    if (_operationStack.Count > 0)
                    {
                        // InvokeSaveAsXamlBatchWorkItem is expected to process an item from _operationStack
                        return true;
                    }
                }
            }

            return false;
        }

        private static
        object
        DoNothingCallback(
            object notUsed
            )
        {
            return null;
        }

        #endregion Internal Methods

        #region Private Member

        private     Dispatcher              _dispatcher;
        private     PrintQueue              _printQueue;
        private     int                     _startDocCnt;
        private     bool                    _isStartPage;
        private     MetroToGdiConverter     _device;
        private     String                  _jobName;
        private     Stack                   _operationStack;
        private     bool                    _isBatchMode;
        private     Queue                   _batchOperationQueue;
        private     bool                    _serializationOperationCanceled;
        private     bool                    _isSimulating;
        private     bool                    _isBatchWorkItemInProgress;
        private     NgcPrintTicketManager   _printTicketManager;
        private     bool                    _isPrintTicketMerged;
        private     Size                    _pageSize;

        #endregion Private Member
    };
}

