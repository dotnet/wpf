// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++
All rights reserved.

--*/

namespace MS.Internal.Printing.Configuration
{
    internal struct PRINTER_INFO_2
    {
        public string pPrinterName;
        public string pPortName;
        public string pDriverName;
        public DevMode pDevMode;
    }
}