// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#ifndef __GDIEXPORTER_PRINTMSG_H__
#define __GDIEXPORTER_PRINTMSG_H__

void FinePrint(
    IN HDC    hDC,
    IN int    NumColors,
    IN bool   SupportJPEGpassthrough,
    IN bool   SupportPNGpassthrough,
    cli::array<Byte>^ devmode
    );

#endif
