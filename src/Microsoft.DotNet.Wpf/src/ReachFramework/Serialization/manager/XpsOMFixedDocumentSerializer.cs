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
    /// serialize a Fixed Document.
    /// </summary>
    internal class XpsOMFixedDocumentSerializer :
                   ReachSerializer
    {
        #region Constructor

        public
        XpsOMFixedDocumentSerializer(
            PackageSerializationManager manager
            ) :
            base(manager)
        {
            ///
            /// Fail if manager is not XpsOMSerializationManager
            /// 
            _xpsOMSerializationManager = (XpsOMSerializationManager)manager;
        }

        #endregion Constructor

        #region Public Methods

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

        #endregion Public Methods

        #region Internal Methods

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
            // The Image table at this time is shared / document
            //
            _xpsOMSerializationManager.ResourcePolicy.ImageCrcTable = new Dictionary<UInt32, Uri>();

            _xpsOMSerializationManager.ResourcePolicy.ImageUriHashTable = new Dictionary<int, Uri>();
            //
            // Create the ColorContextTable required by the Type Converters
            // The Image table at this time is shared / document
            //
            _xpsOMSerializationManager.ResourcePolicy.ColorContextTable = new Dictionary<int, Uri>();
        }

        internal
        override
        void
        PersistObjectData(
            SerializableObjectContext serializableObjectContext
            )
        {
            BeginPersistObjectData(serializableObjectContext);

            if (serializableObjectContext.IsComplexValue)
            {
                SerializeObjectCore(serializableObjectContext);
            }
            else
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_WrongPropertyTypeForFixedDocument));
            }

            EndPersistObjectData();
        }

        internal
        void
        BeginPersistObjectData(
            SerializableObjectContext serializableObjectContext
        )
        {
            if (serializableObjectContext == null)
            {
                throw new ArgumentNullException(nameof(serializableObjectContext));
            }

            _xpsOMSerializationManager.RegisterDocumentStart();

            if (serializableObjectContext.IsComplexValue)
            {
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
            }

            _xpsOMSerializationManager.StartNewDocument();
        }

        internal
        void
        EndPersistObjectData(
        )
        {
            //
            // Clear off the table from the packaging policy
            //
            _xpsOMSerializationManager.ResourcePolicy.ImageCrcTable = null;

            _xpsOMSerializationManager.ResourcePolicy.ImageUriHashTable = null;

            _xpsOMSerializationManager.ReleaseXpsOMWriterForFixedDocument();

            //
            // Signal to any registered callers that the Document has been serialized
            //
            XpsSerializationProgressChangedEventArgs progressEvent =
            new XpsSerializationProgressChangedEventArgs(XpsWritingProgressChangeLevel.FixedDocumentWritingProgress,
                                                         0,
                                                         0,
                                                         null);

            _xpsOMSerializationManager.OnXPSSerializationProgressChanged(progressEvent);

            _xpsOMSerializationManager.RegisterDocumentEnd();
        }

        #endregion Internal Methods

        private XpsOMSerializationManager _xpsOMSerializationManager;
    };
}


