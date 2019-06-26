// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using System;
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
    /// <summary>
    /// Leverages the functionality in XpsOMDocumentSequenceSerializer
    /// to serialize a Document Sequence asynchronously
    /// </summary>
    internal class XpsOMDocumentSequenceSerializerAsync :
                   ReachSerializerAsync
    {
        public
        XpsOMDocumentSequenceSerializerAsync(
            PackageSerializationManager manager
            ) :
            base(manager)
        {
            ///
            /// Fail if manager is not XpsOMSerializationManagerAsync
            /// 
            _xpsOMSerializationManagerAsync = (XpsOMSerializationManagerAsync)manager;
            _syncSerializer = new XpsOMDocumentSequenceSerializer(manager);
        }

        public
        override
        void
        AsyncOperation(
            ReachSerializerContext context
            )
        {
            if (context.Action == SerializerAction.endPersistObjectData)
            {
                EndPersistObjectData();
            }
            else
            {
                base.AsyncOperation(context);
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
            _syncSerializer.BeginPersistObjectData(serializableObjectContext);

            ReachSerializerContext context = new ReachSerializerContext(this,
                                                                        SerializerAction.endPersistObjectData);

            _xpsOMSerializationManagerAsync.OperationStack.Push(context);

            if (serializableObjectContext.IsComplexValue)
            {
                SerializeObjectCore(serializableObjectContext);
            }

        }

        internal
        override
        void
        EndPersistObjectData(
            )
        {

            _syncSerializer.EndPersistObjectData();
        }

        private XpsOMSerializationManagerAsync _xpsOMSerializationManagerAsync;

        ///
        /// This serializer uses its synchronous counterpart in a similar way
        /// as if it was inheriting from it, since we already inherit from
        /// ReachSerializerAsync, we hold a private reference to the
        /// synchronous serializer and call into it to do the bulk of the work
        ///
        private XpsOMDocumentSequenceSerializer _syncSerializer;
    };
}


