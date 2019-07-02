// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#pragma warning disable 0414

namespace Microsoft.Test.RenderingVerification
{
    #region Using directives
        using System;
        using System.Drawing;
        using System.Security;
        using System.Reflection;
        using System.Collections;   
        using System.Drawing.Imaging;
        using System.Runtime.InteropServices;
    #endregion

//    /// <summary>
//    /// Metadata tag of interest
//    /// </summary>
//    internal enum PropertyTag
//    {
//        // http://msdn.microsoft.com/library/default.asp?url=/library/en-us/gdicpp/GDIPlus/GDIPlusreference/constants/imagepropertytagconstants/propertytagsinnumericalorder.asp
//        /*PropertyTag*/GpsVer = 0x0000,
//        /*PropertyTag*/GpsLatitudeRef = 0x0001,
//        /*PropertyTag*/GpsLatitude = 0x0002,
//        /*PropertyTag*/GpsLongitudeRef = 0x0003 ,
//        /*PropertyTag*/GpsLongitude = 0x0004 ,
//        /*PropertyTag*/GpsAltitudeRef = 0x0005, 
//        /*PropertyTag*/GpsAltitude = 0x0006 ,
//        /*PropertyTag*/GpsGpsTime = 0x0007 ,
//        /*PropertyTag*/GpsGpsSatellites = 0x0008 ,
//        /*PropertyTag*/GpsGpsStatus = 0x0009 ,
//        /*PropertyTag*/GpsGpsMeasureMode = 0x000A ,
//        /*PropertyTag*/GpsGpsDop = 0x000B ,
//        /*PropertyTag*/GpsSpeedRef = 0x000C, 
//        /*PropertyTag*/GpsSpeed = 0x000D ,
//        /*PropertyTag*/GpsTrackRef = 0x000E ,
//        /*PropertyTag*/GpsTrack = 0x000F ,
//        /*PropertyTag*/GpsImgDirRef = 0x0010 ,
//        /*PropertyTag*/GpsImgDir = 0x0011 ,
//        /*PropertyTag*/GpsMapDatum = 0x0012, 
//        /*PropertyTag*/GpsDestLatRef = 0x0013 ,
//        /*PropertyTag*/GpsDestLat = 0x0014 ,
//        /*PropertyTag*/GpsDestLongRef = 0x0015 ,
//        /*PropertyTag*/GpsDestLong = 0x0016 ,
//        /*PropertyTag*/GpsDestBearRef = 0x0017 ,
//        /*PropertyTag*/GpsDestBear = 0x0018,
//        /*PropertyTag*/GpsDestDistRef = 0x0019 ,
//        /*PropertyTag*/GpsDestDist = 0x001A ,
//        /*PropertyTag*/NewSubfileType = 0x00FE ,
//        /*PropertyTag*/SubfileType = 0x00FF ,
//        /*PropertyTag*/ImageWidth = 0x0100 ,
//        /*PropertyTag*/ImageHeight = 0x0101 ,
//        /*PropertyTag*/BitsPerSample = 0x0102, 
//        /*PropertyTag*/Compression = 0x0103 ,
//        /*PropertyTag*/PhotometricInterp = 0x0106 ,
//        /*PropertyTag*/ThreshHolding = 0x0107 ,
//        /*PropertyTag*/CellWidth = 0x0108 ,
//        /*PropertyTag*/CellHeight = 0x0109 ,
//        /*PropertyTag*/FillOrder = 0x010A ,
//        /*PropertyTag*/DocumentName = 0x010D ,
//        /*PropertyTag*/ImageDescription = 0x010E ,
//        /*PropertyTag*/EquipMake = 0x010F ,
//        /*PropertyTag*/EquipModel = 0x0110 ,
//        /*PropertyTag*/StripOffsets = 0x0111, 
//        /*PropertyTag*/Orientation = 0x0112 ,
//        /*PropertyTag*/SamplesPerPixel = 0x0115 ,
//        /*PropertyTag*/RowsPerStrip = 0x0116 ,
//        /*PropertyTag*/StripBytesCount = 0x0117 ,
//        /*PropertyTag*/MinSampleValue = 0x0118 ,
//        /*PropertyTag*/MaxSampleValue = 0x0119 ,
//        /*PropertyTag*/XResolution = 0x011A ,
//        /*PropertyTag*/YResolution = 0x011B ,
//        /*PropertyTag*/PlanarConfig = 0x011C ,
//        /*PropertyTag*/PageName = 0x011D ,
//        /*PropertyTag*/XPosition = 0x011E ,
//        /*PropertyTag*/YPosition = 0x011F ,
//        /*PropertyTag*/FreeOffset = 0x0120 ,
//        /*PropertyTag*/FreeByteCounts = 0x0121 ,
//        /*PropertyTag*/GrayResponseUnit = 0x0122 ,
//        /*PropertyTag*/GrayResponseCurve = 0x0123 ,
//        /*PropertyTag*/T4Option = 0x0124 ,
//        /*PropertyTag*/T6Option = 0x0125 ,
//        /*PropertyTag*/ResolutionUnit = 0x0128 ,
//        /*PropertyTag*/PageNumber = 0x0129 ,
//        /*PropertyTag*/TransferFunction = 0x012D ,
//        /*PropertyTag*/SoftwareUsed = 0x0131 ,
//        /*PropertyTag*/DateTime = 0x0132 ,
//        /*PropertyTag*/Artist = 0x013B ,
//        /*PropertyTag*/HostComputer = 0x013C ,
//        /*PropertyTag*/Predictor = 0x013D ,
//        /*PropertyTag*/WhitePoint = 0x013E ,
//        /*PropertyTag*/PrimaryChromaticities = 0x013F ,
//        /*PropertyTag*/ColorMap = 0x0140 ,
//        /*PropertyTag*/HalftoneHints = 0x0141 ,
//        /*PropertyTag*/TileWidth = 0x0142 ,
//        /*PropertyTag*/TileLength = 0x0143 ,
//        /*PropertyTag*/TileOffset = 0x0144 ,
//        /*PropertyTag*/TileByteCounts = 0x0145 ,
//        /*PropertyTag*/InkSet = 0x014C ,
//        /*PropertyTag*/InkNames = 0x014D ,
//        /*PropertyTag*/NumberOfInks = 0x014E ,
//        /*PropertyTag*/DotRange = 0x0150 ,
//        /*PropertyTag*/TargetPrinter = 0x0151 ,
//        /*PropertyTag*/ExtraSamples = 0x0152 ,
//        /*PropertyTag*/SampleFormat = 0x0153 ,
//        /*PropertyTag*/SMinSampleValue = 0x0154 ,
//        /*PropertyTag*/SMaxSampleValue = 0x0155 ,
//        /*PropertyTag*/TransferRange = 0x0156 ,
//        /*PropertyTag*/JPEGProc = 0x0200 ,
//        /*PropertyTag*/JPEGInterFormat = 0x0201 ,
//        /*PropertyTag*/JPEGInterLength = 0x0202 ,
//        /*PropertyTag*/JPEGRestartInterval = 0x0203 ,
//        /*PropertyTag*/JPEGLosslessPredictors = 0x0205 ,
//        /*PropertyTag*/JPEGPointTransforms = 0x0206 ,
//        /*PropertyTag*/JPEGQTables = 0x0207 ,
//        /*PropertyTag*/JPEGDCTables = 0x0208 ,
//        /*PropertyTag*/JPEGACTables = 0x0209 ,
//        /*PropertyTag*/YCbCrCoefficients = 0x0211 ,
//        /*PropertyTag*/YCbCrSubsampling = 0x0212 ,
//        /*PropertyTag*/YCbCrPositioning = 0x0213 ,
//        /*PropertyTag*/REFBlackWhite = 0x0214 ,
//        /*PropertyTag*/Gamma = 0x0301 ,
//        /*PropertyTag*/ICCProfileDescriptor = 0x0302 ,
//        /*PropertyTag*/SRGBRenderingIntent = 0x0303 ,
//        /*PropertyTag*/ImageTitle = 0x0320 ,
//        /*PropertyTag*/ResolutionXUnit = 0x5001 ,
//        /*PropertyTag*/ResolutionYUnit = 0x5002 ,
//        /*PropertyTag*/ResolutionXLengthUnit = 0x5003 ,
//        /*PropertyTag*/ResolutionYLengthUnit = 0x5004 ,
//        /*PropertyTag*/PrintFlags = 0x5005 ,
//        /*PropertyTag*/PrintFlagsVersion = 0x5006 ,
//        /*PropertyTag*/PrintFlagsCrop = 0x5007 ,
//        /*PropertyTag*/PrintFlagsBleedWidth = 0x5008 ,
//        /*PropertyTag*/PrintFlagsBleedWidthScale = 0x5009 ,
//        /*PropertyTag*/HalftoneLPI = 0x500A ,
//        /*PropertyTag*/HalftoneLPIUnit = 0x500B ,
//        /*PropertyTag*/HalftoneDegree = 0x500C ,
//        /*PropertyTag*/HalftoneShape = 0x500D ,
//        /*PropertyTag*/HalftoneMisc = 0x500E ,
//        /*PropertyTag*/HalftoneScreen = 0x500F, 
//        /*PropertyTag*/JPEGQuality = 0x5010 ,
//        /*PropertyTag*/GridSize = 0x5011 ,
//        /*PropertyTag*/ThumbnailFormat = 0x5012 ,
//        /*PropertyTag*/ThumbnailWidth = 0x5013 ,
//        /*PropertyTag*/ThumbnailHeight = 0x5014 ,
//        /*PropertyTag*/ThumbnailColorDepth = 0x5015 ,
//        /*PropertyTag*/ThumbnailPlanes = 0x5016 ,
//        /*PropertyTag*/ThumbnailRawBytes = 0x5017, 
//        /*PropertyTag*/ThumbnailSize = 0x5018 ,
//        /*PropertyTag*/ThumbnailCompressedSize = 0x5019 ,
//        /*PropertyTag*/ColorTransferFunction = 0x501A ,
//        /*PropertyTag*/ThumbnailData = 0x501B ,
//        /*PropertyTag*/ThumbnailImageWidth = 0x5020 ,
//        /*PropertyTag*/ThumbnailImageHeight = 0x5021 ,
//        /*PropertyTag*/ThumbnailBitsPerSample = 0x5022 ,
//        /*PropertyTag*/ThumbnailCompression = 0x5023 ,
//        /*PropertyTag*/ThumbnailPhotometricInterp = 0x5024 ,
//        /*PropertyTag*/ThumbnailImageDescription = 0x5025 ,
//        /*PropertyTag*/ThumbnailEquipMake = 0x5026 ,
//        /*PropertyTag*/ThumbnailEquipModel = 0x5027 ,
//        /*PropertyTag*/ThumbnailStripOffsets = 0x5028, 
//        /*PropertyTag*/ThumbnailOrientation = 0x5029 ,
//        /*PropertyTag*/ThumbnailSamplesPerPixel = 0x502A ,
//        /*PropertyTag*/ThumbnailRowsPerStrip = 0x502B ,
//        /*PropertyTag*/ThumbnailStripBytesCount = 0x502C ,
//        /*PropertyTag*/ThumbnailResolutionX = 0x502D ,
//        /*PropertyTag*/ThumbnailResolutionY = 0x502E ,
//        /*PropertyTag*/ThumbnailPlanarConfig = 0x502F ,
//        /*PropertyTag*/ThumbnailResolutionUnit = 0x5030, 
//        /*PropertyTag*/ThumbnailTransferFunction = 0x5031 ,
//        /*PropertyTag*/ThumbnailSoftwareUsed = 0x5032 ,
//        /*PropertyTag*/ThumbnailDateTime = 0x5033 ,
//        /*PropertyTag*/ThumbnailArtist = 0x5034 ,
//        /*PropertyTag*/ThumbnailWhitePoint = 0x5035 ,
//        /*PropertyTag*/ThumbnailPrimaryChromaticities = 0x5036 ,
//        /*PropertyTag*/ThumbnailYCbCrCoefficients = 0x5037 ,
//        /*PropertyTag*/ThumbnailYCbCrSubsampling = 0x5038 ,
//        /*PropertyTag*/ThumbnailYCbCrPositioning = 0x5039 ,
//        /*PropertyTag*/ThumbnailRefBlackWhite = 0x503A ,
//        /*PropertyTag*/ThumbnailCopyRight = 0x503B ,
//        /*PropertyTag*/LuminanceTable = 0x5090 ,
//        /*PropertyTag*/ChrominanceTable = 0x5091, 
//        /*PropertyTag*/FrameDelay = 0x5100 ,
//        /*PropertyTag*/LoopCount = 0x5101 ,
//        /*PropertyTag*/GlobalPalette = 0x5102 ,
//        /*PropertyTag*/IndexBackground = 0x5103, 
//        /*PropertyTag*/IndexTransparent = 0x5104, 
//        /*PropertyTag*/PixelUnit = 0x5110 ,
//        /*PropertyTag*/PixelPerUnitX = 0x5111 ,
//        /*PropertyTag*/PixelPerUnitY = 0x5112 ,
//        /*PropertyTag*/PaletteHistogram = 0x5113 ,
//        /*PropertyTag*/Copyright = 0x8298 ,
//        /*PropertyTag*/ExifExposureTime = 0x829A ,
//        /*PropertyTag*/ExifFNumber = 0x829D ,
//        /*PropertyTag*/ExifIFD = 0x8769 ,
//        /*PropertyTag*/ICCProfile = 0x8773 ,
//        /*PropertyTag*/ExifExposureProg = 0x8822 ,
//        /*PropertyTag*/ExifSpectralSense = 0x8824 ,
//        /*PropertyTag*/GpsIFD = 0x8825 ,
//        /*PropertyTag*/ExifISOSpeed = 0x8827 ,
//        /*PropertyTag*/ExifOECF = 0x8828 ,
//        /*PropertyTag*/ExifVer = 0x9000 ,
//        /*PropertyTag*/ExifDTOrig = 0x9003 ,
//        /*PropertyTag*/ExifDTDigitized = 0x9004 ,
//        /*PropertyTag*/ExifCompConfig = 0x9101 ,
//        /*PropertyTag*/ExifCompBPP = 0x9102 ,
//        /*PropertyTag*/ExifShutterSpeed = 0x9201 ,
//        /*PropertyTag*/ExifAperture = 0x9202 ,
//        /*PropertyTag*/ExifBrightness = 0x9203, 
//        /*PropertyTag*/ExifExposureBias = 0x9204 ,
//        /*PropertyTag*/ExifMaxAperture = 0x9205 ,
//        /*PropertyTag*/ExifSubjectDist = 0x9206 ,
//        /*PropertyTag*/ExifMeteringMode = 0x9207 ,
//        /*PropertyTag*/ExifLightSource = 0x9208 ,
//        /*PropertyTag*/ExifFlash = 0x9209 ,
//        /*PropertyTag*/ExifFocalLength = 0x920A ,
//        /*PropertyTag*/ExifMakerNote = 0x927C ,
//        /*PropertyTag*/ExifUserComment = 0x9286, 
//        /*PropertyTag*/ExifDTSubsec = 0x9290 ,
//        /*PropertyTag*/ExifDTOrigSS = 0x9291 ,
//        /*PropertyTag*/ExifDTDigSS = 0x9292 ,
//        /*PropertyTag*/ExifFPXVer = 0xA000 ,
//        /*PropertyTag*/ExifColorSpace = 0xA001 ,
//        /*PropertyTag*/ExifPixXDim = 0xA002 ,
//        /*PropertyTag*/ExifPixYDim = 0xA003 ,
//        /*PropertyTag*/ExifRelatedWav = 0xA004 ,
//        /*PropertyTag*/ExifInterop = 0xA005 ,
//        /*PropertyTag*/ExifFlashEnergy = 0xA20B ,
//        /*PropertyTag*/ExifSpatialFR = 0xA20C ,
//        /*PropertyTag*/ExifFocalXRes = 0xA20E ,
//        /*PropertyTag*/ExifFocalYRes = 0xA20F ,
//        /*PropertyTag*/ExifFocalResUnit = 0xA210 ,
//        /*PropertyTag*/ExifSubjectLoc = 0xA214 ,
//        /*PropertyTag*/ExifExposureIndex = 0xA215 ,
//        /*PropertyTag*/ExifSensingMethod = 0xA217 ,
//        /*PropertyTag*/ExifFileSource = 0xA300 ,
//        /*PropertyTag*/ExifSceneType = 0xA301 ,
//        /*PropertyTag*/ExifCfaPattern = 0xA302
//    }

