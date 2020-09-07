// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.IO;
using System.Windows.Ink;

namespace MS.Internal.Ink.InkSerializedFormat
{
    /// <summary>
    ///    <para>[To be supplied.]</para>
    /// </summary>
    internal static class KnownIdCache
    {
        // This id table includes the original Guids that were hardcoded
        //      into ISF for the TabletPC v1 release
        public static Guid[] OriginalISFIdTable = {
            new Guid(0x598a6a8f, 0x52c0, 0x4ba0, 0x93, 0xaf, 0xaf, 0x35, 0x74, 0x11, 0xa5, 0x61),
            new Guid(0xb53f9f75, 0x04e0, 0x4498, 0xa7, 0xee, 0xc3, 0x0d, 0xbb, 0x5a, 0x90, 0x11),
            new Guid(0x735adb30, 0x0ebb, 0x4788, 0xa0, 0xe4, 0x0f, 0x31, 0x64, 0x90, 0x05, 0x5d), 
            new Guid(0x6e0e07bf, 0xafe7, 0x4cf7, 0x87, 0xd1, 0xaf, 0x64, 0x46, 0x20, 0x84, 0x18), 
            new Guid(0x436510c5, 0xfed3, 0x45d1, 0x8b, 0x76, 0x71, 0xd3, 0xea, 0x7a, 0x82, 0x9d), 
            new Guid(0x78a81b56, 0x0935, 0x4493, 0xba, 0xae, 0x00, 0x54, 0x1a, 0x8a, 0x16, 0xc4), 
            new Guid(0x7307502d, 0xf9f4, 0x4e18, 0xb3, 0xf2, 0x2c, 0xe1, 0xb1, 0xa3, 0x61, 0x0c), 
            new Guid(0x6da4488b, 0x5244, 0x41ec, 0x90, 0x5b, 0x32, 0xd8, 0x9a, 0xb8, 0x08, 0x09), 
            new Guid(0x8b7fefc4, 0x96aa, 0x4bfe, 0xac, 0x26, 0x8a, 0x5f, 0x0b, 0xe0, 0x7b, 0xf5), 
            new Guid(0xa8d07b3a, 0x8bf0, 0x40b0, 0x95, 0xa9, 0xb8, 0x0a, 0x6b, 0xb7, 0x87, 0xbf), 
            new Guid(0x0e932389, 0x1d77, 0x43af, 0xac, 0x00, 0x5b, 0x95, 0x0d, 0x6d, 0x4b, 0x2d), 
            new Guid(0x029123b4, 0x8828, 0x410b, 0xb2, 0x50, 0xa0, 0x53, 0x65, 0x95, 0xe5, 0xdc), 
            new Guid(0x82dec5c7, 0xf6ba, 0x4906, 0x89, 0x4f, 0x66, 0xd6, 0x8d, 0xfc, 0x45, 0x6c), 
            new Guid(0x0d324960, 0x13b2, 0x41e4, 0xac, 0xe6, 0x7a, 0xe9, 0xd4, 0x3d, 0x2d, 0x3b), 
            new Guid(0x7f7e57b7, 0xbe37, 0x4be1, 0xa3, 0x56, 0x7a, 0x84, 0x16, 0x0e, 0x18, 0x93), 
            new Guid(0x5d5d5e56, 0x6ba9, 0x4c5b, 0x9f, 0xb0, 0x85, 0x1c, 0x91, 0x71, 0x4e, 0x56), 
            new Guid(0x6a849980, 0x7c3a, 0x45b7, 0xaa, 0x82, 0x90, 0xa2, 0x62, 0x95, 0x0e, 0x89), 
            new Guid(0x33c1df83, 0xecdb, 0x44f0, 0xb9, 0x23, 0xdb, 0xd1, 0xa5, 0xb2, 0x13, 0x6e), 
            new Guid(0x5329cda5, 0xfa5b, 0x4ed2, 0xbb, 0x32, 0x83, 0x46, 0x01, 0x72, 0x44, 0x28), 
            new Guid(0x002df9af, 0xdd8c, 0x4949, 0xba, 0x46, 0xd6, 0x5e, 0x10, 0x7d, 0x1a, 0x8a), 
            new Guid(0x9d32b7ca, 0x1213, 0x4f54, 0xb7, 0xe4, 0xc9, 0x05, 0x0e, 0xe1, 0x7a, 0x38), 
            new Guid(0xe71caab9, 0x8059, 0x4c0d, 0xa2, 0xdb, 0x7c, 0x79, 0x54, 0x47, 0x8d, 0x82), 
            new Guid(0x5c0b730a, 0xf394, 0x4961, 0xa9, 0x33, 0x37, 0xc4, 0x34, 0xf4, 0xb7, 0xeb), 
            new Guid(0x2812210f, 0x871e, 0x4d91, 0x86, 0x07, 0x49, 0x32, 0x7d, 0xdf, 0x0a, 0x9f), 
            new Guid(0x8359a0fa, 0x2f44, 0x4de6, 0x92, 0x81, 0xce, 0x5a, 0x89, 0x9c, 0xf5, 0x8f), 
            new Guid(0x4c4642dd, 0x479e, 0x4c66, 0xb4, 0x40, 0x1f, 0xcd, 0x83, 0x95, 0x8f, 0x00), 
            new Guid(0xce2d9a8a, 0xe58e, 0x40ba, 0x93, 0xfa, 0x18, 0x9b, 0xb3, 0x90, 0x00, 0xae), 
            new Guid(0xc3c7480f, 0x5839, 0x46ef, 0xa5, 0x66, 0xd8, 0x48, 0x1c, 0x7a, 0xfe, 0xc1), 
            new Guid(0xea2278af, 0xc59d, 0x4ef4, 0x98, 0x5b, 0xd4, 0xbe, 0x12, 0xdf, 0x22, 0x34), 
            new Guid(0xb8630dc9, 0xcc5c, 0x4c33, 0x8d, 0xad, 0xb4, 0x7f, 0x62, 0x2b, 0x8c, 0x79), 
            new Guid(0x15e2f8e6, 0x6381, 0x4e8b, 0xa9, 0x65, 0x01, 0x1f, 0x7d, 0x7f, 0xca, 0x38), 
            new Guid(0x7066fbe4, 0x473e, 0x4675, 0x9c, 0x25, 0x00, 0x26, 0x82, 0x9b, 0x40, 0x1f), 
            new Guid(0xbbc85b9a, 0xade6, 0x4093, 0xb3, 0xbb, 0x64, 0x1f, 0xa1, 0xd3, 0x7a, 0x1a), 
            new Guid(0x39143d3, 0x78cb, 0x449c, 0xa8, 0xe7, 0x67, 0xd1, 0x88, 0x64, 0xc3, 0x32), 
            new Guid(0x67743782, 0xee5, 0x419a, 0xa1, 0x2b, 0x27, 0x3a, 0x9e, 0xc0, 0x8f, 0x3d), 
            new Guid(0xf0720328, 0x663b, 0x418f, 0x85, 0xa6, 0x95, 0x31, 0xae, 0x3e, 0xcd, 0xfa), 
            new Guid(0xa1718cdd, 0xdac, 0x4095, 0xa1, 0x81, 0x7b, 0x59, 0xcb, 0x10, 0x6b, 0xfb), 
            new Guid(0x810a74d2, 0x6ee2, 0x4e39, 0x82, 0x5e, 0x6d, 0xef, 0x82, 0x6a, 0xff, 0xc5),
        };

