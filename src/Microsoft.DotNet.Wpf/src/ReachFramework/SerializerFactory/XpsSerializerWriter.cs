// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: XpsSerializerWriter is a concrete implementation for a plug-in SerializerWriter. It punts everything to XpsDocumentWriter
//
//
//

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
    /// XpsSerializerWriter is a concrete implementation for a plug-in SerializerWriter. It punts everything to XpsDocumentWriter
    /// </summary>
    internal class XpsSerializerWriter : SerializerWriter
    {
        #region Constructors

        private XpsSerializerWriter()
        {
        }

        /// <summary>
        /// creates a XpsSerializerWriter
        /// </summary>
        public XpsSerializerWriter(Stream stream)
            : base()
        {
            _package = Package.Open(stream,FileMode.Create,FileAccess.ReadWrite);

            _xpsDocument = new XpsDocument(_package);
            _xpsDocumentWriter = XpsDocument.CreateXpsDocumentWriter(_xpsDocument);
            _xpsDocumentWriter.WritingPrintTicketRequired += new WritingPrintTicketRequiredEventHandler(xsw_WritingPrintTicketRequired);
            _xpsDocumentWriter.WritingProgressChanged += new WritingProgressChangedEventHandler(xsw_WritingProgressChanged);
            _xpsDocumentWriter.WritingCompleted += new WritingCompletedEventHandler(xsw_WritingCompleted);
            _xpsDocumentWriter.WritingCancelled += new WritingCancelledEventHandler(xsw_WritingCancelled);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Write a single Visual and close stream
        /// </summary>
        public override void Write(Visual visual)
        {
            CheckDisposed();

            _xpsDocumentWriter.Write(visual);

            FinalizeWriter();
        }
        
        /// <summary>
        /// Write a single Visual and close stream
        /// </summary>
        public override void Write(Visual visual, PrintTicket printTicket)
        {
            CheckDisposed();

            _xpsDocumentWriter.Write(visual, printTicket);

            FinalizeWriter();
        }

        /// <summary>
        /// Async Write a single Visual and close stream
        /// </summary>
        public override void WriteAsync(Visual visual)
        {
            CheckDisposed();

            _xpsDocumentWriter.WriteAsync(visual);
        }
        
        /// <summary>
        /// Async Write a single Visual and close stream
        /// </summary>
        public override void WriteAsync(Visual visual, PrintTicket printTicket)
        {
            CheckDisposed();

            _xpsDocumentWriter.WriteAsync(visual, printTicket);
        }
        
        /// <summary>
        /// Async Write a single Visual and close stream
        /// </summary>
        public override void WriteAsync(Visual visual, object userState)
        {
            CheckDisposed();

            _xpsDocumentWriter.WriteAsync(visual, userState);
        }
        
        /// <summary>
        /// Async Write a single Visual and close stream
        /// </summary>
        public override void WriteAsync(Visual visual, PrintTicket printTicket, object userState)
        {
            CheckDisposed();

            _xpsDocumentWriter.WriteAsync(visual, printTicket, userState);
        }

        /// <summary>
        /// Write a single DocumentPaginator and close stream
        /// </summary>
        public override void Write(DocumentPaginator paginator)
        {
            CheckDisposed();

            _xpsDocumentWriter.Write(paginator);

            FinalizeWriter();
        }

        /// <summary>
        /// Write a single DocumentPaginator and close stream
        /// </summary>
        public override void Write(DocumentPaginator paginator, PrintTicket printTicket)
        {
            CheckDisposed();

            _xpsDocumentWriter.Write(paginator, printTicket);

            FinalizeWriter();
        }

        /// <summary>
        /// Async Write a single DocumentPaginator and close stream
        /// </summary>
        public override void WriteAsync(DocumentPaginator paginator)
        {
            CheckDisposed();

            _xpsDocumentWriter.WriteAsync(paginator);
        }

        /// <summary>
        /// Async Write a single DocumentPaginator and close stream
        /// </summary>
        public override void WriteAsync(DocumentPaginator paginator, PrintTicket printTicket)
        {
            CheckDisposed();

            _xpsDocumentWriter.WriteAsync(paginator, printTicket);
        }

        /// <summary>
        /// Async Write a single DocumentPaginator and close stream
        /// </summary>
        public override void WriteAsync(DocumentPaginator paginator, object userState)
        {
            CheckDisposed();

            _xpsDocumentWriter.WriteAsync(paginator, userState);
        }

        /// <summary>
        /// Async Write a single DocumentPaginator and close stream
        /// </summary>
        public override void WriteAsync(DocumentPaginator paginator, PrintTicket printTicket, object userState)
        {
            CheckDisposed();

            _xpsDocumentWriter.WriteAsync(paginator, printTicket, userState);
        }

        /// <summary>
        /// Write a single FixedPage and close stream
        /// </summary>
        public override void Write(FixedPage fixedPage)
        {
            CheckDisposed();

            _xpsDocumentWriter.Write(fixedPage);

            FinalizeWriter();
        }

        /// <summary>
        /// Write a single FixedPage and close stream
        /// </summary>
        public override void Write(FixedPage fixedPage, PrintTicket printTicket)
        {
            CheckDisposed();

            _xpsDocumentWriter.Write(fixedPage, printTicket);

            FinalizeWriter();
        }

        /// <summary>
        /// Async Write a single FixedPage and close stream
        /// </summary>
        public override void WriteAsync(FixedPage fixedPage)
        {
            CheckDisposed();

            _xpsDocumentWriter.WriteAsync(fixedPage);
        }

        /// <summary>
        /// Async Write a single FixedPage and close stream
        /// </summary>
        public override void WriteAsync(FixedPage fixedPage, PrintTicket printTicket)
        {
            CheckDisposed();

            _xpsDocumentWriter.WriteAsync(fixedPage, printTicket);
        }

        /// <summary>
        /// Async Write a single FixedPage and close stream
        /// </summary>
        public override void WriteAsync(FixedPage fixedPage, object Async)
        {
            CheckDisposed();

            _xpsDocumentWriter.WriteAsync(fixedPage, Async);
        }

        /// <summary>
        /// Async Write a single FixedPage and close stream
        /// </summary>
        public override void WriteAsync(FixedPage fixedPage, PrintTicket printTicket, object Async)
        {
            CheckDisposed();

            _xpsDocumentWriter.WriteAsync(fixedPage, printTicket, Async);
        }

        /// <summary>
        /// Write a single FixedDocument and close stream
        /// </summary>
        public override void Write(FixedDocument fixedDocument)
        {
            CheckDisposed();

            _xpsDocumentWriter.Write(fixedDocument);

            FinalizeWriter();
        }

        /// <summary>
        /// Write a single FixedDocument and close stream
        /// </summary>
        public override void Write(FixedDocument fixedDocument, PrintTicket printTicket)
        {
            CheckDisposed();

            _xpsDocumentWriter.Write(fixedDocument, printTicket);

            FinalizeWriter();
        }

        /// <summary>
        /// Async Write a single FixedDocument and close stream
        /// </summary>
        public override void WriteAsync(FixedDocument fixedDocument)
        {
            CheckDisposed();

            _xpsDocumentWriter.WriteAsync(fixedDocument);
        }

        /// <summary>
        /// Async Write a single FixedDocument and close stream
        /// </summary>
        public override void WriteAsync(FixedDocument fixedDocument, PrintTicket printTicket)
        {
            CheckDisposed();

            _xpsDocumentWriter.WriteAsync(fixedDocument, printTicket);
        }

        /// <summary>
        /// Async Write a single FixedDocument and close stream
        /// </summary>
        public override void WriteAsync(FixedDocument fixedDocument, object userState)
        {
            CheckDisposed();

            _xpsDocumentWriter.WriteAsync(fixedDocument, userState);
        }

        /// <summary>
        /// Async Write a single FixedDocument and close stream
        /// </summary>
        public override void WriteAsync(FixedDocument fixedDocument, PrintTicket printTicket, object userState)
        {
            CheckDisposed();

            _xpsDocumentWriter.WriteAsync(fixedDocument, printTicket, userState);
        }

        /// <summary>
        /// Write a single FixedDocumentSequence and close stream
        /// </summary>
        public override void Write(FixedDocumentSequence fixedDocumentSequence)
        {
            CheckDisposed();

            _xpsDocumentWriter.Write(fixedDocumentSequence);

            FinalizeWriter();
        }

        /// <summary>
        /// Write a single FixedDocumentSequence and close stream
        /// </summary>
        public override void Write(FixedDocumentSequence fixedDocumentSequence, PrintTicket printTicket)
        {
            CheckDisposed();

            _xpsDocumentWriter.Write(fixedDocumentSequence, printTicket);

            FinalizeWriter();
        }

        /// <summary>
        /// Async Write a single FixedDocumentSequence and close stream
        /// </summary>
        public override void WriteAsync(FixedDocumentSequence fixedDocumentSequence)
        {
            CheckDisposed();

            _xpsDocumentWriter.WriteAsync(fixedDocumentSequence);
        }

        /// <summary>
        /// Async Write a single FixedDocumentSequence and close stream
        /// </summary>
        public override void WriteAsync(FixedDocumentSequence fixedDocumentSequence, PrintTicket printTicket)
        {
            CheckDisposed();

            _xpsDocumentWriter.WriteAsync(fixedDocumentSequence, printTicket);
        }

        /// <summary>
        /// Async Write a single FixedDocumentSequence and close stream
        /// </summary>
        public override void WriteAsync(FixedDocumentSequence fixedDocumentSequence, object userState)
        {
            CheckDisposed();

            _xpsDocumentWriter.WriteAsync(fixedDocumentSequence, userState);
        }

        /// <summary>
        /// Async Write a single FixedDocumentSequence and close stream
        /// </summary>
        public override void WriteAsync(FixedDocumentSequence fixedDocumentSequence, PrintTicket printTicket, object userState)
        {
            CheckDisposed();

            _xpsDocumentWriter.WriteAsync(fixedDocumentSequence, printTicket, userState);
        }

        /// <summary>
        /// Cancel Asynchronous Write
        /// </summary>
        public override void CancelAsync()
        {
            CheckDisposed();

            _xpsDocumentWriter.CancelAsync();
        }

        /// <summary>
        /// Create a SerializerWriterCollator to gobble up multiple Visuals
        /// </summary>
        public override SerializerWriterCollator CreateVisualsCollator()
        {
            CheckDisposed();

            SerializerWriterCollator collator = _xpsDocumentWriter.CreateVisualsCollator();

            // swc will close these
            _xpsDocument = null;
            _xpsDocumentWriter = null;
            _package = null;

            return collator;
        }

        /// <summary>
        /// Create a SerializerWriterCollator to gobble up multiple Visuals
        /// </summary>
        public override SerializerWriterCollator CreateVisualsCollator(PrintTicket documentSequencePT, PrintTicket documentPT)
        {
            CheckDisposed();

            SerializerWriterCollator collator = _xpsDocumentWriter.CreateVisualsCollator(documentSequencePT, documentPT);
    
            // swc will close these
            _xpsDocument = null;
            _xpsDocumentWriter = null;
            _package = null;

            return collator;
        }

        /// <summary>
        /// This event will be invoked if the writer wants a PrintTicker
        /// </summary>
        public override event WritingPrintTicketRequiredEventHandler WritingPrintTicketRequired;

        /// <summary>
        /// This event will be invoked if the writer progress changes
        /// </summary>
        public override event WritingProgressChangedEventHandler WritingProgressChanged;

        /// <summary>
        /// This event will be invoked if the writer is done
        /// </summary>
        public override event WritingCompletedEventHandler WritingCompleted;

        /// <summary>
        /// This event will be invoked if the writer is done
        /// </summary>
        public override event WritingCancelledEventHandler WritingCancelled;

        #endregion

        #region Private Methods

        private void xsw_WritingPrintTicketRequired(object sender, WritingPrintTicketRequiredEventArgs e)
        {
            if (WritingPrintTicketRequired != null)
            {
                WritingPrintTicketRequired.Invoke(sender, e);
            }
        }

        private void xsw_WritingProgressChanged(object sender, WritingProgressChangedEventArgs e)
        {
            if ( WritingProgressChanged != null)
            {
                WritingProgressChanged.Invoke(sender, e);
            }
        }

        private void xsw_WritingCompleted(object sender, WritingCompletedEventArgs e)
        {
            if ( WritingCompleted != null)
            {
                FinalizeWriter();
                WritingCompleted.Invoke(sender, e);
            }
        }

        private void xsw_WritingCancelled(object sender, WritingCancelledEventArgs e)
        {
            if ( WritingCancelled != null)
            {
                FinalizeWriter();
                WritingCancelled.Invoke(sender, e);
            }
        }

        private void CheckDisposed()
        {
            if (_xpsDocumentWriter == null)
            {
                throw new XpsSerializationException(SR.Get(SRID.XpsSerializerFactory_WriterIsClosed));
            }
        }

        private void FinalizeWriter()
        {
            _xpsDocument.Close();
            _package.Close();
            _xpsDocument = null;
            _xpsDocumentWriter = null;
            _package = null;
        }

        #endregion

        #region Data

        private Package             _package;
        private XpsDocument         _xpsDocument;
        private XpsDocumentWriter   _xpsDocumentWriter;

        #endregion
    }
}