    /// <summary>
    /// Metadata tag of interest
    /// </summary>
    internal class PropertyTag
    {
        // http://msdn.microsoft.com/library/default.asp?url=/library/en-us/gdicpp/GDIPlus/GDIPlusreference/constants/imagepropertytagconstants/propertytagsinnumericalorder.asp

        public static readonly int GpsVer = 0x0000;
        public static readonly int GpsLatitudeRef = 0x0001;
        public static readonly int GpsLatitude = 0x0002;
        public static readonly int GpsLongitudeRef = 0x0003;
        public static readonly int GpsLongitude = 0x0004;
        public static readonly int GpsAltitudeRef = 0x0005;
        public static readonly int GpsAltitude = 0x0006;
        public static readonly int psGpsTime = 0x0007;
        public static readonly int GpsGpsSatellites = 0x0008;
        public static readonly int GpsGpsStatus = 0x0009;
        public static readonly int GpsGpsMeasureMode = 0x000A;
        public static readonly int GpsGpsDop = 0x000B;
        public static readonly int GpsSpeedRef = 0x000C;
        public static readonly int GpsSpeed = 0x000D;
        public static readonly int GpsTrackRef = 0x000E;
        public static readonly int GpsTrack = 0x000F;
        public static readonly int GpsImgDirRef = 0x0010;
        public static readonly int GpsImgDir = 0x0011;
        public static readonly int GpsMapDatum = 0x0012;
        public static readonly int GpsDestLatRef = 0x0013;
        public static readonly int GpsDestLat = 0x0014;
        public static readonly int GpsDestLongRef = 0x0015;
        public static readonly int GpsDestLong = 0x0016;
        public static readonly int GpsDestBearRef = 0x0017;
        public static readonly int GpsDestBear = 0x0018;
        public static readonly int GpsDestDistRef = 0x0019;
        public static readonly int GpsDestDist = 0x001A;
        public static readonly int NewSubfileType = 0x00FE;
        public static readonly int SubfileType = 0x00FF;
        public static readonly int ImageWidth = 0x0100;
        public static readonly int ImageHeight = 0x0101;
        public static readonly int BitsPerSample = 0x0102;
        public static readonly int Compression = 0x0103;
        public static readonly int PhotometricInterp = 0x0106;
        public static readonly int ThreshHolding = 0x0107;
        public static readonly int CellWidth = 0x0108;
        public static readonly int CellHeight = 0x0109;
        public static readonly int FillOrder = 0x010A;
        public static readonly int DocumentName = 0x010D;
        public static readonly int ImageDescription = 0x010E;
        public static readonly int EquipMake = 0x010F;
        public static readonly int EquipModel = 0x0110;
        public static readonly int StripOffsets = 0x0111;
        public static readonly int Orientation = 0x0112;
        public static readonly int SamplesPerPixel = 0x0115;
        public static readonly int RowsPerStrip = 0x0116;
        public static readonly int StripBytesCount = 0x0117;
        public static readonly int MinSampleValue = 0x0118;
        public static readonly int MaxSampleValue = 0x0119;
        public static readonly int XResolution = 0x011A;
        public static readonly int YResolution = 0x011B;
        public static readonly int PlanarConfig = 0x011C;
        public static readonly int PageName = 0x011D;
        public static readonly int XPosition = 0x011E;
        public static readonly int YPosition = 0x011F;
        public static readonly int FreeOffset = 0x0120;
        public static readonly int FreeByteCounts = 0x0121;
        public static readonly int GrayResponseUnit = 0x0122;
        public static readonly int GrayResponseCurve = 0x0123;
        public static readonly int T4Option = 0x0124;
        public static readonly int T6Option = 0x0125;
        public static readonly int ResolutionUnit = 0x0128;
        public static readonly int PageNumber = 0x0129;
        public static readonly int TransferFunction = 0x012D;
        public static readonly int SoftwareUsed = 0x0131;
        public static readonly int DateTime = 0x0132;
        public static readonly int Artist = 0x013B;
        public static readonly int HostComputer = 0x013C;
        public static readonly int Predictor = 0x013D;
        public static readonly int WhitePoint = 0x013E;
        public static readonly int PrimaryChromaticities = 0x013F;
        public static readonly int ColorMap = 0x0140;
        public static readonly int HalftoneHints = 0x0141;
        public static readonly int TileWidth = 0x0142;
        public static readonly int TileLength = 0x0143;
        public static readonly int TileOffset = 0x0144;
        public static readonly int TileByteCounts = 0x0145;
        public static readonly int InkSet = 0x014C;
        public static readonly int InkNames = 0x014D;
        public static readonly int NumberOfInks = 0x014E;
        public static readonly int DotRange = 0x0150;
        public static readonly int TargetPrinter = 0x0151;
        public static readonly int ExtraSamples = 0x0152;
        public static readonly int SampleFormat = 0x0153;
        public static readonly int SMinSampleValue = 0x0154;
        public static readonly int SMaxSampleValue = 0x0155;
        public static readonly int TransferRange = 0x0156;
        public static readonly int JPEGProc = 0x0200;
        public static readonly int JPEGInterFormat = 0x0201;
        public static readonly int JPEGInterLength = 0x0202;
        public static readonly int JPEGRestartInterval = 0x0203;
        public static readonly int JPEGLosslessPredictors = 0x0205;
        public static readonly int JPEGPointTransforms = 0x0206;
        public static readonly int JPEGQTables = 0x0207;
        public static readonly int JPEGDCTables = 0x0208;
        public static readonly int JPEGACTables = 0x0209;
        public static readonly int YCbCrCoefficients = 0x0211;
        public static readonly int YCbCrSubsampling = 0x0212;
        public static readonly int YCbCrPositioning = 0x0213;
        public static readonly int REFBlackWhite = 0x0214;
        public static readonly int Gamma = 0x0301;
        public static readonly int ICCProfileDescriptor = 0x0302;
        public static readonly int SRGBRenderingIntent = 0x0303;
        public static readonly int ImageTitle = 0x0320;
        public static readonly int ResolutionXUnit = 0x5001;
        public static readonly int ResolutionYUnit = 0x5002;
        public static readonly int ResolutionXLengthUnit = 0x5003;
        public static readonly int ResolutionYLengthUnit = 0x5004;
        public static readonly int PrintFlags = 0x5005;
        public static readonly int PrintFlagsVersion = 0x5006;
        public static readonly int PrintFlagsCrop = 0x5007;
        public static readonly int PrintFlagsBleedWidth = 0x5008;
        public static readonly int PrintFlagsBleedWidthScale = 0x5009;
        public static readonly int HalftoneLPI = 0x500A;
        public static readonly int HalftoneLPIUnit = 0x500B;
        public static readonly int HalftoneDegree = 0x500C;
        public static readonly int HalftoneShape = 0x500D;
        public static readonly int HalftoneMisc = 0x500E;
        public static readonly int HalftoneScreen = 0x500F;
        public static readonly int JPEGQuality = 0x5010;
        public static readonly int GridSize = 0x5011;
        public static readonly int ThumbnailFormat = 0x5012;
        public static readonly int ThumbnailWidth = 0x5013;
        public static readonly int ThumbnailHeight = 0x5014;
        public static readonly int ThumbnailColorDepth = 0x5015;
        public static readonly int ThumbnailPlanes = 0x5016;
        public static readonly int ThumbnailRawBytes = 0x5017;
        public static readonly int ThumbnailSize = 0x5018;
        public static readonly int ThumbnailCompressedSize = 0x5019;
        public static readonly int ColorTransferFunction = 0x501A;
        public static readonly int ThumbnailData = 0x501B;
        public static readonly int ThumbnailImageWidth = 0x5020;
        public static readonly int ThumbnailImageHeight = 0x5021;
        public static readonly int ThumbnailBitsPerSample = 0x5022;
        public static readonly int ThumbnailCompression = 0x5023;
        public static readonly int ThumbnailPhotometricInterp = 0x5024;
        public static readonly int ThumbnailImageDescription = 0x5025;
        public static readonly int ThumbnailEquipMake = 0x5026;
        public static readonly int ThumbnailEquipModel = 0x5027;
        public static readonly int ThumbnailStripOffsets = 0x5028;
        public static readonly int ThumbnailOrientation = 0x5029;
        public static readonly int ThumbnailSamplesPerPixel = 0x502A;
        public static readonly int ThumbnailRowsPerStrip = 0x502B;
        public static readonly int ThumbnailStripBytesCount = 0x502C;
        public static readonly int ThumbnailResolutionX = 0x502D;
        public static readonly int ThumbnailResolutionY = 0x502E;
        public static readonly int ThumbnailPlanarConfig = 0x502F;
        public static readonly int ThumbnailResolutionUnit = 0x5030;
        public static readonly int ThumbnailTransferFunction = 0x5031;
        public static readonly int ThumbnailSoftwareUsed = 0x5032;
        public static readonly int ThumbnailDateTime = 0x5033;
        public static readonly int ThumbnailArtist = 0x5034;
        public static readonly int ThumbnailWhitePoint = 0x5035;
        public static readonly int ThumbnailPrimaryChromaticities = 0x5036;
        public static readonly int ThumbnailYCbCrCoefficients = 0x5037;
        public static readonly int ThumbnailYCbCrSubsampling = 0x5038;
        public static readonly int ThumbnailYCbCrPositioning = 0x5039;
        public static readonly int ThumbnailRefBlackWhite = 0x503A;
        public static readonly int ThumbnailCopyRight = 0x503B;
        public static readonly int LuminanceTable = 0x5090;
        public static readonly int ChrominanceTable = 0x5091;
        public static readonly int FrameDelay = 0x5100;
        public static readonly int LoopCount = 0x5101;
        public static readonly int GlobalPalette = 0x5102;
        public static readonly int IndexBackground = 0x5103;
        public static readonly int IndexTransparent = 0x5104;
        public static readonly int PixelUnit = 0x5110;
        public static readonly int PixelPerUnitX = 0x5111;
        public static readonly int PixelPerUnitY = 0x5112;
        public static readonly int PaletteHistogram = 0x5113;
        public static readonly int Copyright = 0x8298;
        public static readonly int ExifExposureTime = 0x829A;
        public static readonly int ExifFNumber = 0x829D;
        public static readonly int ExifIFD = 0x8769;
        public static readonly int ICCProfile = 0x8773;
        public static readonly int ExifExposureProg = 0x8822;
        public static readonly int ExifSpectralSense = 0x8824;
        public static readonly int GpsIFD = 0x8825;
        public static readonly int ExifISOSpeed = 0x8827;
        public static readonly int ExifOECF = 0x8828;
        public static readonly int ExifVer = 0x9000;
        public static readonly int ExifDTOrig = 0x9003;
        public static readonly int ExifDTDigitized = 0x9004;
        public static readonly int ExifCompConfig = 0x9101;
        public static readonly int ExifCompBPP = 0x9102;
        public static readonly int ExifShutterSpeed = 0x9201;
        public static readonly int ExifAperture = 0x9202;
        public static readonly int ExifBrightness = 0x9203;
        public static readonly int ExifExposureBias = 0x9204;
        public static readonly int ExifMaxAperture = 0x9205;
        public static readonly int ExifSubjectDist = 0x9206;
        public static readonly int ExifMeteringMode = 0x9207;
        public static readonly int ExifLightSource = 0x9208;
        public static readonly int ExifFlash = 0x9209;
        public static readonly int ExifFocalLength = 0x920A;
        public static readonly int ExifMakerNote = 0x927C;
        public static readonly int ExifUserComment = 0x9286;
        public static readonly int ExifDTSubsec = 0x9290;
        public static readonly int ExifDTOrigSS = 0x9291;
        public static readonly int ExifDTDigSS = 0x9292;
        public static readonly int ExifFPXVer = 0xA000;
        public static readonly int ExifColorSpace = 0xA001;
        public static readonly int ExifPixXDim = 0xA002;
        public static readonly int ExifPixYDim = 0xA003;
        public static readonly int ExifRelatedWav = 0xA004;
        public static readonly int ExifInterop = 0xA005;
        public static readonly int ExifFlashEnergy = 0xA20B;
        public static readonly int ExifSpatialFR = 0xA20C;
        public static readonly int ExifFocalXRes = 0xA20E;
        public static readonly int ExifFocalYRes = 0xA20F;
        public static readonly int ExifFocalResUnit = 0xA210;
        public static readonly int ExifSubjectLoc = 0xA214;
        public static readonly int ExifExposureIndex = 0xA215;
        public static readonly int ExifSensingMethod = 0xA217;
        public static readonly int ExifFileSource = 0xA300;
        public static readonly int ExifSceneType = 0xA301;
        public static readonly int ExifCfaPattern = 0xA302;
    }
/*
    /// <summary>
    /// Custom defined PropertyTag
    /// </summary>
    internal class CustomPropertyTag : PropertyTag
    {
        public static readonly int OriginalColorDepth = 0x9999;
    }
*/


