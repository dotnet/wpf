// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
 
namespace Microsoft.Test.RenderingVerification.Filters
{
    #region using
        using System;
        using System.Drawing;
        using System.Collections;
        using System.Drawing.Imaging;
        using System.Runtime.InteropServices;
        using Microsoft.Test.RenderingVerification;
    #endregion using

    /// <summary>
    /// A Color Transform Filter
    /// </summary>
    [ObsoleteAttribute("This filter will be removed soon, please update your testcase")]
    public class ColorTransformFilter: Filter
    {
        #region mscms.dll stuff 
            [DllImportAttribute("mscms.dll", SetLastError = true)]
            static private extern bool CloseColorProfile(IntPtr hProfile);
            [DllImportAttribute("mscms.dll", SetLastError = true)]
            static private extern IntPtr CreateMultiProfileTransform(IntPtr[] profiles, Int32 nProfiles, Int32[] intentUse, int intentUseCount, Int32 Flags, Int32 indexPreferredCMM);
            [DllImportAttribute("mscms.dll", SetLastError = true)]
            static private extern bool DeleteColorTransform(IntPtr hTransform);
            [DllImportAttribute("mscms.dll", SetLastError = true)]
            static private extern IntPtr OpenColorProfileA(ref Profile profileStruct, Int32 desiredAccess, Int32 sharedMode, Int32 creationMode);
            [DllImportAttribute("mscms.dll", SetLastError = true)]
            static private extern IntPtr OpenColorProfileW(ref Profile profileStruct, Int32 desiredAccess, Int32 sharedMode, Int32 creationMode);
            [DllImportAttribute("mscms.dll", SetLastError = true)]
            static private extern bool TranslateBitmapBits(IntPtr hColorTransform, byte[] pSrcBits, Int32 bmFormatInput, Int32 Width, Int32 Height, Int32 inputStride, byte[] pDestBits, Int32 bmFormatOutput, Int32 outputStride, IntPtr pfnCallback, ulong ulCallbackData);

            [StructLayout(LayoutKind.Sequential)]
            private struct Profile
            { 
                /*DWORD*/ Int32 Type;
                /*PVOID*/ byte[] pProfileData;
                /*DWORD*/ Int32 cbDataSize;
            }

            private enum BitmapFormat
            {
                BM_x555RGB      = 0x0000,
                BM_x555XYZ      = 0x0101,
                BM_x555Yxy,
                BM_x555Lab,
                BM_x555G3CH,
                BM_RGBTRIPLETS  = 0x0002,
                BM_BGRTRIPLETS  = 0x0004,
                BM_XYZTRIPLETS  = 0x0201,
                BM_YxyTRIPLETS,
                BM_LabTRIPLETS,
                BM_G3CHTRIPLETS,
                BM_5CHANNEL,
                BM_6CHANNEL,
                BM_7CHANNEL,
                BM_8CHANNEL,
                BM_GRAY,
                BM_xRGBQUADS    = 0x0008,
                BM_xBGRQUADS    = 0x0010,
                BM_xG3CHQUADS   = 0x0304,
                BM_KYMCQUADS,
                BM_CMYKQUADS    = 0x0020,
                BM_10b_RGB      = 0x0009,
                BM_10b_XYZ      = 0x0401,
                BM_10b_Yxy,
                BM_10b_Lab,
                BM_10b_G3CH,
                BM_NAMED_INDEX,
                BM_16b_RGB      = 0x000A,
                BM_16b_XYZ      = 0x0501,
                BM_16b_Yxy,
                BM_16b_Lab,
                BM_16b_G3CH,
                BM_16b_GRAY,
                BM_565RGB       = 0x0001,
            }
        #endregion mscms.dll stuff 

        #region Constants
            private const string SOURCECOLOR = "SourceColorContext";
            private const string DESTINATIONCOLOR = "DestinationColorContext";
            private const string PIXELFORMAT = "PixelFormat";
        #endregion Constants

