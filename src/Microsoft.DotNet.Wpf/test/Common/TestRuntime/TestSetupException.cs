// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;

namespace Microsoft.Test
{
    /// <summary>
    /// This subclass of TestException is thrown by test code when 
    /// an error occurs while setting up an operation.
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <seealso cref="TestException"/>    
    /// <seealso cref="TestValidationException"/>
    [Serializable]
    public sealed class TestSetupException : TestException
    {
        /// <summary>
        /// Passes message parameter to base constructor.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception. </param>
        public TestSetupException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Passes parameters to base constructor.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception. </param>
        /// <param name="innerException">The exception that is the cause of the current exception. If the innerException parameter is not a null reference, the current exception is raised in a catch block that handles the inner exception. </param>
        public TestSetupException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// This constructor is required for deserializing this type across AppDomains.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public TestSetupException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

}
