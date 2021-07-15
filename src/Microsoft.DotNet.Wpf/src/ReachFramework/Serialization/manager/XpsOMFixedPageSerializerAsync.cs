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
using System.Printing;
using System.Security;
using System.ComponentModel.Design.Serialization;
using System.Windows.Xps.Packaging;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Markup;
using MS.Utility;

namespace System.Windows.Xps.Serialization
{
    /// <summary>
    /// Leverages the functionality in XpsOMFixedPageSerializer
    /// to serialize a Fixed Page asynchronously
    /// </summary>
    internal class XpsOMFixedPageSerializerAsync :
                   ReachSerializerAsync
    {
        #region Constructor

        public
        XpsOMFixedPageSerializerAsync(
            PackageSerializationManager manager
            ) :
            base(manager)
        {
            ///
            /// Fail if manager is not XpsOMSerializationManagerAsync
            /// 
            _xpsOMSerializationManagerAsync = (XpsOMSerializationManagerAsync)manager;
            _syncSerializer = new XpsOMFixedPageSerializer(manager);
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
            if (context != null)
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
                            _syncSerializer.EndPersistObjectData(thisContext.EndVisual, thisContext.TreeWalker);
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
            ReachTreeWalker treeWalker;
            bool needEndVisual = _syncSerializer.BeginPersistObjectData(serializableObjectContext, out treeWalker);

            if (serializableObjectContext.IsComplexValue)
            {
                ReachSerializerContext context = new ReachFixedPageSerializerContext(this,
                                                                                        serializableObjectContext,
                                                                                        SerializerAction.endSerializeReachFixedPage,
                                                                                        needEndVisual,
                                                                                        treeWalker);

                 _xpsOMSerializationManagerAsync.OperationStack.Push(context);

                SerializeObjectCore(serializableObjectContext);
            }
            else
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_WrongPropertyTypeForFixedPage));
            }
        }

        /// <remarks>
        /// Redirect calls to this override coming from 
        /// the serialization engine to the baseSerializer
        /// </remarks>
        internal
        override
        void
        WriteSerializedAttribute(
            SerializablePropertyContext serializablePropertyContext
            )
        {
            _syncSerializer.WriteSerializedAttribute(serializablePropertyContext);
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

        #endregion Public Properties

        private XpsOMSerializationManagerAsync _xpsOMSerializationManagerAsync;

        ///
        /// This serializer uses its synchronous counterpart in a similar way
        /// as if it was inheriting from it, since we already inherit from
        /// ReachSerializerAsync, we hold a private reference to the
        /// synchronous serializer and call into it to do the bulk of the work
        ///
        private XpsOMFixedPageSerializer _syncSerializer;
    };
}
