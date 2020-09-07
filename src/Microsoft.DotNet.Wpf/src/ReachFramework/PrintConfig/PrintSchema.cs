// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++


Abstract:

    Definition of public Print Schema types.


--*/

using System;
using System.Diagnostics;
using System.IO;
using System.Xml;

#pragma warning disable 1634, 1691 // Allows suppression of certain PreSharp messages

/*  How to add new Print Schema standard feature keyword support in the WinFX APIs?
    - add new feature keyword to CapabilityName enum
    - add new enum definition of PrintSchema.<new_feature_keyword>Value with the feature's standard option keywords
    - add new internal static <new_feature_keyword>ValueEnumMin/Max
    - add new internal class PrintSchemaTags.Keywords.<new_feature_keyword>Keys that defines:
        - const strings for feature keyword and property/scored property names
        - arrays of standard option keyword names and enums
    - sd add a new <feature>.cs file to implement <feature>Option/OptionCollection/Capability/Setting classes
    - in "sources", add the new <feature>.cs file to build
    - add new MapEntry for the new feature into PrintSchemaTags.Keywords.FeatureMapTable
    - in PrintCapBuilder class, add new FeatureHandlersTableEntry for the new feature into private _fHandlersTable
    - in PrintCapabilities class, add new public property <feature>Capability to return the new feature's capability object
    - in PrintTicket class, add new public property <feature> to return the new feature's setting object
*/

using System.Printing;

namespace System.Printing
{
    // MUST READ BEFORE MAKING CHANGES TO THE ENUM TYPES
    //
    // All public enum types should have the "Unknown" enum value defined and make it equal to 0.
    //

    #region Public Enum Types

    /// <summary>
    /// Specifies Print Schema's standard collating option.
    /// </summary>
    public enum Collation
    {
        /// <summary>
        /// Collation setting is unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Collating.
        /// </summary>
        Collated,

        /// <summary>
        /// No collating.
        /// </summary>
        Uncollated,
    }

    /// <summary>
    /// Specifies Print Schema's standard device font substitution option.
    /// </summary>
    public enum DeviceFontSubstitution
    {
        /// <summary>
        /// Device font substitution setting is unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Device font substitution disabled.
        /// </summary>
        Off,

        /// <summary>
        /// Device font substitution enabled.
        /// </summary>
        On,
    }

    /// <summary>
    /// Specifies Print Schema's standard duplexing option.
    /// </summary>
    public enum Duplexing
    {
        /// <summary>
        /// Duplexing setting is unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// One sided printing.
        /// </summary>
        OneSided,

        /// <summary>
        /// Two sided printing such that the page is flipped parallel to the MediaSizeWidth direction.
        /// </summary>
        TwoSidedShortEdge,

        /// <summary>
        /// Two sided printing such that the page is flipped parallel to the MediaSizeHeight direction.
        /// </summary>
        TwoSidedLongEdge,
    }

    /// <summary>
    /// Specifies Print Schema's standard MediaSize name.
    /// </summary>
    public enum PageMediaSizeName
    {
        /// <summary>
        /// MediaSize setting is unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        ///
        /// </summary>
        ISOA0,
        /// <summary>
        ///
        /// </summary>
        ISOA1,
        /// <summary>
        ///
        /// </summary>
        ISOA10,
        /// <summary>
        ///
        /// </summary>
        ISOA2,
        /// <summary>
        ///
        /// </summary>
        ISOA3,
        /// <summary>
        ///
        /// </summary>
        ISOA3Rotated,
        /// <summary>
        ///
        /// </summary>
        ISOA3Extra,
        /// <summary>
        ///
        /// </summary>
        ISOA4,
        /// <summary>
        ///
        /// </summary>
        ISOA4Rotated,
        /// <summary>
        ///
        /// </summary>
        ISOA4Extra,
        /// <summary>
        ///
        /// </summary>
        ISOA5,
        /// <summary>
        ///
        /// </summary>
        ISOA5Rotated,
        /// <summary>
        ///
        /// </summary>
        ISOA5Extra,
        /// <summary>
        ///
        /// </summary>
        ISOA6,
        /// <summary>
        ///
        /// </summary>
        ISOA6Rotated,
        /// <summary>
        ///
        /// </summary>
        ISOA7,
        /// <summary>
        ///
        /// </summary>
        ISOA8,
        /// <summary>
        ///
        /// </summary>
        ISOA9,
        /// <summary>
        ///
        /// </summary>
        ISOB0,
        /// <summary>
        ///
        /// </summary>
        ISOB1,
        /// <summary>
        ///
        /// </summary>
        ISOB10,
        /// <summary>
        ///
        /// </summary>
        ISOB2,
        /// <summary>
        ///
        /// </summary>
        ISOB3,
        /// <summary>
        ///
        /// </summary>
        ISOB4,
        /// <summary>
        ///
        /// </summary>
        ISOB4Envelope,
        /// <summary>
        ///
        /// </summary>
        ISOB5Envelope,
        /// <summary>
        ///
        /// </summary>
        ISOB5Extra,
        /// <summary>
        ///
        /// </summary>
        ISOB7,
        /// <summary>
        ///
        /// </summary>
        ISOB8,
        /// <summary>
        ///
        /// </summary>
        ISOB9,
        /// <summary>
        ///
        /// </summary>
        ISOC0,
        /// <summary>
        ///
        /// </summary>
        ISOC1,
        /// <summary>
        ///
        /// </summary>
        ISOC10,
        /// <summary>
        ///
        /// </summary>
        ISOC2,
        /// <summary>
        ///
        /// </summary>
        ISOC3,
        /// <summary>
        ///
        /// </summary>
        ISOC3Envelope,
        /// <summary>
        ///
        /// </summary>
        ISOC4,
        /// <summary>
        ///
        /// </summary>
        ISOC4Envelope,
        /// <summary>
        ///
        /// </summary>
        ISOC5,
        /// <summary>
        ///
        /// </summary>
        ISOC5Envelope,
        /// <summary>
        ///
        /// </summary>
        ISOC6,
        /// <summary>
        ///
        /// </summary>
        ISOC6Envelope,
        /// <summary>
        ///
        /// </summary>
        ISOC6C5Envelope,
        /// <summary>
        ///
        /// </summary>
        ISOC7,
        /// <summary>
        ///
        /// </summary>
        ISOC8,
        /// <summary>
        ///
        /// </summary>
        ISOC9,
        /// <summary>
        ///
        /// </summary>
        ISODLEnvelope,
        /// <summary>
        ///
        /// </summary>
        ISODLEnvelopeRotated,
        /// <summary>
        ///
        /// </summary>
        ISOSRA3,
        /// <summary>
        ///
        /// </summary>
        JapanQuadrupleHagakiPostcard,
        /// <summary>
        ///
        /// </summary>
        JISB0,
        /// <summary>
        ///
        /// </summary>
        JISB1,
        /// <summary>
        ///
        /// </summary>
        JISB10,
        /// <summary>
        ///
        /// </summary>
        JISB2,
        /// <summary>
        ///
        /// </summary>
        JISB3,
        /// <summary>
        ///
        /// </summary>
        JISB4,
        /// <summary>
        ///
        /// </summary>
        JISB4Rotated,
        /// <summary>
        ///
        /// </summary>
        JISB5,
        /// <summary>
        ///
        /// </summary>
        JISB5Rotated,
        /// <summary>
        ///
        /// </summary>
        JISB6,
        /// <summary>
        ///
        /// </summary>
        JISB6Rotated,
        /// <summary>
        ///
        /// </summary>
        JISB7,
        /// <summary>
        ///
        /// </summary>
        JISB8,
        /// <summary>
        ///
        /// </summary>
        JISB9,
        /// <summary>
        ///
        /// </summary>
        JapanChou3Envelope,
        /// <summary>
        ///
        /// </summary>
        JapanChou3EnvelopeRotated,
        /// <summary>
        ///
        /// </summary>
        JapanChou4Envelope,
        /// <summary>
        ///
        /// </summary>
        JapanChou4EnvelopeRotated,
        /// <summary>
        ///
        /// </summary>
        JapanHagakiPostcard,
        /// <summary>
        ///
        /// </summary>
        JapanHagakiPostcardRotated,
        /// <summary>
        ///
        /// </summary>
        JapanKaku2Envelope,
        /// <summary>
        ///
        /// </summary>
        JapanKaku2EnvelopeRotated,
        /// <summary>
        ///
        /// </summary>
        JapanKaku3Envelope,
        /// <summary>
        ///
        /// </summary>
        JapanKaku3EnvelopeRotated,
        /// <summary>
        ///
        /// </summary>
        JapanYou4Envelope,
        /// <summary>
        ///
        /// </summary>
        NorthAmerica10x11,
        /// <summary>
        ///
        /// </summary>
        NorthAmerica10x14,
        /// <summary>
        ///
        /// </summary>
        NorthAmerica11x17,
        /// <summary>
        ///
        /// </summary>
        NorthAmerica9x11,
        /// <summary>
        ///
        /// </summary>
        NorthAmericaArchitectureASheet,
        /// <summary>
        ///
        /// </summary>
        NorthAmericaArchitectureBSheet,
        /// <summary>
        ///
        /// </summary>
        NorthAmericaArchitectureCSheet,
        /// <summary>
        ///
        /// </summary>
        NorthAmericaArchitectureDSheet,
        /// <summary>
        ///
        /// </summary>
        NorthAmericaArchitectureESheet,
        /// <summary>
        ///
        /// </summary>
        NorthAmericaCSheet,
        /// <summary>
        ///
        /// </summary>
        NorthAmericaDSheet,
        /// <summary>
        ///
        /// </summary>
        NorthAmericaESheet,
        /// <summary>
        ///
        /// </summary>
        NorthAmericaExecutive,
        /// <summary>
        ///
        /// </summary>
        NorthAmericaGermanLegalFanfold,
        /// <summary>
        ///
        /// </summary>
        NorthAmericaGermanStandardFanfold,
        /// <summary>
        ///
        /// </summary>
        NorthAmericaLegal,
        /// <summary>
        ///
        /// </summary>
        NorthAmericaLegalExtra,
        /// <summary>
        ///
        /// </summary>
        NorthAmericaLetter,
        /// <summary>
        ///
        /// </summary>
        NorthAmericaLetterRotated,
        /// <summary>
        ///
        /// </summary>
        NorthAmericaLetterExtra,
        /// <summary>
        ///
        /// </summary>
        NorthAmericaLetterPlus,
        /// <summary>
        ///
        /// </summary>
        NorthAmericaMonarchEnvelope,
        /// <summary>
        ///
        /// </summary>
        NorthAmericaNote,
        /// <summary>
        ///
        /// </summary>
        NorthAmericaNumber10Envelope,
        /// <summary>
        ///
        /// </summary>
        NorthAmericaNumber10EnvelopeRotated,
        /// <summary>
        ///
        /// </summary>
        NorthAmericaNumber9Envelope,
        /// <summary>
        ///
        /// </summary>
        NorthAmericaNumber11Envelope,
        /// <summary>
        ///
        /// </summary>
        NorthAmericaNumber12Envelope,
        /// <summary>
        ///
        /// </summary>
        NorthAmericaNumber14Envelope,
        /// <summary>
        ///
        /// </summary>
        NorthAmericaPersonalEnvelope,
        /// <summary>
        ///
        /// </summary>
        NorthAmericaQuarto,
        /// <summary>
        ///
        /// </summary>
        NorthAmericaStatement,
        /// <summary>
        ///
        /// </summary>
        NorthAmericaSuperA,
        /// <summary>
        ///
        /// </summary>
        NorthAmericaSuperB,
        /// <summary>
        ///
        /// </summary>
        NorthAmericaTabloid,
        /// <summary>
        ///
        /// </summary>
        NorthAmericaTabloidExtra,
        /// <summary>
        ///
        /// </summary>
        OtherMetricA4Plus,
        /// <summary>
        ///
        /// </summary>
        OtherMetricA3Plus,
        /// <summary>
        ///
        /// </summary>
        OtherMetricFolio,
        /// <summary>
        ///
        /// </summary>
        OtherMetricInviteEnvelope,
        /// <summary>
        ///
        /// </summary>
        OtherMetricItalianEnvelope,
        /// <summary>
        ///
        /// </summary>
        PRC1Envelope,
        /// <summary>
        ///
        /// </summary>
        PRC1EnvelopeRotated,
        /// <summary>
        ///
        /// </summary>
        PRC10Envelope,
        /// <summary>
        ///
        /// </summary>
        PRC10EnvelopeRotated,
        /// <summary>
        ///
        /// </summary>
        PRC16K,
        /// <summary>
        ///
        /// </summary>
        PRC16KRotated,
        /// <summary>
        ///
        /// </summary>
        PRC2Envelope,
        /// <summary>
        ///
        /// </summary>
        PRC2EnvelopeRotated,
        /// <summary>
        ///
        /// </summary>
        PRC32K,
        /// <summary>
        ///
        /// </summary>
        PRC32KRotated,
        /// <summary>
        ///
        /// </summary>
        PRC32KBig,
        /// <summary>
        ///
        /// </summary>
        PRC3Envelope,
        /// <summary>
        ///
        /// </summary>
        PRC3EnvelopeRotated,
        /// <summary>
        ///
        /// </summary>
        PRC4Envelope,
        /// <summary>
        ///
        /// </summary>
        PRC4EnvelopeRotated,
        /// <summary>
        ///
        /// </summary>
        PRC5Envelope,
        /// <summary>
        ///
        /// </summary>
        PRC5EnvelopeRotated,
        /// <summary>
        ///
        /// </summary>
        PRC6Envelope,
        /// <summary>
        ///
        /// </summary>
        PRC6EnvelopeRotated,
        /// <summary>
        ///
        /// </summary>
        PRC7Envelope,
        /// <summary>
        ///
        /// </summary>
        PRC7EnvelopeRotated,
        /// <summary>
        ///
        /// </summary>
        PRC8Envelope,
        /// <summary>
        ///
        /// </summary>
        PRC8EnvelopeRotated,
        /// <summary>
        ///
        /// </summary>
        PRC9Envelope,
        /// <summary>
        ///
        /// </summary>
        PRC9EnvelopeRotated,
        /// <summary>
        ///
        /// </summary>
        Roll04Inch,
        /// <summary>
        ///
        /// </summary>
        Roll06Inch,
        /// <summary>
        ///
        /// </summary>
        Roll08Inch,
        /// <summary>
        ///
        /// </summary>
        Roll12Inch,
        /// <summary>
        ///
        /// </summary>
        Roll15Inch,
        /// <summary>
        ///
        /// </summary>
        Roll18Inch,
        /// <summary>
        ///
        /// </summary>
        Roll22Inch,
        /// <summary>
        ///
        /// </summary>
        Roll24Inch,
        /// <summary>
        ///
        /// </summary>
        Roll30Inch,
        /// <summary>
        ///
        /// </summary>
        Roll36Inch,
        /// <summary>
        ///
        /// </summary>
        Roll54Inch,
        /// <summary>
        ///
        /// </summary>
        JapanDoubleHagakiPostcard,
        /// <summary>
        ///
        /// </summary>
        JapanDoubleHagakiPostcardRotated,
        /// <summary>
        ///
        /// </summary>
        JapanLPhoto,
        /// <summary>
        ///
        /// </summary>
        Japan2LPhoto,
        /// <summary>
        ///
        /// </summary>
        JapanYou1Envelope,
        /// <summary>
        ///
        /// </summary>
        JapanYou2Envelope,
        /// <summary>
        ///
        /// </summary>
        JapanYou3Envelope,
        /// <summary>
        ///
        /// </summary>
        JapanYou4EnvelopeRotated,
        /// <summary>
        ///
        /// </summary>
        JapanYou6Envelope,
        /// <summary>
        ///
        /// </summary>
        JapanYou6EnvelopeRotated,
        /// <summary>
        ///
        /// </summary>
        NorthAmerica4x6,
        /// <summary>
        ///
        /// </summary>
        NorthAmerica4x8,
        /// <summary>
        ///
        /// </summary>
        NorthAmerica5x7,
        /// <summary>
        ///
        /// </summary>
        NorthAmerica8x10,
        /// <summary>
        ///
        /// </summary>
        NorthAmerica10x12,
        /// <summary>
        ///
        /// </summary>
        NorthAmerica14x17,
        /// <summary>
        ///
        /// </summary>
        BusinessCard,
        /// <summary>
        ///
        /// </summary>
        CreditCard,
    }

