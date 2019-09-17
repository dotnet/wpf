// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using System;
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
    internal class XpsOMSerializationManagerAsync : 
                   XpsOMSerializationManager,
                   IXpsSerializationManagerAsync
    {
        #region Constructor
        public
        XpsOMSerializationManagerAsync(
            XpsOMPackagingPolicy packagingPolicy,
            bool batchMode
            ):
        base(packagingPolicy, batchMode)
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            _serializationOperationCanceled = false;
            _currentPageXmlWriter = null;
            _isBatchWorkItemInProgress = false;

            _operationStack = new Stack();
            _batchOperationQueue = new Queue();
        }

        #endregion Constructor

        #region packageSerializationManager override

        public
        override
        void
        SaveAsXaml(
            Object serializedObject
            )
        {
            if (serializedObject == null)
            {
                throw new ArgumentNullException(nameof(serializedObject));
            }

            if (!XpsSerializationManager.IsSerializedObjectTypeSupported(serializedObject, IsBatchMode))
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_NotSupported));
            }

            if (Simulator == null)
            {
                Simulator = new XpsOMHierarchySimulator(this,
                                                         serializedObject);
            }

            if (!IsSimulating)
            {
                Simulator.BeginConfirmToXPSStructure(IsBatchMode);
                IsSimulating = true;
            }

            if (IsBatchMode)
            {
                //
                // Add the Visual received in to the queue
                //
                BatchOperationWorkItem batchOperationWorkItem = new BatchOperationWorkItem(BatchOperationType.batchWrite,
                                                                                            serializedObject);
                _batchOperationQueue.Enqueue(batchOperationWorkItem);
                PostSerializationTask(new DispatcherOperationCallback(InvokeSaveAsXamlBatchWorkItem));
            }
            else
            {
                ReachSerializer reachSerializer = GetSerializer(serializedObject);

                if (reachSerializer != null)
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

        #endregion packageSerializationManager override

        #region internal methods

        internal
        Object
        InvokeSaveAsXamlWorkItem(
            Object arg
            )
        {
            try
            {
                if (!_serializationOperationCanceled)
                {
                    if (_operationStack.Count > 0)
                    {
                        Object objectOnStack = _operationStack.Pop();

                        if (objectOnStack.GetType() ==
                           typeof(System.Windows.Xps.Serialization.SerializationManagerOperationContextStack))
                        {
                            SerializationManagerOperationContextStack context =
                                                                      (SerializationManagerOperationContextStack)objectOnStack;

                            context.ReachSerializer.SerializeObject(context.SerializedObject);
                        }
                        else if (typeof(System.Windows.Xps.Serialization.ReachSerializerContext).IsAssignableFrom(objectOnStack.GetType()))
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
            catch (Exception e) when (!CriticalExceptions.IsCriticalException(e))
            {

                //
                // Indicate that an error happened
                //
                bool canceled = false;

                XpsSerializationCompletedEventArgs args = new XpsSerializationCompletedEventArgs(canceled,
                                                                                                 null,
                                                                                                 e);

                _serializationOperationCanceled = true;

                PostSerializationTask(new DispatcherOperationCallback(OnXPSSerializationCompleted), args);
            }

            return null;
        }

        internal
        Object
        InvokeSaveAsXamlBatchWorkItem(
            Object arg
            )
        {
            try
            {
                // This logic must be mirrored in IsAsyncWorkPending see remarks.

                if (!_serializationOperationCanceled)
                {
                    if (!_isBatchWorkItemInProgress && _batchOperationQueue.Count > 0)
                    {
                        BatchOperationWorkItem batchOperationWorkItem = (BatchOperationWorkItem)_batchOperationQueue.Dequeue();

                        if (batchOperationWorkItem.OperationType == BatchOperationType.batchWrite)
                        {
                            _currentPageXmlWriter = Simulator.SimulateBeginFixedPage();

                            ReachSerializer reachSerializer = GetSerializer(batchOperationWorkItem.SerializedObject);

                            if (reachSerializer != null)
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
                        else if (batchOperationWorkItem.OperationType == BatchOperationType.batchCommit)
                        {
                            Simulator.EndConfirmToXPSStructure(IsBatchMode);
                            XPSSerializationCompletionMethod();
                        }
                    }
                    else
                    {
                        if (_operationStack.Count > 0)
                        {
                            Object objectOnStack = _operationStack.Pop();

                            if (objectOnStack.GetType() ==
                               typeof(System.Windows.Xps.Serialization.SerializationManagerOperationContextStack))
                            {
                                SerializationManagerOperationContextStack context =
                                                                          (SerializationManagerOperationContextStack)objectOnStack;

                                context.ReachSerializer.SerializeObject(context.SerializedObject);
                            }
                            else if (typeof(System.Windows.Xps.Serialization.ReachSerializerContext).IsAssignableFrom(objectOnStack.GetType()))
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
                            _currentPageXmlWriter = null;
                        }
                    }
                }
            }
            catch (Exception e) when (!CriticalExceptions.IsCriticalException(e))
            {
                XpsSerializationCompletedEventArgs args = new XpsSerializationCompletedEventArgs(false, // Indicate that an error happened
                                                                                                 null,
                                                                                                 e);

                _serializationOperationCanceled = true;

                PostSerializationTask(new DispatcherOperationCallback(OnXPSSerializationCompleted), args);
            }

            return null;
        }

        public
        void
        CancelAsync(
            )
        {
            XpsSerializationCompletedEventArgs e = new XpsSerializationCompletedEventArgs(true,
                                                                                          null,
                                                                                          null);

            _serializationOperationCanceled = true;

            PostSerializationTask(new DispatcherOperationCallback(OnXPSSerializationCompleted), e);
        }

        internal
        override
        void
        Commit(
            )
        {
            if (IsBatchMode && IsSimulating)
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
                serializerType = typeof(XpsOMFixedDocumentSerializerAsync);
            }
            else if (typeof(System.Windows.Documents.PageContentCollection).IsAssignableFrom(objectType))
            {
                serializerType = typeof(ReachPageContentCollectionSerializerAsync);
            }
            else if (typeof(System.Windows.Documents.PageContent).IsAssignableFrom(objectType))
            {
                serializerType = typeof(ReachPageContentSerializerAsync);
            }
            else if (typeof(System.Windows.Controls.UIElementCollection).IsAssignableFrom(objectType))
            {
                serializerType = typeof(ReachUIElementCollectionSerializerAsync);
            }
            else if (typeof(System.Windows.Documents.FixedPage).IsAssignableFrom(objectType))
            {
                serializerType = typeof(XpsOMFixedPageSerializerAsync);
            }
            else if (typeof(System.Windows.Documents.FixedDocumentSequence).IsAssignableFrom(objectType))
            {
                serializerType = typeof(XpsOMDocumentSequenceSerializerAsync);
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
                serializerType = typeof(XpsOMDocumentPaginatorSerializerAsync);
            }
            else if (typeof(System.Windows.Documents.DocumentPage).IsAssignableFrom(objectType))
            {
                serializerType = typeof(XpsOMDocumentPageSerializerAsync);
            }
            else if (typeof(System.Windows.Media.Visual).IsAssignableFrom(objectType))
            {
                serializerType = typeof(ReachVisualSerializerAsync);
            }
            else if (typeof(System.Printing.PrintTicket).IsAssignableFrom(objectType))
            {
                serializerType = typeof(PrintTicketSerializer);
            }

            if (serializerType == null)
            {
                base.GetSerializerType(objectType);
            }

            return serializerType;
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

        Stack
        IXpsSerializationManagerAsync.OperationStack
        {
            get
            {
                return OperationStack;
            }
        }

        #endregion internal methods

        #region private methods

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

            if (!_serializationOperationCanceled)
            {
                if (!_isBatchWorkItemInProgress && _batchOperationQueue.Count > 0)
                {
                    // InvokeSaveAsXamlBatchWorkItem is expected to process an item from _batchOperationQueue
                    return true;
                }
                else
                {
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

        #endregion private methods

        #region events

        public
        event
        XpsSerializationCompletedEventHandler XpsSerializationCompleted;

        #endregion events

        #region private data

        Dispatcher _dispatcher;
        bool _serializationOperationCanceled;
        XmlWriter _currentPageXmlWriter;
        bool _isBatchWorkItemInProgress;
        Stack _operationStack;
        Queue _batchOperationQueue;

        #endregion private data
    }
}
