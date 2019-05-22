// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Windows.Documents.Serialization
{
    public abstract partial class SerializerWriterCollator
    { 
        protected SerializerWriterCollator() { }
        public abstract void BeginBatchWrite();
        public abstract void Cancel();
        public abstract void CancelAsync();
        public abstract void EndBatchWrite();
        public abstract void Write(System.Windows.Media.Visual visual);
        public abstract void Write(System.Windows.Media.Visual visual, System.Printing.PrintTicket printTicket);
        public abstract void WriteAsync(System.Windows.Media.Visual visual);
        public abstract void WriteAsync(System.Windows.Media.Visual visual, object userState);
        public abstract void WriteAsync(System.Windows.Media.Visual visual, System.Printing.PrintTicket printTicket);
        public abstract void WriteAsync(System.Windows.Media.Visual visual, System.Printing.PrintTicket printTicket, object userState);
    }
}