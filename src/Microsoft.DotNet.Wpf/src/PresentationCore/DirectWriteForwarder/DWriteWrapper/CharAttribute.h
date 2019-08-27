// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __CHAR_ATTRIBUTE_H
#define __CHAR_ATTRIBUTE_H

typedef byte CharAttributeType;
namespace MS { namespace Internal { namespace Text { namespace TextInterface
{
    private struct CharAttribute
    {
        enum Enum
        {
            None           = 0x00,
            IsCombining    = 0x01,
            NeedsCaretInfo = 0x02,
            IsIndic        = 0x04,
            IsLatin        = 0x08,
            IsStrong       = 0x10,
            IsExtended     = 0x20
        };
    };
}}}}

#endif //__CHAR_ATTRIBUTE_H