        #region Properties
            #region Private Properties
                private string[] PIXELFORMATS = {"ABGR128", "ARGB32", "ARGB64", "BGR24", "BlackWhite", "CMYK32", "DontCare", "Gray1", "Gray2", "Gray32", "Gray4", "Gray8", "Indexed1", "Indexed2", "Indexed4", "Indexed8", "PABGR128", "PARGB32", "PARGB64", "RGB24", "RGB32", "RGB48", "RGB555", "RGB565", "Undefined"};
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// Source ColorContext
                /// </summary>
                public string SourceColorContext
                {
                    get
                    {
                        return (string)this[SOURCECOLOR].Parameter;
                    }
                    set
                    {
                        this[SOURCECOLOR].Parameter = value;
                    }
                }
                /// <summary>
                /// Destination ColorContext
                /// </summary>
                public string DestinationColorContext
                {
                    get
                    {
                        return (string)this[DESTINATIONCOLOR].Parameter;
                    }
                    set
                    {
                        this[DESTINATIONCOLOR].Parameter = value;
                    }
                }
                /// <summary>
                /// Pixel Format
                /// </summary>
                public string PixelFormat
                {
                    get
                    {
                        return (string)this[PIXELFORMAT].Parameter;
                    }
                    set
                    {
                        if (Array.BinarySearch(PIXELFORMATS, value) < 0)
                        {
                            throw new Exception("PixelFormat not supported!");
                        }

                        this[PIXELFORMAT].Parameter = value;
                    }
                }
                /// <summary>
                /// Get the description for this filter
                /// </summary>
                /// <value></value>
                public override string FilterDescription
                {
                    get
                    {
                        return "MIL Filter (Don't use -- will go away soon)";
                    }
                }
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// Color Transform Filter constructor
            /// </summary>
            public ColorTransformFilter()
            {
                FilterParameter srccolor = new FilterParameter(SOURCECOLOR, "Source Color Context", (string)"sRGB");
                FilterParameter dstcolor = new FilterParameter(DESTINATIONCOLOR, "Destination Color Context", (string)"RGB");
                FilterParameter pixelformat = new FilterParameter(PIXELFORMAT, "Pixel Format", (string)"ARGB32");

                AddParameter(srccolor);
                AddParameter(dstcolor);
                AddParameter(pixelformat);
            }
        #endregion Constructors

        #region Methods
            #region Private Methods
                private BitmapFormat GetNativeBitmapFormat(string bmFormat)
                {
                    BitmapFormat nativeBitmapFormat;
                    switch (bmFormat)
                    {
                        // 16 bits format
                        case "RGB555" :
                            nativeBitmapFormat = BitmapFormat.BM_x555RGB;
                            break;

                        case "RGB565" :
                            nativeBitmapFormat = BitmapFormat.BM_565RGB;
                            break;

                        // 24bpp formats
                        case "RGB24" :
                            nativeBitmapFormat = BitmapFormat.BM_RGBTRIPLETS;
                            break;

                        case "BGR24" :
                            nativeBitmapFormat = BitmapFormat.BM_BGRTRIPLETS;
                            break;

                        // 32bpp format 
                        case "RGB32" :
                        case "ARGB32" :
                        case "PARGB32" :
                            nativeBitmapFormat = BitmapFormat.BM_xRGBQUADS;
                            break;

                        case "CMYK32" : // CMYK formats
                            nativeBitmapFormat = BitmapFormat.BM_CMYKQUADS;
                            break;

                        case "Gray32" : // Floating point scRGB formats
                            nativeBitmapFormat = BitmapFormat.BM_GRAY;  //TODO: ICM Docs say only 8-bit channel is used for grayscale. Is this right?
                            break;

/*                    
                        // scRGB formats. Gamma is 1.0
                        case MILPixelFormat32bppBGR101010:
                            bmFormat = BM_10b_RGB; //32bpp, 10bpc. The 2 most significant bits are ignored.
                            break;
*/
                        // 48 /64  & 128 bits images (Currently Unsupported)
                        case "RGB48" :
                        case "ARGB64" :
                        case "PARGB64" :
                        case "ABGR128" :
                        case "PABGR128" :
//                        case MILPixelFormat128bppBGR:
                            throw new Exception("Unsupported type");

                        // Other types : "BlackWhite", "DontCare", "Gray1", "Gray2", "Gray4", "Gray8", "Indexed1", "Indexed2", "Indexed4", "Indexed8", "Undefined"
                        default :
                            throw new Exception("Unsupported type");
                    }

                    return nativeBitmapFormat;
                }
            #endregion Private Methods
            #region Public Methods
                /// <summary>
                /// Filter Implementation
                /// </summary>
                protected override IImageAdapter ProcessFilter(IImageAdapter source)
                {
/*
                    ImageAdapter iret = new ImageAdapter(source.Width, source.Height);
                    BitmapFormat sourceBitmatFormat = GetNativeBitmapFormat(SourceColorContext);
                    BitmapFormat destinationBitmatFormat = GetNativeBitmapFormat(DestinationColorContext);

//                    m_hTransform = CreateMultiProfileTransform(&rghProfile[0],
//                                                       2,
//                                                       rgdwIntents,
//                                                       2,
//                                                       BEST_MODE |
//                                                       USE_RELATIVE_COLORIMETRIC,
//                                                       0);
                    return iret;
*/
                    throw new NotImplementedException("Not implemented yet, soon to be");
                }
            #endregion Public Methods
        #endregion Methods
    }
}