    /// <summary>
    /// Specifies Print Schema's standard MediaType.
    /// </summary>
    public enum PageMediaType
    {
        /// <summary>
        /// MediaType setting is unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Media will be Automatically selected.
        /// </summary>
        AutoSelect,

        /// <summary>
        /// Archival quality media.
        /// </summary>
        Archival,

        /// <summary>
        /// Speciality back printing film media.
        /// </summary>
        BackPrintFilm,

        /// <summary>
        /// Standard bond media.
        /// </summary>
        Bond,

        /// <summary>
        /// Standard card stock media.
        /// </summary>
        CardStock,

        /// <summary>
        /// Continuous feed media.
        /// </summary>
        Continuous,

        /// <summary>
        /// Standard envelope media.
        /// </summary>
        EnvelopePlain,

        /// <summary>
        /// Windowed envelope media.
        /// </summary>
        EnvelopeWindow,

        /// <summary>
        /// Fabric media.
        /// </summary>
        Fabric,

        /// <summary>
        /// Specialty high resolution media.
        /// </summary>
        HighResolution,

        /// <summary>
        /// Label media.
        /// </summary>
        Label,

        /// <summary>
        /// Attached multi-part forms media.
        /// </summary>
        MultiLayerForm,

        /// <summary>
        /// Separate multi-part forms media.
        /// </summary>
        MultiPartForm,

        /// <summary>
        /// Standard photographic media.
        /// </summary>
        Photographic,

        /// <summary>
        /// Film photographic media.
        /// </summary>
        PhotographicFilm,

        /// <summary>
        /// Glossy photographic media.
        /// </summary>
        PhotographicGlossy,

        /// <summary>
        /// High gloss photographic media.
        /// </summary>
        PhotographicHighGloss,

        /// <summary>
        /// Matte photographic media.
        /// </summary>
        PhotographicMatte,

        /// <summary>
        /// Satin photographic media.
        /// </summary>
        PhotographicSatin,

        /// <summary>
        /// Semi-gloss photographic media.
        /// </summary>
        PhotographicSemiGloss,

        /// <summary>
        /// Standard paper media.
        /// </summary>
        Plain,

        /// <summary>
        /// Output to a output display in continuous form.
        /// </summary>
        Screen,

        /// <summary>
        /// Output to a output display in paged form.
        /// </summary>
        ScreenPaged,

        /// <summary>
        /// Specialty stationery media.
        /// </summary>
        Stationery,

        /// <summary>
        /// Tab stock media that is not pre-cut (single tabs).
        /// </summary>
        TabStockFull,

        /// <summary>
        /// Tab stock media that is pre-cut (multiple tabs).
        /// </summary>
        TabStockPreCut,

        /// <summary>
        /// Transparency media.
        /// </summary>
        Transparency,

        /// <summary>
        /// Specialty T-shirt transfer media.
        /// </summary>
        TShirtTransfer,

