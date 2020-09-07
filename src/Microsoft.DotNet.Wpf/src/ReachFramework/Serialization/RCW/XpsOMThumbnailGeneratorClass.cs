// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
    
    [ClassInterface(ClassInterfaceType.None)]
    [Guid("7E4A23E2-B969-4761-BE35-1A8CED58E323")]
    [TypeLibType(TypeLibTypeFlags.FCanCreate)]
    [ComImport]
    internal class XpsOMThumbnailGeneratorClass : IXpsOMThumbnailGenerator, XpsOMThumbnailGenerator
    {

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        public virtual extern IXpsOMImageResource GenerateThumbnail([In] IXpsOMPage page, [In][ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_IMAGE_TYPE")] XPS_IMAGE_TYPE thumbnailType, [In][ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_THUMBNAIL_SIZE")] XPS_THUMBNAIL_SIZE thumbnailSize, [In] IOpcPartUri imageResourcePartName);
    }
}