        // Size of data used by identified by specified Guid/Id
        public static uint[] OriginalISFIdPersistenceSize = {
                Native.SizeOfInt,           // X                         0
                Native.SizeOfInt,           // Y                         1
                Native.SizeOfInt,           // Z                         2
                Native.SizeOfInt,           // PACKET_STATUS             3
                2 * Native.SizeOfUInt,      // FILETIME : TIMER_TICK     4
                Native.SizeOfUInt,          // SERIAL_NUMBER             5
                Native.SizeOfUShort,        // NORMAL_PRESSURE           6
                Native.SizeOfUShort,        // TANGENT_PRESSURE          7
                Native.SizeOfUShort,        // BUTTON_PRESSURE           8
                Native.SizeOfFloat,         // X_TILT_ORIENTATION        9
                Native.SizeOfFloat,         // Y_TILT_ORIENTATION        10
                Native.SizeOfFloat,         // AZIMUTH_ORIENTATION       11
                Native.SizeOfInt,           // ALTITUDE_ORIENTATION      12
                Native.SizeOfInt,           // TWIST_ORIENTATION         13
                Native.SizeOfUShort,        // PITCH_ROTATION            14
                Native.SizeOfUShort,        // ROLL_ROTATION             15
                Native.SizeOfUShort,        // YAW_ROTATION              16
                Native.SizeOfUShort,        // PEN_STYLE                 17
                Native.SizeOfUInt,          // COLORREF: COLORREF        18
                Native.SizeOfUInt,          // PEN_WIDTH                 19
                Native.SizeOfUInt,          // PEN_HEIGHT                20
                Native.SizeOfByte,          // PEN_TIP                   21
                Native.SizeOfUInt,          // DRAWING_FLAGS             22
                Native.SizeOfUInt,          // CURSORID                  23
                0,                          // WORD_ALTERNATES           24
                0,                          // CHAR_ALTERNATES           25
                5 * Native.SizeOfUInt,      // INKMETRICS                26
                3 * Native.SizeOfUInt,      // GUIDE_STRUCTURE           27
                8 * Native.SizeOfUShort,    // SYSTEMTIME TIME_STAMP     28
                Native.SizeOfUShort,        // LANGUAGE                  29
                Native.SizeOfByte,          // TRANSPARENCY              30
                Native.SizeOfUInt,          // CURVE_FITTING_ERROR       31
                0,                          // RECO_LATTICE              32
                Native.SizeOfInt,           // CURSORDOWN                33
                Native.SizeOfInt,           // SECONDARYTIPSWITCH        34
                Native.SizeOfInt,           // BARRELDOWN                35
                Native.SizeOfInt,           // TABLETPICK                36
                Native.SizeOfInt,           // ROP                       37
            };

