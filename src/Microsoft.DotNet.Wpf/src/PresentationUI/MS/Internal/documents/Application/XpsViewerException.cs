// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description:
//  The XpsViewerException is thrown when an error occurs in XPS Viewer that
//  cannot be handled in the application.

using System;
using System.Runtime.Serialization;
using System.Windows.TrustUI;

namespace MS.Internal.Documents.Application
{
    /// <summary>
    /// The XpsViewerException is thrown when an error occurs in XPS Viewer
    /// that cannot be handled in the application.
    /// </summary>
    [Serializable()]
    internal class XpsViewerException : Exception
    {
        /// <summary>
        /// Creates a new instance of XpsViewerException class.
        /// This constructor initializes the Message property of the new
        /// instance to a generic message that describes an exception in the
        /// XPS Viewer. This message takes into account the current system
        /// culture.
        /// </summary>
        internal XpsViewerException()
            : base(SR.Get(SRID.XpsViewerGenericException))
        {}

        /// <summary>
        /// Creates a new instance of XpsViewerException class.
        /// This constructor initializes the Message property of the new
        /// instance with the specified error message.
        /// The caller of this constructor is required to ensure that this
        /// string has been localized for the current system culture.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        internal XpsViewerException(string message)
            : base(message)
        {}

        /// <summary>
        /// Creates a new instance of XpsViewerException class.
        /// This constructor initializes the Message property of the new
        /// instance with the specified error message.
        /// The caller of this constructor is required to ensure that this
        /// string has been localized for the current system culture.
        /// The InnerException property is initialized using the
        /// innerException parameter.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the
        /// current exception.</param>
        internal XpsViewerException(string message, Exception innerException)
            : base(message, innerException)
        {}
        /// <summary>
        /// Initializes a new instance of the XpsViewerException class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected XpsViewerException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }

    }
}

