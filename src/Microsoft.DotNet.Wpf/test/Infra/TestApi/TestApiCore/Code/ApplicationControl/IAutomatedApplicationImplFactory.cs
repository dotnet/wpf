// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Test.ApplicationControl
{
    /// <summary>
    /// Defines the contract for creating an IAutomatedApplicationImpl instance.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Impl")]
    public interface IAutomatedApplicationImplFactory
    {
        /// <summary>
        /// Factory method for creating the IAutomatedApplicationImpl instance 
        /// to be used by AutomatedApplication.
        /// </summary>
        /// <param name="settings">The settings to be passed the the implementation instance.</param>
        /// <param name="appDomain">
        /// The AppDomain to create the implementation in.  This is intended for in-proc scenarios where
        /// the AutomatedApplication needs to create the proxy in a separate AppDomain.
        /// </param>
        /// <returns>Returns the application implementation to be used by AutomatedApplication</returns>
        IAutomatedApplicationImpl Create(ApplicationSettings settings, AppDomain appDomain);
    }
}
