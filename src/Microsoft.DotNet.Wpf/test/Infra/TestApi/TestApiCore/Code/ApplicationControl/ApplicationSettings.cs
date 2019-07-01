// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Test.ApplicationControl
{
    /// <summary>
    /// Provides configuration information for an <see cref="AutomatedApplication"/>.
    /// </summary>
    [Serializable]
    public class ApplicationSettings
    {
        /// <summary>
        /// The interface used for creation of the AutomatedApplicationImplementation.
        /// </summary>        
        public IAutomatedApplicationImplFactory ApplicationImplementationFactory
        {
            get;
            set;
        }
    }
}