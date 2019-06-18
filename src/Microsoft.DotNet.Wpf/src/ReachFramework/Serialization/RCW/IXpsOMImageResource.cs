// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
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
    
    [Guid("3DB8417D-AE50-485E-9A44-D7758F78A23F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    internal interface IXpsOMImageResource : IXpsOMResource
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IStream GetStream();

        [MethodImpl(MethodImplOptions.InternalCall)]
        void SetContent([MarshalAs(UnmanagedType.Interface)] [In] IStream sourceStream, [ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_IMAGE_TYPE")] [In] XPS_IMAGE_TYPE imageType, [MarshalAs(UnmanagedType.Interface)] [In] IOpcPartUri partName);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_IMAGE_TYPE")]
        XPS_IMAGE_TYPE GetImageType();

}
}
