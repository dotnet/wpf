// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿/*++
All rights reserved.

--*/

namespace MS.Internal.Printing.Configuration
{
    using System;

    internal enum DeviceCapability : short
    {
        /// <summary>
        /// Returns the dmFields member of the printer driver's DEVMODE structure. 
        /// </summary>
        /// <remarks>
        /// The dmFields member indicates which members in the device-independent portion of the structure are supported by the printer driver.
        /// </remarks>
        DC_FIELDS = 1,

        /// <summary>
        /// Retrieves a list of supported paper sizes.
        /// </summary>
        /// <remarks>
        /// The pOutput buffer receives an array of WORD values that indicate the available paper sizes for the printer. The return value indicates the number of entries in the array. For a list of the possible array values, see the description of the dmPaperSize member of the DEVMODE structure. If pOutput is NULL, the return value indicates the required number of entries in the array.
        /// </remarks>
        DC_PAPERS = 2,

        /// <summary>
        /// Retrieves the dimensions, in tenths of a millimeter, of each supported paper size. The pOutput buffer receives an array of POINT structures. Each structure contains the width (x-dimension) and length (y-dimension) of a paper size as if the paper were in the DMORIENT_PORTRAIT orientation. The return value indicates the number of entries in the array.
        /// </summary>
        DC_PAPERSIZE = 3,

        /// <summary>
        /// Returns the minimum paper size that the dmPaperLength and dmPaperWidth members of the printer driver's DEVMODE structure can specify. The LOWORD of the return value contains the minimum dmPaperWidth value, and the HIWORD contains the minimum dmPaperLength value.
        /// </summary>
        DC_MINEXTENT = 4,

        /// <summary>
        /// Returns the maximum paper size that the dmPaperLength and dmPaperWidth members of the printer driver's DEVMODE structure can specify. The LOWORD of the return value contains the maximum dmPaperWidth value, and the HIWORD contains the maximum dmPaperLength value.
        /// </summary>
        DC_MAXEXTENT = 5,

        /// <summary>
        /// Retrieves a list of available paper bins. The pOutput buffer receives an array of WORD values that indicate the available paper sources for the printer. The return value indicates the number of entries in the array. For a list of the possible array values, see the description of the dmDefaultSource member of the DEVMODE structure. If pOutput is NULL, the return value indicates the required number of entries in the array.
        /// </summary>
        DC_BINS = 6,

        /// <summary>
        /// If the printer supports duplex printing, the return value is 1; otherwise, the return value is zero. The pOutput parameter is not used.
        /// </summary>
        DC_DUPLEX = 7,

        /// <summary>
        /// Returns the dmSize member of the printer driver's DEVMODE structure.
        /// </summary>
        DC_SIZE = 8,

        /// <summary>
        /// Returns the number of bytes required for the device-specific portion of the DEVMODE structure for the printer driver.
        /// </summary>
        DC_EXTRA = 9,

        /// <summary>
        /// Returns the specification version to which the printer driver conforms.
        /// </summary>
        DC_VERSION = 10,

        /// <summary>
        /// Returns the version number of the printer driver.
        /// </summary>
        DC_DRIVER = 11,

        /// <summary>
        /// Retrieves the names of the printer's paper bins.
        /// </summary>
        /// <remarks>
        /// The pOutput buffer receives an array of string buffers. Each string buffer is 24 characters long and contains the name of a paper bin. The return value indicates the number of entries in the array. The name strings are null-terminated unless the name is 24 characters long. If pOutput is NULL, the return value is the number of bin entries required.
        /// </remarks>
        DC_BINNAMES = 12,

        /// <summary>
        /// Retrieves a list of the resolutions supported by the printer. The pOutput buffer receives an array of LONG values. For each supported resolution, the array contains a pair of LONG values that specify the x and y dimensions of the resolution, in dots per inch. The return value indicates the number of supported resolutions. If pOutput is NULL, the return value indicates the number of supported resolutions.
        /// </summary>
        DC_ENUMRESOLUTIONS = 13,

