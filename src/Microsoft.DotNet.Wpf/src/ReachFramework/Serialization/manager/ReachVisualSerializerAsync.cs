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

namespace System.Windows.Xps.Serialization
{
    /// <summary>
    /// 
    /// </summary>
    internal class ReachVisualSerializerAsync :
                   ReachSerializerAsync
    {
        /// <summary>
        /// 
        /// </summary>
        public
        ReachVisualSerializerAsync(
            PackageSerializationManager manager
            ):
        base(manager)
        {
        }

        public
        override
        void
        AsyncOperation(
            ReachSerializerContext context
            )
        {
            if(context == null)
            {
           
            }

            switch (context.Action) 
            {
                case SerializerAction.serializeNextTreeNode:
                {
                    ReachVisualSerializerContext thisContext = context as ReachVisualSerializerContext;

                    if(thisContext != null)
                    {
                        SerializeNextTreeNode(thisContext);
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
        public
        override
        void
        SerializeObject(
            object serializedObject
            )
        {
            Visual v = serializedObject as Visual;

            if (v == null)
            {
                throw new ArgumentException(SR.Get(SRID.MustBeOfType, "serializedObject", typeof(Visual)));
            }

            IXpsSerializationManagerAsync manager = (IXpsSerializationManagerAsync)SerializationManager;

            XmlWriter pageWriter  = ((PackageSerializationManager)manager).
                                    PackagingPolicy.AcquireXmlWriterForPage();

            XmlWriter resWriter = ((PackageSerializationManager)manager).
                                    PackagingPolicy.AcquireXmlWriterForResourceDictionary();

            SerializeTree(v, resWriter, pageWriter);
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

        private
        void
        SerializeTree(
            Visual visual,
            XmlWriter resWriter,
            XmlWriter bodyWriter
            )
        {
            Size fixedPageSize = ((IXpsSerializationManager)SerializationManager).FixedPageSize;

            VisualTreeFlattener flattener = ((IXpsSerializationManagerAsync)SerializationManager).
                                              VisualSerializationService.AcquireVisualTreeFlattener(resWriter,
                                                                                                    bodyWriter,
                                                                                                    fixedPageSize);

            if (flattener.StartVisual(visual))
            {
                Stack<NodeContext> contextStack = new Stack<NodeContext>();
                contextStack.Push(new NodeContext(visual));

                ReachVisualSerializerContext context = new ReachVisualSerializerContext(this,
                                                                                        contextStack,
                                                                                        flattener,
                                                                                        SerializerAction.serializeNextTreeNode);

                ((IXpsSerializationManagerAsync)SerializationManager).OperationStack.Push(context);
            }
        }

        private
        void
        SerializeNextTreeNode(
            ReachVisualSerializerContext context
            )
        {
            if(context.ContextStack.Count > 0)
            {
                Stack<NodeContext>  contextStack = context.ContextStack;
                VisualTreeFlattener flattener    = context.VisualFlattener;

                ReachVisualSerializerContext nextContext = new ReachVisualSerializerContext(this,
                                                                                        contextStack,
                                                                                        flattener,
                                                                                        SerializerAction.serializeNextTreeNode);

                ((IXpsSerializationManagerAsync)SerializationManager).OperationStack.Push(nextContext);


                NodeContext ctx = contextStack.Peek();

                Visual v = ctx.GetNextChild();

                if (v != null)
                {
                    if (flattener.StartVisual(v))
                    {
                        contextStack.Push(new NodeContext(v));
                    }
                }
                else
                {
                    contextStack.Pop();
                    flattener.EndVisual();
                }
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
            //
            // Do nothing here
            //
        }

        /// <summary>
        /// 
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

        #region Internal Methods

        /// <summary>
        /// 
        /// </summary>
        internal
        bool
        SerializeDisguisedVisual(
            object serializedObject
            )
        {
            Visual v = serializedObject as Visual;

            if (v == null)
            {
                throw new ArgumentException(SR.Get(SRID.MustBeOfType, "serializedObject", typeof(Visual)));
            }

            IXpsSerializationManagerAsync manager = (IXpsSerializationManagerAsync)SerializationManager;

            XmlWriter           pageWriter  = ((PackageSerializationManager)manager).
                                              PackagingPolicy.AcquireXmlWriterForPage();

            XmlWriter           resWriter   = ((PackageSerializationManager)manager).
                                              PackagingPolicy.AcquireXmlWriterForResourceDictionary();

            Size fixedPageSize = ((IXpsSerializationManager)SerializationManager).FixedPageSize;
            VisualTreeFlattener flattener = ((IXpsSerializationManager)SerializationManager).
                                              VisualSerializationService.AcquireVisualTreeFlattener(resWriter,
                                                                                                    pageWriter,
                                                                                                    fixedPageSize);

            return flattener.StartVisual(v);
        }

        #endregion Internal Methods
        
    };

    internal class ReachVisualSerializerContext :
                   ReachSerializerContext
    {
        public
        ReachVisualSerializerContext(
            ReachSerializerAsync        serializer,
            Stack<NodeContext>          contextStack,
            VisualTreeFlattener         flattener,
            SerializerAction            action
            ):
            base(serializer,action)
        {
            this._contextStack = contextStack;
            this._flattener    = flattener;
        }


        public
        Stack<NodeContext> 
        ContextStack
        {
            get
            {
                return _contextStack;
            }
        }

        public
        VisualTreeFlattener
        VisualFlattener
        {
            get
            {
                return _flattener;
            }
        }


        private
        Stack<NodeContext>   _contextStack;
        VisualTreeFlattener  _flattener;
    };
}
