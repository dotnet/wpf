// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Test.FaultInjection
{
    /// <summary>
    /// An exception that is thrown when and error in the FaultInjection API occurs.
    /// </summary>
    [Serializable]
    public class FaultInjectionException : Exception
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the FaultInjectionException class.
        /// </summary>
        public FaultInjectionException() { }
        
        /// <summary>
        /// Initializes a new instance of the FaultInjectionException class using the specified message.
        /// </summary>
        public FaultInjectionException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the FaultInjectionException class using the specified message and inner
        /// exception.
        /// </summary>
        public FaultInjectionException(string message, Exception innerException) : base(message, innerException) { }
        
        /// <summary>
        /// Constructor used for serialization purposes.
        /// </summary>
        protected FaultInjectionException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        #endregion
    }
}
