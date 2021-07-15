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
    /// Leverages the functionality in XpsOMDocumentPageSerializer
    /// to serialize a Document Page asynchronously
    /// </summary>
    internal class XpsOMDocumentPaginatorSerializerAsync :
                   ReachSerializerAsync
    {
        public
        XpsOMDocumentPaginatorSerializerAsync(
            PackageSerializationManager manager
            )
            :
        base(manager)
        {
            ///
            /// Fail if manager is not XpsOMSerializationManagerAsync
            /// 
            _xpsOMSerializationManagerAsync = (XpsOMSerializationManagerAsync)manager;
            _syncSerializer = new XpsOMDocumentPaginatorSerializer(manager);
        }

        public
        override
        void
        AsyncOperation(
            ReachSerializerContext context
        )
        {
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
            SerializableObjectContext serializableObjectContext = _syncSerializer.SerializeObjectInternal(serializedObject);
            PersistObjectData(serializableObjectContext);
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
            DocumentPaginator paginator = _syncSerializer.BeginPersistObjectData(serializableObjectContext);

            ReachSerializerContext context = new ReachSerializerContext(this,
                                                                        SerializerAction.endPersistObjectData);

            _xpsOMSerializationManagerAsync.OperationStack.Push(context);


            int index = 0;

            DocumentPaginatorSerializerContext
            collectionContext = new DocumentPaginatorSerializerContext(this,
                                                                        serializableObjectContext,
                                                                        paginator,
                                                                        index,
                                                                        SerializerAction.serializeNextDocumentPage);

            _xpsOMSerializationManagerAsync.OperationStack.Push(collectionContext);
        }

        internal
        override
        void
        EndPersistObjectData(
            )
        {
            _syncSerializer.EndPersistObjectData();
        }

        private
        void
        SerializeNextDocumentPage(
            ReachSerializerContext context
            )
        {

            DocumentPaginatorSerializerContext paginatorContext = context as DocumentPaginatorSerializerContext;

            if (paginatorContext != null)
            {
                DocumentPaginator paginator = paginatorContext.Paginator;
                int index = paginatorContext.Index;

                if (!paginator.IsPageCountValid ||
                   (index < paginator.PageCount))
                {
                    index++;

                    DocumentPaginatorSerializerContext
                    collectionContext = new DocumentPaginatorSerializerContext(this,
                                                                               paginatorContext.ObjectContext,
                                                                               paginator,
                                                                               index,
                                                                               SerializerAction.serializeNextDocumentPage);
                    _xpsOMSerializationManagerAsync.OperationStack.Push(collectionContext);

                    DocumentPage page = Toolbox.GetPage(paginator, index - 1);

                    ReachSerializer serializer = SerializationManager.GetSerializer(page);
                    if (serializer != null)
                    {
                        serializer.SerializeObject(page);
                    }
                }
            }
        }

        private XpsOMSerializationManagerAsync _xpsOMSerializationManagerAsync;

        ///
        /// This serializer uses its synchronous counterpart in a similar way
        /// as if it was inheriting from it, since we already inherit from
        /// ReachSerializerAsync, we hold a private reference to the
        /// synchronous serializer and call into it to do the bulk of the work
        ///
        private XpsOMDocumentPaginatorSerializer _syncSerializer;
    };
}


