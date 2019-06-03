// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Windows.Documents.Serialization
{
    public delegate void WritingCancelledEventHandler(object sender, System.Windows.Documents.Serialization.WritingCancelledEventArgs e);
    public delegate void WritingCompletedEventHandler(object sender, System.Windows.Documents.Serialization.WritingCompletedEventArgs e);
    public delegate void WritingProgressChangedEventHandler(object sender, System.Windows.Documents.Serialization.WritingProgressChangedEventArgs e);
    public delegate void WritingPrintTicketRequiredEventHandler(object sender, System.Windows.Documents.Serialization.WritingPrintTicketRequiredEventArgs e);

    public partial class WritingCancelledEventArgs : System.EventArgs
    {
        public WritingCancelledEventArgs(System.Exception exception) { }
        public System.Exception Error { get { throw null; } }
    }

    public partial class WritingCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs
    {
        public WritingCompletedEventArgs(bool cancelled, object state, System.Exception exception) : base (default(System.Exception), default(bool), default(object)) { }
    }

    public partial class WritingProgressChangedEventArgs : System.ComponentModel.ProgressChangedEventArgs
    {
        public WritingProgressChangedEventArgs(System.Windows.Documents.Serialization.WritingProgressChangeLevel writingLevel, int number, int progressPercentage, object state) : base (default(int), default(object)) { }
        public int Number { get { throw null; } }
        public System.Windows.Documents.Serialization.WritingProgressChangeLevel WritingLevel { get { throw null; } }
    }

    public enum WritingProgressChangeLevel
    {
        FixedDocumentSequenceWritingProgress = 1,
        FixedDocumentWritingProgress = 2,
        FixedPageWritingProgress = 3,
        None = 0,
    }

    public partial class WritingPrintTicketRequiredEventArgs : System.EventArgs
    {
        public WritingPrintTicketRequiredEventArgs(System.Windows.Xps.Serialization.PrintTicketLevel printTicketLevel, int sequence) { }
        public System.Printing.PrintTicket CurrentPrintTicket { get { throw null; } set { } }
        public System.Windows.Xps.Serialization.PrintTicketLevel CurrentPrintTicketLevel { get { throw null; } }
        public int Sequence { get { throw null; } }
    }
}