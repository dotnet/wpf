// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        


namespace Microsoft.Test.RenderingVerification
{
    #region using
        using System;
    #endregion using

    /// <summary>
    /// Summary description for RenderingVerificationException
    /// </summary>
    [Serializable]
    public class RenderingVerificationException : Exception
    {
        /// <summary>
        /// VScanExpection constructor
        /// </summary>
        public RenderingVerificationException() : base() {}
        /// <summary>
        /// VScanExpection constructor
        /// </summary>
        /// <param name="message">string to be passed to the base constructor (Exception)</param>
        public RenderingVerificationException(string message): base(message) {}
        /// <summary>
        /// VScanExpection constructor
        /// </summary>
        /// <param name="message">string to be passed to the base constructor (Exception)</param>
        /// <param name="innerException">Inner Exception to passed to the base constructor (Exception)</param>
        public RenderingVerificationException(string message, Exception innerException) : base() {}
    }

    /// <summary>
    /// PackageException will be thrown if the package being saved is not recognized as a standard package
    /// </summary>
//    [PermissionSet(SecurityAction.Assert, Name = "FullTrust")]
    [Serializable]
    public class PackageException: RenderingVerificationException
    {
        /// <summary>
        /// The excpetion being thrown when something wrong occurs in the packaging process
        /// </summary>
        /// <param name="message"></param>
        public PackageException(string message) : base(message) {}
    }

    /// <summary>
    /// Exception thrown by XamlLauncherReflect class
    /// </summary>
    [Serializable]
    public class XamlLauncherReflectException: RenderingVerificationException
    {
        /// <summary>
        /// Instantiate a new XamlLauncherReflectException Exception
        /// </summary>
        public XamlLauncherReflectException() : base() {}
        /// <summary>
        /// Instantiate a new XamlLauncherReflectException Exception with a message
        /// </summary>
        /// <param name="message">The message to be displayed to the user</param>
        public XamlLauncherReflectException(string message) : base(message) {}
        /// <summary>
        /// Instantiate a new XamlLauncherReflectException Exception with a message and an inner Exception
        /// </summary>
        /// <param name="message">The message to be displayed to the user</param>
        /// <param name="innerException">The nested exception causing this to happen</param>
        public XamlLauncherReflectException(string message, Exception innerException) : base(message, innerException) {}
    }
}



