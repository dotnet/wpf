// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#ifndef __LEGACYDEVICE_HPP__
#define __LEGACYDEVICE_HPP__

/*++                                                
    Abstract:
        This file includes the declarations of the ILegacyDevice, implemented by GDIExporter                                                        
--*/

#using PRESENTATIONCORE_DLL as_friend

using namespace System;

using namespace System::Windows;
using namespace System::Windows::Media;
using namespace System::Windows::Media::Imaging;

namespace System { namespace Printing
{
    [AttributeUsage(
    AttributeTargets::Class |
    AttributeTargets::Property |
    AttributeTargets::Method |
    AttributeTargets::Struct |
    AttributeTargets::Enum |
    AttributeTargets::Interface |
    AttributeTargets::Delegate |
    AttributeTargets::Constructor,
    AllowMultiple = false,
    Inherited = true)
    ]
    private ref class FriendAccessAllowedAttribute sealed : Attribute
    {
    };


    /// <summary>
    /// ILegacyDevice interface  -- this interface should be made internal or use link demand
    /// </summary>
    [FriendAccessAllowed]
    private interface class ILegacyDevice
    {
    public:
        /// <summary>
        /// Start a new document
        /// </summary>
        int StartDocument(String^ printerName, String ^ jobName, String^ filename, cli::array<Byte>^ deviceMode);

        /// <summary>
        /// Start a new document without creating a DC
        /// </summary>
        void StartDocumentWithoutCreatingDC(String^ printerName, String ^ jobName, String^ filename);

        /// <summary>
        /// Finish a document
        /// </summary>
        void EndDocument();

        /// <summary>
        /// Create a DC
        /// </summary>
        void CreateDeviceContext(String^ printerName, String ^ jobName, cli::array<Byte>^ deviceMode);

        /// <summary>
        /// Create a DC
        /// </summary>
        void DeleteDeviceContext();

        /// <summary>
        /// Ext Esc to get the file name from MXDW
        /// </summary>
        String^ ExtEscGetName();

        /// <summary>
        /// Ext Esc to set MXDW in pass thru mode
        /// </summary>
        Boolean ExtEscMXDWPassThru();

        /// <summary>
        /// Start a new page
        /// </summary>
        void StartPage(cli::array<Byte>^ deviceMode, int rasterizationDPI); 

        /// <summary>
        /// End the current page
        /// </summary>
        void EndPage();

        /// <summary>
        /// Undo the last PushTransform
        /// </summary>
        void PopTransform();

        /// <summary>
        /// Undo the last PushClip
        /// </summary>
        void PopClip();
        
        /// <summary>
        /// Push clip geometry
        /// </summary>
        void PushClip(Geometry^ clipGeometry);

        /// <summary>
        /// Push transformation
        /// </summary>
        void PushTransform(Matrix transform);

        /// <summary>
        /// Draw geometry
        /// </summary>
        void DrawGeometry(Brush^ brush, Pen^ pen, Brush ^ strokeBrush, Geometry^ geometry);

        /// <summary>
        /// Draw image
        /// </summary>
        void DrawImage(BitmapSource^ source, cli::array<Byte> ^ buffer, Rect rect);

        /// <summary>
        /// Draw glyphrun
        /// </summary>
        void DrawGlyphRun(Brush ^ brush, GlyphRun^ glyphRun);

        /// <summary>
        /// Add comment to output stream
        /// </summary>
        void Comment(String ^ message);
    };

}}


#endif