        /// <summary>
        /// Retrieves the abilities of the driver to use TrueType fonts.
        /// </summary>
        /// <remarks>
        /// For DC_TRUETYPE, the pOutput parameter should be NULL. The return value can be one or more of the following:
        /// DCTT_BITMAPDevice can print TrueType fonts as graphics.
        /// DCTT_DOWNLOADDevice can down-load TrueType fonts.
        /// DCTT_DOWNLOAD_OUTLINEWindows 95/98/Me: Device can download outline TrueType fonts.
        /// DCTT_SUBDEVDevice can substitute device fonts for TrueType fonts.
        /// </remarks>
        DC_TRUETYPE = 15,

        /// <summary>
        /// Retrieves a list of supported paper names (for example, Letter or Legal).
        /// </summary>
        /// <remarks>
        /// The pOutput buffer receives an array of string buffers. Each string buffer is 64 characters long and contains the name of a paper form. The return value indicates the number of entries in the array. The name strings are null-terminated unless the name is 64 characters long. If pOutput is NULL, the return value is the number of paper forms.
        /// </remarks>
        DC_PAPERNAMES = 16,

        /// <summary>
        /// Returns the relationship between portrait and landscape orientations for a device, in terms of the number of degrees that portrait orientation is rotated counterclockwise to produce landscape orientation. 
        /// </summary>
        /// <remarks>
        /// The return value can be one of the following:
        /// 0 No landscape orientation.
        /// 90 Portrait is rotated 90 degrees to produce landscape.
        /// 270 Portrait is rotated 270 degrees to produce landscape.
        /// </remarks>
        DC_ORIENTATION = 17,

        /// <summary>
        /// Returns the number of copies the device can print.
        /// </summary>
        DC_COPIES = 18,

        /// <summary>
        /// If the printer supports collating, the return value is 1; otherwise, the return value is zero. The pOutput parameter is not used.
        /// </summary>
        DC_COLLATE = 22,

        /// <summary>
        /// Windows 2000/XP: Retrieves the names of the paper forms that are currently available for use. The pOutput buffer receives an array of string buffers. Each string buffer is 64 characters long and contains the name of a paper form. The return value indicates the number of entries in the array. The name strings are null-terminated unless the name is 64 characters long. If pOutput is NULL, the return value is the number of paper forms.
        /// </summary>
        DC_MEDIAREADY = 29,

        /// <summary>
        /// Windows 2000/XP: If the printer supports stapling, the return value is a nonzero value; otherwise, the return value is zero. The pOutput parameter is not used.
        /// </summary>
        DC_STAPLE = 30,

        /// <summary>
        /// Windows 2000/XP: If the printer supports color printing, the return value is 1; otherwise, the return value is zero. The pOutput parameter is not used.
        /// </summary>
        DC_COLORDEVICE = 32,

        /// <summary>
        /// Windows 2000/XP: Retrieves an array of integers that indicate that printer's ability to print multiple document pages per printed page. 
        /// </summary>
        /// <remarks>
        /// The pOutput buffer receives an array of DWORD values. Each value represents a supported number of document pages per printed page. The return value indicates the number of entries in the array. If pOutput is NULL, the return value indicates the required number of entries in the array.
        /// </summary>
        DC_NUP = 33,

        /// <summary>
        /// Windows XP: Retrieves the names of the supported media types. 
        /// </summary>
        /// <remarks>
        /// The pOutput buffer receives an array of string buffers. Each string buffer is 64 characters long and contains the name of a supported media type. The return value indicates the number of entries in the array. The strings are null-terminated unless the name is 64 characters long. If pOutput is NULL, the return value is the number of media type names required.
        /// </remarks>
        DC_MEDIATYPENAMES = 34,

        /// <summary>
        /// Windows XP: Retrieves a list of supported media types. The pOutput buffer receives an array of DWORD values that indicate the supported media types. The return value indicates the number of entries in the array. For a list of possible array values, see the description of the dmMediaType member of the DEVMODE structure. If pOutput is NULL, the return value indicates the required number of entries in the array.
        /// </summary>
        DC_MEDIATYPES = 35,
    }
}