// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Windows.Xps.Serialization.RCW
{
    /// <summary>
    /// RCW for xpsobjectmodel.idl found in Windows SDK
    /// This is generated code with minor manual edits. 
    /// i.  Generate TLB
    ///      MIDL /TLB xpsobjectmodel.tlb xpsobjectmodel.IDL //xpsobjectmodel.IDL found in Windows SDK
    /// ii. Generate RCW in a DLL
    ///      TLBIMP xpsobjectmodel.tlb // Generates xpsobjectmodel.dll
    /// iii.Decompile the DLL and copy out the RCW by hand.
    ///      ILDASM xpsobjectmodel.dll
    /// </summary>
    
    internal enum XPS_OBJECT_TYPE
    {
        XPS_OBJECT_TYPE_CANVAS = 1,
        XPS_OBJECT_TYPE_GLYPHS = 2,
        XPS_OBJECT_TYPE_PATH = 3,
        XPS_OBJECT_TYPE_MATRIX_TRANSFORM = 4,
        XPS_OBJECT_TYPE_GEOMETRY = 5,
        XPS_OBJECT_TYPE_SOLID_COLOR_BRUSH = 6,
        XPS_OBJECT_TYPE_IMAGE_BRUSH = 7,
        XPS_OBJECT_TYPE_LINEAR_GRADIENT_BRUSH = 8,
        XPS_OBJECT_TYPE_RADIAL_GRADIENT_BRUSH = 9,
        XPS_OBJECT_TYPE_VISUAL_BRUSH = 10
    }
}