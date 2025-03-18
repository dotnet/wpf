// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;
using System.Windows.Documents;

namespace System.Windows.Xps.Serialization
{
    /// <summary>
    /// Leverages the functionality in XpsOMDocumentPageSerializer
    /// to serialize a Document Page asynchronously
    /// </summary>
    internal class XpsOMDocumentPageSerializerAsync :
                   ReachSerializerAsync
    {
        public
        XpsOMDocumentPageSerializerAsync(
            PackageSerializationManager manager
            )
            :
        base(manager)
        {
            ///
            /// Fail if manager is not XpsOMSerializationManagerAsync
            /// 
            _xpsOMSerializationManagerAsync = (XpsOMSerializationManagerAsync)manager;
            _syncSerializer = new XpsOMDocumentPageSerializer(_xpsOMSerializationManagerAsync);
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

                case SerializerAction.endSerializeDocumentPage:
                    {
                        ReachFixedPageSerializerContext thisContext = context as ReachFixedPageSerializerContext;
                        _syncSerializer.EndSerializeDocumentPage(thisContext.TreeWalker);
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
            _syncSerializer.Initialize();

            base.SerializeObject(serializedObject);
        }

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
            DocumentPage dp = (DocumentPage)serializableObjectContext.TargetObject;
            ReachTreeWalker treeWalker = _syncSerializer.BeginSerializeDocumentPage(serializableObjectContext);

            ReachSerializerContext context = new ReachFixedPageSerializerContext(this,
                                                                                 serializableObjectContext,
                                                                                 SerializerAction.endSerializeDocumentPage,
                                                                                 false,
                                                                                 treeWalker);

            _xpsOMSerializationManagerAsync.OperationStack.Push(context);

            _syncSerializer.SerializeChild(dp.Visual, serializableObjectContext);
        }

        /// <remarks>
        /// Redirect calls to this override coming from 
        /// the serialization engine to the baseSerializer
        /// </remarks>
        public
        override
        XmlWriter
        XmlWriter
        {
            get
            {
                return _syncSerializer.XmlWriter;
            }

            set
            {
                _syncSerializer.XmlWriter = value;
            }
        }

        private XpsOMSerializationManagerAsync _xpsOMSerializationManagerAsync;

        ///
        /// This serializer uses its synchronous counterpart in a similar way
        /// as if it was inheriting from it, since we already inherit from
        /// ReachSerializerAsync, we hold a private reference to the
        /// synchronous serializer and call into it to do the bulk of the work
        ///
        private XpsOMDocumentPageSerializer _syncSerializer;
    };
}
