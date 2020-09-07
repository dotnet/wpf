// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Security;

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

    [Guid("D410DC83-908C-443E-8947-B1795D3C165A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    internal interface IXpsOMGeometryFigure
    {
        IXpsOMGeometryFigure Clone();

        int GetIsClosed();

        int GetIsFilled();

        IXpsOMGeometry GetOwner();

        uint GetSegmentCount();

        void GetSegmentData([In][Out] ref uint dataCount, [In][Out] ref float segmentData);

        uint GetSegmentDataCount();

        XPS_SEGMENT_STROKE_PATTERN GetSegmentStrokePattern();

        void GetSegmentStrokes([In][Out] ref uint segmentCount, [In][Out] ref int segmentStrokes);

        void GetSegmentTypes([In][Out] ref uint segmentCount, [In][Out][ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_SEGMENT_TYPE")] ref XPS_SEGMENT_TYPE segmentTypes);

        XPS_POINT GetStartPoint();

        void SetIsClosed([In] int isClosed);

        void SetIsFilled([In] int isFilled);

        void SetSegments([In] uint segmentCount, [In] uint segmentDataCount, [In][ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_SEGMENT_TYPE")] ref XPS_SEGMENT_TYPE segmentTypes, [In] ref float segmentData, [In] ref int segmentStrokes);

        void SetStartPoint([In][ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_POINT")] ref XPS_POINT startPoint);
    }
}
