// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿/*++

 * Abstract:

    Retrieves safe string resources from COMPSTUI.dll

--*/

namespace MS.Internal.Printing.Configuration
{
    using System;
    using System.Security;
    using System.Text;

    /// <summary>
    /// Resource manager for string resources in compstui.dll
    /// </summary>
    internal sealed class COMPSTUISR
    {
        public string Get(uint srid)
        {
            // Limit input to range of known safe resource string ID's in compstui.dll
            if (srid < IDS_CPSUI_STRID_FIRST || srid > IDS_CPSUI_STRID_LAST)
            {
                if (srid == IDS_NULL)
                {
                    return null;
                }

                throw new ArgumentOutOfRangeException("srid", srid, string.Empty);
            }

            SafeModuleHandle handle = EnsureModuleHandle();
            if (handle != null && !handle.IsInvalid)
            {
                StringBuilder resString = new StringBuilder(MaxSRLength, MaxSRLength);
                int charCount = UnsafeNativeMethods.LoadStringW(handle, srid, resString, resString.Capacity);
                resString.Length = Math.Max(0, Math.Min(charCount, resString.Length));

                // The resource string have ampersands in them for menu acclerators. They need to be removed
                for (int i = 0; i < resString.Length - 1; i++)
                {
                    if (resString[i] == '&')
                    {
                        char next = resString[i + 1];
                        if (char.IsLetterOrDigit(next) || char.IsPunctuation(next))
                        {
                            resString.Remove(i, 1);
                        }
                    }
                }

                return resString.ToString();
            }

            return null;
        }


        public void Release()
        {
            SafeModuleHandle handle = this._compstuiHandle;            
            if (this._compstuiHandle != null)
            {
                this._compstuiHandle.Dispose();
                this._compstuiHandle = null;
            }
        }

        private SafeModuleHandle EnsureModuleHandle()
        {
            if (this._compstuiHandle == null)
            {
                // Load library as data - do not execute code
                this._compstuiHandle = UnsafeNativeMethods.LoadLibraryExW("compstui.dll", IntPtr.Zero, SafeLoadLibraryFlags);
            }

            return this._compstuiHandle;
        }

        private SafeModuleHandle _compstuiHandle;

        public const uint IDS_NULL = uint.MaxValue;

        // Documented at http://msdn.microsoft.com/en-us/library/ms800830.aspx

