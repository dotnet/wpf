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
    /// Class defining common functionality required to
    /// serialize a Document Sequence.
    /// </summary>
    internal class XpsOMDocumentSequenceSerializer :
                   ReachSerializer
    {
        public
        XpsOMDocumentSequenceSerializer(
            PackageSerializationManager manager
            ) :
            base(manager)
        {
            ///
            /// Fail if manager is not XpsOMSerializationManager
            /// 
            _xpsOMSerializationManager = (XpsOMSerializationManager)manager;
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

            EndPersistObjectData();
        }

        internal
        void
        BeginPersistObjectData(
            SerializableObjectContext serializableObjectContext
            )
        {
            _xpsOMSerializationManager.RegisterDocumentSequenceStart();
            _xpsOMSerializationManager.EnsureXpsOMPackageWriter();
        }

        internal
        void
        EndPersistObjectData(
            )
        {
            _xpsOMSerializationManager.ReleaseXpsOMWriterForFixedDocumentSequence();

            //
            // Signal to any registered callers that the Sequence has been serialized
            //
            XpsSerializationProgressChangedEventArgs progressEvent =
            new XpsSerializationProgressChangedEventArgs(XpsWritingProgressChangeLevel.FixedDocumentSequenceWritingProgress,
                                                         0,
                                                         0,
                                                         null);

            
            _xpsOMSerializationManager.RegisterDocumentSequenceEnd();
            _xpsOMSerializationManager.OnXPSSerializationProgressChanged(progressEvent);
        }

        private XpsOMSerializationManager _xpsOMSerializationManager;
    };
}


