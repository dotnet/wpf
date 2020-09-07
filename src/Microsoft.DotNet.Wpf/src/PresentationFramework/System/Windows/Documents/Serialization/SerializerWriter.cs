// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !DONOTREFPRINTINGASMMETA
//
// Description: Plug-in document serializers implement this abstract class
//
//              See spec at <Need to post existing spec>
//
namespace System.Windows.Documents.Serialization
{
    using System;
    using System.Printing;
    using System.Windows.Documents;
    using System.Windows.Media;
    using System.Security;

    /// <summary>
    /// SerializerWriter is an abstract class that is implemented by plug-in document serializers
    /// Objects of this class are instantiated by SerializerProvider.Create
    /// </summary>
    public abstract class SerializerWriter
    {
        /// <summary>
        /// Write a single Visual and close package
        /// </summary>
        public abstract void Write(Visual visual);

       /// <summary>
        /// Write a single Visual and close package
        /// </summary>
        public abstract void Write(Visual visual, PrintTicket printTicket);

        /// <summary>
        /// Asynchronous Write a single Visual and close package
        /// </summary>
        public abstract void WriteAsync(Visual visual);

        /// <summary>
        /// Asynchronous Write a single Visual and close package
        /// </summary>
        public abstract void WriteAsync(Visual visual, object userState);

 
        /// <summary>
        /// Asynchronous Write a single Visual and close package
        /// </summary>
        public abstract void WriteAsync(Visual visual, PrintTicket printTicket);


       /// <summary>
        /// Asynchronous Write a single Visual and close package
        /// </summary>
        public abstract void WriteAsync(Visual visual, PrintTicket printTicket, object userState);

        /// <summary>
        /// Write a single DocumentPaginator and close package
        /// </summary>
        public abstract void Write(DocumentPaginator documentPaginator);

       /// <summary>
        /// Write a single DocumentPaginator and close package
        /// </summary>
        public abstract void Write(DocumentPaginator documentPaginator, PrintTicket printTicket);

        /// <summary>
        /// Asynchronous Write a single DocumentPaginator and close package
        /// </summary>
        public abstract void WriteAsync(DocumentPaginator documentPaginator);

        /// <summary>
        /// Asynchronous Write a single DocumentPaginator and close package
        /// </summary>
        public abstract void WriteAsync(DocumentPaginator documentPaginator, PrintTicket printTicket);

        /// <summary>
        /// Asynchronous Write a single DocumentPaginator and close package
        /// </summary>
        public abstract void WriteAsync(DocumentPaginator documentPaginator, object userState);

        /// <summary>
        /// Asynchronous Write a single DocumentPaginator and close package
        /// </summary>
        public abstract void WriteAsync(DocumentPaginator documentPaginator, PrintTicket printTicket, object userState);

        /// <summary>
        /// Write a single FixedPage and close package
        /// </summary>
        public abstract void Write(FixedPage fixedPage);

        /// <summary>
        /// Write a single FixedPage and close package
        /// </summary>
        public abstract void Write(FixedPage fixedPage, PrintTicket printTicket);

        /// <summary>
        /// Asynchronous Write a single FixedPage and close package
        /// </summary>
        public abstract void WriteAsync(FixedPage fixedPage);

        /// <summary>
        /// Asynchronous Write a single FixedPage and close package
        /// </summary>
        public abstract void WriteAsync(FixedPage fixedPage, PrintTicket printTicket);

        /// <summary>
        /// Asynchronous Write a single FixedPage and close package
        /// </summary>
        public abstract void WriteAsync(FixedPage fixedPage, object userState);

       /// <summary>
        /// Asynchronous Write a single FixedPage and close package
        /// </summary>
        public abstract void WriteAsync(FixedPage fixedPage, PrintTicket printTicket, object userState);


        /// <summary>
        /// Write a single FixedDocument and close package
        /// </summary>
        public abstract void Write(FixedDocument fixedDocument);

        /// <summary>
        /// Write a single FixedDocument and close package
        /// </summary>
        public abstract void Write(FixedDocument fixedDocument, PrintTicket printTicket);

        /// <summary>
        /// Asynchronous Write a single FixedDocument and close package
        /// </summary>
        public abstract void WriteAsync(FixedDocument fixedDocument);

        /// <summary>
        /// Asynchronous Write a single FixedDocument and close package
        /// </summary>
        public abstract void WriteAsync(FixedDocument fixedDocument, PrintTicket printTicket);

        /// <summary>
        /// Asynchronous Write a single FixedDocument and close package
        /// </summary>
        public abstract void WriteAsync(FixedDocument fixedDocument, object userState);

        /// <summary>
        /// Asynchronous Write a single FixedDocument and close package
        /// </summary>
        public abstract void WriteAsync(FixedDocument fixedDocument, PrintTicket printTicket, object userState);

        /// <summary>
        /// Write a single FixedDocumentSequence and close package
        /// </summary>
        public abstract void Write(FixedDocumentSequence fixedDocumentSequence);

        /// <summary>
        /// Write a single FixedDocumentSequence and close package
        /// </summary>
        public abstract void Write(FixedDocumentSequence fixedDocumentSequence, PrintTicket printTicket);

        /// <summary>
        /// Asynchronous Write a single FixedDocumentSequence and close package
        /// </summary>
        public abstract void WriteAsync(FixedDocumentSequence fixedDocumentSequence);

        /// <summary>
        /// Asynchronous Write a single FixedDocumentSequence and close package
        /// </summary>
        public abstract void WriteAsync(FixedDocumentSequence fixedDocumentSequence, PrintTicket printTicket);

        /// <summary>
        /// Asynchronous Write a single FixedDocumentSequence and close package
        /// </summary>
        public abstract void WriteAsync(FixedDocumentSequence fixedDocumentSequence, object userState);

        /// <summary>
        /// Asynchronous Write a single FixedDocumentSequence and close package
        /// </summary>
        public abstract void WriteAsync(FixedDocumentSequence fixedDocumentSequence, PrintTicket printTicket, object userState);

        /// <summary>
        /// Cancel Asynchronous Write
        /// </summary>
        public abstract void CancelAsync();

        /// <summary>
        /// Create a SerializerWriterCollator to gobble up multiple Visuals
        /// </summary>
        public abstract SerializerWriterCollator CreateVisualsCollator();

        /// <summary>
        /// Create a SerializerWriterCollator to gobble up multiple Visuals
        /// </summary>
        public abstract SerializerWriterCollator CreateVisualsCollator(PrintTicket documentSequencePT, PrintTicket documentPT);

        /// <summary>
        /// This event will be invoked if the writer wants a PrintTicker
        /// </summary>
        public abstract event WritingPrintTicketRequiredEventHandler WritingPrintTicketRequired;

        /// <summary>
        /// This event will be invoked if the writer progress changes
        /// </summary>
        public abstract event WritingProgressChangedEventHandler WritingProgressChanged;

        /// <summary>
        /// This event will be invoked if the writer is done
        /// </summary>
        public abstract event WritingCompletedEventHandler WritingCompleted;
        
        /// <summary>
        /// This event will be invoked if the writer has been cancelled
        /// </summary>
        public abstract event WritingCancelledEventHandler           WritingCancelled;
    }
}
#endif
