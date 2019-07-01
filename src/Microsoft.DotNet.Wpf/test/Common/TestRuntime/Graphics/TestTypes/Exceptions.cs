// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Test.Graphics.TestTypes
{
    /// <summary>
    /// Special Exceptions for CoreGraphics Framework
    /// </summary>
    internal class InvalidScriptFileException : Exception
    {
        /// <summary/>
        public InvalidScriptFileException(string message)
            : base(message)
        {
        }

        /// <summary/>
        public InvalidScriptFileException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