        public const uint IDS_CPSUI_FALSE = 64726;
        public const uint IDS_CPSUI_TRUE = 64727;
        public const uint IDS_CPSUI_NO = 64728;
        public const uint IDS_CPSUI_YES = 64729;
        public const uint IDS_CPSUI_OFF = 64730;
        public const uint IDS_CPSUI_ON = 64731;
        public const uint IDS_CPSUI_NONE = 64734;
        public const uint IDS_CPSUI_ORIENTATION = 64738;
        public const uint IDS_CPSUI_SCALING = 64739;
        public const uint IDS_CPSUI_NUM_OF_COPIES = 64740;
        public const uint IDS_CPSUI_SOURCE = 64741;
        public const uint IDS_CPSUI_PRINTQUALITY = 64742;
        public const uint IDS_CPSUI_RESOLUTION = 64743;
        public const uint IDS_CPSUI_COLOR_APPERANCE = 64744;
        public const uint IDS_CPSUI_DUPLEX = 64745;
        public const uint IDS_CPSUI_TTOPTION = 64746;
        public const uint IDS_CPSUI_FORMNAME = 64747;
        public const uint IDS_CPSUI_ICM = 64748;
        public const uint IDS_CPSUI_ICMMETHOD = 64749;
        public const uint IDS_CPSUI_ICMINTENT = 64750;
        public const uint IDS_CPSUI_MEDIA = 64751;
        public const uint IDS_CPSUI_DITHERING = 64752;
        public const uint IDS_CPSUI_PORTRAIT = 64753;
        public const uint IDS_CPSUI_LANDSCAPE = 64754;
        public const uint IDS_CPSUI_ROT_LAND = 64755;
        public const uint IDS_CPSUI_COLLATE = 64756;
        public const uint IDS_CPSUI_COLLATED = 64757;
        public const uint IDS_CPSUI_DRAFT = 64759;
        public const uint IDS_CPSUI_LOW = 64760;
        public const uint IDS_CPSUI_MEDIUM = 64761;
        public const uint IDS_CPSUI_HIGH = 64762;
        public const uint IDS_CPSUI_PRESENTATION = 64763;
        public const uint IDS_CPSUI_COLOR = 64764;
        public const uint IDS_CPSUI_GRAYSCALE = 64765;
        public const uint IDS_CPSUI_MONOCHROME = 64766;
        public const uint IDS_CPSUI_SIMPLEX = 64767;
        public const uint IDS_CPSUI_HORIZONTAL = 64768;
        public const uint IDS_CPSUI_VERTICAL = 64769;
        public const uint IDS_CPSUI_LONG_SIDE = 64770;
        public const uint IDS_CPSUI_SHORT_SIDE = 64771;
        public const uint IDS_CPSUI_TT_PRINTASGRAPHIC = 64772;
        public const uint IDS_CPSUI_TT_DOWNLOADSOFT = 64773;
        public const uint IDS_CPSUI_TT_DOWNLOADVECT = 64774;
        public const uint IDS_CPSUI_TT_SUBDEV = 64775;
        public const uint IDS_CPSUI_ICM_BLACKWHITE = 64776;
        public const uint IDS_CPSUI_ICM_NO = 64777;
        public const uint IDS_CPSUI_ICM_YES = 64778;
        public const uint IDS_CPSUI_ICM_SATURATION = 64779;
        public const uint IDS_CPSUI_ICM_CONTRAST = 64780;
        public const uint IDS_CPSUI_ICM_COLORMETRIC = 64781;
        public const uint IDS_CPSUI_STANDARD = 64782;
        public const uint IDS_CPSUI_GLOSSY = 64783;
        public const uint IDS_CPSUI_TRANSPARENCY = 64784;
        public const uint IDS_CPSUI_UPPER_TRAY = 64799;
        public const uint IDS_CPSUI_ONLYONE = 64800;
        public const uint IDS_CPSUI_LOWER_TRAY = 64801;
        public const uint IDS_CPSUI_MIDDLE_TRAY = 64802;
        public const uint IDS_CPSUI_MANUAL_TRAY = 64803;
        public const uint IDS_CPSUI_ENVELOPE_TRAY = 64804;
        public const uint IDS_CPSUI_ENVMANUAL_TRAY = 64805;
        public const uint IDS_CPSUI_TRACTOR_TRAY = 64806;
        public const uint IDS_CPSUI_SMALLFMT_TRAY = 64807;
        public const uint IDS_CPSUI_LARGEFMT_TRAY = 64808;
        public const uint IDS_CPSUI_LARGECAP_TRAY = 64809;
        public const uint IDS_CPSUI_CASSETTE_TRAY = 64810;
        public const uint IDS_CPSUI_DEFAULT_TRAY = 64811;
        public const uint IDS_CPSUI_FORMSOURCE = 64812;
        public const uint IDS_CPSUI_MANUALFEED = 64813;
        public const uint IDS_CPSUI_COPIES = 64831;
        public const uint IDS_CPSUI_QUALITY_SETTINGS = 64858;
        public const uint IDS_CPSUI_QUALITY_DRAFT = 64859;
        public const uint IDS_CPSUI_QUALITY_BETTER = 64860;
        public const uint IDS_CPSUI_QUALITY_BEST = 64861;
        public const uint IDS_CPSUI_QUALITY_CUSTOM = 64862;
        public const uint IDS_CPSUI_OUTPUTBIN = 64863;
        public const uint IDS_CPSUI_NUP = 64864;

        private const uint IDS_CPSUI_STRID_FIRST = 64700;
        private const uint IDS_CPSUI_STRID_LAST = 64873;
        private const LoadLibraryExFlags SafeLoadLibraryFlags = LoadLibraryExFlags.LOAD_LIBRARY_AS_IMAGE_RESOURCE | LoadLibraryExFlags.LOAD_LIBRARY_AS_DATAFILE_EXCLUSIVE;
        private const int MaxSRLength = 512;
    }
}
