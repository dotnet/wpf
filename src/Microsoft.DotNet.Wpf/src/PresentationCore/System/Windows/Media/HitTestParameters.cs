// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

using System;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

using System.Collections;
using System.Diagnostics;
using MS.Internal;

namespace System.Windows.Media 
{
    /// <summary>
    /// This is the base class for packing together parameters for a hit test pass.
    /// </summary>
    public abstract class HitTestParameters
    {
        // Prevent 3rd parties from extending this abstract base class.
        internal HitTestParameters() {}
    }
}

