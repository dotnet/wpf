// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __ICLASSIFICATION_H
#define __ICLASSIFICATION_H

namespace MS { namespace Internal { namespace Text { namespace TextInterface
{
    /// <summary>
    /// This interface is used as a level on indirection for classes in managed c++ to be able to utilize methods
    /// from the static class Classification present in PresentationCore.dll.
    /// We cannot make MC++ reference PresentationCore.dll since this will result in cirular reference.
    /// </summary>
    private interface class IClassification
    {
        void GetCharAttribute(
            int unicodeScalar,
            [System::Runtime::InteropServices::Out] bool% isCombining,
            [System::Runtime::InteropServices::Out] bool% needsCaretInfo,
            [System::Runtime::InteropServices::Out] bool% isIndic,
            [System::Runtime::InteropServices::Out] bool% isDigit,
            [System::Runtime::InteropServices::Out] bool% isLatin,
            [System::Runtime::InteropServices::Out] bool% isStrong
            );
    };

}}}}//MS::Internal::Text::TextInterface

#endif //__ICLASSIFICATION_H