        /// <summary>
        /// Unknown or unlisted media.
        /// </summary>
        None,
    }

    /// <summary>
    /// Specifies Print Schema's standard pages-per-sheet presentation direction option.
    /// </summary>
    public enum PagesPerSheetDirection
    {
        /// <summary>
        /// Presentation direction setting is unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Left to right, top to bottom presentation direction relative to orientation.
        /// </summary>
        RightBottom,

        /// <summary>
        /// Top to bottom, left to right presentation direction relative to orientation.
        /// </summary>
        BottomRight,

        /// <summary>
        /// Right to left, top to bottom presentation direction relative to orientation.
        /// </summary>
        LeftBottom,

        /// <summary>
        /// Top to bottom, right to left presentation direction relative to orientation.
        /// </summary>
        BottomLeft,

        /// <summary>
        /// Left to right, bottom to top presentation direction relative to orientation.
        /// </summary>
        RightTop,

        /// <summary>
        /// Bottom to top, left to right presentation direction relative to orientation.
        /// </summary>
        TopRight,

        /// <summary>
        /// Right to left, bottom to top presentation direction relative to orientation.
        /// </summary>
        LeftTop,

        /// <summary>
        /// Bottom to top, right to left presentation direction relative to orientation.
        /// </summary>
        TopLeft,
    }

    /// <summary>
    /// Specifies Print Schema's standard color setting of the output.
    /// </summary>
    public enum OutputColor
    {
        /// <summary>
        /// Output color setting is unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Output should be in color.
        /// </summary>
        Color,

        /// <summary>
        /// Output should be in grayscale
        /// </summary>
        Grayscale,

        /// <summary>
        /// Output should be in monochrome.
        /// </summary>
        Monochrome,
    }

    /// <summary>
    /// Specifies Print Schema's standard orientation of the media sheet.
    /// </summary>
    public enum PageOrientation
    {
        /// <summary>
        /// Orientation setting is unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Landscape orientation.
        /// </summary>
        Landscape,

        /// <summary>
        /// Portrait orientation.
        /// </summary>
        Portrait,

        /// <summary>
        /// Landscape orientation rotated 180 degrees relative to the Landscape option.
        /// </summary>
        ReverseLandscape,

        /// <summary>
        /// Portrait orientation rotated 180 degrees relative to the Portrait option.
        /// </summary>
        ReversePortrait,
    }

    /// <summary>
    /// Specifies Print Schema's standard page resolution quality label.
    /// </summary>
    public enum PageQualitativeResolution
    {
        /// <summary>
        /// Resolution quality label setting is unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Resolution default quality label.
        /// </summary>
        Default,

        /// <summary>
        /// Resolution draft quality label.
        /// </summary>
        Draft,

        /// <summary>
        /// Resolution high quality label.
        /// </summary>
        High,

        /// <summary>
        /// Resolution normal quality label.
        /// </summary>
        Normal,

        /// <summary>
        /// Resolution other quality label.
        /// </summary>
        Other,
    }

    /// <summary>
    /// Specifies Print Schema's standard stapling characteristics of the output.
    /// </summary>
    public enum Stapling
    {
        /// <summary>
        /// Stapling setting is unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Saddle stitch stapling.
        /// </summary>
        SaddleStitch,

        /// <summary>
        /// A single staple in the bottom, left corner.
        /// </summary>
        StapleBottomLeft,

        /// <summary>
        /// A single staple in the bottom, right corner.
        /// </summary>
        StapleBottomRight,

        /// <summary>
        /// Two staples along the left edge.
        /// </summary>
        StapleDualLeft,

        /// <summary>
        /// Two staples along the right edge.
        /// </summary>
        StapleDualRight,

        /// <summary>
        /// Two staples along the top edge.
        /// </summary>
        StapleDualTop,

        /// <summary>
        /// Two staples along the bottom edge.
        /// </summary>
        StapleDualBottom,

        /// <summary>
        /// A single staple in the top, left corner.
        /// </summary>
        StapleTopLeft,

        /// <summary>
        /// A single staple in the top, right corner.
        /// </summary>
        StapleTopRight,

        /// <summary>
        /// No stapling.
        /// </summary>
        None,
    }
    /// <summary>
    /// Specifies Print Schema's standard method of TrueType font handling.
    /// </summary>
    public enum TrueTypeFontMode
    {
        /// <summary>
        /// TrueType font handling setting is unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Leave it to system to choose the most optimized TrueType font handling mode.
        /// </summary>
        Automatic,

        /// <summary>
        /// Download TrueType font as outline soft font.
        /// </summary>
        DownloadAsOutlineFont,

        /// <summary>
        /// Download TrueType font as bitmap soft font.
        /// </summary>
        DownloadAsRasterFont,

        /// <summary>
        /// Download TrueType font as device native TrueType soft font.
        /// </summary>
        DownloadAsNativeTrueTypeFont,

        /// <summary>
        /// Print TrueType font as graphics.
        /// </summary>
        RenderAsBitmap,
    }
    /// <summary>
    /// Specifies Print Schema's standard ordering of pages.
    /// </summary>
    public enum PageOrder
    {
        /// <summary>
        /// Page order setting is unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Front to back page order.
        /// </summary>
        Standard,

        /// <summary>
        /// Back to front page order.
        /// </summary>
        Reverse,
    }

    /// <summary>
    /// Specifies Print Schema's standard photo printing intent.
    /// </summary>
    public enum PhotoPrintingIntent
    {
        /// <summary>
        /// Photo printing intent setting is unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// No photo printing intent.
        /// </summary>
        None,

        /// <summary>
        /// Best quality photo printing intent.
        /// </summary>
        PhotoBest,

        /// <summary>
        /// Draft quality photo printing intent.
        /// </summary>
        PhotoDraft,

        /// <summary>
        /// Standard quality photo printing intent.
        /// </summary>
        PhotoStandard,
    }
    /// <summary>
    /// Specifies Print Schema's standard borderless characteristics.
    /// </summary>
    public enum PageBorderless
    {
        /// <summary>
        /// Borderless setting is unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Borderless printing.
        /// </summary>
        Borderless,

        /// <summary>
        /// No borderless printing.
        /// </summary>
        None,
    }

    /// <summary>
    /// Specifies Print Schema's standard output quality.
    /// </summary>
    public enum OutputQuality
    {
        /// <summary>
        /// Output quality setting is unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Intent for automatic output.
        /// </summary>
        Automatic,

        /// <summary>
        /// Intent for draft output.
        /// </summary>
        Draft,

        /// <summary>
        /// Intent for fax output.
        /// </summary>
        Fax,

        /// <summary>
        /// Intent for high quality output.
        /// </summary>
        High,

        /// <summary>
        /// Intent for normal output.
        /// </summary>
        Normal,

        /// <summary>
        /// Intent for photographic output.
        /// </summary>
        Photographic,

        /// <summary>
        /// Intent for text only output.
        /// </summary>
        Text,
    }

    /// <summary>
    /// Specifies Print Schema's standard input bin.
    /// </summary>
    public enum InputBin
    {
        /// <summary>
        /// Input bin setting is unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Device will automatically choose best option based on configuration.
        /// </summary>
        AutoSelect,

        /// <summary>
        /// Cassette feed bin.
        /// </summary>
        Cassette,

        /// <summary>
        /// Tractor feed bin.
        /// </summary>
        Tractor,

        /// <summary>
        /// Automatic sheet feed bin.
        /// </summary>
        AutoSheetFeeder,

        /// <summary>
        /// Manual feed bin.
        /// </summary>
        Manual,
    }

    #endregion Public Enum Types

    #region Internal Enum Types

    /// <summary>
    /// Specifies Print Schema's standard capability.
    /// </summary>
    internal enum CapabilityName : int
    {
        // Features
        // (this first feature must be of value 0)

        /// <summary>
        /// The collating characteristics of the output. Each document will be collated separately.
        /// </summary>
        DocumentCollate = 0,

        /// <summary>
        /// The duplex characteristics of the output. All documents are duplexed together.
        /// </summary>
        JobDuplex,

        /// <summary>
        /// The output of multiple logical pages to a single physical sheet. All documents are compiled together.
        /// </summary>
        JobNUp,

        /// <summary>
        /// The stapling characteristics of the output. All documents are stapled together.
        /// </summary>
        JobStaple,

        /// <summary>
        /// The enabled/disabled state of device font substitution.
        /// </summary>
        PageDeviceFontSubstitution,

        /// <summary>
        /// The MediaSize used for output.
        /// </summary>
        PageMediaSize,

        /// <summary>
        /// The MediaType options and characteristics of each option.
        /// </summary>
        PageMediaType,

        /// <summary>
        /// The orientation of the media sheet.
        /// </summary>
        PageOrientation,

        /// <summary>
        /// The color setting of the output.
        /// </summary>
        PageOutputColor,

        /// <summary>
        /// The output resolution.
        /// </summary>
        PageResolution,

        /// <summary>
        /// The scaling characteristics of the output.
        /// </summary>
        PageScaling,

        /// <summary>
        /// The mode of TrueType font handling to be used.
        /// </summary>
        PageTrueTypeFontMode,

        // global parameters

        /// <summary>
        /// The number of copies of a job.
        /// </summary>
        JobCopyCount,

        // root-level properties

        /// <summary>
        /// Description of the imaged canvas for layout and rendering.
        /// </summary>
        PageImageableSize,

        /// <summary>
        /// The ordering of pages for the job.
        /// </summary>
        JobPageOrder,

        /// <summary>
        /// Photo printing intent.
        /// </summary>
        PagePhotoPrintingIntent,

        /// <summary>
        /// The borderless characteristics of a page.
        /// </summary>
        PageBorderless,

        /// <summary>
        /// Quality of the output.
        /// </summary>
        PageOutputQuality,

