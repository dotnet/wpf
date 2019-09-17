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
    /// Class defining common functionality required to
    /// serialize a Document Paginator.
    /// </summary>
    internal class XpsOMDocumentPaginatorSerializer :
                   ReachSerializer
    {
        public
        XpsOMDocumentPaginatorSerializer(
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
            SerializableObjectContext serializableObjectContext = SerializeObjectInternal(serializedObject);
            PersistObjectData(serializableObjectContext);
        }

        internal
        SerializableObjectContext
        SerializeObjectInternal(
            Object serializedObject
            )
        {
            //
            // Create the ImageTable required by the Type Converters
            // The Image table at this time is shared / document
            //
            _xpsOMSerializationManager.ResourcePolicy.ImageCrcTable = new Dictionary<UInt32, Uri>();

            _xpsOMSerializationManager.ResourcePolicy.ImageUriHashTable = new Dictionary<int, Uri>();
            //
            // Create the ColorContextTable required by the Type Converters
            // The ColorContext table at this time is shared / document
            //
            _xpsOMSerializationManager.ResourcePolicy.ColorContextTable = new Dictionary<int, Uri>();
            SerializableObjectContext serializableObjectContext = new SerializableObjectContext(serializedObject, null);
            return serializableObjectContext;
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
            DocumentPaginator paginator = BeginPersistObjectData(serializableObjectContext);

            for (int i = 0; !paginator.IsPageCountValid || (i < paginator.PageCount); i++)
            {
                DocumentPage page = Toolbox.GetPage(paginator, i);

                ReachSerializer serializer = SerializationManager.GetSerializer(page);

                if (serializer != null)
                {
                    serializer.SerializeObject(page);
                }
            }

            EndPersistObjectData();
        }

        internal
        DocumentPaginator
        BeginPersistObjectData(
            SerializableObjectContext serializableObjectContext
            )
        {
            if (_xpsOMSerializationManager != null)
            {
                _xpsOMSerializationManager.RegisterDocumentStart();
                XpsSerializationPrintTicketRequiredEventArgs e =
                new XpsSerializationPrintTicketRequiredEventArgs(PrintTicketLevel.FixedDocumentPrintTicket,
                                                                 0);

                _xpsOMSerializationManager.OnXPSSerializationPrintTicketRequired(e);

                //
                // Serialize the data for the PrintTicket
                //
                if (e.Modified)
                {
                    if (e.PrintTicket != null)
                    {
                        PrintTicketSerializer serializer = new PrintTicketSerializer(SerializationManager);
                        serializer.SerializeObject(e.PrintTicket);
                    }
                }

                _xpsOMSerializationManager.StartNewDocument();
            }

            DocumentPaginator paginator = (DocumentPaginator)serializableObjectContext.TargetObject;

            XmlLanguage language = null;

            DependencyObject dependencyObject = paginator.Source as DependencyObject;
            if (dependencyObject != null)
            {
                language = (XmlLanguage)dependencyObject.GetValue(FrameworkContentElement.LanguageProperty);
            }

            if (language == null)
            {
                //If the language property is null, assign the language to the default
                language = XmlLanguage.GetLanguage(XpsS0Markup.XmlLangValue);
            }

            SerializationManager.Language = language;

            return paginator;
        }

        internal
        void
        EndPersistObjectData(
            )
        {
            _xpsOMSerializationManager.ReleaseXpsOMWriterForFixedDocument();

            //
            // Clear off the table from the resource policy
            //
            _xpsOMSerializationManager.ResourcePolicy.ImageCrcTable = null;

            _xpsOMSerializationManager.ResourcePolicy.ImageUriHashTable = null;
            //
            // Clear off the table from the resource policy
            //
            _xpsOMSerializationManager.ResourcePolicy.ColorContextTable = null;
            //
            // Signal to any registered callers that the Document has been serialized
            //
            XpsSerializationProgressChangedEventArgs progressEvent =
            new XpsSerializationProgressChangedEventArgs(XpsWritingProgressChangeLevel.FixedDocumentWritingProgress,
                                                         0,
                                                         0,
                                                         null);

            _xpsOMSerializationManager.RegisterDocumentEnd();

            _xpsOMSerializationManager.OnXPSSerializationProgressChanged(progressEvent);
        }

        private XpsOMSerializationManager _xpsOMSerializationManager;
    };
}


