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
using System.Windows.Media.Imaging;
using System.Windows.Markup;
using System.Windows.Threading;
using MS.Internal;
using System.Printing;

namespace System.Windows.Xps.Serialization
{
    /// <summary>
    /// This class defines all necessary methods that are necessary to provide
    /// asynchronous serialization services for persisting an AVALON root object
    /// into an XPS package. It glues together all necessary serializers and
    /// type converters for different type of objects to produce the correct
    /// serialized content in the package.
    /// </summary>
    public sealed class XpsSerializationManagerAsync :
                        XpsSerializationManager,
                        IXpsSerializationManagerAsync
    {
        /// <summary>
        /// Constructor to create and initialize the XpsSerializationManagerAsync
        /// </summary>
        public
        XpsSerializationManagerAsync(
            BasePackagingPolicy  packagingPolicy,
            bool                 batchMode
            ):
        base(packagingPolicy,  batchMode)
        {
            _dispatcher                           = Dispatcher.CurrentDispatcher;
            _serializationOperationCanceled       = false;
            this._currentPageXmlWriter            = null;
            this._isBatchWorkItemInProgress       = false;

            _operationStack                       = new Stack();
            _batchOperationQueue                  = new Queue();

            XpsDriverDocEventManager    xpsDocEventManager = base.GetXpsDriverDocEventManager();

            if (xpsDocEventManager != null)
            {
                XpsSerializationCompletedInternal += new XpsSerializationCompletedEventHandler(xpsDocEventManager.ForwardSerializationCompleted);

            }
        }


        /// <summary>
        ///
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

            if(Simulator == null)
            {
                Simulator = new ReachHierarchySimulator(this,
                                                         serializedObject);
            }

            if(!IsSimulating)
            {
                Simulator.BeginConfirmToXPSStructure(IsBatchMode);
                IsSimulating = true;
            }

            if(IsBatchMode)
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
                        else if(typeof(System.Windows.Xps.Serialization.ReachSerializerContext).IsAssignableFrom(objectOnStack.GetType()))
                        {
                            ReachSerializerContext context = (ReachSerializerContext)objectOnStack;
                            context.Serializer.AsyncOperation(context);
                        }
                        PostSerializationTask(new DispatcherOperationCallback(InvokeSaveAsXamlWorkItem));
                    }
                    else
                    {
                        Simulator.EndConfirmToXPSStructure(IsBatchMode);
                        XPSSerializationCompletionMethod();
                    }
                }
            }
            catch(Exception e)
            {
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

                PostSerializationTask(new DispatcherOperationCallback(OnXPSSerializationCompleted), args);

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

                if(!_serializationOperationCanceled)
                {
                    if(!_isBatchWorkItemInProgress && _batchOperationQueue.Count > 0)
                    {
                        BatchOperationWorkItem batchOperationWorkItem = (BatchOperationWorkItem)_batchOperationQueue.Dequeue();

                        if(batchOperationWorkItem.OperationType == BatchOperationType.batchWrite)
                        {
                            _currentPageXmlWriter = Simulator.SimulateBeginFixedPage();

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
                            Simulator.EndConfirmToXPSStructure(IsBatchMode);
                            XPSSerializationCompletionMethod();
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
                            else if(typeof(System.Windows.Xps.Serialization.ReachSerializerContext).IsAssignableFrom(objectOnStack.GetType()))
                            {
                                ReachSerializerContext context = (ReachSerializerContext)objectOnStack;
                                context.Serializer.AsyncOperation(context);
                            }
                            PostSerializationTask(new DispatcherOperationCallback(InvokeSaveAsXamlBatchWorkItem));
                        }
                        else
                        {
                            Simulator.SimulateEndFixedPage(_currentPageXmlWriter);
                            _isBatchWorkItemInProgress = false;
                            _currentPageXmlWriter      = null;
                        }
                    }
                }
            }
            catch(Exception e)
            {
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

                PostSerializationTask(new DispatcherOperationCallback(OnXPSSerializationCompleted), args);

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

            PostSerializationTask(new DispatcherOperationCallback(OnXPSSerializationCompleted), e);
        }

        /// <summary>
        ///
        /// </summary>
        public
        override
        void
        Commit(
            )
        {
            if(IsBatchMode && IsSimulating)
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

        /*/// <summary>
        ///
        /// </summary>
        public
        event
        XPSSerializationProgressChangedEventHandler XPSSerializationProgressChanged;*/

        /// <summary>
        ///
        /// </summary>
        public
        event
        XpsSerializationCompletedEventHandler               XpsSerializationCompleted;

        /// <summary>
        /// XpsDriverDocEventManager subscribes for this event. We want to avoid chaining internal
        /// subscribers as they might delay the event to be raised for external (app) subscribers.
        /// </summary>
        internal
        event
        XpsSerializationCompletedEventHandler XpsSerializationCompletedInternal;

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


            if (typeof(System.Windows.Documents.FixedDocument).IsAssignableFrom(objectType))
            {
                serializerType = typeof(FixedDocumentSerializerAsync);
            }
            else if (typeof(System.Windows.Documents.PageContentCollection).IsAssignableFrom(objectType))
            {
                serializerType = typeof(ReachPageContentCollectionSerializerAsync);
            }
            else if(typeof(System.Windows.Documents.PageContent).IsAssignableFrom(objectType))
            {
                serializerType = typeof(ReachPageContentSerializerAsync);
            }
            else if(typeof(System.Windows.Controls.UIElementCollection).IsAssignableFrom(objectType))
            {
                serializerType = typeof(ReachUIElementCollectionSerializerAsync);
            }
            else if(typeof(System.Windows.Documents.FixedPage).IsAssignableFrom(objectType))
            {
                serializerType = typeof(FixedPageSerializerAsync);
            }
            else if (typeof(System.Windows.Documents.FixedDocumentSequence).IsAssignableFrom(objectType))
            {
                serializerType = typeof(DocumentSequenceSerializerAsync);
            }
            else if (typeof(System.Windows.Documents.DocumentReferenceCollection).IsAssignableFrom(objectType))
            {
                serializerType = typeof(ReachDocumentReferenceCollectionSerializerAsync);
            }
            else if (typeof(System.Windows.Documents.DocumentReference).IsAssignableFrom(objectType))
            {
                serializerType = typeof(ReachDocumentReferenceSerializerAsync);
            }
            else if (typeof(System.Windows.Documents.DocumentPaginator).IsAssignableFrom(objectType))
            {
                serializerType = typeof(DocumentPaginatorSerializerAsync);
            }
            else if (typeof(System.Windows.Documents.DocumentPage).IsAssignableFrom(objectType))
            {
                serializerType = typeof(DocumentPageSerializerAsync);
            }
            else if (typeof(System.Windows.Media.Visual).IsAssignableFrom(objectType))
            {
                serializerType = typeof(ReachVisualSerializerAsync);
            }
            else if (typeof(System.Printing.PrintTicket).IsAssignableFrom(objectType))
            {
                serializerType = typeof(PrintTicketSerializer);
            }

            if(serializerType == null)
            {
                base.GetSerializerType(objectType);
            }

            return serializerType;
        }

        Stack
        IXpsSerializationManagerAsync.OperationStack
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
        void
        XPSSerializationCompletionMethod(
            )
        {
            bool canceled = false;

            XpsSerializationCompletedEventArgs e = new XpsSerializationCompletedEventArgs(canceled,
                                                                                          null,
                                                                                          null);

            PostSerializationTask(new DispatcherOperationCallback(OnXPSSerializationCompleted), e);
        }

        private
        object
        OnXPSSerializationCompleted(
            object operationState
            )
        {
            XpsSerializationCompletedEventArgs e = operationState as XpsSerializationCompletedEventArgs;

            if (XpsSerializationCompleted != null)
            {
                XpsSerializationCompleted(this, e);
            }

            if (XpsSerializationCompletedInternal != null)
            {
                XpsSerializationCompletedInternal(this, e);
            }
            return null;
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

        private
        Dispatcher                  _dispatcher;

        private
        Stack                       _operationStack;

        private
        Queue                       _batchOperationQueue;

        private
        bool                        _serializationOperationCanceled;

        private
        XmlWriter                   _currentPageXmlWriter;

        private
        bool                        _isBatchWorkItemInProgress;
    };

    internal class SerializationManagerOperationContextStack
    {
        public
        SerializationManagerOperationContextStack(
            ReachSerializer serializer,
            Object          serializedObject
            )
        {
            this._serializer       = serializer;
            this._serializedObject = serializedObject;
        }

        public
        ReachSerializer
        ReachSerializer
        {
            get
            {
                return _serializer;
            }
        }

        public
        Object
        SerializedObject
        {
            get
            {
                return _serializedObject;
            }
        }

        private
        ReachSerializer     _serializer;

        private
        Object              _serializedObject;

    }

    /// <summary>
    ///
    /// </summary>
    public
    delegate
    void
    XpsSerializationProgressChangedEventHandler(
        object                                   sender,
        XpsSerializationProgressChangedEventArgs e
        );

    /// <summary>
    ///
    /// </summary>
    public
    delegate
    void
    XpsSerializationCompletedEventHandler(
        object                              sender,
        XpsSerializationCompletedEventArgs  e
        );

    /// <summary>
    ///
    /// </summary>
    public class XpsSerializationCompletedEventArgs :
                 AsyncCompletedEventArgs
    {
        /// <summary>
        ///
        /// </summary>
        public
        XpsSerializationCompletedEventArgs(
            bool        canceled,
            object      state,
            Exception   exception
            ) :
            base(exception, canceled, state)
        {
        }
    };

    /// <summary>
    ///
    /// </summary>
    public class XpsSerializationProgressChangedEventArgs :
                 ProgressChangedEventArgs
    {
        /// <summary>
        ///
        /// </summary>
        public
        XpsSerializationProgressChangedEventArgs(
            XpsWritingProgressChangeLevel   writingLevel,
            int                             pageNumber,
            int                             progressPercentage,
            object                          userToken) : 
            base( progressPercentage, userToken )
        {
            this._pageNumber   = pageNumber;
            this._writingLevel = writingLevel;
        }

        /// <summary>
        ///
        /// </summary>
        public 
        XpsWritingProgressChangeLevel
        WritingLevel
        {
            get
            {
                return _writingLevel;
            }
        }

        /// <summary>
        ///
        /// </summary>
        public int PageNumber
        {
            get
            {
                return _pageNumber;
            }
        }

        private int                           _pageNumber   = 0;
        private XpsWritingProgressChangeLevel _writingLevel = XpsWritingProgressChangeLevel.None;
    }

    /// <summary>
    ///
    /// </summary>
    public class XpsSerializationPrintTicketRequiredEventArgs :
                 EventArgs
    {
        /// <summary>
        ///
        /// </summary>
        public
        XpsSerializationPrintTicketRequiredEventArgs(
            PrintTicketLevel printTicketLevel,
            int              sequence
            )
        {
            _level          = printTicketLevel;
            _sequence       = sequence;
            _printTicket    = null;
            _modified       = false;
        }

        /// <summary>
        ///
        /// </summary>
        public
        PrintTicket
        PrintTicket
        {
            set
            {
                _printTicket = value;
            }

            get
            {
                return _printTicket;
            }
        }

        /// <summary>
        ///
        /// </summary>
        public
        PrintTicketLevel
        PrintTicketLevel
        {
            get
            {
                return _level;
            }
        }

        internal
        bool
        Modified
        {
            set
            {
                _modified = value;
            }

            get
            {
                return _modified;
            }
        }


        /// <summary>
        ///
        /// </summary>
        public
        int
        Sequence
        {
            get
            {
                return _sequence;
            }
        }

        private
        int                 _sequence;

        private
        bool                _modified;

        private
        PrintTicket         _printTicket;

        private
        PrintTicketLevel    _level;
    };

    /// <summary>
    ///
    /// </summary>
    public enum PrintTicketLevel
    {
        /// <summary>
        ///
        /// </summary>
        None                             = 0,
        /// <summary>
        ///
        /// </summary>
        FixedDocumentSequencePrintTicket = 1,
        /// <summary>
        ///
        /// </summary>
        FixedDocumentPrintTicket         = 2,
        /// <summary>
        ///
        /// </summary>
        FixedPagePrintTicket             = 3
    };


    /// <summary>
    /// 
    /// </summary>
    public enum XpsWritingProgressChangeLevel
    {
        /// <summary>
        ///
        /// </summary>
        None                                 = 0,
        /// <summary>
        ///
        /// </summary>
        FixedDocumentSequenceWritingProgress = 1,
        /// <summary>
        ///
        /// </summary>
        FixedDocumentWritingProgress         = 2,
        /// <summary>
        ///
        /// </summary>
        FixedPageWritingProgress             = 3
    };


    internal enum BatchOperationType
    {
        batchWrite  = 1,
        batchCommit = 2
    };

    internal class BatchOperationWorkItem
    {
        public
        BatchOperationWorkItem(
            BatchOperationType  type,
            Object              serializedObject
            )
        {
            this._type             = type;
            this._serializedObject = serializedObject;
        }

        public
        BatchOperationType
        OperationType
        {
            get
            {
                return _type;
            }
        }

        public
        Object
        SerializedObject
        {
            get
            {
                return _serializedObject;
            }
        }

        private
        BatchOperationType  _type;
        Object              _serializedObject;
    };
}