        /// <summary>
        /// Job input bins.
        /// </summary>
        JobInputBin,

        /// <summary>
        /// Document input bins.
        /// </summary>
        DocumentInputBin,

        /// <summary>
        /// Page input bins.
        /// </summary>
        PageInputBin,
    }

    /// <summary>
    /// Specifies Print Schema's standard scaling characteristics of the output.
    /// </summary>
    internal enum PageScaling
    {
        /// <summary>
        /// Scaling not specified.
        /// </summary>
        Unspecified = 0,

        /// <summary>
        /// No scaling should be applied.
        /// </summary>
        None,

        /// <summary>
        /// Custom scaling should be applied.
        /// </summary>
        Custom,

        /// <summary>
        /// Custom square scaling should be applied.
        /// </summary>
        CustomSquare,
    }

    #endregion Internal Enum Types
}

namespace MS.Internal.Printing.Configuration
{
    #region Public Print Schema Types

    /// <summary>
    /// The class that contains standard Print Schema enums and constants.
    /// </summary>
    internal sealed class PrintSchema
    {
        #region Constructors

        // Prevents the class from being instantiated.
        private PrintSchema() {}

        #endregion Constructors

        #region Public Types

        // READ THIS BEFORE MAKING CHANGES.
        //
        // 1. "Features" enum values are internally used as array index in PrintCaps feature and parameter-def index.
        //    Internal rules must follow are (see PrintCaps constructors to see how these rules are used):
        //    a) The first feature enum must be of value 0 for the special "Unknown" value.
        //    b) The enum value must be incremented sequentially by 1.
        //
        // 2. If you are changing any enum types in this PrintSchema class, you must also change the XxxEnumMin and
        //    XxxEnumMax internal values.

        internal const int EnumUnspecifiedValue = -1;
        internal const int EnumUnknownValue = 0;  // This 0 value matches to the "Unknown" enum values defined in public enums

        internal static CapabilityName CapabilityNameEnumMin = CapabilityName.DocumentCollate;
        internal static CapabilityName CapabilityNameEnumMax = CapabilityName.PageInputBin;

        internal static Collation CollationEnumMin = Collation.Unknown;
        internal static Collation CollationEnumMax = Collation.Uncollated;

        internal static DeviceFontSubstitution DeviceFontSubstitutionEnumMin = DeviceFontSubstitution.Unknown;
        internal static DeviceFontSubstitution DeviceFontSubstitutionEnumMax = DeviceFontSubstitution.On;

        internal static Duplexing DuplexingEnumMin = Duplexing.Unknown;
        internal static Duplexing DuplexingEnumMax = Duplexing.TwoSidedLongEdge;

        internal static PageMediaSizeName PageMediaSizeNameEnumMin = PageMediaSizeName.Unknown;
        internal static PageMediaSizeName PageMediaSizeNameEnumMax = PageMediaSizeName.CreditCard;

        internal struct StdMediaSizeEntry
        {
            public PageMediaSizeName          SizeValue;
            public int                        SizeW;
            public int                        SizeH;

            public StdMediaSizeEntry(PageMediaSizeName value, int width, int height)
            {
                this.SizeValue = value;
                this.SizeW = width;
                this.SizeH = height;
            }
        }

