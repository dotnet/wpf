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
    using System.Windows.Media;
    using System.Security;

    /// <summary>
    /// SerializerWriterCollator is an abstract class that is implemented by plug-in document serializers
    /// Objects of this class are instantiated by SerializerWriter.CreateVisualCellator
    /// </summary>
    public abstract class SerializerWriterCollator
    {
        /// <summary>
        /// prepare for batch writing
        /// </summary>
        public abstract void BeginBatchWrite();
    
        /// <summary>
        /// Complete batch Writing
        /// </summary>
        public abstract void EndBatchWrite();
        
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
        /// Cancel Asynchronous Write
        /// </summary>
        ///
        public abstract void CancelAsync();

        /// <summary>
        /// Cancel Write
        /// </summary>
        ///
        public abstract void Cancel();
    }
}
#endif
