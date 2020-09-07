// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;

namespace System.Windows.Reach
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class ReachPackagingException : ReachException
    {
        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        public
        ReachPackagingException(
            )
            :
            base()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public
        ReachPackagingException(
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
        public ReachPackagingException(
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
        ReachPackagingException(
            SerializationInfo   info,
            StreamingContext    context
            )
            : base(info, context)
        {
        }

        #endregion Constructors
    }
}