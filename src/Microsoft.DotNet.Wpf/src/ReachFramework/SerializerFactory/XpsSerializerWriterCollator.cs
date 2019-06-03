// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//---------------------------------------------------------------------------
//
//
//
//
// Description: XpsSerializerWriter is a concrete implementation for a plug-in SerializerWriter. It punts everything to XpsDocumentWriter
//
//              See spec at <Need to post existing spec>
//
//
//
//
//---------------------------------------------------------------------------
namespace System.Windows.Xps.Serialization
{
    using System;
    using System.IO;
    using System.IO.Packaging;
    using System.Printing;
    using System.Windows.Xps;
    using System.Windows.Documents;
    using System.Windows.Documents.Serialization;
    using System.Windows.Media;
    using System.Windows.Xps.Packaging;

    /// <summary>
    /// XpsSerializerWriterCollator is a concrete implementation for a plug-in SerializerWriterCollator. It punts everything to XpsDocumentWriter
    /// </summary>
    internal class XpsSerializerWriterCollator : SerializerWriterCollator
    {
        #region Constructors

        private XpsSerializerWriterCollator()
        {
        }

        /// <summary>
        /// creates a XpsSerializerWriter
        /// </summary>
        public XpsSerializerWriterCollator(Package package,XpsDocument xpsDocument,VisualsToXpsDocument collator)
            : base()
        {
            _package = package;
            _xpsDocument = xpsDocument;
            _collator = collator;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Write a single Visual and close stream
        /// </summary>
        public override void Write(Visual visual, PrintTicket printTicket)
        {
            CheckDisposed();

            _collator.Write(visual, printTicket);
        }

        /// <summary>
        /// Asynchronous Write a single Visual and close stream
        /// </summary>
        public override void WriteAsync(Visual visual, PrintTicket printTicket, object userState)
        {
            CheckDisposed();

            _collator.WriteAsync(visual, printTicket, userState);
        }

        /// <summary>
        /// Close this collator
        /// </summary>
        public override void Close()
        {
            CheckDisposed();

            _collator.EndBatchWrite();
            _xpsDocument.Close();
            _package.Close();
            _collator = null;
            _xpsDocument = null;
            _package = null;
        }

        /// <summary>
        /// Cancel Asynchronous Write
        /// </summary>
        public override void CancelAsync()
        {
            CheckDisposed();

            _collator.CancelAsync();
        }

        /// <summary>
        /// Cancel Write
        /// </summary>
        public override void Cancel()
        {
            CheckDisposed();

            _collator.Cancel();
        }

        #endregion

        #region Private Methods

        private void CheckDisposed()
        {
            if (_collator == null)
            {
                throw new XpsSerializationException(SR.Get(SRID.XpsSerializerFactory_WriterIsClosed));
            }
        }

        #endregion

        #region Data

        private VisualsToXpsDocument        _collator;
        private Package                     _package;
        private XpsDocument                 _xpsDocument;

        #endregion
    }
}
