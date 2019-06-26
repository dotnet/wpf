// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Exception thrown when client attemps to interact with a non-
// enabled control (eg. Invoke a non-enabled button)

using System.Windows.Automation;
using System;
using System.Runtime.Serialization;
using System.Security;
using MS.Internal.Automation;

namespace System.Windows.Automation
{
    /// <summary>
    /// This exception is thrown when client code attemps to manipulate
    /// an element or control that is currently not enabled.
    /// </summary>  
    [Serializable]
#if (INTERNAL_COMPILE)
    internal class ElementNotEnabledException : InvalidOperationException
#else
    public class ElementNotEnabledException : InvalidOperationException
#endif
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public ElementNotEnabledException()
             : base(SR.ElementNotEnabled)
        {
            HResult = UiaCoreTypesApi.UIA_E_ELEMENTNOTENABLED;
        }
        
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="message"></param>
        public ElementNotEnabledException(String message) : base(message)
        {
            HResult = UiaCoreTypesApi.UIA_E_ELEMENTNOTENABLED;
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public  ElementNotEnabledException(string message, Exception innerException) : base(message, innerException)
        {
            HResult = UiaCoreTypesApi.UIA_E_ELEMENTNOTENABLED;
        }

        /// <internalonly>
        /// Constructor for serialization
        /// </internalonly>
        protected ElementNotEnabledException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            HResult = UiaCoreTypesApi.UIA_E_ELEMENTNOTENABLED;
        }

        /// <summary>
        /// Populates a SerializationInfo with the data needed to serialize the target object.
        /// </summary>
        /// <param name="info">The SerializationInfo to populate with data.</param>
        /// <param name="context">The destination for this serialization.</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }
}

