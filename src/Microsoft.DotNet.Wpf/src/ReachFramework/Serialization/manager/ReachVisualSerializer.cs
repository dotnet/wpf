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
using System.Windows.Media.Media3D;
using System.Windows.Markup;
using MS.Utility;

namespace System.Windows.Xps.Serialization
{
    /// <summary>
    ///
    /// </summary>
    internal class ReachVisualSerializer :
                   ReachSerializer
    {
        /// <summary>
        ///
        /// </summary>
        public
        ReachVisualSerializer(
            PackageSerializationManager manager
            ):
        base(manager)
        {
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

            //  The new class XpsOMSerializationManager now also interacts with this class
            // the cast below is shorthand for cast to either XpsSerializationManager or XpsOMSerializationManager
            // we want this to throw an InvalidCastException if it fails to mantain compatibility.
            if((IXpsSerializationManager)SerializationManager != null)
            {
                XmlWriter pageWriter  = SerializationManager.
                                        PackagingPolicy.AcquireXmlWriterForPage();

                XmlWriter resWriter   = SerializationManager.
                                        PackagingPolicy.AcquireXmlWriterForResourceDictionary();

                SerializeTree(v, resWriter, pageWriter);
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

        private
        void
        SerializeTree(
            Visual visual,
            XmlWriter resWriter,
            XmlWriter bodyWriter
            )
        {
            Toolbox.EmitEvent(EventTrace.Event.WClientDRXSerializeTreeStart);


            Size fixedPageSize = ((IXpsSerializationManager)SerializationManager).FixedPageSize;

            VisualTreeFlattener flattener  = ((IXpsSerializationManager)SerializationManager).
                                              VisualSerializationService.AcquireVisualTreeFlattener(resWriter,
                                                                                                    bodyWriter,
                                                                                                    fixedPageSize);

            Stack<NodeContext> contextStack = new Stack<NodeContext>();
            if (flattener.StartVisual(visual))
            {
                contextStack.Push(new NodeContext(visual));
            }

            while (contextStack.Count > 0)
            {
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

            Toolbox.EmitEvent(EventTrace.Event.WClientDRXSerializeTreeEnd);
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

            //  The new class XpsOMSerializationManager now also interacts with this class
            // the cast below is shorthand for cast to either XpsSerializationManager or XpsOMSerializationManager
            // we want this to throw an InvalidCastException if it fails to mantain compatibility.
            if ((IXpsSerializationManager)SerializationManager != null)
            {
                XmlWriter           pageWriter  = SerializationManager.
                                                  PackagingPolicy.AcquireXmlWriterForPage();

                XmlWriter           resWriter   = SerializationManager.
                                                  PackagingPolicy.AcquireXmlWriterForResourceDictionary();

                Size fixedPageSize = ((IXpsSerializationManager)SerializationManager).FixedPageSize;

                VisualTreeFlattener flattener  = ((IXpsSerializationManager)SerializationManager).
                                                  VisualSerializationService.AcquireVisualTreeFlattener(resWriter,
                                                                                                        pageWriter,
                                                                                                        fixedPageSize);

                return flattener.StartVisual(v);
            }

            return false;
        }

        #endregion Internal Methods

    };

    class NodeContext
    {
        #region Constructor

        public NodeContext(Visual v)
        {
            nodeVisual = v;
            index = 0;
        }

        #endregion Constructor

        #region Public properties

        public Visual NodeVisual
        {
            get
            {
                return nodeVisual;
            }
        }

        #endregion Public properties

        #region Public methods

        public Visual GetNextChild()
        {
            Visual child = null;

            if (index < VisualTreeHelper.GetChildrenCount(nodeVisual))
            {
                // VisualTreeFlattener will flatten Viewport3DVisual into an image.  We shouldn't
                // be attempting to walk Visual3D children.
                child = (Visual) VisualTreeHelper.GetChild(nodeVisual, index);
                index++;
            }

            return child;
        }

        #endregion Public methods

        #region Private data

        private Visual nodeVisual;
        private int index;

        #endregion Private data
    }
}

