// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Test.Serialization.CustomElements
{
    /// <summary>
    /// Defines contract for custom elements displayed by round-trip serialization tests.
    /// </summary>
    public interface ICustomElement
    {
        /// <summary>
        /// Component+Type+Function to invoke for optional verification.
        /// </summary>
        string Verifier { get; set; }

        /// <summary>
        ///  Event Handler 
        /// </summary>
        event EventHandler RenderedEvent;
    }
}
