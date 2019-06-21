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

    [Guid("ED360180-6F92-4998-890D-2F208531A0A0"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    internal interface IXpsOMPageReference
    {
        IXpsOMPageReference Clone();

        IXpsOMNameCollection CollectLinkTargets();

        IXpsOMPartResources CollectPartResources();

        void DiscardPage();

        XPS_SIZE GetAdvisoryPageDimensions();

        IXpsOMDocument GetOwner();

        IXpsOMPage GetPage();

        IXpsOMPrintTicketResource GetPrintTicketResource();

        IXpsOMStoryFragmentsResource GetStoryFragmentsResource();

        IXpsOMImageResource GetThumbnailResource();

        int HasRestrictedFonts();

        int IsPageLoaded();

        void SetAdvisoryPageDimensions([In][ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_SIZE")] ref XPS_SIZE pageDimensions);

        void SetPage([In] IXpsOMPage page);

        void SetPrintTicketResource([In] IXpsOMPrintTicketResource printTicketResource);

        void SetStoryFragmentsResource([In] IXpsOMStoryFragmentsResource storyFragmentsResource);

        void SetThumbnailResource([In] IXpsOMImageResource imageResource);
    }
}
