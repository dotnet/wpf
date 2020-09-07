// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __IFONTSOURCE_H
#define __IFONTSOURCE_H

//---------------------------------------------------------------------------
//

// 
// Description: The FontSourceCollection class represents a collection of font files.
//
//  
//
//
//---------------------------------------------------------------------------

using namespace System;
using namespace System::IO;


namespace MS { namespace Internal { namespace Text { namespace TextInterface
{
    private interface class IFontSource
    {
        void                                TestFileOpenable();
        System::IO::UnmanagedMemoryStream ^ GetUnmanagedStream();
        System::DateTime                    GetLastWriteTimeUtc();
        property System::Uri^ Uri
        {
            System::Uri^ get();
        }

        property bool IsComposite
        {
            bool get();
        }
       
    };

    private interface class IFontSourceFactory
    {
        IFontSource^ Create(System::String^);
    };

}}}}//MS::Internal::Text::TextInterface

#endif //__IFONTSOURCE_H