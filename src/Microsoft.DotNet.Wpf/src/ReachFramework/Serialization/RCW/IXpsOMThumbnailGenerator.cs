// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

    [Guid("15B873D5-1971-41E8-83A3-6578403064C7"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    internal interface IXpsOMThumbnailGenerator
    {
        IXpsOMImageResource GenerateThumbnail([In] IXpsOMPage page, [In][ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_IMAGE_TYPE")] XPS_IMAGE_TYPE thumbnailType, [In][ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_THUMBNAIL_SIZE")] XPS_THUMBNAIL_SIZE thumbnailSize, [In] IOpcPartUri imageResourcePartName);
    }
}
