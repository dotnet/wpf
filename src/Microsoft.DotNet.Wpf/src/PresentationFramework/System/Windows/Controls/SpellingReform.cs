// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: An enumeration specifying spelling reform behavior for the
//              spell checker.
//

namespace System.Windows.Controls
{
    /// <summary>
    /// An enumeration specifying spelling reform behavior for the spell checker.
    /// </summary>
    public enum SpellingReform
    { 
        /// <summary>
        /// Accept text following preform or postreform spelling rules.
        /// </summary>
        PreAndPostreform,

        /// <summary>
        /// Accept text following preform spelling rules.
        /// </summary>
        Prereform,

        /// <summary>
        /// Accept text following postreform spelling rules.
        /// </summary>
        Postreform,
    };
}