        // Internal table that stores Print Schema defined standard media size dimensions.
        // X/Y size values are in micron unit, where "-1" means do not set that size in PrintTicket XML.
        internal static StdMediaSizeEntry[] StdMediaSizeTable = {
            new StdMediaSizeEntry(PageMediaSizeName.ISOA0, 841000, 1189000),
            new StdMediaSizeEntry(PageMediaSizeName.ISOA1, 594000, 841000),
            new StdMediaSizeEntry(PageMediaSizeName.ISOA10, 26000, 37000),
            new StdMediaSizeEntry(PageMediaSizeName.ISOA2, 420000, 594000),
            new StdMediaSizeEntry(PageMediaSizeName.ISOA3, 297000, 420000),
            new StdMediaSizeEntry(PageMediaSizeName.ISOA3Rotated, 420000, 297000),
            new StdMediaSizeEntry(PageMediaSizeName.ISOA3Extra, 322000, 445000),
            new StdMediaSizeEntry(PageMediaSizeName.ISOA4, 210000, 297000),
            new StdMediaSizeEntry(PageMediaSizeName.ISOA4Rotated, 297000, 210000),
            new StdMediaSizeEntry(PageMediaSizeName.ISOA4Extra, 235500, 322300),
            new StdMediaSizeEntry(PageMediaSizeName.ISOA5, 148000, 210000),
            new StdMediaSizeEntry(PageMediaSizeName.ISOA5Rotated, 210000, 148000),
            new StdMediaSizeEntry(PageMediaSizeName.ISOA5Extra, 174000, 235000),
            new StdMediaSizeEntry(PageMediaSizeName.ISOA6, 105000, 148000),
            new StdMediaSizeEntry(PageMediaSizeName.ISOA6Rotated, 148000, 105000),
            new StdMediaSizeEntry(PageMediaSizeName.ISOA7, 74000, 105000),
            new StdMediaSizeEntry(PageMediaSizeName.ISOA8, 52000, 74000),
            new StdMediaSizeEntry(PageMediaSizeName.ISOA9, 37000, 52000),
            new StdMediaSizeEntry(PageMediaSizeName.ISOB0, 1000000, 1414000),
            new StdMediaSizeEntry(PageMediaSizeName.ISOB1, 707000, 1000000),
            new StdMediaSizeEntry(PageMediaSizeName.ISOB10, 31000, 44000),
            new StdMediaSizeEntry(PageMediaSizeName.ISOB2, 500000, 707000),
            new StdMediaSizeEntry(PageMediaSizeName.ISOB3, 353000, 500000),
            new StdMediaSizeEntry(PageMediaSizeName.ISOB4, 250000, 353000),
            new StdMediaSizeEntry(PageMediaSizeName.ISOB4Envelope, 250000, 353000),
            new StdMediaSizeEntry(PageMediaSizeName.ISOB5Envelope, 176000, 250000),
            new StdMediaSizeEntry(PageMediaSizeName.ISOB5Extra, 201000, 276000),
            new StdMediaSizeEntry(PageMediaSizeName.ISOB7, 88000, 125000),
            new StdMediaSizeEntry(PageMediaSizeName.ISOB8, 62000, 88000),
            new StdMediaSizeEntry(PageMediaSizeName.ISOB9, 44000, 62000),
            new StdMediaSizeEntry(PageMediaSizeName.ISOC0, 917000, 1297000),
            new StdMediaSizeEntry(PageMediaSizeName.ISOC1, 648000, 917000),
            new StdMediaSizeEntry(PageMediaSizeName.ISOC10, 28000, 40000),
            new StdMediaSizeEntry(PageMediaSizeName.ISOC2, 458000, 648000),
            new StdMediaSizeEntry(PageMediaSizeName.ISOC3, 324000, 458000),
            new StdMediaSizeEntry(PageMediaSizeName.ISOC3Envelope, 324000, 458000),
            new StdMediaSizeEntry(PageMediaSizeName.ISOC4, 229000, 324000),
            new StdMediaSizeEntry(PageMediaSizeName.ISOC4Envelope, 229000, 324000),
            new StdMediaSizeEntry(PageMediaSizeName.ISOC5, 162000, 229000),
            new StdMediaSizeEntry(PageMediaSizeName.ISOC5Envelope, 162000, 229000),
            new StdMediaSizeEntry(PageMediaSizeName.ISOC6, 114000, 162000),
            new StdMediaSizeEntry(PageMediaSizeName.ISOC6Envelope, 114000, 162000),
            new StdMediaSizeEntry(PageMediaSizeName.ISOC6C5Envelope, 114000, 229000),
            new StdMediaSizeEntry(PageMediaSizeName.ISOC7, 81000, 114000),
            new StdMediaSizeEntry(PageMediaSizeName.ISOC8, 57000, 81000),
            new StdMediaSizeEntry(PageMediaSizeName.ISOC9, 40000, 57000),
            new StdMediaSizeEntry(PageMediaSizeName.ISODLEnvelope, 110000, 220000),
            new StdMediaSizeEntry(PageMediaSizeName.ISODLEnvelopeRotated, 220000, 110000),
            new StdMediaSizeEntry(PageMediaSizeName.ISOSRA3, 320000, 450000),
            new StdMediaSizeEntry(PageMediaSizeName.JapanQuadrupleHagakiPostcard, 200000, 296000),
            new StdMediaSizeEntry(PageMediaSizeName.JISB0, 1030000, 1456000),
            new StdMediaSizeEntry(PageMediaSizeName.JISB1, 728000, 1030000),
            new StdMediaSizeEntry(PageMediaSizeName.JISB10, 32000, 45000),
            new StdMediaSizeEntry(PageMediaSizeName.JISB2, 515000, 728000),
            new StdMediaSizeEntry(PageMediaSizeName.JISB3, 364000, 515000),
            new StdMediaSizeEntry(PageMediaSizeName.JISB4, 257000, 364000),
            new StdMediaSizeEntry(PageMediaSizeName.JISB4Rotated, 364000, 257000),
            new StdMediaSizeEntry(PageMediaSizeName.JISB5, 182000, 257000),
            new StdMediaSizeEntry(PageMediaSizeName.JISB5Rotated, 257000, 182000),
            new StdMediaSizeEntry(PageMediaSizeName.JISB6, 128000, 182000),
            new StdMediaSizeEntry(PageMediaSizeName.JISB6Rotated, 182000, 128000),
            new StdMediaSizeEntry(PageMediaSizeName.JISB7, 91000, 128000),
            new StdMediaSizeEntry(PageMediaSizeName.JISB8, 64000, 91000),
            new StdMediaSizeEntry(PageMediaSizeName.JISB9, 45000, 64000),
            new StdMediaSizeEntry(PageMediaSizeName.JapanChou3Envelope, 120000, 235000),
            new StdMediaSizeEntry(PageMediaSizeName.JapanChou3EnvelopeRotated, 235000, 120000),
            new StdMediaSizeEntry(PageMediaSizeName.JapanChou4Envelope, 90000, 205000),
            new StdMediaSizeEntry(PageMediaSizeName.JapanChou4EnvelopeRotated, 205000, 90000),
            new StdMediaSizeEntry(PageMediaSizeName.JapanHagakiPostcard, 100000, 148000),
            new StdMediaSizeEntry(PageMediaSizeName.JapanHagakiPostcardRotated, 148000, 100000),
            new StdMediaSizeEntry(PageMediaSizeName.JapanKaku2Envelope, 240000, 332000),
            new StdMediaSizeEntry(PageMediaSizeName.JapanKaku2EnvelopeRotated, 332000, 240000),
            new StdMediaSizeEntry(PageMediaSizeName.JapanKaku3Envelope, 216000, 277000),
            new StdMediaSizeEntry(PageMediaSizeName.JapanKaku3EnvelopeRotated, 277000, 216000),
            new StdMediaSizeEntry(PageMediaSizeName.JapanYou4Envelope, 105000, 235000),
            new StdMediaSizeEntry(PageMediaSizeName.NorthAmerica10x11, 254000, 279400),
            new StdMediaSizeEntry(PageMediaSizeName.NorthAmerica10x14, 254000, 355600),
            new StdMediaSizeEntry(PageMediaSizeName.NorthAmerica11x17, 279400, 431800),
            new StdMediaSizeEntry(PageMediaSizeName.NorthAmerica9x11, 228600, 279400),
            new StdMediaSizeEntry(PageMediaSizeName.NorthAmericaArchitectureASheet, 228600, 304800),
            new StdMediaSizeEntry(PageMediaSizeName.NorthAmericaArchitectureBSheet, 304800, 457200),
            new StdMediaSizeEntry(PageMediaSizeName.NorthAmericaArchitectureCSheet, 457200, 609600),
            new StdMediaSizeEntry(PageMediaSizeName.NorthAmericaArchitectureDSheet, 609600, 914400),
            new StdMediaSizeEntry(PageMediaSizeName.NorthAmericaArchitectureESheet, 914400, 1219200),
            new StdMediaSizeEntry(PageMediaSizeName.NorthAmericaCSheet, 431800, 558800),
            new StdMediaSizeEntry(PageMediaSizeName.NorthAmericaDSheet, 558800, 863600),
            new StdMediaSizeEntry(PageMediaSizeName.NorthAmericaESheet, 863600, 1117600),
            new StdMediaSizeEntry(PageMediaSizeName.NorthAmericaExecutive, 184150, 266700),
            new StdMediaSizeEntry(PageMediaSizeName.NorthAmericaGermanLegalFanfold, 215900, 330200),
            new StdMediaSizeEntry(PageMediaSizeName.NorthAmericaGermanStandardFanfold, 215900, 304800),
            new StdMediaSizeEntry(PageMediaSizeName.NorthAmericaLegal, 215900, 355600),
            new StdMediaSizeEntry(PageMediaSizeName.NorthAmericaLegalExtra, 241300, 381000),
            new StdMediaSizeEntry(PageMediaSizeName.NorthAmericaLetter, 215900, 279400),
            new StdMediaSizeEntry(PageMediaSizeName.NorthAmericaLetterRotated, 279400, 215900),
            new StdMediaSizeEntry(PageMediaSizeName.NorthAmericaLetterExtra, 241300, 304800),
            new StdMediaSizeEntry(PageMediaSizeName.NorthAmericaLetterPlus, 215900, 322326),
            new StdMediaSizeEntry(PageMediaSizeName.NorthAmericaMonarchEnvelope, 98425, 177800),
            new StdMediaSizeEntry(PageMediaSizeName.NorthAmericaNote, 215900, 279400),
            new StdMediaSizeEntry(PageMediaSizeName.NorthAmericaNumber10Envelope, 104775, 241300),
            new StdMediaSizeEntry(PageMediaSizeName.NorthAmericaNumber10EnvelopeRotated, 241300, 104775),
            new StdMediaSizeEntry(PageMediaSizeName.NorthAmericaNumber9Envelope, 98425, 225425),
            new StdMediaSizeEntry(PageMediaSizeName.NorthAmericaNumber11Envelope, 114300, 263525),
            new StdMediaSizeEntry(PageMediaSizeName.NorthAmericaNumber12Envelope, 120650, 279400),
            new StdMediaSizeEntry(PageMediaSizeName.NorthAmericaNumber14Envelope, 127000, 292100),
            new StdMediaSizeEntry(PageMediaSizeName.NorthAmericaPersonalEnvelope, 92075, 165100),
            new StdMediaSizeEntry(PageMediaSizeName.NorthAmericaQuarto, 215000, 275000),
            new StdMediaSizeEntry(PageMediaSizeName.NorthAmericaStatement, 139700, 215900),
            new StdMediaSizeEntry(PageMediaSizeName.NorthAmericaSuperA, 227000, 356000),
            new StdMediaSizeEntry(PageMediaSizeName.NorthAmericaSuperB, 305000, 487000),
            new StdMediaSizeEntry(PageMediaSizeName.NorthAmericaTabloid, 279400, 431800),
            new StdMediaSizeEntry(PageMediaSizeName.NorthAmericaTabloidExtra, 296926, 457200),
            new StdMediaSizeEntry(PageMediaSizeName.OtherMetricA4Plus, 210000, 330000),
            new StdMediaSizeEntry(PageMediaSizeName.OtherMetricA3Plus, 329000, 483000),
            new StdMediaSizeEntry(PageMediaSizeName.OtherMetricFolio, 215900, 330200),
            new StdMediaSizeEntry(PageMediaSizeName.OtherMetricInviteEnvelope, 220000, 220000),
            new StdMediaSizeEntry(PageMediaSizeName.OtherMetricItalianEnvelope, 110000, 230000),
            new StdMediaSizeEntry(PageMediaSizeName.PRC1Envelope, 102000, 165000),
            new StdMediaSizeEntry(PageMediaSizeName.PRC1EnvelopeRotated, 165000, 102000),
            new StdMediaSizeEntry(PageMediaSizeName.PRC10Envelope, 324000, 458000),
            new StdMediaSizeEntry(PageMediaSizeName.PRC10EnvelopeRotated, 458000, 324000),
            new StdMediaSizeEntry(PageMediaSizeName.PRC16K, 146000, 215000),
            new StdMediaSizeEntry(PageMediaSizeName.PRC16KRotated, 215000, 146000),
            new StdMediaSizeEntry(PageMediaSizeName.PRC2Envelope, 102000, 176000),
            new StdMediaSizeEntry(PageMediaSizeName.PRC2EnvelopeRotated, 176000, 102000),
            new StdMediaSizeEntry(PageMediaSizeName.PRC32K, 97000, 151000),
            new StdMediaSizeEntry(PageMediaSizeName.PRC32KRotated, 151000, 97000),
            new StdMediaSizeEntry(PageMediaSizeName.PRC32KBig, 97000, 151000),
            new StdMediaSizeEntry(PageMediaSizeName.PRC3Envelope, 125000, 176000),
            new StdMediaSizeEntry(PageMediaSizeName.PRC3EnvelopeRotated, 176000,  125000),
            new StdMediaSizeEntry(PageMediaSizeName.PRC4Envelope, 110000, 208000),
            new StdMediaSizeEntry(PageMediaSizeName.PRC4EnvelopeRotated, 208000, 110000),
            new StdMediaSizeEntry(PageMediaSizeName.PRC5Envelope, 110000, 220000),
            new StdMediaSizeEntry(PageMediaSizeName.PRC5EnvelopeRotated, 220000, 110000),
            new StdMediaSizeEntry(PageMediaSizeName.PRC6Envelope, 120000, 230000),
            new StdMediaSizeEntry(PageMediaSizeName.PRC6EnvelopeRotated, 230000, 120000),
            new StdMediaSizeEntry(PageMediaSizeName.PRC7Envelope, 160000, 230000),
            new StdMediaSizeEntry(PageMediaSizeName.PRC7EnvelopeRotated, 230000, 160000),
            new StdMediaSizeEntry(PageMediaSizeName.PRC8Envelope, 120000, 309000),
            new StdMediaSizeEntry(PageMediaSizeName.PRC8EnvelopeRotated, 309000, 120000),
            new StdMediaSizeEntry(PageMediaSizeName.PRC9Envelope, 229000, 324000),
            new StdMediaSizeEntry(PageMediaSizeName.PRC9EnvelopeRotated, 324000, 229000),
            new StdMediaSizeEntry(PageMediaSizeName.Roll04Inch, 101600, -1),
            new StdMediaSizeEntry(PageMediaSizeName.Roll06Inch, 152400, -1),
            new StdMediaSizeEntry(PageMediaSizeName.Roll08Inch, 203200, -1),
            new StdMediaSizeEntry(PageMediaSizeName.Roll12Inch, 304800, -1),
            new StdMediaSizeEntry(PageMediaSizeName.Roll15Inch, 381000, -1),
            new StdMediaSizeEntry(PageMediaSizeName.Roll18Inch, 457200, -1),
            new StdMediaSizeEntry(PageMediaSizeName.Roll22Inch, 558800, -1),
            new StdMediaSizeEntry(PageMediaSizeName.Roll24Inch, 609600, -1),
            new StdMediaSizeEntry(PageMediaSizeName.Roll30Inch, 762000, -1),
            new StdMediaSizeEntry(PageMediaSizeName.Roll36Inch, 914400, -1),
            new StdMediaSizeEntry(PageMediaSizeName.Roll54Inch, 1371600, -1),
            new StdMediaSizeEntry(PageMediaSizeName.JapanDoubleHagakiPostcard, 200000, 148000),
            new StdMediaSizeEntry(PageMediaSizeName.JapanDoubleHagakiPostcardRotated, 148000, 200000),
            new StdMediaSizeEntry(PageMediaSizeName.JapanLPhoto, 89000, 127000),
            new StdMediaSizeEntry(PageMediaSizeName.Japan2LPhoto, 127000, 178000),
            new StdMediaSizeEntry(PageMediaSizeName.JapanYou1Envelope, 120000, 176000),
            new StdMediaSizeEntry(PageMediaSizeName.JapanYou2Envelope, 114000, 162000),
            new StdMediaSizeEntry(PageMediaSizeName.JapanYou3Envelope, 98000, 148000),
            new StdMediaSizeEntry(PageMediaSizeName.JapanYou4EnvelopeRotated, 235000, 105000),
            new StdMediaSizeEntry(PageMediaSizeName.JapanYou6Envelope, 98000, 190000),
            new StdMediaSizeEntry(PageMediaSizeName.JapanYou6EnvelopeRotated, 190000, 98000),
            new StdMediaSizeEntry(PageMediaSizeName.NorthAmerica4x6, 101600, 152400),
            new StdMediaSizeEntry(PageMediaSizeName.NorthAmerica4x8, 101600, 203200),
            new StdMediaSizeEntry(PageMediaSizeName.NorthAmerica5x7, 127000, 177800),
            new StdMediaSizeEntry(PageMediaSizeName.NorthAmerica8x10, 203200, 254000),
            new StdMediaSizeEntry(PageMediaSizeName.NorthAmerica10x12, 254000, 304800),
            new StdMediaSizeEntry(PageMediaSizeName.NorthAmerica14x17, 355600, 431800),
            new StdMediaSizeEntry(PageMediaSizeName.BusinessCard, 55000, 91000),
            new StdMediaSizeEntry(PageMediaSizeName.CreditCard, 54000, 86000),
        };