        public enum OriginalISFIdIndex : uint
        {
            X = 0,
            Y = 1,
            Z = 2,
            PacketStatus = 3,
            TimerTick = 4,
            SerialNumber = 5,
            NormalPressure = 6,
            TangentPressure = 7,
            ButtonPressure = 8,
            XTiltOrientation = 9,
            YTiltOrientation = 10,
            AzimuthOrientation = 11,
            AltitudeOrientation = 12,
            TwistOrientation = 13,
            PitchRotation = 14,
            RollRotation = 15,
            YawRotation = 16,
            PenStyle = 17,
            ColorRef = 18,
            StylusWidth = 19,
            StylusHeight = 20,
            PenTip = 21,
            DrawingFlags = 22,
            CursorId = 23,
            WordAlternates = 24,
            CharAlternates = 25,
            InkMetrics = 26,
            GuideStructure = 27,
            Timestamp = 28,
            Language = 29,
            Transparency = 30,
            CurveFittingError = 31,
            RecoLattice = 32,
            CursorDown = 33,
            SecondaryTipSwitch = 34,
            BarrelDown = 35,
            TabletPick = 36,
            RasterOperation = 37,
            MAXIMUM = 37,
        }

        // This id table includes the Guids that used the internal persistence APIs
        //      - meaning they didn't have the data type information encoded in ISF
        public static Guid[] TabletInternalIdTable = {
                // Highlighter
            new Guid(0x9b6267b8, 0x3968, 0x4048, 0xab, 0x74, 0xf4, 0x90, 0x40, 0x6a, 0x2d, 0xfa),
                // Ink properties
            new Guid(0x7fc30e91, 0xd68d, 0x4f07, 0x8b, 0x62, 0x6, 0xf6, 0xd2, 0x73, 0x1b, 0xed),
                // Ink Style Bold
            new Guid(0xe02fb5c1, 0x9693, 0x4312, 0xa4, 0x34, 0x0, 0xde, 0x7f, 0x3a, 0xd4, 0x93),
                // Ink Style Italics
            new Guid(0x5253b51, 0x49c6, 0x4a04, 0x89, 0x93, 0x64, 0xdd, 0x9a, 0xbd, 0x84, 0x2a),
                // Stroke Timestamp
            new Guid(0x4ea66c4, 0xf33a, 0x461b, 0xb8, 0xfe, 0x68, 0x7, 0xd, 0x9c, 0x75, 0x75),
                // Stroke Time Id
            new Guid(0x50b6bc8, 0x3b7d, 0x4816, 0x8c, 0x61, 0xbc, 0x7e, 0x90, 0x5b, 0x21, 0x32),
                // Stroke Lattice
            new Guid(0x82871c85, 0xe247, 0x4d8c, 0x8d, 0x71, 0x22, 0xe5, 0xd6, 0xf2, 0x57, 0x76),
                // Ink Custom Strokes
            new Guid(0x33cdbbb3, 0x588f, 0x4e94, 0xb1, 0xfe, 0x5d, 0x79, 0xff, 0xe7, 0x6e, 0x76),
        };
            // lookup indices for table of GUIDs used with non-Automation APIs
        internal enum TabletInternalIdIndex
        {
            Highlighter = 0,
            InkProperties = 1,
            InkStyleBold = 2,
            InkStyleItalics = 3,
            StrokeTimestamp = 4,
            StrokeTimeId = 5,
            InkStrokeLattice = 6,
            InkCustomStrokes = 7,
            MAXIMUM = 7
        }

