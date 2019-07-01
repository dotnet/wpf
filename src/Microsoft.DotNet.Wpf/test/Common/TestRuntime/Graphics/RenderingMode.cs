// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/******************************************************************* 
 * Purpose: Enum RenderingMode, equalvalent Enum System.Windows.Interop.RenderingMode
 *          in PresentationCore. We need this Enum since System.Windows.Interop.RenderingMode
 *          is internal. 
 ********************************************************************/

using System;
namespace Microsoft.Test.Graphics
{   
    /// <summary>
    /// Equivalent enum as that in System.Windows.Interop
    /// </summary>
    public enum RenderingMode
    {
        Default,
        Software,
        Hardware,
        HardwareReference
    }
}
