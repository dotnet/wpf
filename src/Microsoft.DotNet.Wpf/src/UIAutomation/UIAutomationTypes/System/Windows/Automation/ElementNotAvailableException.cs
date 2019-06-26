// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Exception indicating that a clickable point could not be found

using System.Windows.Automation;
using System;
using System.Runtime.Serialization;
using System.Security;
using MS.Internal.Automation;

namespace System.Windows.Automation
{
    /// <summary>
    /// The exception that is thrown when accesses a AutomationElement or a
    /// RawElement that corresponds to UI that is no longer available. This can
    /// happen if the UI was in a dialog that was closed, or an application that
    /// was closed or terminated.
    /// </summary>  
    [Serializable]
#if (INTERNAL_COMPILE)
    internal class ElementNotAvailableException : SystemException
#else
    public class ElementNotAvailableException : SystemException
#endif
    {
        /// <summary>
        /// Initializes a new instance of the ElementNotAvailableException class.
        /// </summary>
        public ElementNotAvailableException() : base(SR.ElementNotAvailable)
        {
            HResult = UiaCoreTypesApi.UIA_E_ELEMENTNOTAVAILABLE;
        }

        /// <summary>
        /// Initializes an instance of the ElementNotAvailableException class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ElementNotAvailableException(String message) : base(message)
        {
            HResult = UiaCoreTypesApi.UIA_E_ELEMENTNOTAVAILABLE;
        }

        /// <summary>
        /// Initializes a new instance of the ElementNotAvailableException class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public ElementNotAvailableException(string message, Exception innerException) : base(message, innerException)
        {
            HResult = UiaCoreTypesApi.UIA_E_ELEMENTNOTAVAILABLE;
        }

        /// <summary>
        /// Initializes a new instance of the ElementNotAvailableException class with a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public ElementNotAvailableException(Exception innerException) : base(SR.ElementNotAvailable, innerException)
        {
            HResult = UiaCoreTypesApi.UIA_E_ELEMENTNOTAVAILABLE;
        }

        /// <internalonly>
        /// Initializes a new instance of the ElementNotAvailableException class with serialized data.
        /// </internalonly>
        protected ElementNotAvailableException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            HResult = UiaCoreTypesApi.UIA_E_ELEMENTNOTAVAILABLE;
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
