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

namespace System.Windows.Xps.Serialization
{
    /// <summary>
    /// Class defining common functionality required to
    /// serialize a Document Page.
    /// </summary>
    internal class XpsOMDocumentPageSerializer :
                   ReachSerializer
    {
        public
        XpsOMDocumentPageSerializer(
            PackageSerializationManager manager
            )
            :
        base(manager)
        {
            ///
            /// Fail if manager is not XpsOMSerializationManager
            /// 
            _xpsOMSerializationManager = (XpsOMSerializationManager)manager;
        }


        public
        override
        void
        SerializeObject(
            Object serializedObject
            )
        {
            Initialize();
            base.SerializeObject(serializedObject);
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
            Initialize();
            base.SerializeObject(serializedProperty);
        }

        internal
        void
        Initialize(
            )
        {
            //
            // Create the ImageTable required by the Type Converters
            // The Image table at this time is shared / currentPage
            //
            _xpsOMSerializationManager.ResourcePolicy.CurrentPageImageTable = new Dictionary<int, Uri>();
            //
            // Create the ColorContextTable required by the Type Converters
            // The Image table at this time is shared / currentPage
            //
            _xpsOMSerializationManager.ResourcePolicy.CurrentPageColorContextTable = new Dictionary<int, Uri>();
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

            DocumentPage dp = (DocumentPage)serializableObjectContext.TargetObject;
            ReachTreeWalker treeWalker = BeginSerializeDocumentPage(serializableObjectContext);


            SerializeChild(dp.Visual, serializableObjectContext);

            EndSerializeDocumentPage(treeWalker);
        }

        internal
        ReachTreeWalker
        BeginSerializeDocumentPage(
            SerializableObjectContext serializableObjectContext
            )
        {
            PrintTicket printTicket = null;

            _xpsOMSerializationManager.RegisterPageStart();

            XpsSerializationPrintTicketRequiredEventArgs e =
                new XpsSerializationPrintTicketRequiredEventArgs(PrintTicketLevel.FixedPagePrintTicket,
                                                            0);

            _xpsOMSerializationManager.OnXPSSerializationPrintTicketRequired(e);

            if (e.Modified)
            {
                printTicket = e.PrintTicket;
            }

            //
            // Serialize the data for the PrintTicket
            //
            if (printTicket != null)
            {
                PrintTicketSerializer serializer = new PrintTicketSerializer(SerializationManager);
                serializer.SerializeObject(printTicket);
            }

            //
            // A DocumentPage is persisted as a FixedPage
            //
            DocumentPage dp = (DocumentPage)serializableObjectContext.TargetObject;

            ReachTreeWalker treeWalker = new ReachTreeWalker(this);
            treeWalker.SerializeLinksInDocumentPage(dp);

            XmlWriter.WriteStartElement(XpsS0Markup.FixedPage);

            String xmlnsForType = SerializationManager.GetXmlNSForType(typeof(FixedPage));
            if (xmlnsForType != null)
            {
                XmlWriter.WriteAttributeString(XpsS0Markup.Xmlns, xmlnsForType);
                XmlWriter.WriteAttributeString(XpsS0Markup.XmlnsX, XpsS0Markup.XmlnsXSchema);

                if (SerializationManager.Language != null)
                {
                    XmlWriter.WriteAttributeString(XpsS0Markup.XmlLang, SerializationManager.Language.ToString());
                }
                else
                {
                    XmlWriter.WriteAttributeString(XpsS0Markup.XmlLang, XpsS0Markup.XmlLangValue);
                }
            }



            Size size = Toolbox.ValidateDocumentSize(dp.Size, printTicket);

            _xpsOMSerializationManager.FixedPageSize = size;

            //
            //write length and width elements
            //
            WriteAttribute(XmlWriter, XpsS0Markup.PageWidth, size.Width);
            WriteAttribute(XmlWriter, XpsS0Markup.PageHeight, size.Height);

            return treeWalker;
        }

        internal
        void
        EndSerializeDocumentPage(
            ReachTreeWalker treeWalker
            )
        {
            _xpsOMSerializationManager.PackagingPolicy.PreCommitCurrentPage();

            //copy hyperlinks into stream
            treeWalker.CommitHyperlinks();

            XmlWriter.WriteEndElement();
            XmlWriter = null;
            //
            // Free the image table in use for this page
            //
            _xpsOMSerializationManager.ResourcePolicy.CurrentPageImageTable = null;
            //
            // Free the colorContext table in use for this page
            //
            _xpsOMSerializationManager.ResourcePolicy.CurrentPageColorContextTable = null;

            IXpsSerializationManager xpsSerializationManager = ((IXpsSerializationManager)SerializationManager);

            xpsSerializationManager.VisualSerializationService.ReleaseVisualTreeFlattener();

            xpsSerializationManager.RegisterPageEnd();

            //
            // Signal to any registered callers that the Page has been serialized
            //
            XpsSerializationProgressChangedEventArgs progressEvent =
            new XpsSerializationProgressChangedEventArgs(XpsWritingProgressChangeLevel.FixedPageWritingProgress,
                                                         0,
                                                         0,
                                                         null);

            xpsSerializationManager.OnXPSSerializationProgressChanged(progressEvent);
        }

        internal 
        void 
        SerializeChild(
            Visual child,
            SerializableObjectContext parentContext
            )
        {
            ReachSerializer serializer = SerializationManager.GetSerializer(child);

            if (serializer != null)
            {
                serializer.SerializeObject(child);
            }
        }

        private 
        void 
        WriteAttribute(
            XmlWriter writer,
            string name,
            object value
            )
        {
            writer.WriteAttributeString(name, TypeDescriptor.GetConverter(value).ConvertToInvariantString(value));
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

        private XpsOMSerializationManager _xpsOMSerializationManager;
    };
}