    /// <summary>
    /// define the type store in the metadata (byte/int/string/...)
    /// </summary>
    internal enum PropertyTagType : short   // from gdiplusImaging.h
    { 
        PropertyTagTypeByte         =   1,
        PropertyTagTypeASCII        =   2,
        PropertyTagTypeShort        =   3,
        PropertyTagTypeLong         =   4,
        PropertyTagTypeRational     =   5,
        PropertyTagTypeUndefined    =   7,
        PropertyTagTypeSLONG        =   9,
        PropertyTagTypeSRational    =   10
    }

    /// <summary>
    /// Factory for PropertyItem type
    /// </summary>
    internal class PropertyItemFactory
    {
        #region Constructors
            private PropertyItemFactory() { } // block instantiation
        #endregion Constructors

        #region Static Methods
            static public PropertyItem CreateInstance()
            {
                // The PropertyItem default constructor is marked as PRIVATE for managed but not for native (accessible in C++) !
                // Therefore we can assume that it is *relatively* safe to call the private constructor.
                // Furthermore if this ever change, this will be the only source you'll need to update
                PropertyItem retVal = (PropertyItem)Activator.CreateInstance(typeof(PropertyItem), true);
                retVal.Type = (short)PropertyTagType.PropertyTagTypeUndefined;
                retVal.Value = new byte[0];
                return retVal;
            }
            static public PropertyItem CreateInstance(int PropertyTagId, PropertyTagType propertyTagType, byte[] PropertyValue)
            {
                PropertyItem retVal = CreateInstance();
                retVal.Id = PropertyTagId;
                retVal.Len = PropertyValue.Length;
                retVal.Type = (short)propertyTagType;
                retVal.Value = PropertyValue;
                return retVal;

            }
            static public PropertyItem CreateInstance(int PropertyTagId, byte PropertyValue)
            {
                PropertyItem retVal = CreateInstance();
                retVal.Id = PropertyTagId;
                retVal.Len = 1;
                retVal.Type = (short)PropertyTagType.PropertyTagTypeByte;
                retVal.Value = new byte[] { PropertyValue };
                return retVal;

            }
            static public PropertyItem CreateInstance(int PropertyTagId, int PropertyValue)
            {
                PropertyItem retVal = CreateInstance();
                retVal.Id = PropertyTagId;
                retVal.Len = 4;
                retVal.Type = (short)PropertyTagType.PropertyTagTypeLong;
                retVal.Value = IntToPropertyTagValue(PropertyValue);
                return retVal;

            }
            static public PropertyItem CreateInstance(int PropertyTagId, string PropertyValue)
            {
                PropertyItem retVal = CreateInstance();
                retVal.Id = PropertyTagId;
                retVal.Len = PropertyValue.Length;
                retVal.Type = (short)PropertyTagType.PropertyTagTypeASCII;
                retVal.Value = System.Text.ASCIIEncoding.ASCII.GetBytes(PropertyValue);
                return retVal;
            }

            static private byte[] IntToPropertyTagValue(int valueToConvert)
            {
                byte[] retVal = new byte[4];
                retVal[3] = (byte)(valueToConvert >> 24);
                retVal[2] = (byte)((valueToConvert >> 16) & 0xFF);
                retVal[1] = (byte)((valueToConvert >> 8) & 0xFF);
                retVal[0] = (byte)(valueToConvert & 0xFF);
                return retVal;
            }
        #endregion Static Methods
    }
}

#pragma warning restore 0414
