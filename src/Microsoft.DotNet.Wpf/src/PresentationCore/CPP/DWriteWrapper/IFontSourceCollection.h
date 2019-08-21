// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __IFONTSOURCECOLLECTION_H
#define __IFONTSOURCECOLLECTION_H

#include "IFontSource.h"

using namespace System;
namespace MS { namespace Internal { namespace Text { namespace TextInterface
{
    private interface class IFontSourceCollection : IEnumerable<IFontSource^>
    {
    };

    private interface class IFontSourceCollectionFactory
    {
        IFontSourceCollection^ Create(String^);
    };
    
}}}}//MS::Internal::Text::TextInterface

#endif //__IFONTSOURCECOLLECTION_H