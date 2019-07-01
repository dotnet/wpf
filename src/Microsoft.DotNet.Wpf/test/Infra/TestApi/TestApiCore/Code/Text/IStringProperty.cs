// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Test.Text
{
    /// <summary>
    /// Inteface for specific string property class
    /// </summary>
    internal interface IStringProperty
    {
        /// <summary>
        /// Get next random code point or points that belongs to a specific
        /// string property. number of code points does not necessarily translate
        /// to number of chars since surrogate pair are two bytes
        /// </summary>
        string GetRandomCodePoints(int numOfProperty, int seed);

        /// <summary>
        /// Check if code point is in the property range
        /// </summary>
        bool IsInPropertyRange(int codePoint);
    }
}

