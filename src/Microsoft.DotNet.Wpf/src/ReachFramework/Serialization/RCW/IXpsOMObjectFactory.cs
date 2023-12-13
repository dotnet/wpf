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
    
    [Guid("F9B2A685-A50D-4FC2-B764-B56E093EA0CA"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    internal interface IXpsOMObjectFactory
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMPackage CreatePackage();

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMPackage CreatePackageFromFile([MarshalAs(UnmanagedType.LPWStr)] [In] string fileName, [In] int reuseObjects);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMPackage CreatePackageFromStream([MarshalAs(UnmanagedType.Interface)] [In] IStream stream, [In] int reuseObjects);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMStoryFragmentsResource CreateStoryFragmentsResource([MarshalAs(UnmanagedType.Interface)] [In] IStream acquiredStream, [MarshalAs(UnmanagedType.Interface)] [In] IOpcPartUri partUri);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMDocumentStructureResource CreateDocumentStructureResource([MarshalAs(UnmanagedType.Interface)] [In] IStream acquiredStream, [MarshalAs(UnmanagedType.Interface)] [In] IOpcPartUri partUri);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMSignatureBlockResource CreateSignatureBlockResource([MarshalAs(UnmanagedType.Interface)] [In] IStream acquiredStream, [MarshalAs(UnmanagedType.Interface)] [In] IOpcPartUri partUri);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMRemoteDictionaryResource CreateRemoteDictionaryResource([MarshalAs(UnmanagedType.Interface)] [In] IXpsOMDictionary dictionary, [MarshalAs(UnmanagedType.Interface)] [In] IOpcPartUri partUri);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMRemoteDictionaryResource CreateRemoteDictionaryResourceFromStream([MarshalAs(UnmanagedType.Interface)] [In] IStream dictionaryMarkupStream, [MarshalAs(UnmanagedType.Interface)] [In] IOpcPartUri dictionaryPartUri, [MarshalAs(UnmanagedType.Interface)] [In] IXpsOMPartResources resources);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMPartResources CreatePartResources();

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMDocumentSequence CreateDocumentSequence([MarshalAs(UnmanagedType.Interface)] [In] IOpcPartUri partUri);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMDocument CreateDocument([MarshalAs(UnmanagedType.Interface)] [In] IOpcPartUri partUri);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMPageReference CreatePageReference([ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_SIZE")] [In] ref XPS_SIZE advisoryPageDimensions);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMPage CreatePage([ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_SIZE")] [In] ref XPS_SIZE pageDimensions, [MarshalAs(UnmanagedType.LPWStr)] [In] string language, [MarshalAs(UnmanagedType.Interface)] [In] IOpcPartUri partUri);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMPage CreatePageFromStream([MarshalAs(UnmanagedType.Interface)] [In] IStream pageMarkupStream, [MarshalAs(UnmanagedType.Interface)] [In] IOpcPartUri partUri, [MarshalAs(UnmanagedType.Interface)] [In] IXpsOMPartResources resources, [In] int reuseObjects);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMCanvas CreateCanvas();

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMGlyphs CreateGlyphs([MarshalAs(UnmanagedType.Interface)] [In] IXpsOMFontResource fontResource);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMPath CreatePath();

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMGeometry CreateGeometry();

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMGeometryFigure CreateGeometryFigure([ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_POINT")] [In] ref XPS_POINT startPoint);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMMatrixTransform CreateMatrixTransform([ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_MATRIX")] [In] ref XPS_MATRIX matrix);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMSolidColorBrush CreateSolidColorBrush([ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_COLOR")] [In] ref XPS_COLOR color, [MarshalAs(UnmanagedType.Interface)] [In] IXpsOMColorProfileResource colorProfile);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMColorProfileResource CreateColorProfileResource([MarshalAs(UnmanagedType.Interface)] [In] IStream acquiredStream, [MarshalAs(UnmanagedType.Interface)] [In] IOpcPartUri partUri);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMImageBrush CreateImageBrush([MarshalAs(UnmanagedType.Interface)] [In] IXpsOMImageResource image, [ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_RECT")] [In] ref XPS_RECT viewbox, [ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_RECT")] [In] ref XPS_RECT viewport);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMVisualBrush CreateVisualBrush([ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_RECT")] [In] ref XPS_RECT viewbox, [ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_RECT")] [In] ref XPS_RECT viewport);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMImageResource CreateImageResource([MarshalAs(UnmanagedType.Interface)] [In] IStream acquiredStream, [ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_IMAGE_TYPE")] [In] XPS_IMAGE_TYPE contentType, [MarshalAs(UnmanagedType.Interface)] [In] IOpcPartUri partUri);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMPrintTicketResource CreatePrintTicketResource([MarshalAs(UnmanagedType.Interface)] [In] IStream acquiredStream, [MarshalAs(UnmanagedType.Interface)] [In] IOpcPartUri partUri);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMFontResource CreateFontResource([MarshalAs(UnmanagedType.Interface)] [In] IStream acquiredStream, [ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_FONT_EMBEDDING")] [In] XPS_FONT_EMBEDDING fontEmbedding, [MarshalAs(UnmanagedType.Interface)] [In] IOpcPartUri partUri, [In] int isObfSourceStream);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMGradientStop CreateGradientStop([ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_COLOR")] [In] ref XPS_COLOR color, [MarshalAs(UnmanagedType.Interface)] [In] IXpsOMColorProfileResource colorProfile, [In] float offset);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMLinearGradientBrush CreateLinearGradientBrush([MarshalAs(UnmanagedType.Interface)] [In] IXpsOMGradientStop gradStop1, [MarshalAs(UnmanagedType.Interface)] [In] IXpsOMGradientStop gradStop2, [ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_POINT")] [In] ref XPS_POINT startPoint, [ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_POINT")] [In] ref XPS_POINT endPoint);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMRadialGradientBrush CreateRadialGradientBrush([MarshalAs(UnmanagedType.Interface)] [In] IXpsOMGradientStop gradStop1, [MarshalAs(UnmanagedType.Interface)] [In] IXpsOMGradientStop gradStop2, [ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_POINT")] [In] ref XPS_POINT centerPoint, [ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_POINT")] [In] ref XPS_POINT gradientOrigin, [ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_SIZE")] [In] ref XPS_SIZE radiiSizes);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMCoreProperties CreateCoreProperties([MarshalAs(UnmanagedType.Interface)] [In] IOpcPartUri partUri);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMDictionary CreateDictionary();

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMPartUriCollection CreatePartUriCollection();

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMPackageWriter CreatePackageWriterOnFile([MarshalAs(UnmanagedType.LPWStr)] [In] string fileName, [In] IntPtr securityAttributes, [In] uint flagsAndAttributes, [In] int optimizeMarkupSize, [ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_INTERLEAVING")] [In] XPS_INTERLEAVING interleaving, [MarshalAs(UnmanagedType.Interface)] [In] IOpcPartUri documentSequencePartName, [MarshalAs(UnmanagedType.Interface)] [In] IXpsOMCoreProperties coreProperties, [MarshalAs(UnmanagedType.Interface)] [In] IXpsOMImageResource packageThumbnail, [MarshalAs(UnmanagedType.Interface)] [In] IXpsOMPrintTicketResource documentSequencePrintTicket, [MarshalAs(UnmanagedType.Interface)] [In] IOpcPartUri discardControlPartName);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IXpsOMPackageWriter CreatePackageWriterOnStream([MarshalAs(UnmanagedType.Interface)] [In] ISequentialStream outputStream, [In] int optimizeMarkupSize, [ComAliasName("System.Windows.Xps.Serialization.RCW.XPS_INTERLEAVING")] [In] XPS_INTERLEAVING interleaving, [MarshalAs(UnmanagedType.Interface)] [In] IOpcPartUri documentSequencePartName, [MarshalAs(UnmanagedType.Interface)] [In] IXpsOMCoreProperties coreProperties, [MarshalAs(UnmanagedType.Interface)] [In] IXpsOMImageResource packageThumbnail, [MarshalAs(UnmanagedType.Interface)] [In] IXpsOMPrintTicketResource documentSequencePrintTicket, [MarshalAs(UnmanagedType.Interface)] [In] IOpcPartUri discardControlPartName);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IOpcPartUri CreatePartUri([MarshalAs(UnmanagedType.LPWStr)] [In] string uri);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IStream CreateReadOnlyStreamOnFile([MarshalAs(UnmanagedType.LPWStr)] [In] string fileName);
    }
}
