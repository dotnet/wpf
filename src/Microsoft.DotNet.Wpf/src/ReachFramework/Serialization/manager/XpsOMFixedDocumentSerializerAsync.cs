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
using System.Windows.Markup;

namespace System.Windows.Xps.Serialization
{
    /// <summary>
    /// Leverages the functionality in XpsOMFixedDocumentSerializer
    /// to serialize a Fixed Document asynchronously
    /// </summary>
    internal class XpsOMFixedDocumentSerializerAsync :
                   ReachSerializerAsync
    {
        #region Constructor

        public
        XpsOMFixedDocumentSerializerAsync(
            PackageSerializationManager manager
            ) :
            base(manager)
        {
            ///
            /// Fail if manager is not XpsOMSerializationManagerAsync
            /// 
            _xpsOMSerializationManagerAsync = (XpsOMSerializationManagerAsync)manager;
            _syncSerializer = new XpsOMFixedDocumentSerializer(manager);
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
            if (context.Action == SerializerAction.endPersistObjectData)
            {
                EndPersistObjectData();
            }

            else
            {
                base.AsyncOperation(context);
            }
        }

        public
        override
        void
        SerializeObject(
            Object serializedObject
            )
        {
            _syncSerializer.Initialize();

            base.SerializeObject(serializedObject);
        }

        #endregion Public Methods

        #region Internal Methods

        internal
        override
        void
        SerializeObject(
            SerializablePropertyContext serializedProperty
            )
        {
            _syncSerializer.Initialize();

            base.SerializeObject(serializedProperty);
        }

        internal
        override
        void
        PersistObjectData(
            SerializableObjectContext serializableObjectContext
            )
        {
            _syncSerializer.BeginPersistObjectData(serializableObjectContext);

            if (serializableObjectContext.IsComplexValue)
            {
                ReachSerializerContext context = new ReachSerializerContext(this,
                                                                            SerializerAction.endPersistObjectData);

                _xpsOMSerializationManagerAsync.OperationStack.Push(context);

                SerializeObjectCore(serializableObjectContext);
            }
            else
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_WrongPropertyTypeForFixedDocument));
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

        #endregion Internal Methods

        private XpsOMSerializationManagerAsync _xpsOMSerializationManagerAsync;

        ///
        /// This serializer uses its synchronous counterpart in a similar way
        /// as if it was inheriting from it, since we already inherit from
        /// ReachSerializerAsync, we hold a private reference to the
        /// synchronous serializer and call into it to do the bulk of the work
        ///
        private XpsOMFixedDocumentSerializer _syncSerializer;
    };
}