        internal static PageMediaType PageMediaTypeEnumMin = PageMediaType.Unknown;
        internal static PageMediaType PageMediaTypeEnumMax = PageMediaType.None;

        internal static PagesPerSheetDirection PagesPerSheetDirectionEnumMin = PagesPerSheetDirection.Unknown;
        internal static PagesPerSheetDirection PagesPerSheetDirectionEnumMax = PagesPerSheetDirection.TopLeft;

        internal static PageOrientation PageOrientationEnumMin = PageOrientation.Unknown;
        internal static PageOrientation PageOrientationEnumMax = PageOrientation.ReversePortrait;

        internal static OutputColor OutputColorEnumMin = OutputColor.Unknown;
        internal static OutputColor OutputColorEnumMax = OutputColor.Monochrome;

        internal static PageQualitativeResolution PageQualitativeResolutionEnumMin = PageQualitativeResolution.Unknown;
        internal static PageQualitativeResolution PageQualitativeResolutionEnumMax = PageQualitativeResolution.Other;

        internal static PageScaling PageScalingEnumMin = PageScaling.Unspecified;
        internal static PageScaling PageScalingEnumMax = PageScaling.CustomSquare;

        internal static Stapling StaplingEnumMin = Stapling.Unknown;
        internal static Stapling StaplingEnumMax = Stapling.None;

        internal static TrueTypeFontMode TrueTypeFontModeEnumMin = TrueTypeFontMode.Unknown;
        internal static TrueTypeFontMode TrueTypeFontModeEnumMax = TrueTypeFontMode.RenderAsBitmap;

        internal static PageOrder PageOrderEnumMin = PageOrder.Unknown;
        internal static PageOrder PageOrderEnumMax = PageOrder.Reverse;

        internal static PhotoPrintingIntent PhotoPrintingIntentEnumMin = PhotoPrintingIntent.Unknown;
        internal static PhotoPrintingIntent PhotoPrintingIntentEnumMax = PhotoPrintingIntent.PhotoStandard;

        internal static PageBorderless PageBorderlessEnumMin = PageBorderless.Unknown;
        internal static PageBorderless PageBorderlessEnumMax = PageBorderless.None;

        internal static OutputQuality OutputQualityEnumMin = OutputQuality.Unknown;
        internal static OutputQuality OutputQualityEnumMax = OutputQuality.Text;

        internal static InputBin InputBinEnumMin = InputBin.Unknown;
        internal static InputBin InputBinEnumMax = InputBin.Manual;

        /// <summary>
        /// Print Schema constant to represent an unspecified integer value.
        /// </summary>
        public const int UnspecifiedIntValue = System.Int32.MinValue;

        /// <summary>
        /// Print Schema constant to represent an unspecified double value.
        /// </summary>
        public const double UnspecifiedDoubleValue = System.Double.MinValue;

        #endregion Public Types
    }

    #endregion Public Print Schema Types

    #region Internal Types

    internal enum PrintSchemaSubFeatures
    {
        NUpPresentationDirection = 0,
    }

    /// <summary>
    /// This is only used internally as parameter array index.
    /// </summary>
    internal enum PrintSchemaLocalParameterDefs
    {
        PageScalingScaleWidth,
        PageScalingScaleHeight,
        PageSquareScalingScale,
    }

    /// <summary>
    /// Different types of Print Schema XML node
    /// </summary>
    [Flags]
    internal enum PrintSchemaNodeTypes
    {
        None            = 0x0000,

        Attribute       = 0x0001,

        AttributeSet    = 0x0002,

        AttributeSetRef = 0x0004,

        Feature         = 0x0008,

        Option          = 0x0010,

        ParameterDef    = 0x0020,

        ParameterRef    = 0x0040,

        Parameter       = 0x0080,

        Property        = 0x0100,

        ScoredProperty  = 0x0200,

        Value           = 0x0400,

        /// <summary>
        /// Root level types could be Feature, ParameterDef, Property
        /// </summary>
        RootLevelTypes  = Feature | ParameterDef | Property,

        /// <summary>
        /// Parent-Feature level types could be Feature, Option, Property
        /// </summary>
        FeatureLevelTypesWithSubFeature = Feature | Property | Option,

        /// <summary>
        /// Single-Feature level types could be Option, Property
        /// </summary>
        FeatureLevelTypesWithoutSubFeature = Property | Option,

        /// <summary>
        /// Option level types could be Property, ScoredProperty
        /// </summary>
        OptionLevelTypes = Property | ScoredProperty,

        /// <summary>
        /// ScoredProperty level types could be Value or ParameterRef
        /// </summary>
        ScoredPropertyLevelTypes = Value | ParameterRef,
    }

    /// <summary>
    /// Internal class to hold standard namespaces used by Print Schema
    /// </summary>
    internal class PrintSchemaNamespaces
    {
        private PrintSchemaNamespaces() {}

        public const string Framework = "http://schemas.microsoft.com/windows/2003/08/printing/printschemaframework";
        public const string StandardKeywordSet = "http://schemas.microsoft.com/windows/2003/08/printing/printschemakeywords";
        public const string xsi = "http://www.w3.org/2001/XMLSchema-instance";
        public const string xsd = "http://www.w3.org/2001/XMLSchema";
        public const string xmlns = "http://www.w3.org/2000/xmlns/";

        // Print Schema decides not to qualify any standard Framework XML attributes (e.g. version,
        // name, constrained, etc).
        public const string FrameworkAttrForXmlReader = null;
        public const string FrameworkAttrForXmlDOM = "";
    }

    /// <summary>
    /// Internal class to hold prefixes for Print Schema standard namespaces
    /// </summary>
    internal class PrintSchemaPrefixes
    {
        private PrintSchemaPrefixes() {}

        public const string Framework =             "psf";
        public const string StandardKeywordSet =    "psk";
        public const string xsi =                   "xsi";
        public const string xsd =                   "xsd";
        public const string xmlns =                 "xmlns";
    }

    /// <summary>
    /// Internal class to hold const strings for standard xsi types
    /// </summary>
    internal class PrintSchemaXsiTypes
    {
        private PrintSchemaXsiTypes() {}

        public const string Integer = "integer";
        public const string String =  "string";
        public const string QName =   "QName";
    }

    /// <summary>
    /// Internal class to hold public Print Schema constant keywords
    /// </summary>
    internal class PrintSchemaTags
    {
        private PrintSchemaTags() {}

        internal struct MapEntry
        {
            public string SchemaName;
            public int    EnumValue;
            public MapEntry(string schemaName, int enumValue)
            {
                this.SchemaName = schemaName;
                this.EnumValue = enumValue;
            }
        }

        internal class Framework
        {
            private Framework() {}

            internal const decimal SchemaVersion = 1;

            internal const string PrintTicketRoot =   "PrintTicket";
            internal const string PrintCapRoot =      "PrintCapabilities";

            internal const string Feature =           "Feature";
            internal const string Option =            "Option";
            internal const string ParameterDef =      "ParameterDef";
            internal const string ParameterRef =      "ParameterRef";
            internal const string ParameterInit =     "ParameterInit";
            internal const string Property =          "Property";
            internal const string ScoredProperty =    "ScoredProperty";
            internal const string Value =             "Value";

            internal static MapEntry[] NodeTypeMapTable = {
                new MapEntry(Feature,        (int)PrintSchemaNodeTypes.Feature),
                new MapEntry(ParameterDef,   (int)PrintSchemaNodeTypes.ParameterDef),
                new MapEntry(Option,         (int)PrintSchemaNodeTypes.Option),
                new MapEntry(Property,       (int)PrintSchemaNodeTypes.Property),
                new MapEntry(ScoredProperty, (int)PrintSchemaNodeTypes.ScoredProperty),
                new MapEntry(Value,          (int)PrintSchemaNodeTypes.Value),
                new MapEntry(ParameterRef,   (int)PrintSchemaNodeTypes.ParameterRef),
            };

