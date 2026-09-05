// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _ITEM_SPAN_H
#define _ITEM_SPAN_H

#include "Common.h"
namespace MS { namespace Internal
{
    private value struct Span
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="element">Span element</param>
        /// <param name="length">Span length</param>
        Span(Object^ element, int length);

        /// <summary>
        /// Span element
        /// </summary>
        initonly Object^  element;

        /// <summary>
        /// Span length
        /// </summary>
        initonly int      length;
    };
}}//MS::Internal
#endif //_ITEM_SPAN_H