        static internal KnownTagCache.KnownTagIndex KnownGuidBaseIndex = (KnownTagCache.KnownTagIndex)KnownTagCache.MaximumPossibleKnownTags;

            // The maximum value that can be encoded into a single byte is 127.
            // To improve the chances of storing all of the guids in the ISF guid table
            //      with single-byte lookups, the guids are broken into two ranges
            // 0-50 known tags
            // 50-100 known guids (reserved)
            // 101-127 custom guids (user-defined guids)
            // 128-... more custom guids, but requiring multiples bytes for guid table lookup

            // These values aren't currently used, so comment them out
        // static internal uint KnownGuidIndexLimit = MaximumPossibleKnownGuidIndex;
        static internal uint MaximumPossibleKnownGuidIndex = 100;
        static internal uint CustomGuidBaseIndex = MaximumPossibleKnownGuidIndex;

        // This id table includes the Guids that have been added to ISF as ExtendedProperties
        //      Note that they are visible to 3rd party applications
        public static Guid[] ExtendedISFIdTable = {
                // Highlighter
                new Guid(0x9b6267b8, 0x3968, 0x4048, 0xab, 0x74, 0xf4, 0x90, 0x40, 0x6a, 0x2d, 0xfa),
                // Ink properties
                new Guid(0x7fc30e91, 0xd68d, 0x4f07, 0x8b, 0x62, 0x6, 0xf6, 0xd2, 0x73, 0x1b, 0xed),
                // Ink Style Bold
                new Guid(0xe02fb5c1, 0x9693, 0x4312, 0xa4, 0x34, 0x0, 0xde, 0x7f, 0x3a, 0xd4, 0x93),
                // Ink Style Italics
                new Guid(0x5253b51, 0x49c6, 0x4a04, 0x89, 0x93, 0x64, 0xdd, 0x9a, 0xbd, 0x84, 0x2a),
                // Stroke Timestamp
                new Guid(0x4ea66c4, 0xf33a, 0x461b, 0xb8, 0xfe, 0x68, 0x7, 0xd, 0x9c, 0x75, 0x75),
                // Stroke Time Id
                new Guid(0x50b6bc8, 0x3b7d, 0x4816, 0x8c, 0x61, 0xbc, 0x7e, 0x90, 0x5b, 0x21, 0x32),
                // Stroke Lattice
                new Guid(0x82871c85, 0xe247, 0x4d8c, 0x8d, 0x71, 0x22, 0xe5, 0xd6, 0xf2, 0x57, 0x76),
                // Ink Custom Strokes
                new Guid(0x33cdbbb3, 0x588f, 0x4e94, 0xb1, 0xfe, 0x5d, 0x79, 0xff, 0xe7, 0x6e, 0x76),
        };
    }
    internal static class KnownTagCache
    {
        internal enum KnownTagIndex : uint
        {
            Unknown = 0,
            InkSpaceRectangle = 0,
            GuidTable = 1,
            DrawingAttributesTable = 2,
            DrawingAttributesBlock = 3,
            StrokeDescriptorTable = 4,
            StrokeDescriptorBlock = 5,
            Buttons = 6,
            NoX = 7,
            NoY = 8,
            DrawingAttributesTableIndex = 9,
            Stroke = 10,
            StrokePropertyList = 11,
            PointProperty = 12,
            StrokeDescriptorTableIndex = 13,
            CompressionHeader = 14,
            TransformTable = 15,
            Transform = 16,
            TransformIsotropicScale = 17,
            TransformAnisotropicScale = 18,
            TransformRotate = 19,
            TransformTranslate = 20,
            TransformScaleAndTranslate = 21,
            TransformQuad = 22,
            TransformTableIndex = 23,
            MetricTable = 24,
            MetricBlock = 25,
            MetricTableIndex = 26,
            Mantissa = 27,
            PersistenceFormat = 28,
            HimetricSize = 29,
            StrokeIds = 30,
            ExtendedTransformTable = 31,
        }

            // See comments for KnownGuidBaseIndex to determine ranges of tags/guids/indices
        static internal uint MaximumPossibleKnownTags = 50;
        static internal uint KnownTagCount = (byte)MaximumPossibleKnownTags;
    }
}
