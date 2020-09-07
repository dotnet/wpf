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
    /// Behavior for filtering visuals while hit tesitng
    /// </summary>
    // This enum intentionally does not have a [Flags] attribute.  Internally we break this enum
    // into flags, but the enum values already contain all legal combinations.  Users should not
    // be combining these flags.  (Windows OS #1010970)
    public enum HitTestFilterBehavior
    {
        /// <summary>
        /// Hit test against current visual and not its children.
        /// </summary>
        ContinueSkipChildren = HTFBInterpreter.c_DoHitTest,

        /// <summary>
        /// Do not hit test against current visual or its children.
        /// </summary>
        ContinueSkipSelfAndChildren = 0,

        /// <summary>
        /// Do not hit test against current visual but hit test against children.
        /// </summary>
        ContinueSkipSelf = HTFBInterpreter.c_IncludeChidren,

        /// <summary>
        /// Hit test against current visual and children.
        /// </summary>
        Continue = HTFBInterpreter.c_DoHitTest | HTFBInterpreter.c_IncludeChidren,

        /// <summary>
        /// Stop any further hit testing and return.
        /// </summary>
        Stop = HTFBInterpreter.c_Stop
    }
    
    /// <summary>
    /// Delegate for hit tester to control whether to test against the
    /// current scene graph node.
    /// </summary>
    public delegate HitTestFilterBehavior HitTestFilterCallback(DependencyObject potentialHitTestTarget);

    // Static helper class with methods for interpreting the HitTestFilterBehavior enum.
    internal static class HTFBInterpreter
    {
        internal const int c_DoHitTest        = (1 << 1);
        internal const int c_IncludeChidren   = (1 << 2);
        internal const int c_Stop             = (1 << 3);

        internal static bool DoHitTest(HitTestFilterBehavior behavior)
        {
            return (((int)behavior) & c_DoHitTest) == c_DoHitTest;
        }

        internal static bool IncludeChildren(HitTestFilterBehavior behavior)
        {
            return (((int)behavior) & c_IncludeChidren) == c_IncludeChidren;
        }

        internal static bool Stop(HitTestFilterBehavior behavior)
        {
            return (((int)behavior) & c_Stop) == c_Stop;
        }

        internal static bool SkipSubgraph(HitTestFilterBehavior behavior)
        {
            return behavior == HitTestFilterBehavior.ContinueSkipSelfAndChildren;
        }
    }
}

