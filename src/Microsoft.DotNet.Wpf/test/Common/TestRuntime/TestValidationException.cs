// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Test
{
    /// <summary>
    /// This subclass of TestException is thrown by Core test code when 
    /// validation of some result fails.
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <seealso cref="TestException"/>    
    /// <seealso cref="TestSetupException"/>
    [Serializable]
    public class TestValidationException : TestException
    {
        /// <summary>
        /// Passes message parameter to base constructor.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception. </param>
        public TestValidationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Passes parameters to base constructor.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception. </param>
        /// <param name="innerException">The exception that is the cause of the current exception. If the innerException parameter is not a null reference, the current exception is raised in a catch block that handles the inner exception. </param>
        public TestValidationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// This constructor is required for deserializing this type across AppDomains.
        /// </summary>
        /// <param name="info">The object that holds the serialized object data.</param>
        /// <param name="context">The contextual information about the source or destination.</param>
        protected TestValidationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }


}
