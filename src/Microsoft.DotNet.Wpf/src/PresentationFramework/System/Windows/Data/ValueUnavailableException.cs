// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Defines the ValueUnavailableException, thrown when a value requested
//              by a validation rule is not available.
//

using System;

namespace System.Windows.Data
{
    ///<summary>Exception class thrown when a value requested by a validation rule is not available</summary>
    [Serializable]
    public class ValueUnavailableException : SystemException
    {
        #region Constructors

        ///<summary>
        /// Constructor
        ///</summary>
        public ValueUnavailableException() : base ()
        {
        }

        ///<summary>
        /// Constructor
        ///</summary>
        ///<param name="message">
        /// Exception message
        ///</param>
        public ValueUnavailableException(string message) : base (message)
        {
        }

        ///<summary>
        /// Constructor
        ///</summary>
        ///<param name="message">Exception message</param>
        ///<param name="innerException">exception occured</param>
        public ValueUnavailableException(string message, Exception innerException) : base(message, innerException)
        {
        }

        ///<summary>
        /// Constructor
        ///</summary>
        ///<param name="message">Exception message</param>
        ///<param name="innerException">exception occured</param>
        protected ValueUnavailableException(System.Runtime.Serialization.SerializationInfo info,
                                            System.Runtime.Serialization.StreamingContext context) : base(info, context)
        {
        }

        #endregion Constructors
    }
}
