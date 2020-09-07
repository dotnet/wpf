// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Windows.Documents.Serialization
{
    public abstract partial class SerializerWriter
    {
        protected SerializerWriter() { }
        public abstract event System.Windows.Documents.Serialization.WritingCancelledEventHandler WritingCancelled;
        public abstract event System.Windows.Documents.Serialization.WritingCompletedEventHandler WritingCompleted;
        public abstract event System.Windows.Documents.Serialization.WritingPrintTicketRequiredEventHandler WritingPrintTicketRequired;
        public abstract event System.Windows.Documents.Serialization.WritingProgressChangedEventHandler WritingProgressChanged;
        public abstract void CancelAsync();
        public abstract System.Windows.Documents.Serialization.SerializerWriterCollator CreateVisualsCollator();
        public abstract System.Windows.Documents.Serialization.SerializerWriterCollator CreateVisualsCollator(System.Printing.PrintTicket documentSequencePT, System.Printing.PrintTicket documentPT);
        public abstract void Write(System.Windows.Documents.DocumentPaginator documentPaginator);
        public abstract void Write(System.Windows.Documents.DocumentPaginator documentPaginator, System.Printing.PrintTicket printTicket);
        public abstract void Write(System.Windows.Documents.FixedDocument fixedDocument);
        public abstract void Write(System.Windows.Documents.FixedDocument fixedDocument, System.Printing.PrintTicket printTicket);
        public abstract void Write(System.Windows.Documents.FixedDocumentSequence fixedDocumentSequence);
        public abstract void Write(System.Windows.Documents.FixedDocumentSequence fixedDocumentSequence, System.Printing.PrintTicket printTicket);
        public abstract void Write(System.Windows.Documents.FixedPage fixedPage);
        public abstract void Write(System.Windows.Documents.FixedPage fixedPage, System.Printing.PrintTicket printTicket);
        public abstract void Write(System.Windows.Media.Visual visual);
        public abstract void Write(System.Windows.Media.Visual visual, System.Printing.PrintTicket printTicket);
        public abstract void WriteAsync(System.Windows.Documents.DocumentPaginator documentPaginator);
        public abstract void WriteAsync(System.Windows.Documents.DocumentPaginator documentPaginator, object userState);
        public abstract void WriteAsync(System.Windows.Documents.DocumentPaginator documentPaginator, System.Printing.PrintTicket printTicket);
        public abstract void WriteAsync(System.Windows.Documents.DocumentPaginator documentPaginator, System.Printing.PrintTicket printTicket, object userState);
        public abstract void WriteAsync(System.Windows.Documents.FixedDocument fixedDocument);
        public abstract void WriteAsync(System.Windows.Documents.FixedDocument fixedDocument, object userState);
        public abstract void WriteAsync(System.Windows.Documents.FixedDocument fixedDocument, System.Printing.PrintTicket printTicket);
        public abstract void WriteAsync(System.Windows.Documents.FixedDocument fixedDocument, System.Printing.PrintTicket printTicket, object userState);
        public abstract void WriteAsync(System.Windows.Documents.FixedDocumentSequence fixedDocumentSequence);
        public abstract void WriteAsync(System.Windows.Documents.FixedDocumentSequence fixedDocumentSequence, object userState);
        public abstract void WriteAsync(System.Windows.Documents.FixedDocumentSequence fixedDocumentSequence, System.Printing.PrintTicket printTicket);
        public abstract void WriteAsync(System.Windows.Documents.FixedDocumentSequence fixedDocumentSequence, System.Printing.PrintTicket printTicket, object userState);
        public abstract void WriteAsync(System.Windows.Documents.FixedPage fixedPage);
        public abstract void WriteAsync(System.Windows.Documents.FixedPage fixedPage, object userState);
        public abstract void WriteAsync(System.Windows.Documents.FixedPage fixedPage, System.Printing.PrintTicket printTicket);
        public abstract void WriteAsync(System.Windows.Documents.FixedPage fixedPage, System.Printing.PrintTicket printTicket, object userState);
        public abstract void WriteAsync(System.Windows.Media.Visual visual);
        public abstract void WriteAsync(System.Windows.Media.Visual visual, object userState);
        public abstract void WriteAsync(System.Windows.Media.Visual visual, System.Printing.PrintTicket printTicket);
        public abstract void WriteAsync(System.Windows.Media.Visual visual, System.Printing.PrintTicket printTicket, object userState);
    }
}