            internal const string OptionNameProperty = "OptionName";
            internal const string RootVersionAttr =    "version";
            internal const string NameAttr =           "name";
            internal const string Unspecified =        "Unspecified";
            internal const string Unknown     =        "Unknown";

            internal const string EmptyPrintTicket =
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<" + PrintSchemaPrefixes.Framework + ":" + PrintSchemaTags.Framework.PrintTicketRoot +
                    " xmlns:" + PrintSchemaPrefixes.Framework + "=\"" + PrintSchemaNamespaces.Framework + "\"" +
                    " xmlns:" + PrintSchemaPrefixes.StandardKeywordSet + "=\"" + PrintSchemaNamespaces.StandardKeywordSet + "\"" +
                    " xmlns:" + PrintSchemaPrefixes.xsi + "=\"" + PrintSchemaNamespaces.xsi + "\"" +
                    " xmlns:" + PrintSchemaPrefixes.xsd + "=\"" + PrintSchemaNamespaces.xsd + "\"" +
                    " version=\"1\">" +
                "</" + PrintSchemaPrefixes.Framework + ":" + PrintSchemaTags.Framework.PrintTicketRoot + ">";
        }

        internal class Keywords
        {
            private Keywords() {}

            internal class CollateKeys
            {
                private CollateKeys() { }

                internal const string DocumentCollate = "DocumentCollate";

                internal static string[] CollationNames = Enum.GetNames(typeof(Collation));
                internal static int[] CollationEnums = (int[])Enum.GetValues(typeof(Collation));
            }

            internal class DuplexKeys
            {
                private DuplexKeys() { }

                internal const string JobDuplex = "JobDuplexAllDocumentsContiguously";

                internal static string[] DuplexNames = Enum.GetNames(typeof(Duplexing));
                internal static int[] DuplexEnums = (int[])Enum.GetValues(typeof(Duplexing));
            }

            internal class NUpKeys
            {
                private NUpKeys() {}

                internal const string JobNUp = "JobNUpAllDocumentsContiguously";
                internal const string PagesPerSheet = "PagesPerSheet";
                internal const string PresentationDirection = "PresentationDirection";

                internal static string[] DirectionNames = Enum.GetNames(typeof(PagesPerSheetDirection));
                internal static int[] DirectionEnums = (int[])Enum.GetValues(typeof(PagesPerSheetDirection));
            }

            internal class StapleKeys
            {
                private StapleKeys() {}

                internal const string JobStaple = "JobStapleAllDocuments";

                internal static string[] StaplingNames = Enum.GetNames(typeof(Stapling));
                internal static int[] StaplingEnums = (int[])Enum.GetValues(typeof(Stapling));
            }

            internal class PageDeviceFontSubstitutionKeys
            {
                private PageDeviceFontSubstitutionKeys() {}

                internal const string Self = "PageDeviceFontSubstitution";

                internal static string[] SubstitutionNames = Enum.GetNames(typeof(DeviceFontSubstitution));
                internal static int[] SubstitutionEnums = (int[])Enum.GetValues(typeof(DeviceFontSubstitution));
            }

            internal class PageMediaSizeKeys
            {
                private PageMediaSizeKeys() { }

                internal const string Self =                  "PageMediaSize";
                internal const string MediaSizeWidth        = "MediaSizeWidth";
                internal const string MediaSizeHeight       = "MediaSizeHeight";

                internal const string CustomMediaSize       = "CustomMediaSize";
                // CustomMediaSizeWidth/Height strings are only used internally for PTPropMapEntry[] to
                // differentiate from fixed size MediaSizeWidth/Height. In XML document, PageMediaSize option has
                // same scored properties "MediaSizeWidth/Height" for both fixed and non-PS custom sizes.
                internal const string CustomMediaSizeWidth =        "CustomMediaSizeWidth";
                internal const string CustomMediaSizeHeight =       "CustomMediaSizeHeight";

                internal static string[] MediaSizeNames = Enum.GetNames(typeof(PageMediaSizeName));
                internal static int[] MediaSizeEnums = (int[])Enum.GetValues(typeof(PageMediaSizeName));
            }

            internal class PageImageableSizeKeys
            {
                private PageImageableSizeKeys() { }

                internal const string Self                   = "PageImageableSize";
                internal const string ImageableSizeWidth     = "ImageableSizeWidth";
                internal const string ImageableSizeHeight    = "ImageableSizeHeight";

                internal const string ImageableArea          = "ImageableArea";

                internal const string OriginWidth        =     "OriginWidth";
                internal const string OriginHeight       =     "OriginHeight";
                internal const string ExtentWidth        =     "ExtentWidth";
                internal const string ExtentHeight       =     "ExtentHeight";
            }

            internal class PageMediaTypeKeys
            {
                private PageMediaTypeKeys() {}

                internal const string Self =         "PageMediaType";

                internal static string[] MediaTypeNames = Enum.GetNames(typeof(PageMediaType));
                internal static int[] MediaTypeEnums = (int[])Enum.GetValues(typeof(PageMediaType));
            }

            internal class PageOrientationKeys
            {
                private PageOrientationKeys() {}

                internal const string Self = "PageOrientation";

                internal static string[] OrientationNames = Enum.GetNames(typeof(PageOrientation));
                internal static int[] OrientationEnums = (int[])Enum.GetValues(typeof(PageOrientation));
            }

            internal class PageOutputColorKeys
            {
                private PageOutputColorKeys() {}

                internal const string Self =   "PageOutputColor";

                internal static string[] ColorNames = Enum.GetNames(typeof(OutputColor));
                internal static int[] ColorEnums = (int[])Enum.GetValues(typeof(OutputColor));
            }

            internal class PageResolutionKeys
            {
                private PageResolutionKeys() {}

                internal const string Self =                  "PageResolution";
                internal const string ResolutionX =           "ResolutionX";
                internal const string ResolutionY =           "ResolutionY";
                internal const string QualitativeResolution = "QualitativeResolution";

                internal static string[] QualityNames = Enum.GetNames(typeof(PageQualitativeResolution));
                internal static int[] QualityEnums = (int[])Enum.GetValues(typeof(PageQualitativeResolution));
            }

            internal class PageScalingKeys
            {
                private PageScalingKeys() { }

                internal const string Self =               "PageScaling";
                internal const string CustomScaleWidth   = "ScaleWidth";
                internal const string CustomScaleHeight  = "ScaleHeight";
                internal const string CustomSquareScale  = "Scale";

                internal static string[] ScalingNames = Enum.GetNames(typeof(PageScaling));
                internal static int[] ScalingEnums = (int[])Enum.GetValues(typeof(PageScaling));
            }

            internal class PageTrueTypeFontModeKeys
            {
                private PageTrueTypeFontModeKeys() { }

                internal const string Self = "PageTrueTypeFontMode";

                internal static string[] ModeNames = Enum.GetNames(typeof(TrueTypeFontMode));
                internal static int[] ModeEnums = (int[])Enum.GetValues(typeof(TrueTypeFontMode));
            }

            internal class JobPageOrderKeys
            {
                private JobPageOrderKeys() { }

                internal const string Self = "JobPageOrder";

                internal static string[] PageOrderNames = Enum.GetNames(typeof(PageOrder));
                internal static int[] PageOrderEnums = (int[])Enum.GetValues(typeof(PageOrder));
            }

            internal class PagePhotoPrintingIntentKeys
            {
                private PagePhotoPrintingIntentKeys() { }

                internal const string Self = "PagePhotoPrintingIntent";

                internal static string[] PhotoIntentNames = Enum.GetNames(typeof(PhotoPrintingIntent));
                internal static int[] PhotoIntentEnums = (int[])Enum.GetValues(typeof(PhotoPrintingIntent));
            }

            internal class PageBorderlessKeys
            {
                private PageBorderlessKeys() { }

                internal const string Self = "PageBorderless";

                internal static string[] BorderlessNames = Enum.GetNames(typeof(PageBorderless));
                internal static int[] BorderlessEnums = (int[])Enum.GetValues(typeof(PageBorderless));
            }

            internal class PageOutputQualityKeys
            {
                private PageOutputQualityKeys() { }

                internal const string Self = "PageOutputQuality";

                internal static string[] OutputQualityNames = Enum.GetNames(typeof(OutputQuality));
                internal static int[] OutputQualityEnums = (int[])Enum.GetValues(typeof(OutputQuality));
            }

            internal class InputBinKeys
            {
                private InputBinKeys() { }

                internal const string JobInputBin = "JobInputBin";
                internal const string DocumentInputBin = "DocumentInputBin";
                internal const string PageInputBin = "PageInputBin";

                internal static string[] InputBinNames = Enum.GetNames(typeof(InputBin));
                internal static int[] InputBinEnums = (int[])Enum.GetValues(typeof(InputBin));
            }

            internal class ParameterProps
            {
                private ParameterProps() {}

                internal const string DefaultValue = "DefaultValue";
                internal const string MinValue =     "MinValue";
                internal const string MaxValue =     "MaxValue";
            }

            internal class ParameterDefs
            {
                private ParameterDefs() {}

                internal const string JobCopyCount = "JobCopiesAllDocuments";

                // for non-PS custom media size.
                internal const string PageMediaSizeMediaSizeWidth        = "PageMediaSizeMediaSizeWidth";
                internal const string PageMediaSizeMediaSizeHeight       = "PageMediaSizeMediaSizeHeight";

