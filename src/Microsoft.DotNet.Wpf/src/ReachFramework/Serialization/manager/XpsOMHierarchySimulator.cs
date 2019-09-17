// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Printing;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Xps.Packaging;
using System.Xml;

namespace System.Windows.Xps.Serialization
{
    internal class XpsOMHierarchySimulator : ReachHierarchySimulator
    {

        #region Constructor

        public
        XpsOMHierarchySimulator(
            XpsOMSerializationManager manager,
            Object serializedObject
            ) : base (manager, serializedObject)
        {
            _xpsOMSerializationManager = manager;
        }

        #endregion Constructor

        #region Internal Override Methods

        internal
        override
        XmlWriter
        SimulateBeginFixedDocumentSequence(
            )
        {
            _xpsOMSerializationManager.RegisterDocumentSequenceStart();
            _xpsOMSerializationManager.EnsureXpsOMPackageWriter();

            return null;

        }

        internal
        override
        void
        SimulateEndFixedDocumentSequence(
            XmlWriter xmlWriter
            )
        {
            //
            // Release the Package writer
            //
            _xpsOMSerializationManager.ReleaseXpsOMWriterForFixedDocumentSequence();

            _xpsOMSerializationManager.RegisterDocumentSequenceEnd();

            //
            // Inform any registered listener that the document sequence has been serialized
            //
            XpsSerializationProgressChangedEventArgs progressEvent =
            new XpsSerializationProgressChangedEventArgs(XpsWritingProgressChangeLevel.FixedDocumentSequenceWritingProgress,
                                                            0,
                                                            0,
                                                            null);
            (_serializationManager as IXpsSerializationManager)?.OnXPSSerializationProgressChanged(progressEvent);
        }


        internal
        override
        XmlWriter
        SimulateBeginFixedDocument(
            )
        {
            _xpsOMSerializationManager.RegisterDocumentStart();

            // Build the Image Table

            _xpsOMSerializationManager.ResourcePolicy.ImageCrcTable = new Dictionary<UInt32, Uri>();

            _xpsOMSerializationManager.ResourcePolicy.ImageUriHashTable = new Dictionary<int, Uri>();

            //
            // Build the ColorContext Table
            //
            _xpsOMSerializationManager.ResourcePolicy.ColorContextTable = new Dictionary<int, Uri>();


            XpsSerializationPrintTicketRequiredEventArgs e =
            new XpsSerializationPrintTicketRequiredEventArgs(PrintTicketLevel.FixedDocumentPrintTicket,
                                                                0);

            SimulatePrintTicketRaisingEvent(e);

            _xpsOMSerializationManager.StartNewDocument();


            return null;
        }

        internal
        override
        void
        SimulateEndFixedDocument(
            XmlWriter xmlWriter
            )
        {
            _xpsOMSerializationManager.ReleaseXpsOMWriterForFixedDocument();

            //
            // Clear off the table from the packaging policy
            //
            _xpsOMSerializationManager.ResourcePolicy.ImageCrcTable = null;

            _xpsOMSerializationManager.ResourcePolicy.ImageUriHashTable = null;
            //
            // Clear off the table from the packaging policy
            //
            _xpsOMSerializationManager.ResourcePolicy.ColorContextTable = null;
        
            _xpsOMSerializationManager.RegisterDocumentEnd();

            // Inform any registered listener that the document has been serialized
            //
            XpsSerializationProgressChangedEventArgs progressEvent =
            new XpsSerializationProgressChangedEventArgs(XpsWritingProgressChangeLevel.FixedDocumentWritingProgress,
                                                            0,
                                                            0,
                                                       null);
                     
            (_serializationManager as IXpsSerializationManager)?.OnXPSSerializationProgressChanged(progressEvent);
        }

        #endregion Internal Methods
        
        #region Private Data

        private XpsOMSerializationManager _xpsOMSerializationManager;

        #endregion Private Data
    };
}
