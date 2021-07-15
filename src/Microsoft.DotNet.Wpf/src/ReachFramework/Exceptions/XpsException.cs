// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;

namespace System.Windows.Xps
{
    /// <summary>
    /// This class is the base class for all exceptions that are
    /// thrown by the Xps packaging and serialization APIs.
    /// </summary>
    [Serializable]
    public class XpsException : Exception
    {
        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        public
        XpsException(
            )
            : base()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public
        XpsException(
            string              message
            )
            : base(message)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public
        XpsException(
            string              message,
            Exception           innerException
            )
            : base(message, innerException)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected
        XpsException(
            SerializationInfo   info,
            StreamingContext    context
            )
            : base(info, context)
        {
        }

        #endregion Constructors
    }
}