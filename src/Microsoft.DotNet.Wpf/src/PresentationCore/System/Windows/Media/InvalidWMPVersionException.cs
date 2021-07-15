// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;

namespace System.Windows.Media
{
    ///
    ///<summary>Exception class for when WMP 10 is not installed</summary>
    ///
    [Serializable]
    public class InvalidWmpVersionException : SystemException
    {
        ///<summary>
        /// Constructor
        ///</summary>
        public InvalidWmpVersionException()
        {}

        ///<summary>
        /// Constructor
        ///</summary>
        ///<param name="message">
        /// Exception message
        ///</param>
        public InvalidWmpVersionException(string message) : base(message)
        {}


        /// <summary>
        /// Creates a new instance of InvalidWmpVersionException class and initializes it with serialized data.
        /// This constructor is called during deserialization to reconstitute the exception object transmitted over a stream.
        /// </summary>
        /// <param name="info">
        /// The object that holds the serialized object data.
        /// </param>
        /// <param name="context">
        /// The contextual information about the source or destination.
        /// </param>
        protected InvalidWmpVersionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}

        ///<summary>
        /// Constructor
        ///</summary>
        ///<param name="message">
        /// Exception message
        ///</param>
        ///<param name="innerException">
        /// Inner exception
        ///</param>
        public InvalidWmpVersionException(string message, Exception innerException) : base(message, innerException)
        {}
    }
}