                internal const string PageScalingScaleWidth   = "PageScalingScaleWidth";
                internal const string PageScalingScaleHeight  = "PageScalingScaleHeight";
                internal const string PageSquareScalingScale  = "PageScalingScale";
            }

            internal static MapEntry[] FeatureMapTable = {
                new MapEntry(CollateKeys.DocumentCollate,         (int)CapabilityName.DocumentCollate),
                new MapEntry(DuplexKeys.JobDuplex,                (int)CapabilityName.JobDuplex),
                new MapEntry(NUpKeys.JobNUp,                      (int)CapabilityName.JobNUp),
                new MapEntry(StapleKeys.JobStaple,                (int)CapabilityName.JobStaple),
                new MapEntry(PageDeviceFontSubstitutionKeys.Self, (int)CapabilityName.PageDeviceFontSubstitution),
                new MapEntry(PageMediaSizeKeys.Self,              (int)CapabilityName.PageMediaSize),
                new MapEntry(PageMediaTypeKeys.Self,              (int)CapabilityName.PageMediaType),
                new MapEntry(PageOrientationKeys.Self,            (int)CapabilityName.PageOrientation),
                new MapEntry(PageOutputColorKeys.Self,            (int)CapabilityName.PageOutputColor),
                new MapEntry(PageResolutionKeys.Self,             (int)CapabilityName.PageResolution),
                new MapEntry(PageScalingKeys.Self,                (int)CapabilityName.PageScaling),
                new MapEntry(PageTrueTypeFontModeKeys.Self,       (int)CapabilityName.PageTrueTypeFontMode),
                new MapEntry(JobPageOrderKeys.Self,               (int)CapabilityName.JobPageOrder),
                new MapEntry(PagePhotoPrintingIntentKeys.Self,    (int)CapabilityName.PagePhotoPrintingIntent),
                new MapEntry(PageBorderlessKeys.Self,             (int)CapabilityName.PageBorderless),
                new MapEntry(PageOutputQualityKeys.Self,          (int)CapabilityName.PageOutputQuality),
                new MapEntry(InputBinKeys.JobInputBin,            (int)CapabilityName.JobInputBin),
                new MapEntry(InputBinKeys.DocumentInputBin,       (int)CapabilityName.DocumentInputBin),
                new MapEntry(InputBinKeys.PageInputBin,           (int)CapabilityName.PageInputBin),
            };

            internal static MapEntry[] SubFeatureMapTable = {
                new MapEntry(NUpKeys.PresentationDirection, (int)PrintSchemaSubFeatures.NUpPresentationDirection),
            };

            internal static MapEntry[] GlobalParameterMapTable = {
                new MapEntry(ParameterDefs.JobCopyCount, (int)CapabilityName.JobCopyCount),
            };

            internal static MapEntry[] LocalParameterMapTable = {
                new MapEntry(ParameterDefs.PageScalingScaleWidth,
                             (int)PrintSchemaLocalParameterDefs.PageScalingScaleWidth),

                new MapEntry(ParameterDefs.PageScalingScaleHeight,
                             (int)PrintSchemaLocalParameterDefs.PageScalingScaleHeight),

                new MapEntry(ParameterDefs.PageSquareScalingScale,
                             (int)PrintSchemaLocalParameterDefs.PageSquareScalingScale),
            };
        }
    }

    internal class UnitConverter
    {
        private UnitConverter() {}

        /// <summary>
        /// Converts internal micron length value to DIP length value (in 1/96 inch unit).
        /// </summary>
        /// <param name="micronValue">micron length value</param>
        /// <returns>DIP length value</returns>
        public static double LengthValueFromMicronToDIP(int micronValue)
        {
            // Handle the special case that "micronValue" is UnspecifiedIntValue
            // (All length-related values are stored internally as int. The values are
            // only converted to double DIP value when client is accessing it.)
            if (micronValue == PrintSchema.UnspecifiedIntValue)
                return PrintSchema.UnspecifiedDoubleValue;

            return ((double)micronValue / 25400) * 96;
        }

        /// <summary>
        /// Converts client input DIP length value to internal micron length value.
        /// </summary>
        /// <param name="dipValue">DIP length value</param>
        /// <returns>micron length value</returns>
        public static int LengthValueFromDIPToMicron(double dipValue)
        {
            // This is only used when converting client input DIP value into micron value.
            // So there is no need to hanle the special UnspecifiedIntValue/UnspecifiedDoubleValue case.

            return (int)((dipValue / 96) * 25400 + 0.5);
        }
    }

    internal static class PrintSchemaMapper
    {
        public static int SchemaNameToEnumValueWithMap(PrintSchemaTags.MapEntry[] map,
                                                       string schemaName)
        {
            int enumValue = -1;

            for (int i=0; i < map.Length; i++)
            {
                if (map[i].SchemaName == schemaName)
                {
                    enumValue = map[i].EnumValue;
                    break;
                }
            }

            return enumValue;
        }

        public static int SchemaNameToEnumValueWithArray(string[] enumNames,
                                                         int[] enumValues,
                                                         string schemaName)
        {
            #if _DEBUG
            if (((enumNames[0] != PrintSchemaTags.Framework.Unspecified) &&
                 (enumNames[0] != PrintSchemaTags.Framework.Unknown)) ||
                (enumValues[0] != 0) ||
                (enumNames.Length != enumValues.Length))
            {
                // This is for internal checking of correct enum defintion only
                throw new InvalidOperationException("_DEBUG: enum definition error: should be 'Unknown/Unspecified' and 0 and equal array length");
            }
            #endif

            int enumValue = -1;

            // We need to skip the first enum value "Unspecified" or "Unknown"
            for (int i=1; i < enumNames.Length; i++)
            {
                if (enumNames[i] == schemaName)
                {
                    enumValue = enumValues[i];
                    break;
                }
            }

            return enumValue;
        }

        /// <remarks>
        /// If returns true, then the returned enumValue is guaranteed to be one of the input
        /// schema enum values and is not "Unspecified".
        /// </remarks>
        /// <exception cref="XmlException">XML is not well-formed.</exception>
        public static bool CurrentPropertyQValueToEnumValue(XmlPrintCapReader reader,
                                                            string[] schemaNames,
                                                            int[]    schemaEnums,
                                                            out int enumValue)
        {
            bool foundMatch = false;
            string valueLocalName = null;

            enumValue = -1;

            try
            {
                valueLocalName = reader.GetCurrentPropertyQNameValueWithException();
            }
            // We want to catch internal FormatException to skip recoverable XML content syntax error
            #pragma warning suppress 56502
            #if _DEBUG
            catch (FormatException e)
            #else
            catch (FormatException)
            #endif
            {
                #if _DEBUG
                Trace.WriteLine("-Error- " + e.Message);
                #endif
            }

            if (valueLocalName != null)
            {
                enumValue = SchemaNameToEnumValueWithArray(schemaNames, schemaEnums, valueLocalName);

                if (enumValue > 0)
                {
                    foundMatch = true;
                }
                else
                {
                    #if _DEBUG
                    Trace.Assert(enumValue != 0, "THIS SHOULD NOT HAPPEN: 'Unspecified' enum matched");
                    Trace.WriteLine("-Warning- Value element at line number " +
                                    reader._xmlReader.LineNumber + ", line position " +
                                    reader._xmlReader.LinePosition + " has unknown public text value '" +
                                    valueLocalName + "'");
                    #endif
                }
            }

            return foundMatch;
        }

        public static string EnumValueToSchemaNameWithArray(string[] enumNames,
                                                            int[] enumValues,
                                                            int enumValue)
        {
            #if _DEBUG
            if (((enumNames[0] != PrintSchemaTags.Framework.Unspecified) &&
                 (enumNames[0] != PrintSchemaTags.Framework.Unknown)) ||
                (enumValues[0] != 0) ||
                (enumNames.Length != enumValues.Length))
            {
                // This is for internal checking of correct enum defintion only
                throw new InvalidOperationException("_DEBUG: enum definition error: should be 'Unknown/Unspecified' and 0 and equal array length");
            }
            #endif

            string schemaName = null;

            // We need to skip the first enum value "Unspecified" or "Unknown"
            for (int i=1; i < enumNames.Length; i++)
            {
                if (enumValues[i] == enumValue)
                {
                    schemaName = enumNames[i];
                    break;
                }
            }

            return schemaName;
        }
    }

    internal static class XmlConvertHelper
    {
        /// <summary>
        /// Converts a string to Int32 value
        /// </summary>
        /// <exception cref="FormatException">thrown if the string is invalid to be converted</exception>
        public static int ConvertStringToInt32(string s)
        {
            int intValue = 0;
            bool validValue = true;
            string errMsg = null;

            try
            {
                intValue = XmlConvert.ToInt32(s);
            }
            catch (FormatException e)
            {
                errMsg = e.Message;
                validValue = false;
            }
            catch (OverflowException e)
            {
                errMsg = e.Message;
                validValue = false;
            }

            if (!validValue)
            {
                throw new FormatException(errMsg);
            }

            return intValue;
        }

        // will generic methods help here?

        /// <summary>
        /// Converts a string to decimal value
        /// </summary>
        /// <exception cref="FormatException">thrown if the string is invalid to be converted</exception>
        public static decimal ConvertStringToDecimal(string s)
        {
            decimal decValue = 0;
            bool validValue = true;
            string errMsg = null;

            try
            {
                decValue = XmlConvert.ToDecimal(s);
            }
            catch (FormatException e)
            {
                errMsg = e.Message;
                validValue = false;
            }
            catch (OverflowException e)
            {
                errMsg = e.Message;
                validValue = false;
            }

            if (!validValue)
            {
                throw new FormatException(errMsg);
            }

            return decValue;
        }
    }

    #endregion Internal Types
}
