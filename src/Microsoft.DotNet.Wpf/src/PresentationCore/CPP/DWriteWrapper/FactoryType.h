// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __FACTORYTYPE_H
#define __FACTORYTYPE_H

namespace MS { namespace Internal { namespace Text { namespace TextInterface
{
    /// <summary>
    ///The type of the Factory.
    /// </summary>
    private enum class FactoryType
    {
        Shared,
        Isolated
    };

}}}}//MS::Internal::Text::TextInterface

#endif //__FACTORYTYPE_H