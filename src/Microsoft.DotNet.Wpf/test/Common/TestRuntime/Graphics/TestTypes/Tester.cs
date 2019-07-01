// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Test.Graphics.TestTypes
{
    /// <summary>
    /// Base class for all testers
    /// </summary>
    internal abstract class GraphicsTestLoader
    {
        /// <summary/>
        public GraphicsTestLoader(TokenList tokens)
        {
            this.tokens = tokens;
        }

        /// <summary/>
        public abstract bool RunMyTests();

        /// <summary/>
        public static bool IsRunAll = false;

        /// <summary/>
        protected TokenList tokens;
    }
}
